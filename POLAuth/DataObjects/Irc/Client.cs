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
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Crystal.POLAuth.DataObjects.Irc
{
    public class Client
    {
        //Connection
        private const int SEND_TIMEOUT_MS = 10000;
        private readonly Socket ClientSocket;
        public readonly byte[] RecvBuffer = new byte[2048];
        public int LastPartialSize = 0;
        private readonly IPAddress ClientIp;
        private readonly int ClientPort;
        private bool Disconnected = false;
        private uint LoginTime;

        //Crypto
        private byte[] CurrentPublicRsaKey;
        private SqCrypto CurrentCrypto; // Initialized with a "USER" command.
        private string InitInfoBase32; // Used to generate password MD5

        //Irc Related
        private AuthServer Server;
        public readonly UserModeList Modes;
        private string UserName; // Always "x"
        private string NickName; // Scrambled POLID
        private bool IsAway = false;
        private string AwayMessage = "";
        private bool IsBot;

        public Client(AuthServer server, Socket socket)
        {
            Server = server;
            Modes = new UserModeList(server);

            ClientSocket = socket;
            ClientSocket.SendTimeout = SEND_TIMEOUT_MS;
            IPEndPoint endpoint = socket.RemoteEndPoint as IPEndPoint;
            ClientIp = endpoint.Address;
            ClientPort = endpoint.Port;
        }

        public string Ip { get => ClientIp.ToString(); }

        public int Port { get => ClientPort; }

        public string GetAddress() => $"{ClientIp}:{ClientPort}";

        public bool IsEncrypted() => CurrentCrypto != null;

        public SqCrypto GetCrypto() => CurrentCrypto;

        public void Disconnect(bool replacingSession = false)
        {
            if (Disconnected)
                return;

            Disconnected = true;

            // Drop out of any channels and notify the remaining members
            QuitChannels();

            // If POLPRO Notifier, clear it from server
            if (GetPolID() != null && GetPolID().Equals(Server.PolProId))
            {
                Server.SetNotifyClient(null);
                Program.Log.Info($"PolPro Notifier {GetPolID()} has disconnected.");
                Database.DeleteAccountSession(this);
                ShutdownSocket();
                return;
            }

            //Delete session
            Database.DeleteAccountSession(this);

            //Notify friends they are going offline
            if (!replacingSession)
                NotifyOffline();

            ShutdownSocket();
        }

        // Deliberately does not Close() the socket: MainLoop still polls it until
        // its cleanup pass removes it from IrcClients and closes it there.
        private void ShutdownSocket()
        {
            try
            {
                ClientSocket.Shutdown(SocketShutdown.Both);
                ClientSocket.Disconnect(false);
            }
            catch (SocketException) { }        // Already reset by the peer
            catch (ObjectDisposedException) { }
        }

        public void QuitChannels(string message = "Bye!")
        {
            foreach (Channel channel in Server.IrcChannels.Values.Where(c => c.Members.Contains(this)).ToList())
                channel.Quit(this, message);
        }

        public void KickSameSession()
        {
            SendLine($":{Server.ServerName} KILL {NickName}: {Server.ServerIp}!{Server.ServerName}[unknown@{Server.ServerIp}]!Kicked by same NICK");
            SendLine($"ERROR :Closing Link: {NickName}[~{UserName}@{ClientIp}] {Server.ServerIp}(Killed(Kicked by same NICK))");
            Disconnect(true);
        }

        public void DoInit()
        {
            Program.Log.Info($"{this} is initializing...");

            LoginTime = Utils.UnixTimeStampUTC();
            InitData initInfo = new(LoginTime, Server.ServerIp, AuthServer.AUTHENTICATION_PORT, ClientIp.ToString(), ClientPort);
            InitInfoBase32 = initInfo.GetBase32();
            SendLine($":{Server.ServerName} 300 * {InitInfoBase32}", false);
        }

        public void DoHandshake(byte[] publicRsaKey)
        {
            byte[] blowfishKey = new byte[8];
            Random random = new();
            random.NextBytes(blowfishKey);

            CurrentPublicRsaKey = publicRsaKey;
            CurrentCrypto = new SqCrypto(blowfishKey, publicRsaKey);

            Array.Reverse(blowfishKey);

            byte[] encrypted = SqCrypto.RSAEncrypt(publicRsaKey, blowfishKey);
            string result = SqCrypto.EncodeBase64(encrypted, encrypted.Length, false);

            SendLine($":{Server.ServerName} 300 * {result}", false);
        }

        public void DoPolProNotifierLogin(string scrambledPOLID, string passwordMd5)
        {
            // Check IP if we are only accepting a single one
            //if (!ClientIp.ToString().Equals(Server.PolProIpAddress))
            //{
            //    SendLine($"ERROR :Closing Link: [unknown@{ClientIp}] (Shit)");
            //    Disconnect();
            //    return;
            //}

            SetNick(scrambledPOLID);

            byte[] notifierPwd = Encoding.ASCII.GetBytes(Server.PolProPassword);
            string polProMd5 = SqCrypto.GeneratePOLPasswordMD5(InitInfoBase32, notifierPwd);

            // If gucci, create session and respond            
            //if (!GetPolID().Equals(Server.PolProId) && !passwordMd5.Equals(polProMd5))
            //{
                // Clear out an active sessions and add this one!
                Client sameSessionClient = Server.CheckForSameSession(this);
                if (sameSessionClient != null)
                    sameSessionClient.KickSameSession();
                Database.CreateAccountSession(this);

                IrcReplies.ReplyWelcome(this, Server.ServerName, "Gucci");
                IsBot = true;
                Server.SetNotifyClient(this);
                Program.Log.Info($"PolPro Notifier {GetPolID()} has connected.");
            //}
            //else
            //{
                //SendLine($"ERROR :Closing Link: [unknown@{ClientIp}] (Shit)");
               // Disconnect();
           // }
        }

        public void DoLogin(string scrambledPOLID, string passwordMd5, string clientInfoEncoded)
        {         
            SetNick(scrambledPOLID);

            // Grab pw and generate our check md5
            byte[] passwordBytes = Database.GetDecryptedPasswordForPolId(GetPolID());
            
            // Was there an account?
            if (passwordBytes != null)
            {
                string checkMd5 = SqCrypto.GeneratePOLPasswordMD5(InitInfoBase32, passwordBytes);
                
                // If gucci, create session and respond            
                if (passwordMd5.Equals(checkMd5))
                {
                    Array.Clear(passwordBytes);

                    Program.Log.Info($"{this} has logged on! [NICK]");

                    // Clear out an active sessions and add this one!
                    Client sameSessionClient = Server.CheckForSameSession(this);
                    if (sameSessionClient != null)
                        sameSessionClient.KickSameSession();
                    Database.CreateAccountSession(this);

                    AdminData adminData = new()
                    {
                        Volume = 0x0,
                        Domain = 0x0,
                        Status = 0x10,
                        MasterPolId = SqCrypto.PolProDataToPolId(GetPolID(), 0x0, 0)
                    };

                    LoginNotification loginNotify = new(SqCrypto.PolProDataToPolId(polIdData: GetPolID(), volume: 0, domain: 0), 0x00, Utils.UnixTimeStampUTC(), Utils.UnixTimeStampUTC(), Utils.UnixTimeStampUTC(), Utils.UnixTimeStampUTC());
                    IrcReplies.ServerCommand(this, Server.ServerName, adminData.ToBase32());
                    IrcReplies.ReplyWelcome(this, Server.ServerName, "TTTTT7TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT");
                    IrcReplies.ErrorNoMOTD(this, Server.ServerName, NickName);
                    //POL-Server sends a reply that the client is invisible here.... but it isn't
                    IrcReplies.Notice(this, Server.ServerName, NickName, loginNotify.GetBase64());
                    return;
                }
                else
                    IrcReplies.AdminError(this, 0xC9);
                return;
            }

            // If we got here, auth failed            
            IrcReplies.Kick(this, Server.ServerName);            
        }

        public void SendLine(string message, bool encryptMessage = true)
        {
            string checksum = SqCrypto.CheckSumToStr(SqCrypto.CalcCheckSum(message));
            string finalMessage = $"{message}{checksum}\r\n";

//#if POL_PRINT_IRC_SEND
            if (!message.Contains("PING") && !message.Contains("PONG"))
                Program.Log.Debug("Sending{1}: {0}", finalMessage, encryptMessage && CurrentCrypto != null ? " (Encrypted)" : "");
//#endif

            byte[] result = Encoding.GetEncoding("shift_jis").GetBytes(finalMessage);
            if (encryptMessage && CurrentCrypto != null)
                CurrentCrypto.SqCrypt64(result, result.Length, 1);

            try 
            {
                ClientSocket.Send(result);
            } 
            catch (SocketException)
            {
                Disconnect();
            }
        }

        public void NotifyOffline()
        {
            if (Server.GetNotifyClient() != null)
                Server.GetNotifyClient().SendLine($":{Server.ServerName} 300 {GetNick()} OFFLINE {GetPolID()}");
        }

        public int GetIpAsNumber()
        {
            byte[] ipBytes = ClientIp.GetAddressBytes();
            Array.Reverse(ipBytes);
            return BitConverter.ToInt32(ipBytes, 0);
        }

        public uint GetLoginTime() => LoginTime;

        public void SetUserName(string userName) => this.UserName = $"~{userName}";

        public string GetUserName() => UserName;

        public void SetNick(string nickName) => this.NickName = nickName;

        public string GetNick() => NickName;

        public string GetPolID() => SqCrypto.UnScramblePolId(NickName);

        public bool GetIsBot() => IsBot;

        public string GetRealName() => "POL-INFO"; // Static, never changes

        public string GetIdentifier() => $"{NickName}!{UserName}@";

        public void SetAway(string message)
        {
            IsAway = true;
            AwayMessage = message;
            IrcReplies.NowAway(this, Server.ServerName, NickName);
        }

        public void ClearAway()
        {
            IsAway = false;
            AwayMessage = "";
            IrcReplies.UnAway(this, Server.ServerName, NickName);
        }

        public bool GetIsAway() => IsAway;

        public void SendPrvMsg(Client from, string message)
        {
            if (IsAway)
                IrcReplies.ReplyAway(from, Server.ServerName, NickName, message);            
            IrcReplies.PrivMsg(this, from.GetIdentifier(), NickName, message);
            from.SendLine($":{Server.ServerName} 300 {from.GetNick()} {GetNick()}");
        }

        public void SendNotice(Client client, string message) => IrcReplies.Notice(this, client.GetIdentifier(), NickName, message);
        
        public void SendNoticePushMsg(string botId, string message) => IrcReplies.Notice(this, botId, NickName, message);

        public override string ToString()
        {
            if (NickName != null)
                return $"{NickName} - {GetPolID()}";
            else
                return GetAddress();
        }
    }
}