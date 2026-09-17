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

using Crystal.Common.MiniGame;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Crystal.Common.MiniGame.PolFileSystem.FileEntries
{
    [StructLayout(LayoutKind.Explicit, Size = SIZE)]
    public unsafe struct TableFileEntry
    {
        [FieldOffset(0x00)] public ulong Id;
        [FieldOffset(0x08)] public uint Unk0;
        [FieldOffset(0x0C)] public uint NumMembers;
        [FieldOffset(0x10)] public uint IsValidUnk;
        [FieldOffset(0x14)] public uint State;
        [FieldOffset(0x18)] private fixed byte IrcChannelBuff[0x10];
        [FieldOffset(0x28)] private fixed byte GameDataBuff[0x40];

        public const int SIZE = 0x68;

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
                    byte[] managed = new byte[0x40];
                    new ReadOnlySpan<byte>(ptr, 0x40).CopyTo(managed);
                    return managed;
                }
            }

            set
            {
                fixed (byte* pStr = &GameDataBuff[0])
                {
                    value.CopyTo(new Span<byte>(pStr, 0x40));
                }
            }
        }

        public static TableFileEntry Create(MgTable table)
        {
            return new()
            {
                Id = table.Id,
                Unk0 = table.Unk1,
                NumMembers = table.NumMembers,
                IsValidUnk = table.IsValidUnk,
                State = table.State,
                IrcChannel = table.IrcChannel,
                GameData = table.GameData
            };
        }
    }
}
