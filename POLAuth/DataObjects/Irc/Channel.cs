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
using System.Text;

namespace Crystal.POLAuth.DataObjects.Irc
{
    public class Channel
    {
        public AuthServer Server;

        public string Name;
        public string Topic = "";

        public readonly List<Client> Members = [];

        public readonly ChannelModeList Modes;

        public readonly bool IsPermanent;

        public Channel(AuthServer server, string name, bool isPermanent = false, int limit = 256)
        {
            Server = server;
            Name = name;
            Modes = new ChannelModeList(this, limit);
            IsPermanent = isPermanent;
            if (isPermanent)
                Modes.Add(null, 'n', "", true);
        }

        public void Join(Client client, string channelKey = "")
        {            
            if (Members.Contains(client))
            {                
                return;
            }

            if (!Modes.CheckKey(channelKey))
            {
                IrcReplies.ErrorBadChannelKey(client, this, Server.ServerName);
                return;
            }

            if (!Modes.CheckLimit(Members.Count))
            {
                IrcReplies.ErrorChannelIsFull(client, this, Server.ServerName);
                return;
            }

            if (Modes.CheckBanned(client))
            {
                IrcReplies.ErrorBannedFromChannel(client, this, Server.ServerName);
                return;
            }

            Members.Add(client);

            foreach (Client c in Members)
                IrcReplies.Join(c, client.GetIdentifier(), Name);

            IrcReplies.TopicReply(client, Server.ServerName, client.GetNick(), Name, Topic);
            IrcReplies.NamesReply(client, this, Server.ServerName, client.GetNick());
            IrcReplies.EndOfNameList(client, this, Server.ServerName, client.GetNick());
            // Unsure if this is supposed to be here but it fixes the title not appearing.
            IrcReplies.Topic(client, client.GetIdentifier(), Name, Topic);

            Database.UpdateChatroomPersonCount(Name, true);
        }

        public void Part(Client client)
        {
            if (!Members.Contains(client))
            {
                IrcReplies.ErrorNotOnChannel(client, this, Server.ServerName);
                return;
            }

            Members.Remove(client);

            foreach (Client c in Members)
                IrcReplies.Part(c, client.GetIdentifier(), Name, client.GetNick());

            IrcReplies.Part(client, client.GetIdentifier(), Name, client.GetNick());

            if (Members.Count == 0 && !IsPermanent)
            {
                Server.IrcChannels.Remove(Name);
                Database.DeleteChatroom(Name);
            }
            else
                Database.UpdateChatroomPersonCount(Name, false);
        }

        // Like Part, but silent to the leaver: relays QUIT to the remaining
        // members so their clients drop this user from their member lists.
        public void Quit(Client client, string message)
        {
            if (!Members.Remove(client))
                return;

            foreach (Client c in Members)
                IrcReplies.Quit(c, client.GetIdentifier(), message);

            if (Members.Count == 0 && !IsPermanent)
            {
                Server.IrcChannels.Remove(Name);
                Database.DeleteChatroom(Name);
            }
            else
                Database.UpdateChatroomPersonCount(Name, false);
        }

        public string GetNameList()
        {
            StringBuilder builder = new();
            foreach (Client c in Members)
                builder.Append($"{(Modes.CheckOperator(c.GetNick()) ? "@" : "")}{c.GetNick()} ");

            return builder.ToString();
        }

        public void SendPrvMsg(Client from, string message)
        {
            if (Modes.CheckNoExternal())
            {
                foreach (Client c in Members)
                {
                    if (c.Equals(from))
                        continue;
                    IrcReplies.PrivMsg(c, from.GetIdentifier(), Name, message);
                }

                from.SendLine($":{Server.ServerName} 300 {from.GetNick()} {Name}");
            }
            else
                IrcReplies.ErrorCannotSendToChannel(from, this, Server.ServerName);
        }

        public void SendNotice(Client from, string message)
        {
            if (!Modes.CheckNoExternal() && Members.Contains(from))
            {
                foreach (Client c in Members)
                {
                    if (c.Equals(from))
                        continue;
                    IrcReplies.Notice(c, from.GetIdentifier(), Name, message);
                }
            }
        }

        public void SendWhoList(Client target)
        {
            if (IsPermanent)
                IrcReplies.WhoReplyPermaOperator(target, this, Server.ServerName, target.GetNick());
            foreach (Client c in Members)
                IrcReplies.WhoReply(target, c, this, Server.ServerName, target.GetNick());
            IrcReplies.EndOfWhoList(target, this, Server.ServerName, target.GetNick());
        }

        public void SetTopic(Client client, string topic)
        {
            Topic = topic;

            if (client != null)
            {
                foreach (Client c in Members)
                    IrcReplies.Topic(c, client.GetIdentifier(), Name, topic);
                Database.UpdateChatroomTopicData(Name, topic);
            }
        }

        public string GetTopic()
        {
            return Topic;
        }

        public void Kick(Client from, string kickedUserNick)
        {
            if (!Members.Contains(from))            
                IrcReplies.ErrorNotOnChannel(from, this, Server.ServerName);
            if (!Modes.CheckOperator(from.GetNick()))
                IrcReplies.ErrorChanPrivsNeeded(from, this, Server.ServerName);
            
            Client target = Members.Find(c => c.GetNick().Equals(kickedUserNick));
            if (target != null)
            {                
                foreach (Client c in Members)
                    IrcReplies.Kick(c, from.GetIdentifier(), Name, kickedUserNick, from.GetNick());
            }
            else
                IrcReplies.ErrorNoSuchNick(from, kickedUserNick, Server.ServerName);
        }

        public void Mode(Client from, string[] args)
        {
            if (!Members.Contains(from))
            {
                IrcReplies.ErrorNotOnChannel(from, this, Server.ServerName);
                return;
            }

            //Set Mode
            if (args.Length >= 2)
            {
                StringBuilder builderModes = new();
                StringBuilder builderParams = new();

                for (int i = 1; i < args.Length; i++)
                {
                    char op = args[i].ElementAt(0);
                    char mode = args[i].ElementAt(1);
                    string arg = "";

                    if (i + 1 < args.Length && !(args[i + 1].StartsWith('+') || args[i + 1].StartsWith('-')))
                        arg = args[i++ + 1];

                    if (op == '+')
                        Modes.Add(from, mode, arg);
                    else if (op == '-')
                        Modes.Remove(from, mode, arg);

                    builderModes.Append(op);
                    builderModes.Append(mode);

                    if (arg.Length != 0)
                        builderParams.Append(arg + " ");
                }

                foreach (Client c in Members)
                    c.SendLine($":{from.GetIdentifier()} MODE {Name} {builderModes} {builderParams}");
            }
            //Get Mode
            else if (args.Length == 1)
            {
                IrcReplies.ChannelModeIs(from, this, from.GetIdentifier(), Server.ServerName);
            }
        }
    }
}
