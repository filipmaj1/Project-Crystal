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
using System.Diagnostics;
using System.IO;

namespace Crystal.POLProfile.Packets
{
    class PacketBuilders
    {
        /// <summary>
        /// Packet Type is defined by the first byte in the sent packet, though packets always arrive in the same order.
        /// </summary>
        public abstract class Packet {};

        /// <summary>
        /// The first thing the POL client will do when connecting to the Profile Server is send the
        /// IP and Port the client is connected to the Auth Server on. This can be used to retrieved
        /// the required Blowfish keys to begin encrypted communication.
        /// </summary>
        public class HandshakePacket : Packet
        {
            public readonly uint ip;
            public readonly ushort port;

            public HandshakePacket(uint ip, ushort port)
            {
                this.ip = ip;
                this.port = port;
            }
        }

        /// <summary>
        /// Once communication is established, the request is made. Requests are defined by two opcodes
        /// and an optional payload "object". If an object is included, it will immediatly follow up to
        /// `objectSize` number of bytes.
        /// </summary>
        public class RequestPacket : Packet
        {
            public readonly ushort opcode;
            public readonly uint objectSize;
            public readonly byte[] hash;

            public RequestPacket(ushort opcode, uint objectSize, byte[] hash)
            {
                this.opcode = opcode;
                this.objectSize = objectSize;
                this.hash = hash;
            }
        }
        
        /// <summary>
        /// Reads the first byte and based on it's value creates a `Handshake` or `Request` packet.
        /// </summary>
        /// <param name="data">The parsed packet data</param>
        /// <returns></returns>
        public static Packet ParsePacket(byte[] data)
        {
            Debug.Assert(data != null && data.Length == 0x28);

            using MemoryStream memStream = new(data);
            using BinaryReader binReader = new(memStream);
            var type = binReader.ReadByte();

            if (type == 0)
            {
                binReader.BaseStream.Seek(3, SeekOrigin.Current);
                ushort unknown = binReader.ReadUInt16();
                Debug.Assert(unknown == 0x1, "Hello packet should've been 0x1");
                ushort port = binReader.ReadUInt16();
                byte[] ipBytes = binReader.ReadBytes(4);
                uint ip = (uint)((ipBytes[0] << 24) | (ipBytes[1] << 16) | (ipBytes[2] << 8) | ipBytes[3]);

                return new HandshakePacket(ip, port);
            }
            if (type == 2)
            {
                byte opcode1 = binReader.ReadByte();
                ushort opcode2 = binReader.ReadUInt16();
                uint objSize = binReader.ReadUInt32();

                binReader.BaseStream.Seek(0x18, SeekOrigin.Begin);
                byte[] hash = binReader.ReadBytes(0x10);

                ushort combinedOpcode = (ushort)((opcode1 << 8) | opcode2);

                return new RequestPacket(combinedOpcode, objSize, hash);
            }
            //else
            //Debug.Assert(false, "Unknown packet type found: " + type);
            return null;
        }

        /* Error Codes:
         * E4 - Registed Name
         * 79 - No member with that address
         */
        public static byte[] BuildResponsePacket(uint serverIp, byte[] obj = null, byte error = 0)
        {
            byte[] data = new byte[0x18];

            using (MemoryStream memStream = new(data))
            using (BinaryWriter binWriter = new(memStream))
            {
                binWriter.Write((Byte)0x83);
                binWriter.Write((Byte)error);
                binWriter.Write((UInt16)0);

                if (obj == null)
                    binWriter.Write((UInt32)0x0);
                else
                    binWriter.Write((UInt32)obj.Length);

                binWriter.Write((UInt32)serverIp);
            }

            return data;
        }
    }
}
