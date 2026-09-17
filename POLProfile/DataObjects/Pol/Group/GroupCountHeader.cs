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

namespace Crystal.POLProfile.DataObjects.Pol.Group
{
    struct GroupCountHeader
    {
        public byte NumGroups;
        public byte NumMembers1;
        public byte NumMembers2;
        public byte NumMembers3;
        public byte NumMembers4;

        public const int SIZE = 0x8;

        public GroupCountHeader(byte numGroups, byte[] memberCounts)
        {
            this.NumGroups = numGroups;
            this.NumMembers1 = memberCounts[0];
            this.NumMembers2 = memberCounts[1];
            this.NumMembers3 = memberCounts[2];
            this.NumMembers4 = memberCounts[3];
        }
    }
}
