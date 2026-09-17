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
    public unsafe struct RoomFileEntry
    {
        [FieldOffset(0x00)] public ulong Id;
        [FieldOffset(0x10)] public uint NumPlayers;
        [FieldOffset(0x14)] public uint NumPlayers2;
        [FieldOffset(0x18)] public uint NumClosedTables;
        [FieldOffset(0x1C)] public uint UnkJang0;
        [FieldOffset(0x20)] public uint JangIsValid;
        [FieldOffset(0x24)] public ushort Volume;
        [FieldOffset(0x27)] public byte Domain;
        [FieldOffset(0x28)] private fixed byte GameDataBuff[0x80];
        [FieldOffset(0x78)] private fixed byte UnknownBuff[0x10];
        [FieldOffset(0xB8)] private fixed byte IrcChannelBuff[0x10];
        [FieldOffset(0xC5)] private fixed byte Unknown2Buff[0x3];

        public const int SIZE = 0xC8;

        public string IrcChannel
        {
            get
            {
                fixed (byte* ptr = &IrcChannelBuff[0])
                {
                    string str = Encoding.ASCII.GetString(new ReadOnlySpan<byte>(ptr, 0x10));
                    return str[..str.IndexOf('\0')];
                }
            }

            set
            {
                ReadOnlySpan<byte> str = Encoding.ASCII.GetBytes(value);
                int len = str.Length <= 0x10 ? str.Length : 0x10;
                fixed (byte* pStr = &IrcChannelBuff[0])
                {
                    str.CopyTo(new Span<byte>(pStr, len));
                    pStr[0xF] = 0;
                }
            }
        }

        public byte[] GameData
        {
            get
            {
                fixed (byte* ptr = &GameDataBuff[0])
                {
                    byte[] managed = new byte[0x80];
                    new ReadOnlySpan<byte>(ptr, 0x80).CopyTo(managed);
                    return managed;
                }
            }

            set
            {
                fixed (byte* ptr = &GameDataBuff[0])
                {
                    value.CopyTo(new Span<byte>(ptr, 0x80));
                }
            }
        }
    }
}
