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

namespace Crystal.POLAuth.DataObjects
{
    [StructLayout(LayoutKind.Sequential, Size = SIZE)]
    unsafe struct MailAccountData
    {
        public uint Mode;
        public uint LastUpdate;
        private fixed byte MainEmailNameBuff[0x10];
        private fixed byte ExtraEmail1Buff[0x80];
        private fixed byte ExtraEmail2Buff[0x80];
        private fixed byte ExtraEmail3Buff[0x80];
        private fixed byte ExtraEmail4Buff[0x80];
        private fixed byte ExtraEmail5Buff[0x80];

        public const int SIZE = 0x298;

        public string MainEmailName
        {
            get
            {
                fixed (byte* ptr = &MainEmailNameBuff[0])
                {
                    string str = Encoding.UTF8.GetString(new ReadOnlySpan<byte>(ptr, 0x10));
                    return str[..str.IndexOf('\0')];
                }
            }

            set
            {
                ReadOnlySpan<byte> str = Encoding.UTF8.GetBytes(value);
                int len = str.Length <= 0x10 ? str.Length : 0x10;
                fixed (byte* pName = &MainEmailNameBuff[0])
                {
                    str.CopyTo(new Span<byte>(pName, len));
                }
            }
        }

        public string ExtraEmail1
        {
            get
            {
                fixed (byte* ptr = &ExtraEmail1Buff[0])
                {
                    string str = Encoding.UTF8.GetString(new ReadOnlySpan<byte>(ptr, 0x80));
                    return str[..str.IndexOf('\0')];
                }
            }

            set
            {
                ReadOnlySpan<byte> str = Encoding.UTF8.GetBytes(value);
                int len = str.Length <= 0x80 ? str.Length : 0x80;
                fixed (byte* pName = &ExtraEmail1Buff[0])
                {
                    str.CopyTo(new Span<byte>(pName, len));
                }
            }
        }

        public string ExtraEmail2
        {
            get
            {
                fixed (byte* ptr = &ExtraEmail2Buff[0])
                {
                    string str = Encoding.UTF8.GetString(new ReadOnlySpan<byte>(ptr, 0x80));
                    return str[..str.IndexOf('\0')];
                }
            }

            set
            {
                ReadOnlySpan<byte> str = Encoding.UTF8.GetBytes(value);
                int len = str.Length <= 0x80 ? str.Length : 0x80;
                fixed (byte* pName = &ExtraEmail2Buff[0])
                {
                    str.CopyTo(new Span<byte>(pName, len));
                }
            }
        }

        public string ExtraEmail3
        {
            get
            {
                fixed (byte* ptr = &ExtraEmail3Buff[0])
                {
                    string str = Encoding.UTF8.GetString(new ReadOnlySpan<byte>(ptr, 0x80));
                    return str[..str.IndexOf('\0')];
                }
            }

            set
            {
                ReadOnlySpan<byte> str = Encoding.UTF8.GetBytes(value);
                int len = str.Length <= 0x80 ? str.Length : 0x80;
                fixed (byte* pName = &ExtraEmail3Buff[0])
                {
                    str.CopyTo(new Span<byte>(pName, len));
                }
            }
        }

        public string ExtraEmail4
        {
            get
            {
                fixed (byte* ptr = &ExtraEmail4Buff[0])
                {
                    string str = Encoding.UTF8.GetString(new ReadOnlySpan<byte>(ptr, 0x80));
                    return str[..str.IndexOf('\0')];
                }
            }

            set
            {
                ReadOnlySpan<byte> str = Encoding.UTF8.GetBytes(value);
                int len = str.Length <= 0x80 ? str.Length : 0x80;
                fixed (byte* pName = &ExtraEmail4Buff[0])
                {
                    str.CopyTo(new Span<byte>(pName, len));
                }
            }
        }

        public string ExtraEmail5
        {
            get
            {
                fixed (byte* ptr = &ExtraEmail5Buff[0])
                {
                    string str = Encoding.UTF8.GetString(new ReadOnlySpan<byte>(ptr, 0x80));
                    return str[..str.IndexOf('\0')];
                }
            }

            set
            {
                ReadOnlySpan<byte> str = Encoding.UTF8.GetBytes(value);
                int len = str.Length <= 0x80 ? str.Length : 0x80;
                fixed (byte* pName = &ExtraEmail5Buff[0])
                {
                    str.CopyTo(new Span<byte>(pName, len));
                }
            }
        }
    }
}
