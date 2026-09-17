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

namespace Crystal.TetraMaster
{
    internal class TmConstants
    {
        // Deals with zone and room lists as well as the ID that TM initially
        // connects to. ID is hardcoded.
        public const ulong  BALANCER_POLID  = 0x000000384ea5822c; // pp0001
        public const byte   BALANCER_DOMAIN = 2;
        public const byte   BALANCER_VOLUME = 0;

        // Profile retrieval, ID is hardcoded.
        public const ulong  PROFILE_POLID   = 0x0000011341108228; // pp0002

        // Auction server, ID is hardcoded.
        public const ulong  AUCTION_POLID   = 0x0000015c3c898227; // pp0003
        public const byte   AUCTION_DOMAIN = 2;
        public const ushort AUCTION_VOLUME = 0;

        // Rank server, ID is hardcoded.
        public const ulong  RANK_POLID      = 0x000000dc8475c22a; // pp0004
        public const byte   RANK_DOMAIN = 2;
        public const ushort RANK_VOLUME = 0;

        // Shop Menu server that deals with both shop function and the options menu.
        public const ulong  SHOPMENU_POLID  = 0x000000cbef2bfd2c;
        public const byte   SHOPMENU_DOMAIN = 2;
        public const ushort SHOPMENU_VOLUME = 0;
    }
}
