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

namespace Crystal.Common.PolClient.PolProfile
{
    internal struct CharacterUpdate
    {
        public const int SIZE = 0x8;

#pragma warning disable CS0649
        public byte Mode;
        public byte HandleNumber;
        public byte CharacterIndexOfHandleNameList;
#pragma warning disable CS0169
        private byte Padding1;
        private uint Padding2;
#pragma warning restore CS0169
#pragma warning restore CS0649

        public override string ToString()
        {
            if (Mode == 0)
                return "SKIP";
            return $"[{(Mode == 1 ? "Link" : "Unlink")}], Pos: {HandleNumber}, Indx: {CharacterIndexOfHandleNameList}";
        }
    }

}
