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

using Crystal.POLProfile.DataObjects.Pol.Status;

namespace Crystal.POLProfile.DataObjects
{
    public class DbGroupMemberStatusResult
    {
        public readonly string PolIdData;
        public readonly ulong HandleId;
        public readonly byte HandlePosition;
        public readonly string HandleName;
        public readonly byte Rank;
        public readonly string Comment;
        public readonly uint Portrait;
        public readonly StatusData GroupStatus;

        public DbGroupMemberStatusResult(string polIdData, ulong handleId, byte handlePosition, string handleName, byte rank, string comment, uint portrait, StatusData status)
        {
            PolIdData = polIdData;
            HandleId = handleId;
            HandlePosition = handlePosition;
            HandleName = handleName;
            Rank = rank;
            Comment = comment;
            Portrait = portrait;
            GroupStatus = status;
        }
    }
}