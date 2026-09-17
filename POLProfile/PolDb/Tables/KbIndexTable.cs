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
    public class KbIndextable() : PolDbTable(1005, "poldb_kb_faqs", [
        new("z_id", "groupCode", Datatype.STRINGS),
        new("z_cate1", "zoneCount", Datatype.USHORT16),
        new("z_cate2", "zoneCount", Datatype.USHORT16),
        new("z_que", "zoneCount", Datatype.STRINGS, 0x101),
        new("z_ans", "zoneCount", Datatype.STRINGS, 0x1),
        new("z_sr1", "zoneCount", Datatype.STRINGS, 0x1),
        new("z_sr2", "zoneCount", Datatype.STRINGS, 0x1),
        new("z_ctime", "zoneCount", Datatype.UINT32),
        new("z_mtime", "zoneCount", Datatype.UINT32),
        new("z_locl", "zoneCount", Datatype.STRINGS, 0x9),
        ], 0x10)
    {
    }
}   