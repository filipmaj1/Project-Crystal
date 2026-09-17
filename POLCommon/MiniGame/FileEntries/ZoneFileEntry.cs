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
using System.Text;

namespace Crystal.Common.MiniGame.PolFileSystem.FileEntries
{
    [StructLayout(LayoutKind.Explicit, Size = SIZE)]
    public unsafe struct ZoneFileEntry
    {
        [FieldOffset(0x00)] public ulong NumPlayer;
        [FieldOffset(0x04)] public uint NumRooms;
        [FieldOffset(0x08)] public int UnkRequired;
        [FieldOffset(0x0D)] private fixed byte GameDataBuff[0x1F];
        [FieldOffset(0x2C)] private fixed byte IrcHostBuff[0x10];
        [FieldOffset(0x3C)] public uint RoomNum;

        public const int SIZE = 0x40;

        public byte[] GameData
        {
            get
            {
                fixed (byte* ptr = &GameDataBuff[0])
                {
                    byte[] managed = new byte[0x30];
                    new ReadOnlySpan<byte>(ptr, 0x30).CopyTo(managed);
                    return managed;
                }
            }

            set
            {
                fixed (byte* pStr = &GameDataBuff[0])
                {
                    value.CopyTo(new Span<byte>(pStr, 0x30));
                }
            }
        }

        public string IrcHost
        {
            get
            {
                fixed (byte* ptr = &IrcHostBuff[0])
                {
                    string str = Encoding.GetEncoding("shift_jis").GetString(new ReadOnlySpan<byte>(ptr, 0x10));
                    return str[..str.IndexOf('\0')];
                }
            }

            set
            {
                ReadOnlySpan<byte> str = Encoding.GetEncoding("shift_jis").GetBytes(value);
                int len = str.Length < 0x10 ? str.Length : 0xF;
                fixed (byte* pStr = &IrcHostBuff[0])
                {
                    str.CopyTo(new Span<byte>(pStr, len));
                    pStr[len] = 0;
                }
            }
        }
    }
}
