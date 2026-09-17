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

using Crystal.Common.Notification;

namespace Crystal.POLProfile.DataObjects.Pol.Status
{
    public struct StatusData
    {
        public byte ActiveHandleNumber;
        public byte HasActiveCharacter;
        public byte ActiveCharacterIndex;
        public byte CanReceiveOnlineMessage;
        public ushort CurrentContentsClass;
        public byte OpenStat;
        public byte FriendAuthMode;
        public uint LastLoginTime;
        public uint CurrentLoginTime;

        public const int SIZE = 0x10;

        public NotifyStatusData ToNotifyStatus()
        {
            return new NotifyStatusData {
                IsLoginAndReceiveMsg = CanReceiveOnlineMessage,
                Status = OpenStat,
                HasActiveCharacter = HasActiveCharacter == 1,
                CurrentActiveCharacterIndex = ActiveCharacterIndex,
                Purpose = 0,
                ContentClass = CurrentContentsClass,
            };
        }
    }
}
