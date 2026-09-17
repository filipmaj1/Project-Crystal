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
using System.Text;

namespace Crystal.POLPatch.Packets.Send
{
    class AskNewestResponse
    {
        public const uint OPCODE = 0x08;

        private readonly int patchTimeUTC;
        private readonly byte[] versionData;
        private readonly string rootFolder;

        public AskNewestResponse(int patchTimeUTC, byte[] versionData, string patchRootFolder)
        {
            this.patchTimeUTC = patchTimeUTC;
            this.versionData = versionData;
            this.rootFolder = patchRootFolder;            
        }        

        public BasePacket GetDataBytes()
        {
            int size = 0x4c + Encoding.ASCII.GetByteCount(rootFolder) + 1;
            if (size % 4 != 0)
                size += 4 - (size % 4);
            byte[] data = new byte[size];

            using (MemoryStream mem = new(data))
            {
                using BinaryWriter binWriter = new(mem);
                binWriter.Write((UInt32)patchTimeUTC);
                binWriter.Write((UInt32)0);
                binWriter.Write(versionData);
                binWriter.Write((UInt32)Encoding.ASCII.GetByteCount(rootFolder) + 1);
                binWriter.Write(Encoding.ASCII.GetBytes(rootFolder));
            }

            BasePacketHeader header = BasePacket.CreateHeader(OPCODE);             
            return new BasePacket(header, data);
        }
    }
}
