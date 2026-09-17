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

using System.Runtime.InteropServices;

namespace Crystal.Mahjong.Packets.GamePackets
{
    /* Packet to acknowledge reservation requests. 
     * ResultCode: The result given back to the client for the action they preformed.
     *      1 - Succesfully reserved a table (Member Version)
     *      2 - Succesfully reserved a table (Master Version)
     *      3 - You are already in a table, would you like to cancel and join this one?
     *      4 - Duplicate of code 3.
     *      5 - Successfully cancelled your reservation.
     *      6 - Error while trying to cancel your reservation.
     *      7 - Your reservation was rejected.
     *      8 - Your reservation was timed out.
     *      9 - The password you entered was wrong.
     *     10 - You do not meet certain conditions.
     *     11 - POL Data Flush
     * ResultData: Extra data for the 1, 2, and 10 results.
     *    1,2 - 
     *     10 - Bitfield for the conditions that you have not met: (Money, Level, Title)
     */
    [StructLayout(LayoutKind.Sequential, Size = SIZE)]
    public struct ReserveAck
    {
        public byte resultCode;
        public byte resultData;

        public const int SIZE = 0x8;
    }
}
