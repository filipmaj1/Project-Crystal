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

using Crystal.POLProfile.DataObjects.Pol.Status;

namespace Crystal.POLProfile.DataObjects
{
    public class DbFriendStatusResult
    {
        public readonly StatusData FriendStatus;
        public readonly string FriendPolIdData;
        public readonly byte MyFriendListPosition;
        public readonly byte OtherFriendListPosition;
        public readonly byte[] PortraitAndCommentPayload;

        public DbFriendStatusResult(StatusData friendStatus, string polIdData, byte myFriendListPosition, byte otherFriendListPosition, byte[] portraitAndCommentPayload)
        {
            FriendStatus = friendStatus;
            FriendPolIdData = polIdData;
            MyFriendListPosition = myFriendListPosition;
            OtherFriendListPosition = otherFriendListPosition;
            PortraitAndCommentPayload = portraitAndCommentPayload;
        }
    }
}
