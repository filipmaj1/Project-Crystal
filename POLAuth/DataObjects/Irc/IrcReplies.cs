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

using System.Text;

namespace Crystal.POLAuth.DataObjects.Irc
{
    class IrcReplies
    {
        #region Relays
        public static void Ping(Client client, string word)
        {
            client.SendLine($"PING :{word}");
        }

        public static void Join(Client client, string source, string channelName)
        {
            client.SendLine($":{source} JOIN {channelName}");
        }

        public static void Part(Client client, string source, string channelName, string message)
        {
            client.SendLine($":{source} PART {channelName} :{message}");
        }

        public static void PrivMsg(Client client, string source, string target, string message)
        {
            client.SendLine($":{source} PRIVMSG {target} :{message}");
        }

        public static void Notice(Client client, string source, string target, string message)
        {
            client.SendLine($":{source} NOTICE {target} :{message}");
        }

        public static void Topic(Client client, string source, string channelName, string topic)
        {
            client.SendLine($":{source} TOPIC {channelName} :{topic}");
        }

        public static void Kick(Client client, string source, string channelName, string kickedUserNick, string sourceNick)
        {
            client.SendLine($":{source} KICK {channelName} {kickedUserNick} :{sourceNick}");
        }

        public static void Quit(Client client, string source)
        {
            client.SendLine($":{source} QUIT :Bye! AAA");
        }

        public static void Quit(Client client, string source, string message)
        {
            client.SendLine($":{source} QUIT :{message}");
        }

        #endregion
        #region Replies
        public static void ReplyWelcome(Client client, string source, string message)
        {
            client.SendLine($":{source} 001 {message}");
        }

        public static void UserModeIs(Client client, string source)
        {
            client.SendLine($":{source} 221 {client.GetNick()} :{client.Modes.ToModeString()}");
        }
        public static void ChannelModeIs(Client client, Channel channel, string source, string target)
        {
            client.SendLine($":{source} 324 {target} {channel.Name} {channel.Modes.ToModeString()} ");
        }

        public static void ServerCommand(Client client, string source, string message)
        {
            client.SendLine($":{source} 300 * {message}");
        }

        public static void TopicReply(Client client, string source, string target, string channelName, string topicMsg)
        {
            client.SendLine($":{source} 332 {target} {channelName} :{topicMsg}");
        }

        public static void NamesReply(Client client, Channel channel, string source, string target)
        {
            string namesList = channel.GetNameList();
            client.SendLine($":{source} 353 {target} = {channel.Name} :{namesList}");
        }

        public static void EndOfNameList(Client client, Channel channel, string source, string target)
        {
            client.SendLine($":{source} 366 {target} {channel.Name} :End of NAMES list.");
        }

        public static void WhoReplyPermaOperator(Client targetClient, Channel channel, string source, string target)
        {
            targetClient.SendLine($":{source} 352 {target} {channel.Name} ~x {channel.Server.ServerIp} {channel.Server.ServerIp} PXANNNNXK H@ :2 *Not On This Net*");
        }

        public static void WhoReply(Client targetClient, Client infoClient, Channel channel, string source, string target)
        {
            targetClient.SendLine($":{source} 352 {target} {channel.Name} {infoClient.GetUserName()} {infoClient.Ip} {source} {infoClient.GetNick()} {(infoClient.GetIsAway() ? "G" : "H")}{(channel.Modes.CheckOperator(infoClient.GetNick()) ? "@" : "")} :0 {infoClient.GetRealName()}");
        }

        public static void EndOfWhoList(Client client, Channel channel, string source, string target)
        {
            client.SendLine($":{source} 315 {target} {channel.Name} :End of WHO list.");
        }
        public static void ReplyAway(Client client, string source, string target, string awayMessage)
        {
            client.SendLine($":{source} 301 {target} :{awayMessage}");
        }

        public static void UnAway(Client client, string source, string target)
        {
            client.SendLine($":{source} 305 {target} :You are no longer marked as being away");
        }

        public static void NowAway(Client client, string source, string target)
        {
            client.SendLine($":{source} 306 {target} :You have been marked as being away");
        }

        public static void ReplyNoTopic(Client client, string source, string target, string channel)
        {
            client.SendLine($":{source} 331 {target} :No topic is set");
        }

        public static void ReplyTopic(Client client, string source, string target, string channel, string topic)
        {
            client.SendLine($":{source} 332 {target} {channel} :{topic}");
        }
        #endregion
        #region ERROR_REPLIES
        public static void ErrorNoSuchNick(Client client, string nickName, string source)
        {
            client.SendLine($":{source} 401 {nickName} :No such nick/channel");
        }

        public static void ErrorNoSuchChannel(Client client, string channelName, string source)
        {
            client.SendLine($":{source} 403 {channelName} :No such channel");
        }

        public static void ErrorCannotSendToChannel(Client client, Channel channel, string source)
        {
            client.SendLine($":{source} 404 {channel.Name} :Cannot send to channel");
        }

        public static void ErrorNoMOTD(Client client, string source, string target)
        {
            client.SendLine($":{source} 422 {target} :m");
        }

        public static void ErrorNotOnChannel(Client client, Channel channel, string source)
        {
            client.SendLine($":{source} 442 {channel.Name} :You have been marked as being away");
        }

        public static void ErrorKeySet(Client client, Channel channel, string source)
        {
            client.SendLine($":{source} 467 {channel.Name} :Channel key already set");
        }

        public static void ErrorChannelIsFull(Client client, Channel channel, string source)
        {
            client.SendLine($":{source} 471 {channel.Name} :Cannot join channel (+l)");
        }

        public static void ErrorUnknownMode(Client client, string mode, string source)
        {
            client.SendLine($":{source} 472 {mode} :is unknown mode char to me");
        }

        public static void ErrorInviteOnlyChannel(Client client, Channel channel, string source)
        {
            client.SendLine($":{source} 473 {channel.Name} :Cannot join channel (+i)");
        }

        public static void ErrorBannedFromChannel(Client client, Channel channel, string source)
        {
            client.SendLine($":{source} 474 {channel.Name} :Cannot join channel (+B)");
        }

        public static void ErrorBadChannelKey(Client client, Channel channel, string source)
        {
            client.SendLine($":{source} 475 {channel.Name} :Cannot join channel (+k)");
        }


        public static void ErrorChanPrivsNeeded(Client client, Channel channel, string source)
        {
            client.SendLine($":{source} 482 {channel.Name} :You're not channel operator");
        }

        public static void ErrorUserModeUnknownFlag(Client client, string source, string target)
        {
            client.SendLine($":{source} 501 {target} :Unknown MODE flag");
        }

        public static void ErrorUserDontMatch(Client client, string source, string target)
        {
            client.SendLine($":{source} 502 {target} :Cant change mode for other users");
        }
        #endregion

        public static void AdminError(Client client, byte errorCode)
        {
            client.SendLine($"ERROR :Closing Link: [unknown@{client.Ip}] (POL {AdminData.Error(errorCode).ToBase32()})");
        }

        public static void Kick(Client client, string source)
        {
            client.SendLine($":{source} 433");
        }

        //:pol-1044-51245.pol.com 352 UO7MG2TXN #XXL000000000002BDB1 ~x 75.119.239.206 pol-1044-51245.pol.com UO7MG2TXN H@ :0 POL-INFOZnvY
        //:pol-1044-51245.pol.com 315 UO7MG2TXN #XXL000000000002BDB1 :End of WHO list.
    }
}
