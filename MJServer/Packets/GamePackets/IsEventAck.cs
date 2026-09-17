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

namespace Crystal.Mahjong.Packets.GamePackets
{
    /* Packet to acknowledge if the game is in an event. 
     */
    [StructLayout(LayoutKind.Sequential, Size = SIZE)]
    public unsafe struct IsEventAck
    {
        public uint Result;
        public uint Result2;
        private fixed byte UnkBuff[0x40];
        public uint Unknown;

        public const int SIZE = 0x4C;

        public string Unk
        {
            get
            {
                fixed (byte* ptr = &UnkBuff[0])
                {
                    string str = Encoding.UTF8.GetString(new ReadOnlySpan<byte>(ptr, 0x40));
                    return str[..str.IndexOf('\0')];
                }
            }

            set
            {
                ReadOnlySpan<byte> str = Encoding.UTF8.GetBytes(value);
                int len = str.Length <= 0x40 ? str.Length : 0x40;
                fixed (byte* pBuff = &UnkBuff[0])
                {
                    str.CopyTo(new Span<byte>(pBuff, len));
                }
            }
        }

    }
}
