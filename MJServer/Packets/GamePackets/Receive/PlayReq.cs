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

namespace Crystal.Mahjong.Packets.GamePackets.Receive
{
    [StructLayout(LayoutKind.Sequential, Size = SIZE)]
    public unsafe struct PlayReq
    {
        public ulong PlayerId;
        public ulong TableId;
        public ulong ContentId;
        public int ContentSubId;
        public int Unk1;
        public int HandlePosition;
        public short PlayerDomain;
        public short PlayerVolume;
        public short Unk2;
        public short PlayerVoice;

        private fixed byte EnteredPwdBuff[0x11];
        private fixed byte PlayerNameBuff[0x10];
        private fixed byte UnkBuff1[0x40];
        //private fixed byte UnkBuff2[0x80];

        public const int SIZE = 0xF0;

        public string PlayerName
        {
            get
            {
                fixed (byte* ptr = &PlayerNameBuff[0])
                {
                    string str = Encoding.UTF8.GetString(new ReadOnlySpan<byte>(ptr, 0x10));
                    return str[..str.IndexOf('\0')];
                }
            }

            set
            {
                ReadOnlySpan<byte> str = Encoding.UTF8.GetBytes(value);
                int len = str.Length <= 0x10 ? str.Length : 0x10;
                fixed (byte* pBuff = &PlayerNameBuff[0])
                {
                    str.CopyTo(new Span<byte>(pBuff, len));
                }
            }
        }

        public string EnteredPwd
        {
            get
            {
                fixed (byte* ptr = &EnteredPwdBuff[0])
                {
                    string str = Encoding.UTF8.GetString(new ReadOnlySpan<byte>(ptr, 0x11));
                    return str[..str.IndexOf('\0')];
                }
            }

            set
            {
                ReadOnlySpan<byte> str = Encoding.UTF8.GetBytes(value);
                int len = str.Length <= 0x11 ? str.Length : 0x11;
                fixed (byte* pBuff = &EnteredPwdBuff[0])
                {
                    str.CopyTo(new Span<byte>(pBuff, len));
                }
            }

        }
    }
}
