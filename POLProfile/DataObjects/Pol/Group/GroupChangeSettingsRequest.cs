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

namespace Crystal.POLProfile.DataObjects.Pol.Group
{
    [StructLayout(LayoutKind.Explicit, Size = SIZE)]
    struct GroupChangeSettingsRequest
    {
        [FieldOffset(0x00)]
        [MarshalAs(UnmanagedType.U8)]
        public ulong Id;
        [FieldOffset(0x08)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x64)]
        public byte[] CommentBytes;
        [FieldOffset(0x6E)]
        [MarshalAs(UnmanagedType.U1)]
        public byte HandlePosition;
        [FieldOffset(0x6F)]
        [MarshalAs(UnmanagedType.U1)]
        public byte OnlineStatus;

        public const int SIZE = 0x78;

        public string Comment
        {
            get
            {
                string str = Encoding.Unicode.GetString(CommentBytes);
                int end = str.IndexOf('\0');
                return end >= 0 ? str[..end] : str;
            }
            set
            {
                Array.Clear(CommentBytes, 0, CommentBytes.Length);
                Encoding.Unicode.GetBytes(value, 0, Math.Min(value.Length, 0x32), CommentBytes, 0);
            }
        }

        public static GroupChangeSettingsRequest FromBytes(byte[] request, int offset = 0)
        {
            IntPtr ptr = Marshal.AllocHGlobal(SIZE);
            Marshal.Copy(request, offset, ptr, SIZE);
            GroupChangeSettingsRequest dataStruct = (GroupChangeSettingsRequest)Marshal.PtrToStructure(ptr, typeof(GroupChangeSettingsRequest));
            Marshal.FreeHGlobal(ptr);
            return dataStruct;
        }
    }
}
