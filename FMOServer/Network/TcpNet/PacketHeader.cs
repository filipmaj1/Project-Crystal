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

using System.Runtime.InteropServices;

namespace Crystal.FrontMissionOnline.Network.TcpNet
{
    [StructLayout(LayoutKind.Explicit, Size = SIZE)]
    unsafe struct PacketHeader
    {
        [FieldOffset(0x00)] public ushort Size;
        [FieldOffset(0x02)] public ushort Flags;
        [FieldOffset(0x04)] public ushort Checksum;
        [FieldOffset(0x06)] public ushort CommandId;
        [FieldOffset(0x08)] public ushort ErrorId;
        [FieldOffset(0x10)] public uint SessionId;
        public const int SIZE = 0x14;

        public bool IsEncrypted
        {
            get
            {
                return (Flags & 0x0100) != 0;
            }

            set
            {
                if (value)
                    Flags |= 0x0100;
                else
                    Flags &= 0xFEFF;
            }
        }

        public bool HasChecksum
        {
            get
            {
                return (Flags & 0x0200) != 0;
            }

            set
            {
                if (value)
                    Flags |= 0x0200;
                else
                    Flags &= 0xFDFF;
            }
        }
    }
}
