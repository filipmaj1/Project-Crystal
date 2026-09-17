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
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Crystal.Common;
using NLog;
using NLog.Targets;

namespace Crystal.POLPatch
{
    [StructLayout(LayoutKind.Sequential)]
    public struct BasePacketHeader
    {
        public uint PacketSize;
        public uint Checksum;
        public uint Signature;
        public uint Opcode;
    }

    public class BasePacket
    {
        public const int BASEPACKET_SIZE = 0x10;
        public byte[] data;

        public BasePacketHeader header;

        // Read received bytes into a BasePacket
        public unsafe BasePacket(byte[] bytes, ref int offset)
        {
            if (bytes.Length < offset + BASEPACKET_SIZE)
                throw new OverflowException("Packet Error: Packet was too small");

            fixed (byte* pdata = &bytes[offset])
            {
                header = (BasePacketHeader)Marshal.PtrToStructure(new IntPtr(pdata), typeof(BasePacketHeader));
            }

            int packetSize = (int) header.PacketSize;

            if (bytes.Length < offset + header.PacketSize)
                throw new OverflowException("Packet Error: Packet size didn't equal given size");

            data = new byte[packetSize - BASEPACKET_SIZE];
            Array.Copy(bytes, offset + BASEPACKET_SIZE, data, 0, packetSize - BASEPACKET_SIZE);

            offset += packetSize;
        }

        // Create a BasePacket for sending
        public BasePacket(BasePacketHeader header, byte[] data)
        {
            this.header = header;
            this.header.PacketSize = (uint)data.Length + 0x10;
            this.data = data;            
            CreateAddCheckSum();
        }        

        public static unsafe BasePacketHeader GetHeader(byte[] bytes)
        {
            BasePacketHeader header;
            if (bytes.Length < BASEPACKET_SIZE)
                throw new OverflowException("Packet Error: Packet was too small");

            fixed (byte* pdata = &bytes[0])
            {
                header = (BasePacketHeader)Marshal.PtrToStructure(new IntPtr(pdata), typeof(BasePacketHeader));
            }

            return header;
        }

        public static BasePacketHeader CreateHeader(uint opcode)
        {
            BasePacketHeader header = new()
            {
                Signature = 0x504C4F50, //POLP
                Opcode = opcode
            };
            return header;
        }

        public byte[] GetHeaderBytes()
        {
            var size = Marshal.SizeOf(header);
            var arr = new byte[size];

            var ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(header, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }

        public byte[] GetPacketBytes()
        {
            var outBytes = new byte[header.PacketSize];
            Array.Copy(GetHeaderBytes(), 0, outBytes, 0, BASEPACKET_SIZE);
            Array.Copy(data, 0, outBytes, BASEPACKET_SIZE, data.Length);
            return outBytes;
        }

        private uint CreateAddCheckSum()
        {
            using MD5 md5Hash = MD5.Create();
            byte[] hash = md5Hash.ComputeHash(GetPacketBytes(), 8, GetPacketBytes().Length - 8);
            uint checksum = BitConverter.ToUInt32(hash, 0);
            header.Checksum = checksum;
            return checksum;
        }

        public void DebugPrintPacket()
        {
#if DEBUG
            Program.Log.Debug(
                string.Format("Size:0x{0:X} Opcode:{1}",
                    header.PacketSize, header.Opcode,
                    Environment.NewLine, Utils.ByteArrayToHex(GetHeaderBytes())));
            Program.Log.Debug(Environment.NewLine + Utils.ByteArrayToHex(GetPacketBytes(), BASEPACKET_SIZE));
#endif
        }
    }

}