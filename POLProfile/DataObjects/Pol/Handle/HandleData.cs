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

namespace Crystal.POLProfile.DataObjects.Pol
{
    [StructLayout(LayoutKind.Explicit, Size = SIZE)]
    unsafe struct HandleData
    {
        [FieldOffset(0x0)] public byte CreationPosition;
        [FieldOffset(0x1)] public byte CustomPosition;
        [FieldOffset(0x2)] public byte OpenLevel;
        [FieldOffset(0x3)] public byte Unknown;
        [FieldOffset(0x4)] public uint PortraitId;
        [FieldOffset(0x8)] public ulong HandleId;
        [FieldOffset(0x10)] private fixed byte NameBuff[0x10];
        [FieldOffset(0x20)] private fixed byte CommentBuff[0x64];

        public const int SIZE = 0x88;

        public string Name
        {
            get
            {
                fixed (byte* ptr = &NameBuff[0])
                {
                    string str = Encoding.UTF8.GetString(new ReadOnlySpan<byte>(ptr, 0x10));
                    return str[..str.IndexOf('\0')];
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

        public string Comment
        {
            get
            {
                fixed (byte* ptr = &CommentBuff[0])
                {
                    string str = Encoding.Unicode.GetString(new ReadOnlySpan<byte>(ptr, 0x10));
                    return str[..str.IndexOf('\0')];
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

        public static HandleData FromSql(MySqlDataReader reader)
        {
            uint id = reader.GetUInt32("id");
            byte creationPosition = reader.GetByte("creationPosition");
            byte customPosition = reader.GetByte("customPosition");
            uint portraitId = reader.GetUInt32("portrait");
            string name = reader.GetString("name");
            string comment = reader.GetString("comment");

            HandleData handleData = new()
            {
                CreationPosition = creationPosition,
                CustomPosition = customPosition,
                OpenLevel = 0,
                Unknown = 0,
                PortraitId = portraitId,
                HandleId = id,
                Name = name,
                Comment = comment,
            };

            return handleData;
        }


    }
}
