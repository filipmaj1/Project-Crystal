/*
===========================================================================
Copyright (C) 2019-2026 Project Crystal Dev Team

This file is part of Project Crystal Server.

Project Crystal Server is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Project Crystal Server is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with Project Crystal Server. If not, see <https://www.gnu.org/licenses/>.
===========================================================================
*/

using Crystal.Common;
using Crystal.FrontMissionOnline.Network.UdpNet.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using static Crystal.Common.FmoBlowfish;

namespace Crystal.FrontMissionOnline.Network.UdpNet
{
    public class Client(Socket socket, FmoBlowfish.BlowfishContext blowCtx)
    {
        ushort ServerSequence;
        ushort ClientAckSequence;
        readonly Socket UdpSocket = socket;
        BlowfishContext BFCtx = blowCtx;
        EndPoint? SenderEP;

        Queue<UdpCommand> QueuedCommands = new Queue<UdpCommand>();

        public void Update(UdpPacket commands)
        {
            uint cmdID;

            //Program.Log.Info($"PING: Start: {commands.Header.StartSeq:X}, End: {commands.Header.EndSeq:X}, Client Acked: {commands.Header.AckedSeq:X}");

            if (commands.Header.Size == 0x28)
            {
                SendAck(commands.Header, commands.Header.StartSeq);
                return;
            }

            ClientAckSequence = commands.Header.StartSeq;
            if (commands.Header.AckedSeq > ServerSequence)
            {
                ServerSequence = commands.Header.AckedSeq;
            }

            ushort ackCount = commands.Header.StartSeq;

            while ((cmdID = commands.GetNextCommand(out byte[]? cmdData)) != 0)
            {
                if (cmdData == null)
                    continue;

                if (cmdID != 0x12D && cmdID != 0x12C)
                    Program.Log.Info($"Got UDP CommandID: {cmdID:X}\n{Utils.ByteArrayToHex(cmdData)}");

                switch (cmdID)
                {
                    case 0x1:
                        QueuedCommands.Enqueue(new UdpCommand(0x001, 0xDEAD, []));
                        QueuedCommands.Enqueue(new UdpCommand(0x007, 0xDEAD, File.ReadAllBytes("packets/cmd_pop")));
                        QueuedCommands.Enqueue(new UdpCommand(0x007, 0xDEAD, File.ReadAllBytes("packets/cmd_pop_npc")));
                        break;
                    case 0xB:
                    case 0xC:
                        Program.Log.Info($"Got logout command");
                        break;
                    case 0x0F0:
                        if (cmdData.Length == 0xc || cmdData.Length == 0x14)
                        { 
                            CmdMoveUpdate moveUpdate = MemoryMarshal.Cast<byte, CmdMoveUpdate>(cmdData)[0];
                            //Program.Log.Info($"Got move command - State: {moveUpdate.state}, X:{moveUpdate.x}, Y:{moveUpdate.y}");
                            //Program.Log.Info($"MOVEMENT: {cmdID:X}\n{Utils.ByteArrayToHex(cmdData)}");
                        }
                        break;
                    case 0x12C:
                        QueuedCommands.Enqueue(new UdpCommand(0x12C, 0xDEAD, []));
                        break;
                    case 0x12D:
                        QueuedCommands.Enqueue(new UdpCommand(0x12D, 0xDEAD, [0,0,0,0]));
                        break;
                    default:
                        //Program.Log.Info($"Unk CommandID: {cmdID:X}\n{Utils.ByteArrayToHex(cmdData)}");
                        break;
                }

                ackCount++;
            }

            SendAck(commands.Header, ackCount);
            FlushCommands();
        }

