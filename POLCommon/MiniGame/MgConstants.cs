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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crystal.Common.MiniGame
{
    public class MgConstants
    {
        // MiniGame
        public const int MG_IRC_PORT = 51241;

        // File Info
        public const int MAX_ZONES = 0x20;
        public const int ZONE_SIZE = 0x40;
        public const int ZONE_COUNT_OFFSET = 0x40;
        public const int ZONE_DATA_OFFSET = 0x48;

        public const int MAX_ROOMS = 0x100;
        public const int ROOM_SIZE = 0xC8;
        public const int ROOM_COUNT_OFFSET = 0x40;
        public const int ROOM_DATA_OFFSET = 0x48;

        public const int MAX_PLAYERS = 0x100;
        public const int PLAYER_SIZE = 0x58;
        public const int PLAYER_COUNT_OFFSET = 0x44;
        public const int PLAYER_DATA_OFFSET = 0x50;

        public const int MAX_TABLES = 0x80;
        public const int TABLE_SIZE = 0x68;
        public const int TABLE_COUNT_OFFSET = 0x48;
        public const int TABLE_DATA_OFFSET = 0x5850;
    }
}
