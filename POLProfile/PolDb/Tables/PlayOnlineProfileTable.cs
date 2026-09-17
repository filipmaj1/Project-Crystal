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

using System.Collections.Generic;

namespace Crystal.POLProfile.PolDb.Tables
{
    public class PlayOnlineProfileTable() : PolDbTable(1000, "poldb_profiles_view", [
        new("z_phead", "", Datatype.PROFPTR),
        new("z_name", "name", Datatype.STRINGS, 0x10),
        new("z_hid", "handleId", Datatype.ULONG64),
        new("z_age", "age", Datatype.INT32),
        new("z_age_f", "ageVisibility", Datatype.BINARY),
        new("z_sex", "sex", Datatype.BINARY),
        new("z_area", "locationContinent", Datatype.BINARY),
        new("z_cntry", "locationCountry", Datatype.BINARY),
        new("z_wide", "locationProvinceState", Datatype.BINARY),
        new("z_local", "locale", Datatype.BINARY),
        new("z_lang0", "language0", Datatype.BINARY),
        new("z_lang1", "language1", Datatype.BINARY),
        new("z_lang2", "language2", Datatype.BINARY),
        new("z_job", "job", Datatype.BINARY),
        new("z_fav0", "interest0", Datatype.USHORT16),
        new("z_fav1", "interest1", Datatype.USHORT16),
        new("z_fav2", "interest2", Datatype.USHORT16),
        new("z_purp", "purpose", Datatype.BINARY),
        new("z_mail", "email", Datatype.STRINGS, 0x140),
        new("z_ficon", "portrait", Datatype.UINT32),
        new("z_rlang", "rlang", Datatype.BINARY),
        new("z_aa_mt", "aa_mt", Datatype.BINARY),
        new("z_aa_dt", "aa_dt", Datatype.BINARY),
        new("z_aa_t", "aa_t", Datatype.BINARY),
        new("z_aa_m", "aa_m", Datatype.BINARY),
        new("z_aa_c", "aa_c", Datatype.BINARY),
        new("z_aa_w", "aa_w", Datatype.BINARY),
        new("z_aa_d", "aa_d", Datatype.BINARY),
        new("z_aa_e", "aa_e", Datatype.BINARY),
        new("z_utime", "updateTime", Datatype.UINT32),
        new("z_pnum", "handleNum", Datatype.BINARY),
        new("z_up_name", "upName", Datatype.STRINGS, 0x0),
        ], 0x1A8)
    {
        protected override string SelectList() => $"visibility, {base.SelectList()}";

        protected override string SetClause(byte visibility, List<PolBesColumnArgument> updateCols) =>
            $"visibility = {visibility}, {base.SetClause(visibility, updateCols)}";
    }
}