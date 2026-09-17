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

using Crystal.POLAuth.DataObjects.Irc.Modes.ChannelModes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Crystal.POLAuth.DataObjects.Irc
{
    public class ChannelModeList 
    {
        private AuthServer Server;
        private Channel Channel;
        private SortedList<char, ChannelMode> ModeList = [];

        public ChannelModeList(Channel parentChannel, int limit = 256)
        {
            Channel = parentChannel;
            Server = parentChannel.Server;
            ModeList.Add('l', new LimitChannelMode(limit));
            ModeList.Add('b', new BanChannelMode());
        }

        public void Add(Client client, char modeChar, string args, bool dontSend = true)
        {
            string[] argsSplit = args.Split(' ');

            ChannelMode mode = null;
            switch (modeChar)
            {
                case 'o':
                    if (!ModeList.TryGetValue('o', out ChannelMode value))
                    {
                        mode = new OperatorChannelMode(args);
                        ModeList.Add(modeChar, mode);
                        return;
                    }                        
                    else
                    {
                        (value as OperatorChannelMode).Add(args);
                        client.SendLine(string.Format(":{0} MODE {1} +{2} {3}", client.GetIdentifier(), Channel.Name, modeChar, args).Trim());
                        return;
                    }
                case 'b':
                    if (argsSplit.Length == 1) {
                        (ModeList['b'] as BanChannelMode).Add(args);
                        client.SendLine(string.Format(":{0} MODE {1} +{2} {3}", client.GetIdentifier(), Channel.Name, modeChar, args).Trim());
                        return;
                    }
                    break;
                case 'n':
                    mode = new NoExternalMsgChannelMode();                    
                    break;
                case 'l':
                    if (argsSplit.Length == 1)
                    {
                        try
                        {
                            int limit = int.Parse(argsSplit[0]);
                            if (limit > 0)
                            {
                                mode = new LimitChannelMode(limit);
                                Database.UpdateChatroomMaxPerson(Channel.Name, limit);
                            }
                            throw new FormatException();
                        }
                        catch (FormatException)
                        {
                            break;
                        }
                    }
                    break;
                case 'k':
                    if (argsSplit.Length == 1)
                    {
                        mode = new KeyChannelMode(argsSplit[0]);
                        Database.UpdateChatroomHasPassword(Channel.Name, true);
                        break;
                    }
                    break;
            }

            if (mode == null)
            {
                IrcReplies.ErrorUserModeUnknownFlag(client, Server.ServerName, client.GetNick());
            }
            else
            {
                if (modeChar != 'o') 
                    ModeList.Remove(modeChar);

                ModeList.Add(modeChar, mode);
            }
        }

        public void Remove(Client client, char modeChar, string args = "")
        {
            if (modeChar == 'o')
            {
                if (ModeList.ContainsKey(modeChar)) {
                    (ModeList['o'] as OperatorChannelMode).Remove(args);
                }
            }
            else if (modeChar == 'b')
            {
                (ModeList['b'] as BanChannelMode).Remove(args);                
            }
            else if (ModeList.ContainsKey(modeChar))
                ModeList.Remove(modeChar);

            if (modeChar == 'k')
                Database.UpdateChatroomHasPassword(Channel.Name, false);
        }

        public string ToModeString()
        {
            StringBuilder builderModes = new();
            StringBuilder builderParams = new();

            foreach (var mode in ModeList.Values)
            {
                if (mode.Character == 'o' || mode.Character == 'b')
                    continue;
                builderModes.Append(mode.Character);
                builderParams.Append(mode.GetParamString());
            }

            return "+" + builderModes.ToString() + " " + builderParams.ToString().Trim();
        }

        public bool CheckOperator(string nick)
        {
            if (ModeList.TryGetValue('o', out ChannelMode value))
            {
                return (value as OperatorChannelMode).IsOp(nick);
            }
            return false;
        }

        public bool CheckLimit(int currentCount)
        {
            if (ModeList.TryGetValue('l', out ChannelMode value))
            {
                return (value as LimitChannelMode).CurrentLimit >= currentCount + 1;                    
            }
            return true;
        }

        public bool CheckKey(string channelKey)
        {
            if (ModeList.TryGetValue('k', out ChannelMode value))
            {
                return (value as KeyChannelMode).KeyValue.Equals(channelKey);
            }
            return true;
        }
        public bool CheckNoExternal()
        {
            return ModeList.ContainsKey('n');
        }

        public bool CheckBanned(Client client)
        {
            if (ModeList.TryGetValue('b', out ChannelMode value))
            {
                return (value as BanChannelMode).IsBanned(client.GetNick());
            }
            return false;
        }
    }
}
