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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Crystal.POLProfile.DataObjects.Pol.Handle
{

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct HandleNamePrimitive
    {
        public const int SIZE = 0x18;

        private ulong Bitfield;
        private fixed byte NameBuff[0x10];

        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (Bitfield & 1) == 1;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Bitfield = value ? Bitfield | 1 : Bitfield & ~(ulong)1;
        }

        public ulong Id
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Bitfield >> 1 & 0xFFFFFFFFFFF;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Bitfield = Bitfield & 0xFFFFE00000000001 | (value & 0xFFFFFFFFFFF) << 1;
        }

        public byte CreationPosition
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (byte)(Bitfield >> 45 & 0x3F);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Bitfield = Bitfield & 0xFFF81FFFFFFFFFFF | ((ulong)value & 0x3F) << 45;
        }

        public string Name
        {
            get
            {
                fixed (byte* ptr = &NameBuff[0])
                {
                    string str = Encoding.UTF8.GetString(new ReadOnlySpan<byte>(ptr, 0x10));
                    return str.Substring(0, str.IndexOf('\0'));
                }
            }

            set
            {
                ReadOnlySpan<byte> name = Encoding.UTF8.GetBytes(value);
                int len = name.Length <= 0x10 ? name.Length : 0x10;
                fixed (byte* pName = &NameBuff[0])
                {
                    name.CopyTo(new Span<byte>(pName, len));
                }
            }
        }

        public override string ToString()
        {
            string isValid = IsValid ? "O" : "X";
            return $"[{isValid}] Pos: {CreationPosition}, Id: {Id}, Name: {Name}";
        }
    }
}
