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

using System.IO;

namespace Crystal.POLProfile.Packets.Receive
{
    class UpdateStatusRequest
    {
        public readonly byte activeHandleNumber;
        public readonly ushort currentContentClass;
        public readonly byte currentOnlineStatus; // 0: Offline, 1: Online, 2: Away, 5: Invisible

        public UpdateStatusRequest(byte[] receiveObject)
        {
            using MemoryStream memStream = new(receiveObject);
            using BinaryReader binReader = new(memStream);
            binReader.BaseStream.Seek(0x10, SeekOrigin.Begin);
            activeHandleNumber = binReader.ReadByte();
            binReader.ReadByte();
            binReader.ReadUInt16();
            currentContentClass = binReader.ReadUInt16();
            currentOnlineStatus = binReader.ReadByte();
        }
    }
}
