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

using Crystal.POLAuth.DataObjects.Irc.Modes;
using Crystal.POLAuth.DataObjects.Irc.Modes.UserModes;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Crystal.POLAuth.DataObjects.Irc
{
    public class UserModeList
    {
        private AuthServer Server;
        private SortedList<char, UserMode> ModeList = [];

        public UserModeList(AuthServer server)
        {
            Server = server;
        }

        public void Add(Client client, char modeChar, string args)
        {
            UserMode mode;
            switch (modeChar)
            {
                case 'i':
                    mode = new InvisibleUserMode(); 
                    break;
                default:
                    IrcReplies.ErrorUserModeUnknownFlag(client, Server.ServerName, client.GetNick());
                    return;
            }

            ModeList.Add(modeChar, mode);
            
        }

        public void Remove(Client client, char mode)
        {
            if (ModeList.ContainsKey(mode))
                ModeList.Remove(mode);
        }

        public bool ProcessCommand(string command, Client client)
        {
            return ModeList.Values.All(mode => mode.ProcessCommand(command, client));
        }

        public string ToModeString()
        {
            StringBuilder builder = new();

            foreach (var mode in ModeList.Values)
                builder.Append(mode.Character);

            return $"+{builder}";
        }
    }
}
