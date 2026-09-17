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
using System.IO;
using System.Text;

namespace Crystal.Common.Notification.Payload
{
    public class FriendPayloadBuilder
    {
        private const byte UPDATE_STATUS        = 0x01;
        private const byte UPDATE_GROUP         = 0x02;
        private const byte UPDATE_DISPLAYPIC    = 0x04;
        private const byte UPDATE_UNKNOWN       = 0x08;
        private const byte UPDATE_NAME          = 0x10;
        private const byte UPDATE_COMMENT       = 0x20;
        private const byte UPDATE_TOPRIGHT      = 0x40;

        private byte StatusBits = 0;
        private ushort PayloadSize = 8;

        private uint DisplayPicId = 0;
        private ulong GroupId = 0;
        private ulong GroupPackedInfo = 0;
        private string NameStr = null;
        private string CommentStr = null;
        private string TopRightStr = null;

        public FriendPayloadBuilder(bool updateStatus = false)
        {
            if (updateStatus)
                StatusBits |= UPDATE_STATUS;            
        }

        public FriendPayloadBuilder GroupData(ulong groupId, ulong packedInfo)
        {
            GroupId = groupId;
            GroupPackedInfo = packedInfo;
            StatusBits |= UPDATE_GROUP;
            PayloadSize += 0x10;
            return this;
        }

        public FriendPayloadBuilder DisplayPic(uint dpId)
        {
            DisplayPicId = dpId;
            StatusBits |= UPDATE_DISPLAYPIC;
            PayloadSize += 0x8;
            return this;
        }
        public FriendPayloadBuilder Unknown()
        {
            StatusBits |= UPDATE_UNKNOWN;
            PayloadSize += 0x10;
            return this;
        }

        public FriendPayloadBuilder Name(string name)
        {
            NameStr = name;
            StatusBits |= UPDATE_NAME;
            PayloadSize += 0x10;
            return this;
        }

        public FriendPayloadBuilder Comment(string comment)
        {
            CommentStr = comment;
            StatusBits |= UPDATE_COMMENT;
            PayloadSize += 0x68;
            return this;
        }

        public FriendPayloadBuilder TopRight(string topRight)
        {
            TopRightStr = topRight;
            StatusBits |= UPDATE_TOPRIGHT;
            PayloadSize += 0x10;
            return this;
        }

        public byte[] BuildPayload()
        {
            byte[] payload = new byte[PayloadSize];

            using (MemoryStream memStream = new(payload))
            using (BinaryWriter writer = new(memStream))
            {
                writer.Write((UInt64)StatusBits);

                if ((StatusBits & UPDATE_GROUP) != 0)
                {
                    writer.Write(GroupId);
                    writer.Write(GroupPackedInfo);
                }

                if ((StatusBits & UPDATE_DISPLAYPIC) != 0)
                {
                    writer.Write((UInt64)DisplayPicId);
                }

                if ((StatusBits & UPDATE_UNKNOWN) != 0)
                {
                    int pos = (int)writer.BaseStream.Position;
                    writer.Seek(pos + 0x10, SeekOrigin.Begin);
                }

                if ((StatusBits & UPDATE_NAME) != 0)
                {
                    int pos = (int) writer.BaseStream.Position;
                    byte[] byteStr = Encoding.ASCII.GetBytes(NameStr);
                    writer.Write(byteStr, 0, byteStr.Length > 0xF ? 0xF : byteStr.Length);
                    writer.Seek(pos + 0x10, SeekOrigin.Begin);
                }

                if ((StatusBits & UPDATE_COMMENT) != 0)
                {
                    int pos = (int)writer.BaseStream.Position;
                    byte[] byteStr = Encoding.Unicode.GetBytes(CommentStr);
                    writer.Write(byteStr, 0, byteStr.Length > 0x64 ? 0x64 : byteStr.Length);
                    writer.Seek(pos + 0x68, SeekOrigin.Begin);
                }

                if ((StatusBits & UPDATE_TOPRIGHT) != 0)
                {
                    byte[] byteStr = Encoding.ASCII.GetBytes(TopRightStr);
                    writer.Write(byteStr, 0, byteStr.Length > 0xF ? 0xF : byteStr.Length);
                }
            }

            return payload;
        }
    }
}