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

namespace Crystal.POLPatch.Packets.Receive
{
    class AskNewestRequest
    {
        public const uint OPCODE = 0x07;

        public readonly string HardwareId;
        public readonly string ApplicationId;
        public readonly byte[] VersionData;
        public readonly String VersionString;

        public bool InvalidPacket = false;

        public AskNewestRequest(byte[] data)
        {
            using MemoryStream mem = new(data);
            using BinaryReader binReader = new(mem);
            try
            {
                HardwareId = Encoding.ASCII.GetString(binReader.ReadBytes(4)).Trim('\0');
                ApplicationId = Encoding.ASCII.GetString(binReader.ReadBytes(4)).Trim('\0');
                VersionData = binReader.ReadBytes(0x40);
                VersionString = Encoding.ASCII.GetString(VersionData).Trim('\0');
            }
            catch (Exception)
            {
                InvalidPacket = true;
            }
        }

    }
}
