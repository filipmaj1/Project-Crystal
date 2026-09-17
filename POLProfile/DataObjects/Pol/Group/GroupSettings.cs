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

using MySqlConnector;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Crystal.POLProfile.DataObjects.Pol.Group
{
    [StructLayout(LayoutKind.Explicit)]
    unsafe struct GroupSettings
    {
        [FieldOffset(0x00)] public ulong Id; // Id of Group
        [FieldOffset(0x08)] public fixed byte CommentBuff[0x64]; // Requester's Comment
        [FieldOffset(0x6E)] public byte HandlePosition; // Requester's handle creationPosition (0-63)
        [FieldOffset(0x6F)] public byte OnlineStatus; // Requester's Status
        [FieldOffset(0x70)] public fixed byte NameBuff[0x14]; // Name of Group
        [FieldOffset(0x85)] public byte Rank; // Requester's Rank

        public const int SIZE = 0x88;

        // The client holds four group slots for the whole account, not per handle
        public const int MAX_GROUPS = 4;


        public string Name
        {
            get
            {
                fixed (byte* ptr = &NameBuff[0])
                {
                    string str = Encoding.UTF8.GetString(new ReadOnlySpan<byte>(ptr, 0x14));
                    int end = str.IndexOf('\0');
                    return end >= 0 ? str[..end] : str;
                }
            }

            set
            {
                ReadOnlySpan<byte> name = Encoding.UTF8.GetBytes(value);
                int len = name.Length <= 0x14 ? name.Length : 0x14;
                fixed (byte* pName = &NameBuff[0])
                {
                    name.CopyTo(new Span<byte>(pName, len));
                }
            }
        }

        public string Comment
        {
            get
            {
                fixed (byte* ptr = &CommentBuff[0])
                {
                    string str = Encoding.Unicode.GetString(new ReadOnlySpan<byte>(ptr, 0x64));
                    int end = str.IndexOf('\0');
                    return end >= 0 ? str[..end] : str;
                }
            }

            set
            {
                ReadOnlySpan<byte> comment = Encoding.Unicode.GetBytes(value);
                int len = comment.Length <= 0x64 ? comment.Length : 0x64;
                fixed (byte* pComment = &CommentBuff[0])
                {
                    comment.CopyTo(new Span<byte>(pComment, len));
                }
            }
        }

        public static GroupSettings FromSql(MySqlDataReader reader)
        {
            ulong id = reader.GetUInt64("groupId");
            string name = reader.GetString("groupName");
            byte myStatus = reader.GetByte("myOnlineStatus");
            string myComment = reader.GetString("myComment");
            byte myHandlePosition = reader.GetByte("myHandlePosition");
            byte myRank = reader.GetByte("myRank");

            GroupSettings groupData = new()
            {
                Id = id,
                Name = name,
                OnlineStatus = myStatus,
                Comment = myComment,
                HandlePosition = myHandlePosition,
                Rank = myRank
            };

            return groupData;
        }
    }
}