        private unsafe void SendAck(UdpPacketHeader header, ushort ackCount)
        {
            int alignedData = 0x28 & 0xfff8;

            BlowfishContext bctx = BFCtx;
            
            byte[] toSend = new byte[alignedData];
            header.UdpSysID = 2;
            header.Size = 0x28;
            header.CommandDataSize = (ushort)alignedData;
            header.AckedSeq = (ushort)(ackCount);
            header.StartSeq = (ushort)(ServerSequence);
            header.EndSeq = (ushort)(ServerSequence);
            header.MaxRecvCmds = 0xFFFF;

            Span<byte> toSendSpan = toSend.AsSpan();

            // Write what we have so far and md5
            MemoryMarshal.Write(toSend, header);

            using (MD5 md5Hash = MD5.Create())
            {
                byte[] md5 = md5Hash.ComputeHash(toSend[0x1C..header.Size].ToArray());
                md5.CopyTo(toSendSpan[0xc..]);
            }

            // Encrypt
            fixed (byte* toEncrypt = &toSend[0xc])
                FrontMissionOnlineDll.fmoBlowfishEncrypt(&bctx, toEncrypt, toSend.Length - 0xc);

            if (SenderEP == null)
                return;

            UdpSocket.SendTo(toSend, SenderEP);
        }

        public unsafe void FlushCommands()
        {
            if (QueuedCommands.Count == 0)
                return;

            int numCmds = 0;
            int cmdDataSize = 0;
            Span<byte> toSendSpan = new byte[0x4000];

            foreach (UdpCommand cmd in QueuedCommands)
            {
                cmd.AsSpan().CopyTo(toSendSpan[(0x28 + cmdDataSize)..]);
                cmdDataSize += cmd.Data.Length + 0x10;
                numCmds++;
            }

            int alignedData = (((0x28 + cmdDataSize) + 7) & 0xfff8);

            BlowfishContext bctx = BFCtx;
            UdpPacketHeader header = new()
            {
                SourceID = 0xDEAD,
                Size = (ushort)alignedData,
                UdpSysID = 2,
                MapType = 2,
                StartSeq = ServerSequence,
                EndSeq = (ushort)(ServerSequence + numCmds),
                AckedSeq = ClientAckSequence,
                MaxRecvCmds = 0xFFFF,
                CommandDataSize = (ushort)(0x28 + cmdDataSize),
            };

            // Write what we have so far and md5
            MemoryMarshal.Write(toSendSpan, header);

            using (MD5 md5Hash = MD5.Create())
            {
                byte[] md5 = md5Hash.ComputeHash(toSendSpan[0x1C..header.Size].ToArray());
                md5.CopyTo(toSendSpan[0xc..]);
            }

            // Encrypt
            fixed (byte* toEncrypt = &toSendSpan[0xc])
                FrontMissionOnlineDll.fmoBlowfishEncrypt(&bctx, toEncrypt, alignedData - 0xc);

            if (SenderEP == null)
                return;


            UdpSocket.SendTo(toSendSpan[0..header.Size], SenderEP);
            
            QueuedCommands.Clear();
        }

        public unsafe void SendCommand(byte[] data, int numCmds = 8)
        {
            int alignedData = (int)(((0x28 + data.Length) + 7) & 0xfff8);

            BlowfishContext bctx = BFCtx;
            UdpPacketHeader header = new() {
                SourceID = 0xDEAD,
                Size = (ushort)alignedData,
                UdpSysID = 2,
                MapType = 2,
                StartSeq = ServerSequence,
                EndSeq = (ushort)(ServerSequence + numCmds),
                AckedSeq = ClientAckSequence,
                MaxRecvCmds = 0xFFFF,
                CommandDataSize = (ushort)(0x28 + data.Length),
            };
            byte[] toSend = new byte[alignedData];
            Span<byte> toSendSpan = toSend.AsSpan();

            // Write what we have so far and md5
            MemoryMarshal.Write(toSend, header);
            data.CopyTo(toSendSpan[0x28..]);

            using (MD5 md5Hash = MD5.Create())
            {
                byte[] md5 = md5Hash.ComputeHash(toSend[0x1C..header.Size].ToArray());
                md5.CopyTo(toSendSpan[0xc..]);
            }

            // Encrypt
            fixed (byte* toEncrypt = &toSend[0xc])
                FrontMissionOnlineDll.fmoBlowfishEncrypt(&bctx, toEncrypt, toSend.Length - 0xc);

            if (SenderEP == null)
                return;

            UdpSocket.SendTo(toSend, SenderEP);
        }

        public void SetEP(EndPoint senderRemote)
        {
            if (SenderEP == null)
                SenderEP = senderRemote;
        }

        public void SendNpc()
        {
            QueuedCommands.Enqueue(new UdpCommand(0x007, 0xDEAD, File.ReadAllBytes("packets/cmd_pop_npc")));
        }
    }
}
