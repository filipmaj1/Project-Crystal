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
using Crystal.FrontMissionOnline.Network.UdpNet;
using Crystal.FrontMissionOnline.packets;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using static Crystal.Common.FmoBlowfish;

namespace Crystal.FrontMissionOnline
{
    class MapServer
    {
        private const int BACKLOG = 100;

        public readonly string ServerIp;
        public readonly ushort ServerPort;
        private bool IsAlive = false;
        private Socket? ServerSocket;
        private Thread? ServerLoopThread;

        Dictionary<uint, Network.UdpNet.Client> ConnectedClients = new();

        public MapServer(string ip, ushort port)
        {
            ServerIp = ip;
            ServerPort = port;
        }

        public void StopServer()
        {
            IsAlive = false;
        }

        public void StartServer()
        {
            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Any, ServerPort);
            try
            {
                ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                ServerSocket.Bind(serverEndPoint);

                Program.Log.Info($"Server socket created on {ServerPort}...");
            }
            catch (Exception e)
            {
                throw new ApplicationException("Could not Create socket, check to make sure not duplicating port", e);
            }

            IsAlive = true;

            ServerLoopThread = new Thread(MainLoop);
            ServerLoopThread.Start();

            Program.Log.Info("Server thread has started...");
        }

        public ushort GetPort()
        {
            return ServerPort;
        }

        public string GetIp()
        {
            return ServerIp;
        }

        private unsafe void MainLoop()
        {
            byte[] buffer = new byte[4000];
            Span<byte> buffSpan = buffer.AsSpan();
            IPEndPoint sender = new(IPAddress.Any, 0);
            EndPoint senderRemote = sender;

            if (ServerSocket == null)
                return;


            BlowfishContext blowfishCtx = new();
            ReadOnlySpan<byte> keyBuff = "101173flobby"u8;
            fixed (byte* keyPtr = &keyBuff[0])
                FrontMissionOnlineDll.fmoBlowfishInit(&blowfishCtx, keyPtr, 0xC);
            string key = "101173flobby";
            byte[] keyData = Encoding.ASCII.GetBytes(key);

            FmoBlowfish bf = new FmoBlowfish(keyData, keyData.Length);

            while (IsAlive)
            {
                // Receive and process packets
                int bytesReceived;
                int i = 0;
                do
                {
                    bytesReceived = ServerSocket.ReceiveFrom(buffer, 1400, SocketFlags.None, ref senderRemote);
                    if (bytesReceived == BitConverter.ToInt32(buffSpan.Slice(4)))
                        break;
                } while (i < 500);

                UdpPacket? packet = UdpPacket.Process(blowfishCtx, buffer.AsSpan(), bytesReceived);
                if (packet != null)
                {
                    //Console.WriteLine($"Got PACKET - SeqStart: {packet.Header.StartSeq}, SeqEnd: {packet.Header.EndSeq}\n");
                    if (!ConnectedClients.ContainsKey((uint)packet.Header.SourceID))
                    {
                        Network.UdpNet.Client newClient = new(ServerSocket, blowfishCtx);
                        newClient.SetEP(senderRemote);
                        ConnectedClients.Add((uint)packet.Header.SourceID, newClient);
                    }
                    ConnectedClients[(uint)packet.Header.SourceID].Update(packet);
                }
            }

            ServerSocket?.Close(5);
            GC.Collect();
        }
        unsafe static bool VerifyPacket(BlowfishContext ctx, Span<byte> inBuff, int inSize, out ReadOnlySpan<byte> firstCommandOut)
        {
            CmdPacketHeader header = MemoryMarshal.Cast<byte, CmdPacketHeader>(inBuff)[0];
            firstCommandOut = null;

            // Check size and byte alignment
            if (header.Size == 0 || (header.Size & 3) != 0 || (header.Size & 7) != 0 || header.Size != inSize || header.Size < 0x28)
                return false;

            // Decrypt the packet
            fixed (byte* toDecrypt = &inBuff[0xc])
                FrontMissionOnlineDll.fmoBlowfishDecrypt(&ctx, toDecrypt, header.Size - 0xC);

            // Verify SHA1
            if (!VerifySHA1(new Span<byte>(&header.SHA1, 0x10), inBuff[0x1c..(header.Size - 0x1c)]))
                return false;

            // Verify Commands
            ReadOnlySpan<byte> commandBytes = inBuff[0x28..];
            int bytesVerified = header.CommandDataSize - 0x28;
            firstCommandOut = commandBytes;
            while (bytesVerified > 0)
            {
                CommandHeader cmdHeader = MemoryMarshal.Cast<byte, CommandHeader>(commandBytes)[0];
                if ((cmdHeader.Size & 3) != 0)
                    return false;
                if (cmdHeader.Size > 0x7FF)
                    return false;
                if (cmdHeader.CommandID > 0x12F)
                    return false;
                bytesVerified -= (int)cmdHeader.Size;
                commandBytes = commandBytes[cmdHeader.Size..];
            }

            return true;
        }

        private static bool VerifySHA1(Span<byte> pktSHA1, Span<byte> data)
        {
            Span<byte> computedSHA1 = [0x10];
            using (SHA1 sha1Hash = SHA1.Create())
            {
                sha1Hash.TryComputeHash(data, computedSHA1, out int written);
                if (written != 0x10)
                    return false;
            }

            for (int i = 0; i < 0x10; i++)
            {
                if (computedSHA1[i] != pktSHA1[i])
                    return false;
            }

            return true;
        }

        public void SendNpc()
        {
            foreach (var client in ConnectedClients)
            {
                client.Value.SendNpc();
            }
        }
    }
}
