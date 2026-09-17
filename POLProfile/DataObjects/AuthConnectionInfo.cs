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

namespace Crystal.POLProfile.DataObjects
{
    class AuthConnectionInfo(string polId, ulong currentHandleId, byte[] blowfishKey, uint rsaKey1, uint rsaKey2, uint loginTime)
    {
        public readonly string PolId = polId;
        public readonly ulong CurrentHandleId = currentHandleId;
        public readonly byte[] BlowfishKey = blowfishKey;
        public readonly uint RsaKey1 = rsaKey1;
        public readonly uint RsaKey2 = rsaKey2;
        public readonly uint LoginTime = loginTime;
    }
}
