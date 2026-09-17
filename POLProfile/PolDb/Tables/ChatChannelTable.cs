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

namespace Crystal.POLProfile.PolDb.Tables
{
    public class ChatChannelTable() : PolDbTable(1001, "chatroom", [
        new("z_pf", "", Datatype.BINARY),
        new("z_lang", "languageCode", Datatype.BINARY),
        new("z_use", "usersMax", Datatype.BINARY),
        new("z_kind", "", Datatype.BINARY),
        new("z_idx", "", Datatype.STRINGS, 0x2E),
        new("z_npers", "usersCurrent", Datatype.INT32),
        new("z_zone", "zoneCode", Datatype.USHORT16),
        new("z_cate0", "memberCode", Datatype.USHORT16),
        new("z_cate1", "purposeCode", Datatype.USHORT16),
        new("z_cate2", "languageCode", Datatype.USHORT16),
        new("z_topic", "chatroomName", Datatype.STRINGS, 0x49),
        new("z_capa", "usersMax", Datatype.INT32),
        new("z_chlock", "hasPassword", Datatype.BINARY),
        new("z_ctime", "createTime", Datatype.UINT32),
        ], 0xA0)
    {
    }
}

/*

            // Knowledge Base FAQ
            [1004] = new Dictionary<byte, PolDbColumn>
            {
                [0] = new PolDbColumn("z_id", Datatype.UINT32),
                [1] = new PolDbColumn("z_cate1", Datatype.USHORT16),
                [2] = new PolDbColumn("z_cate2", Datatype.USHORT16),
                [3] = new PolDbColumn("z_que", Datatype.STRINGS),
                [4] = new PolDbColumn("z_ans", Datatype.STRINGS),
                [5] = new PolDbColumn("z_sr1", Datatype.STRINGS),
                [6] = new PolDbColumn("z_sr2", Datatype.STRINGS),
                [7] = new PolDbColumn("z_ctime", Datatype.UINT32),
                [8] = new PolDbColumn("z_mtime", Datatype.UINT32),
                [9] = new PolDbColumn("z_locl", Datatype.STRINGS),
            },
            // Knowledge Base Index
            [1005] = new Dictionary<byte, PolDbColumn>
            {
                [0] = new PolDbColumn("z_id", Datatype.UINT32),
                [1] = new PolDbColumn("z_cate1", Datatype.USHORT16),
                [2] = new PolDbColumn("z_cate2", Datatype.USHORT16),
                [3] = new PolDbColumn("z_que", Datatype.STRINGS),
                [4] = new PolDbColumn("z_ans", Datatype.STRINGS),
                [5] = new PolDbColumn("z_sr1", Datatype.STRINGS),
                [6] = new PolDbColumn("z_sr2", Datatype.STRINGS),
                [7] = new PolDbColumn("z_ctime", Datatype.UINT32),
                [8] = new PolDbColumn("z_mtime", Datatype.UINT32),
                [9] = new PolDbColumn("z_locl", Datatype.STRINGS),
            },*/