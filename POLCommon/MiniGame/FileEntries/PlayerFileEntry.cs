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
    public unsafe struct PlayerFileEntry
    {
        [FieldOffset(0x00)] public ulong Id;
        [FieldOffset(0x08)] public ulong RoomId;
        [FieldOffset(0x10)] public int Time;
        [FieldOffset(0x18)] public uint GameId;
        [FieldOffset(0x1C)] public uint Level;
        [FieldOffset(0x20)] public uint Rating;
        [FieldOffset(0x24)] public ushort Volume;
        [FieldOffset(0x26)] public byte Domain;
        [FieldOffset(0x27)] public byte Class;
        [FieldOffset(0x28)] private fixed byte NameBuff[0x10];
        [FieldOffset(0x38)] private fixed byte GameDataBuff[0x20];

        public const int SIZE = 0x58;

        public string Name
        {
            get
            {
                fixed (byte* ptr = &NameBuff[0])
                {
                    string str = Encoding.GetEncoding("shift_jis").GetString(new ReadOnlySpan<byte>(ptr, 0x10));
                    return str[..str.IndexOf('\0')];
                }
            }

            set
            {
                ReadOnlySpan<byte> str = Encoding.GetEncoding("shift_jis").GetBytes(value);
                int len = str.Length < 0x10 ? str.Length : 0xF;
                fixed (byte* pStr = &NameBuff[0])
                {
                    str.CopyTo(new Span<byte>(pStr, len));
                    pStr[len] = 0;
                }
            }
        }

        public byte[] GameData
        {
            get
            {
                fixed (byte* ptr = &GameDataBuff[0])
                {
                    byte[] managed = new byte[0x20];
                    new ReadOnlySpan<byte>(ptr, 0x20).CopyTo(managed);
                    return managed;
                }
            }

            set
            {
                fixed (byte* pStr = &GameDataBuff[0])
                {
                    value.CopyTo(new Span<byte>(pStr, 0x20));
                }
            }
        }

        public static PlayerFileEntry Create(MgPlayer player)
        {
            return new()
            {
                Id = player.Id,
                RoomId = player.RoomId,
                Time = player.EnterTime,
                GameId = (uint)player.GameId,
                Level = (uint)player.Level,
                Rating = (uint)player.Rating,
                Volume = player.Volume,
                Domain = player.Domain,
                Class = 2,
                Name = player.Name,
                GameData = player.GameData
            };
        }
    }
}
