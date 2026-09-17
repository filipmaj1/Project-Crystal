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
    public class FrontMissionOnlineTable() : PolDbTable(4, "poldb_fmo", [
        new("z_phead", "", Datatype.PROFPTR),
        new("z_ctsid", "contentSubId", Datatype.INT32),
        new("z_ctid", "contentId", Datatype.ULONG64),
        new("z_rlang", "lang", Datatype.BINARY),
        new("z_firstname", "firstName", Datatype.STRINGS, 0x11),
        new("z_lastname", "lastName", Datatype.STRINGS, 0x11),
        new("z_worldname", "worldName", Datatype.STRINGS, 0x10),
        new("z_countryid", "countryId", Datatype.SHORT16),
        new("z_zoneid", "zoneId", Datatype.SHORT16),
        new("z_name", "name", Datatype.STRINGS, 0x10),
        ], 0xF8)
    {
    }
}