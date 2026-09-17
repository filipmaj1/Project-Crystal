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
    public class JanghourouTable() : PolDbTable(3, "poldb_jang", [
            new("z_phead", "", Datatype.PROFPTR),
            new("z_ctsid", "contentSubId", Datatype.INT32),
            new("z_ctid", "contentId", Datatype.ULONG64),
            new("z_name", "name", Datatype.STRINGS, 0x10),
            new("z_purp", "purpose", Datatype.BINARY),
            new("z_rlang", "lang", Datatype.BINARY),
            new("z_status", "status", Datatype.SHORT16),
            new("z_contentsno", "contentsNo", Datatype.SHORT16),
            new("z_polid", "polId", Datatype.CHARACTERS),
            new("z_attrstr", "", Datatype.STRINGS, 0x40),
            new("z_attrsl0", "", Datatype.ULONG64),
            new("z_attrsl1", "", Datatype.ULONG64),
            new("z_attrflt0", "", Datatype.INT32),
            new("z_attrflt1", "", Datatype.INT32),
            new("z_attrflt2", "", Datatype.INT32),
            new("z_attrflt3", "", Datatype.INT32),
            new("z_attrsi0", "", Datatype.INT32),
            new("z_attrsi1", "", Datatype.INT32),
            new("z_attrsi2", "", Datatype.INT32),
            new("z_attrsi3", "", Datatype.INT32),
            new("z_attrsi4", "", Datatype.INT32),
            new("z_attrsi5", "", Datatype.INT32),
            new("z_attrsi6", "", Datatype.INT32),
            new("z_attrsi7", "", Datatype.INT32),
            new("z_attrss0", "", Datatype.SHORT16),
            new("z_attrss1", "", Datatype.SHORT16),
            new("z_attrss2", "", Datatype.SHORT16),
            new("z_attrss3", "", Datatype.SHORT16),
            new("z_attrss4", "", Datatype.SHORT16),
            new("z_attrss5", "", Datatype.SHORT16),
            new("z_attrss6", "", Datatype.SHORT16),
            new("z_attrss7", "", Datatype.SHORT16),
            new("z_attrsb0", "", Datatype.SHORT16),
            new("z_attrsb1", "", Datatype.SHORT16),
            new("z_attrsb2", "", Datatype.SHORT16),
            new("z_attrsb3", "", Datatype.SHORT16),
            new("z_attrsb4", "", Datatype.SHORT16),
            new("z_attrsb5", "", Datatype.SHORT16),
            new("z_attrsb6", "", Datatype.SHORT16),
            new("z_attrsb7", "", Datatype.SHORT16),
        ], 0xF8)
    {
    }
}