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

using Crystal.Common;
using MySqlConnector;
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Crystal.POLProfile.DataObjects.Pol.Group
{
    unsafe struct GroupMember
    {
        // The client's member array per group is exactly this many rows
        public const int MAX_MEMBERS = 0x40;

        public const byte RANK_REMOVED = 0x1;
        public const byte RANK_INVITED = 0x2;
        public const byte RANK_NORMAL = 0x3;
        public const byte RANK_SUBMASTER = 0x4;
        public const byte RANK_MASTER = 0x5;

        /* 
         PackedInfo bit layout

          63      59 58      53 52    50 49      44 43                                          0
         +----------+----------+--------+----------+--------------------------------------------+
         |  Unused  | HandlePos|  Rank  |  Unused  |                 ProfileId                  |
         |  5 bits  |  6 bits  | 3 bits |  6 bits  |                  44 bits                   |
         +----------+----------+--------+----------+--------------------------------------------+

           ProfileId      - Id to retrieve profile
           Rank           - 3 = normal, 4 = submaster, 5 = master (client accepts 2-5)
           HandlePosition - handle creationPosition (0-63), part of member identity
        */

        public ulong PolProId;
        public ulong PackedInfo;
        public fixed byte NameBuff[0x10];

        public const int SIZE = 0x20;

        public ulong ProfileId
        {
            get => PackedInfo & 0xFFFFFFFFFFF;
            set => PackedInfo = (value & 0xFFFFFFFFFFF) | (PackedInfo & ~(ulong)0xFFFFFFFFFFF);
        }

        public byte Rank
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (byte)((PackedInfo >> 50) & 0x7);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => PackedInfo = ((ulong)(value & 0x7) << 50) | (PackedInfo & ~((ulong)0x7 << 50));
        }

        public byte HandlePosition
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (byte)((PackedInfo >> 53) & 0x3F);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => PackedInfo = ((ulong)(value & 0x3F) << 53) | (PackedInfo & ~((ulong)0x3F << 53));
        }

        public string Name
        {
            get
            {
                fixed (byte* ptr = &NameBuff[0])
                {
                    string str = Encoding.UTF8.GetString(new ReadOnlySpan<byte>(ptr, 0x10));
                    int end = str.IndexOf('\0');
                    return end >= 0 ? str[..end] : str;
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

        public static GroupMember FromSql(MySqlDataReader reader)
        {
            string polProData = reader.GetString("polId");
            ulong profileId = reader.GetUInt64("handleId");
            byte handlePosition = reader.GetByte("creationPosition");
            string name = reader.GetString("name");
            byte rank = reader.GetByte("rank");

            GroupMember member = new()
            {
                PolProId = SqCrypto.PolProDataToPolId(polProData, 0, 0),
                ProfileId = profileId,
                HandlePosition = handlePosition,
                Rank = rank,
                Name = name
            };

            return member;
        }
    }
}
