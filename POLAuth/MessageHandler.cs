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
using Crystal.POLAuth.DataObjects.Irc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Crystal.POLAuth
{
    class MessageHandler
    {
        private readonly AuthServer Server;
        private readonly Dictionary<string, Func<Client, string, string[], int>> CommandHandlers = [];

        public MessageHandler(AuthServer server)
        {
            Server = server;
            CommandHandlers.Add("PING", ProcessPingCommand);
            CommandHandlers.Add("PONG", ProcessPongCommand);
            CommandHandlers.Add("USER", ProcessUserCommand);
            CommandHandlers.Add("NICK", ProcessNickCommand);
            CommandHandlers.Add("AWAY", ProcessAwayCommand);
            CommandHandlers.Add("JOIN", ProcessJoinCommand);
            CommandHandlers.Add("PART", ProcessPartCommand);
            CommandHandlers.Add("MODE", ProcessModeCommand);
            CommandHandlers.Add("WHO", ProcessWhoCommand);
            CommandHandlers.Add("TOPIC", ProcessTopicCommand);
            CommandHandlers.Add("NOTICE", ProcessNoticeCommand);
            CommandHandlers.Add("PRIVMSG", ProcessPrivMessageCommand);
            CommandHandlers.Add("KICK", ProcessKickCommand);
            CommandHandlers.Add("QUIT", ProcessQuitCommand);
        }
        private int ProcessPingCommand(Client client, string command, string[] args)
        {
            if (args.Length != 1)
                return -1;

            string pingKey = args[0];

            client.SendLine("PING :" + pingKey);

            return 0;
        }
        private int ProcessPongCommand(Client client, string command, string[] args)
        {
            if (args.Length != 1)
                return -1;

            string pongKey = args[0];

            client.SendLine("PONG :" + pongKey);

            return 0;
        }

        public int ParseMessage(Client client, string line)
        {
            if (line == null || line.Length == 0)
                return 0;

            string message = line;

            if (!message.Contains("PING") && !message.Contains("PONG"))
                Program.Log.Debug("Receiving{1}: {0}", message, client.IsEncrypted() ? " (Encrypted)" : "");

            // Split message and handle it
            var spacedMsg = "";
            int escapedPosition = message.Substring(1).IndexOf(" :");
            if (escapedPosition != -1)
            {
                escapedPosition += 2;
                spacedMsg = message.Substring(escapedPosition + 1);
                message = message.Remove(escapedPosition).Trim();
            }

            var splitMsg = message.Split(new[] { ' ' });
            if (splitMsg.Length == 0)
            {
                Program.Log.Info("Received invalid command from {0}.", client);
                return -1;
            }

            string command = splitMsg[0];
            string[] args = splitMsg.Skip(1).ToArray();

            if (spacedMsg != "")
                args = args.Concat(new string[] { spacedMsg }).ToArray();

            if (CommandHandlers.ContainsKey(command))
                return CommandHandlers[command].Invoke(client, command, args);

            Program.Log.Info("Received an unknown or unsupported command from {0}.", client);

            return 0;
        }

        // Client is sending it's RSA Public Key and is asking for a Blowfish Key
        private int ProcessUserCommand(Client client, string command, string[] args)
        {
            if (args.Length != 4)
                return -1;

            string userName = args[0];
            string flags = args[1];
            string rsaKeyInBase64 = args[3];
            byte[] publicRsaKey = SqCrypto.DecodeBase64(rsaKeyInBase64, rsaKeyInBase64.Length);

            Program.Log.Info($"{client} is handshaking crypto... [USER]");

            client.SetUserName(userName);            
            client.DoHandshake(publicRsaKey);

            return 0;
        }

        // Client is logging in
        private int ProcessNickCommand(Client client, string command, string[] args)
        {
            if (args.Length != 1)
                return -1;

            var splitArg = args[0].Split(new char[] { ':' });
            if (splitArg.Length != 3)
                return -1;

            string scrambledPOLId = splitArg[0];
            string passwordMD5 = splitArg[1];
            string clientInfoEncoded = splitArg[2];

            if (!clientInfoEncoded.Equals("BotBotBot"))
                client.DoLogin(scrambledPOLId, passwordMD5, clientInfoEncoded);
            else
                client.DoPolProNotifierLogin(scrambledPOLId, passwordMD5);

            return 0;
        }

        //Client requesting info about users
        private int ProcessWhoCommand(Client client, string command, string[] args)
        {
            if (args.Length != 1 && args.Length != 2)
                return -1;

            string channelName = args[0];

            bool opFlag = args.Length == 2 && args[1].Equals("o");

            if (Server.IrcChannels.ContainsKey(channelName))
            {
                Channel channel = Server.IrcChannels[channelName];
                channel.SendWhoList(client);
                return 0;
            }
            return -1;
        }

        // Client switching modes
        private int ProcessModeCommand(Client client, string command, string[] args)
        {
            if (args.Length == 0)
                return -1;

            string channelOrNick = args[0];

            // Check if this is a channel
            if (channelOrNick.ElementAt(0) == '#' || channelOrNick.ElementAt(0) == '&')                
            {
                if (Server.IrcChannels.ContainsKey(channelOrNick))
                {
                    Channel channel = Server.IrcChannels[channelOrNick];
                    channel.Mode(client, args);
                    return 0;
                }
                else
                {
                    IrcReplies.ErrorNoSuchChannel(client, channelOrNick, Server.ServerName);
                    return 0;
                }
            }
            // Check if we are changing ourselves
            else if (channelOrNick.Equals(client.GetNick()))
            {
                string mode = args[1];

                //Change Mode
                if (args.Length == 2)
                {
                    if (mode.ElementAt(0) == '+')
                    {
                        client.Modes.Add(client, mode.ElementAt(1), args[2]);
                        return 0;
                    }
                    else if (mode.ElementAt(0) == '-')
                    {
                        client.Modes.Remove(client, mode.ElementAt(1));
                        return 0;
                    }
                    return -1;
                }
                //Get Mode
                else if (args.Length == 1)
                {
                    IrcReplies.UserModeIs(client, Server.ServerName);
                    return 0;
                }
            }
            return -1;
        }

        // Client toggling away state
        private int ProcessAwayCommand(Client client, string command, string[] args)
        {
            if (args.Length == 0)
            {
                client.ClearAway();
                return 0;
            }
            if (args.Length == 1)
            {
                client.SetAway(args[0]);
                return 0;
            }
            return -1;
        }

        // Client joining a channel
        private int ProcessJoinCommand(Client client, string command, string[] args)
        {
            if (args.Length != 1 && args.Length != 2)
                return -1;

            string channelArg = args[0];
            string keyArg = args.Length == 2 ? args[1] : "";

            string[] channels = channelArg.Split(',');
            string[] keys = keyArg.Split(',');

            for (int i = 0; i < channels.Length; i++)
            {
                string channelName = channels[0];
                string key = "";
                if (keys.Length > i)
                    key = keys[0];

                if (Server.IrcChannels.ContainsKey(channelName))
                {
                    Channel channel = Server.IrcChannels[channelName];
                    channel.Join(client, key);
                }
                else
                {
                    Channel channel = new(Server, channelName);
                    Server.IrcChannels.Add(channelName, channel);
                    channel.Modes.Add(client, 'o', client.GetNick());
                    channel.Join(client, key);
                    Database.CreateChatroom(channelName);
                }
            }            

            return 0;
        }

        private int ProcessPartCommand(Client client, string command, string[] args)
        {
            if (args.Length != 1)
                return -1;

            string[] channels = args[0].Split(',');

            foreach (string channelName in channels)
            {
                if (Server.IrcChannels.ContainsKey(channelName))
                    Server.IrcChannels[channelName].Part(client);
                else
                    IrcReplies.ErrorNoSuchChannel(client, channelName, Server.ServerName);            
            }

            return 0;
        }

        private int ProcessPrivMessageCommand(Client client, string command, string[] args)
        {
            if (args.Length < 2)
                return -1;

            string[] receivers = args[0].Split(',');
            string message = args[1];

            foreach (string receiver in receivers)
            {
                //Sending to channel?
                if (receiver.ElementAt(0) == '#' || receiver.ElementAt(0) == '&')
                {
                    if (Server.IrcChannels.ContainsKey(receiver))
                        Server.IrcChannels[receiver].SendPrvMsg(client, message);
                    else
                        IrcReplies.ErrorNoSuchChannel(client, receiver, Server.ServerName);
                }
                //Sending to user?
                else
                {
                    Client targetClient = Server.FindClient(receiver);
                    if (targetClient != null)                    
                        targetClient.SendPrvMsg(client, message);                    
                    else
                        IrcReplies.ErrorNoSuchNick(client, receiver, Server.ServerName);
                }
            }            

            return 0;
        }
        private int ProcessNoticeCommand(Client client, string command, string[] args)
        {
            if (args.Length < 2)
                return -1;

            string[] receivers = args[0].Split(',');
            string message = args[1];

            foreach (string receiver in receivers)
            {
                //Sending to channel?
                if (receiver.ElementAt(0) == '#' || receiver.ElementAt(0) == '&')
                {
                    if (Server.IrcChannels.ContainsKey(receiver))
                        Server.IrcChannels[receiver].SendNotice(client, message);
                }
                //Sending to user?
                else
                {
                    Client targetClient = Server.FindClient(receiver);
                    if (targetClient != null)
                        targetClient.SendNotice(client, message);
                }
            }
            return 0;
        }

        private int ProcessTopicCommand(Client client, string command, string[] args)
        {
            if (args.Length != 1 && args.Length != 2)
                return -1;

            string channelName = args[0];
            string topic = args.Length == 2 ? args[1] : "";

            if (Server.IrcChannels.ContainsKey(channelName))
            {
                Channel channel = Server.IrcChannels[channelName];

                if (!channel.Members.Contains(client))
                {
                    IrcReplies.ErrorNotOnChannel(client, channel, Server.ServerName);
                    return 0;
                }

                if (topic.Length == 0)
                {
                    if (channel.GetTopic().Length == 0)
                        IrcReplies.ReplyNoTopic(client, Server.ServerName, client.GetIdentifier(), channelName);
                    else
                        IrcReplies.ReplyTopic(client, Server.ServerName, client.GetIdentifier(), channelName, topic);
                }
                else
                {
                    channel.SetTopic(client, topic);
                }
            }

            return 0;
        }

        private int ProcessKickCommand(Client client, string command, string[] args)
        {
            if (args.Length != 2)
                return -1;

            string channelName = args[0];
            string user = args[1];

            if (Server.IrcChannels.ContainsKey(channelName))
            {
                Channel channel = Server.IrcChannels[channelName];
                channel.Kick(client, user);
            }
            else
                IrcReplies.ErrorNoSuchChannel(client, channelName, Server.ServerName);

            return 0;
        }
        private int ProcessQuitCommand(Client client, string command, string[] args)
        {
            string message = args.Length >= 1 ? args[0] : "Bye!";

            client.QuitChannels(message);
            IrcReplies.Quit(client, Server.ServerName);
            client.Disconnect();

            return 0;
        }
    }
}
