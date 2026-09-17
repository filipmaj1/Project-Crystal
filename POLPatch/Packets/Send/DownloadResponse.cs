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
using System;
using System.IO;
using System.Text;

namespace Crystal.POLPatch.Packets.Send
{
    class DownloadResponse
    {
        public const uint OPCODE = 0x04;

        private readonly int fileSize;
        private readonly string filePath;
        private readonly byte[] file;

        public DownloadResponse(string filePath, byte[] file, int fileSize)
        {
            this.fileSize = fileSize;
            this.filePath = filePath;
            this.file = file;   
        }        

        public BasePacket GetDataBytes()
        {
            int size = 0xC + (Encoding.ASCII.GetByteCount(filePath) + 1) + file.Length;
           
            byte[] data = new byte[size];

            using (MemoryStream mem = new(data))
            {
                using BinaryWriter binWriter = new(mem);
                binWriter.Write((UInt32)0);
                binWriter.Write((UInt32)fileSize);
                binWriter.Write((UInt32)Encoding.ASCII.GetByteCount(filePath) + 1);
                Utils.WriteNullTermString(binWriter, filePath, 256, false);
                binWriter.Write(file);
            }

            BasePacketHeader header = BasePacket.CreateHeader(OPCODE);             
            return new BasePacket(header, data);
        }
    }
}