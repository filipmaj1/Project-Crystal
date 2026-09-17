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

using Crystal.Common;
using System.IO;

namespace Crystal.POLProfile.Notifications
{
    class NewMessageNotification : Notification
    {
        readonly byte[] msgHeader;

        public NewMessageNotification(byte[] msgHeader)
        {
            this.msgHeader = msgHeader;
        }

        public override string GetNoticeMsg()
        {
            byte[] friendNotify = new byte[0x48];
            using (MemoryStream memStream = new(friendNotify))
            using (BinaryWriter binWriter = new(memStream))
            {
            }

            string header = SqCrypto.EncodeBase64(friendNotify, friendNotify.Length, false);
            return "";
        }
    }
}
