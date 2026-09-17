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

namespace Crystal.POLProfile.DataObjects.Pol.Group
{
    [StructLayout(LayoutKind.Sequential, Size = SIZE)]
    struct GroupChangeMembersRequest
    {
        [MarshalAs(UnmanagedType.U8)]
        public ulong GroupId;
        [MarshalAs(UnmanagedType.U8)]
        public ulong NumMembers1;
        [MarshalAs(UnmanagedType.U8)]
        public ulong MemberPolProId;
        [MarshalAs(UnmanagedType.U1)]
        public byte HandlePosition;
        [MarshalAs(UnmanagedType.U1)]
        public byte Rank;

        public const int SIZE = 0x28;

        public static GroupChangeMembersRequest FromBytes(byte[] bytes)
        {
            IntPtr ptr = Marshal.AllocHGlobal(SIZE);
            Marshal.Copy(bytes, 0, ptr, SIZE);
            GroupChangeMembersRequest dataStruct = (GroupChangeMembersRequest)Marshal.PtrToStructure(ptr, typeof(GroupChangeMembersRequest));
            Marshal.FreeHGlobal(ptr);
            return dataStruct;
        }
    }
}
