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


using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using static Crystal.Common.FmoBlowfish;

namespace Crystal.FrontMissionOnline.Network.UdpNet
{
    public class UdpPacket
    {
        [StructLayout(LayoutKind.Sequential, Size = 0x10)]
        public struct CommandHeader
        {
            public int Size;
            public uint CommandID;
            public uint SourceClientID;
            public uint Reserved;
        }

        public UdpPacketHeader Header;
        private byte[] CommandBuff;
        private int BufferLength;
        private int BytesRead;

        public static UdpPacket? Process(BlowfishContext ctx, Span<byte> inBuff, int inSize)
        {
            if (VerifyPacket(ctx, inBuff, inSize, out UdpPacketHeader header, out byte[]? commandBuff, out int commandLength))
                return new UdpPacket(header, commandBuff, commandLength);
            return null;
        }

        public UdpPacket(UdpPacketHeader header, byte[] commandBuff, int commandLength)
        {
            Header = header;
            CommandBuff = commandBuff;
            BufferLength = commandLength;
        }

        public uint GetNextCommand(out byte[]? data)
        {
            if (CommandBuff.Length == 0)
            {
                data = null;
                return 0;
            }

            CommandHeader header = MemoryMarshal.Cast<byte, CommandHeader>(CommandBuff)[0];
            data = CommandBuff[0x10..header.Size];
            CommandBuff = CommandBuff[header.Size..];

            return header.CommandID;
        }

        unsafe static bool VerifyPacket(BlowfishContext ctx, Span<byte> inBuff, int inSize, out UdpPacketHeader header, out byte[]? firstCommandOut, out int lengthOut)
        {
            header = MemoryMarshal.Cast<byte, UdpPacketHeader>(inBuff)[0];
            firstCommandOut = null;
            lengthOut = 0;

            // Check size and byte alignment
            if (header.Size == 0 || (header.Size & 3) != 0 || (header.Size & 7) != 0 || header.Size != inSize || header.Size < 0x28)
                return false;

            // Decrypt the packet
            fixed (byte* toDecrypt = &inBuff[0xc])
                FrontMissionOnlineDll.fmoBlowfishDecrypt(&ctx, toDecrypt, header.Size - 0xC);
            header = MemoryMarshal.Cast<byte, UdpPacketHeader>(inBuff)[0];

            // Verify MD5
            if (!VerifyMD5(header.MD5, inBuff[0x1c..header.Size]))
                return false;

            // Verify Commands
            ReadOnlySpan<byte> commandBytes = inBuff[0x28..header.CommandDataSize];
            int bytesVerified = header.CommandDataSize - 0x28;
            firstCommandOut = commandBytes.ToArray();
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

            lengthOut = header.CommandDataSize;

            string bleh = header.Size != 0x28 ? "Command List:" : "";
            //Program.Log.Info($"Got PKT: \n    CSeqStart: 0x{header.StartClientSequence:X}\n    CSeqEnd: 0x{header.EndClientSequence:X}\n    SerSeq: 0x{header.ServerSequence:X}\n    Param5: 0x{header.Param5:X}\n    MaxCmds: 0x{header.MaxRecvCmds:X}\n    {bleh}\n");

            return true;
        }

        private static bool VerifyMD5(Span<byte> pktMD5, Span<byte> data)
        {
            byte[] md5;
            using (MD5 md5Hash = MD5.Create())
            {
                md5 = md5Hash.ComputeHash(data.ToArray());
            }

            if (md5 == null)
                return false;

            for (int i = 0; i < 0x10; i++)
            {
                if (md5[i] != pktMD5[i])
                    return false;
            }

            return true;
        }
    }
}
