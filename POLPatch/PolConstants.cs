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

namespace Crystal.POLPatch
{
    class PolConstants
    {
        public static class HardwareID
        {
            public const string WindowsJP = "W20\0";
            public const string WindowsUS = "W2U\0";

            public const string PS2JP = "PS2\0";
            public const string PS2US = "P2U\0";

            public const string XbxJP = "XB2\0";
            public const string XbxUS = "X2U\0";
        }

        public static class ApplicationID
        {
            public const string PlayOnline = "1000";
            public const string FinalFantasyXI = "0001";
            public const string TetraMaster = "0002";
            public const string Janhourou = "0003";
            public const string FrontMissionOnline = "0004";
            public const string DirgeOfCerberus = "0010";
            public const string POLFriendList = "0014";
        }
    }
}
