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
    public class TableRules
    {
        public readonly uint Wager;
        public readonly bool DoubleUp;
        public readonly bool SpecialTile;
        public readonly bool ChanceBlock;
        public readonly bool RotatingBlock;
        public readonly byte QuitMode;
        public readonly byte TimerId;

        public TableRules(uint wager, bool doubleUp, bool specialTile, bool chanceBlock, bool rotatingBlock, byte quitMode, byte timerId)
        {
            Wager = wager;
            DoubleUp = doubleUp;
            SpecialTile = specialTile;
            ChanceBlock = chanceBlock;
            RotatingBlock = rotatingBlock;
            QuitMode = quitMode;
            TimerId = timerId;
        }

        public byte GetBitfield()
        {
#pragma warning disable CS0675 // Bitwise-or operator used on a sign-extended operand
            byte bitfield = (byte)((DoubleUp ? 0x01U : 0x00) |
                                   (SpecialTile ? 0x02U : 0x00) |
                                   (ChanceBlock ? 0x04U : 0x00) |
                                   (RotatingBlock ? 0x08U : 0x00));
            bitfield <<= 4;
            bitfield |= TimerId;

#pragma warning restore CS0675 // Bitwise-or operator used on a sign-extended operand
            return bitfield;
        }
    }
}
