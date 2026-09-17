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

namespace Crystal.POLProfile.DataObjects.Pol.Character
{
    [StructLayout(LayoutKind.Explicit, Size = SIZE)]
    unsafe struct CharacterData
    {
        [FieldOffset(0x00)] public byte CreationPosition;
        [FieldOffset(0x01)] public byte CustomPosition;
        [FieldOffset(0x02)] public byte Unknown1;
        [FieldOffset(0x03)] public byte Language;
        [FieldOffset(0x04)] public byte IsAttached;
        [FieldOffset(0x05)] public byte HandlePosition;
        [FieldOffset(0x06)] public byte LinkedIndexPosition;
        [FieldOffset(0x08)] public ushort ContentClass;
        [FieldOffset(0x0A)] public byte Unknown2;
        [FieldOffset(0x0B)] public byte Unknown3;
        [FieldOffset(0x0C)] public uint ContentUserSubId;
        [FieldOffset(0x10)] public ulong ContentUserId;
        [FieldOffset(0x18)] private fixed byte NameBuff[0x10];
        [FieldOffset(0x28)] private fixed byte InfoBuff[0x40];

        public const int SIZE = 0x68;

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

        public string Info
        {
            get
            {
                fixed (byte* ptr = &InfoBuff[0])
                {
                    string str = Encoding.UTF8.GetString(new ReadOnlySpan<byte>(ptr, 0x40));
                    return str[..str.IndexOf('\0')];
                }
            }

            set
            {
                ReadOnlySpan<byte> info = Encoding.UTF8.GetBytes(value);
                int len = info.Length <= 0x40 ? info.Length : 0x40;
                fixed (byte* pInfo = &InfoBuff[0])
                {
                    info.CopyTo(new Span<byte>(pInfo, len));
                }
            }
        }

        public static CharacterData FromSql(MySqlDataReader reader)
        {
            byte creationPosition = reader.GetByte("creationPosition");
            byte customPosition = reader.GetByte("customPosition");
            byte handlePosition = !reader.IsDBNull(reader.GetOrdinal("handleCreationPosition")) ? reader.GetByte("handleCreationPosition") : (byte) 0xFF;
            byte linkPosition = reader.GetByte("linkPosition");
            ushort contentClass = reader.GetUInt16("contentClass");
            string name = reader.GetString("name");
            string info = reader.GetString("info");
            ulong id = reader.GetUInt64("id");
            uint subId = reader.GetUInt32("subId");

            CharacterData characterData = new()
            {
                CreationPosition = creationPosition,
                CustomPosition = customPosition,
                IsAttached = (byte)(handlePosition != 0xFF ? 1 : 0),
                HandlePosition = handlePosition,
                LinkedIndexPosition = linkPosition,
                ContentClass = contentClass,
                Language = 0,
                Unknown1 = 0x7F,
                Unknown2 = 0x30,
                Unknown3 = 0x30,
                ContentUserId = id,
                ContentUserSubId = subId,
                Name = name,
                Info = info,
            };

            return characterData;
        }
    }
}
