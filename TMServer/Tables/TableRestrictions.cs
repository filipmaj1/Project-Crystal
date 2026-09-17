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

namespace Crystal.TetraMaster.Tables
{
    public class TableRestrictions
    {
        public readonly byte ObserveMode;
        public readonly uint CardPowerMin;
        public readonly uint CardPowerMax;
        public readonly uint UnkMin;
        public readonly uint UnkMax;
        public readonly byte CommentId;
        public readonly byte HasPassword;
        public readonly string Password;

        public TableRestrictions(byte observeMode, uint cardPowerMin, uint cardPowerMax, uint unkMin, uint unkMax, byte commentId, byte hasPassword, string password)
        {
            ObserveMode = observeMode;
            CardPowerMin = cardPowerMin;
            CardPowerMax = cardPowerMax;
            UnkMin = unkMin;
            UnkMax = unkMax;
            CommentId = commentId;
            HasPassword = hasPassword;
            Password = password;
        }

        public uint GetBitfield()
        {
            uint bitfield = 0;

            return bitfield;
        }
    }
}
