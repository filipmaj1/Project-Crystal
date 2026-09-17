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

namespace Crystal.Common.Notification.Payload
{
    [StructLayout(LayoutKind.Explicit, Size = SIZE)]
    struct GroupMemberStatus
    {
        [FieldOffset(0x00)]
        [MarshalAs(UnmanagedType.U2)]
        public ushort DataFlags;
        [FieldOffset(0x04)]
        [MarshalAs(UnmanagedType.U4)]
        public uint Unknown;
        [FieldOffset(0x08)]
        [MarshalAs(UnmanagedType.U8)]
        public ulong GroupId;
        [FieldOffset(0x10)]
        [MarshalAs(UnmanagedType.U8)]
        public ulong PackedInfo;
        [FieldOffset(0x18)]
        [MarshalAs(UnmanagedType.U4)]
        public uint DisplayIcon;
        [FieldOffset(0x20)]
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x10)]
        public string Name;
        [FieldOffset(0x30)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x64)]
        public byte[] CommentBytes;
        [FieldOffset(0x98)]
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x18)]
        public string TopRightText;

        public const int SIZE = 0xEC;

        public void InitDataFlags()
        {
            DataFlags = 0x77;
        }

        public static GroupMemberStatus FromBytes(byte[] notifyStatus, int offset = 0)
        {
            IntPtr ptr = Marshal.AllocHGlobal(SIZE);
            Marshal.Copy(notifyStatus, offset, ptr, SIZE);
            GroupMemberStatus dataStruct = (GroupMemberStatus)Marshal.PtrToStructure(ptr, typeof(GroupMemberStatus));
            Marshal.FreeHGlobal(ptr);
            return dataStruct;
        }

        public static byte[] ToBytes(GroupMemberStatus notifyStatus)
        {
            byte[] bytes = new byte[SIZE];
            IntPtr ptr = Marshal.AllocHGlobal(SIZE);
            Marshal.StructureToPtr(notifyStatus, ptr, true);
            Marshal.Copy(ptr, bytes, 0, SIZE);
            Marshal.FreeHGlobal(ptr);
            return bytes;
        }
    }
}
