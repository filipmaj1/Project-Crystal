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
    /* Packet sent when joining a table.
     * ResultCode: The result given back to the client for the action they preformed.
     * If one of the errors; a generic error box appears.
     *      1 - Successfully executed the command.
     *      2 - General cmd failure.
     *      3 - Cmd failed because you didn't have authority (not master).
     *      4 - Cmd failed because of "POLproDataFlush".
     */
    [StructLayout(LayoutKind.Sequential, Size = SIZE)]
    public unsafe struct MasterCmdAck
    {
        public int Result;

        public const int SIZE = 0x4;
    }
}
