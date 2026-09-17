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
    public class DirgeOfCerberusTable() : PolDbTable(10, "poldb_dc", [
        new("z_phead", "", Datatype.PROFPTR),
        new("z_ctsid", "contentSubId", Datatype.INT32),
        new("z_ctid", "contentId", Datatype.ULONG64),
        new("z_rlang", "lang", Datatype.BINARY),
        new("z_name", "name", Datatype.STRINGS, 0x10),
        new("z_glevel", "glevel", Datatype.UINT32),
        new("z_class", "class", Datatype.USHORT16),
        new("z_mvp", "mvp", Datatype.UINT32),
        new("z_rankpoint", "rankPoint", Datatype.UINT32),
        ], 0xF8)
    {
    }
}