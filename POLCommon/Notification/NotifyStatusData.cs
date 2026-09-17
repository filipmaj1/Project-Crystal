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
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Crystal.Common.Notification
{
    public struct NotifyStatusData
    {
        public byte IsLoginAndReceiveMsg;
        public byte Status;
        public byte ActiveCharacterBits;
        public byte Purpose;
        public uint ContentClass;

        public const int SIZE = 0x8;

        public byte CurrentActiveCharacterIndex
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (byte)((ActiveCharacterBits >> 1) & 0x7);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                ActiveCharacterBits = (byte)(((value & 0x7) << 0x1) | (ActiveCharacterBits & 0xE));
            }
        }

        public bool HasActiveCharacter
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (ActiveCharacterBits & 1) == 1;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => ActiveCharacterBits = value ? (byte)(ActiveCharacterBits | 1) : (byte)(ActiveCharacterBits & 0xFE);
        }

        public NotifyStatusData Copy()
        {
            return (NotifyStatusData)MemberwiseClone();
        }
    }
}
