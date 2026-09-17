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

namespace Crystal.POLProfile.DataObjects.Pol
{
    internal class Chatroom
    {
        public readonly byte language;
        public readonly char use = 'C';
        public readonly bool permanent;
        public readonly string channel;

        public readonly string chatroomName;
        public readonly uint usersCurrent;
        public readonly uint usersMax;
        public readonly bool hasPassword;
        public readonly ushort memberCategory;
        public readonly ushort purposeCategory;
        public readonly ushort langaugeCategory;
        public readonly ushort zoneCode;

        public readonly uint creationTime;

        public Chatroom(string channel, byte language, string chatroomName, uint usersCurrent, uint usersMax, bool permanent, bool hasPassword, ushort memberCategory, ushort purposeCategory, ushort langaugeCategory, ushort zoneCode, uint creationTime)
        {
            this.channel = channel;
            this.language = language;
            this.chatroomName = chatroomName;
            this.usersCurrent = usersCurrent;
            this.usersMax = usersMax;
            this.permanent = permanent;
            this.hasPassword = hasPassword;
            this.memberCategory = memberCategory;
            this.purposeCategory = purposeCategory;
            this.langaugeCategory = langaugeCategory;
            this.zoneCode = zoneCode;
            this.creationTime = creationTime;
        }
    }
}