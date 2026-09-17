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
using Crystal.Common.MiniGame;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Crystal.Mahjong
{
    public class PolIrcClient
    {
        private ulong polId;
        private Socket clientSocket;
        private byte[] receiveBuffer = new byte[0xFFFF];
        private SqCrypto currentCrypto;
        private SqRSAKeyPair rsaKeys;

        private bool isConnected = false;
        private bool isAdminData = false;
        private bool isReady = false;
        private bool isAuthenticated = false;
        private bool authError = false;

        private string ConsoleName;

        private string CurrentChannel = null;

        private Action<string,string> ReceiveMsgCallback = null;
        private Action<string> JoinChannelCallback = null;

        public PolIrcClient(string consoleName, ulong polId, Action<string,string> receiveMsgCallback)
        {
            this.polId = Mg.MjKey(polId);
            rsaKeys = SqCrypto.RSAGenerate();
            ReceiveMsgCallback = receiveMsgCallback;
            ConsoleName = consoleName;
        }

        public bool Connect(string host, int port)
        {
            authError = false;

            if (!IPAddress.TryParse(host, out IPAddress address))
            {
                IPHostEntry ipHostInfo = Dns.GetHostEntry(host);
                address = ipHostInfo.AddressList[0];
            }
            IPEndPoint remoteEP = new(address, port);
            clientSocket = new Socket(address.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);
 
            clientSocket.BeginConnect(remoteEP,
                new AsyncCallback(ConnectCallback), null);
            return false;
        }      

        public void Disconnect()
        {
            isConnected = isReady = isAuthenticated = false;
            SendLine("QUIT");
            clientSocket.Close();            
        }

        public bool IsConnected()
        {
            return isConnected;
        }

        public bool IsReady()
        {
            return isReady;
        }

        public bool IsAuthenticated()
        {
            return isAuthenticated;
        }

        public bool AuthenticationFailed()
        {
            return authError;
        }

        public bool JoinChannel(string channelName, Action<string> callback)
        {
            if (JoinChannelCallback != null)
                return false;

            JoinChannelCallback = callback;
            SendLine($"JOIN {channelName}");
            return true;
        }

        public void Notice(string target, string message)
        {
            SendLine($"NOTICE {SqCrypto.ScramblePolId(target)} :{message}");
        }

        private void SendLine(string message, bool encryptMessage = true)
        {
            string checksum = SqCrypto.CheckSumToStr(SqCrypto.CalcCheckSum(message));
            string finalMessage = String.Format("{0}{1}\r\n", message, checksum);

            //Program.Log.Debug("Sending{1}: {0}", finalMessage, encryptMessage && currentCrypto != null ? " (Encrypted)" : "");

            byte[] result = Encoding.ASCII.GetBytes(finalMessage);
            if (encryptMessage && currentCrypto != null)
                currentCrypto.SqCrypt64(result, result.Length, 1);

            try
            {
                clientSocket.Send(result);
            } catch (SocketException)
            {

            }
        }

        public void SendLine2(string message, string message2)
        {
            string checksum = SqCrypto.CheckSumToStr(SqCrypto.CalcCheckSum(message));
            string finalMessage = String.Format("{0}{1}\r\n", message, checksum);
            byte[] result = Encoding.ASCII.GetBytes(finalMessage);
            if (currentCrypto != null)
                currentCrypto.SqCrypt64(result, result.Length, 1);


            string checksum2 = SqCrypto.CheckSumToStr(SqCrypto.CalcCheckSum(message2));
            string finalMessage2 = String.Format("{0}{1}\r\n", message2, checksum2);
            byte[] result2 = Encoding.ASCII.GetBytes(finalMessage2);
            if (currentCrypto != null)
                currentCrypto.SqCrypt64(result2, result2.Length, 1);

            byte[] result3 = new byte[result.Length + result2.Length];
            Array.Copy(result, result3, result.Length);
            Array.Copy(result, 0, result3, result.Length, result2.Length);

            try
            {
                clientSocket.Send(result3);
            }
            catch (SocketException)
            {

            }
        }

        #region Crypto

        private void StartHandshake()
        {
            Program.Log.Info($"[{ConsoleName}] - 0x{Mg.MjKey(polId):X16} - POL-Auth handshake started");
            byte[] modulus = rsaKeys.Public.Modulus.ToByteArrayUnsigned();
            Array.Reverse(modulus);
            string rsaB32 = SqCrypto.EncodeBase64(modulus, modulus.Length, true);
            SendLine(string.Format("USER x 8 * :{0}", rsaB32));
        }

        private void InitCrypto(string serverMsg)
        {
            byte[] result = SqCrypto.DecodeBase64(serverMsg, serverMsg.Length);
            byte[] cleanedResult = new byte[32];
            Array.Copy(result, cleanedResult, 32);

            byte[] blowfishKey = SqCrypto.RSADecrypt(rsaKeys.Private, cleanedResult);
            byte[] modulus = rsaKeys.Public.Modulus.ToByteArrayUnsigned();
            Array.Reverse(modulus);
            Array.Reverse(blowfishKey);
            currentCrypto = new SqCrypto(blowfishKey, modulus);
            isReady = true;

            SendLine(string.Format("NICK :{0}:{1}:{2}", SqCrypto.ScramblePolId(SqCrypto.PolIdToPolProData(polId)), "password", "BotBotBot"));
        }

        private void WelcomeConfirm(string msg)
        {
            if (msg.Equals("Gucci"))
            {
                isAuthenticated = true;
                Program.Log.Info($"[{ConsoleName}] - 0x{Mg.MjKey(polId):X16} - POL-Auth handshake complete");
            }
            else
            {
                isAuthenticated = false;
                authError = true;
                Program.Log.Info($"[{ConsoleName}] - 0x{Mg.MjKey(polId):X16} - POL-Auth handshake failed");
                Disconnect();
            }
        }
        #endregion

        #region Sockets
        private void ReceiveCallback(IAsyncResult result)
        {
            int bytesRead = clientSocket.EndReceive(result);
            Span<byte> receive = receiveBuffer.AsSpan();
            int readLength = 0;

            while (true)
            {
                // Get next msg
                int length = FindNextNewline(receiveBuffer);
                if (length == -1)
                    break;
                readLength += length;

                // Grab next amount and decrypt if needed
                var messageBytes = receive[..length];
                if (isReady)
                    currentCrypto.SqCrypt64(messageBytes, length, 1);
                string message = Encoding.ASCII.GetString(messageBytes[..(length - 2)]);

                // Handle Msg
                ParseMessage(message);

                // Break if done or get next chunk
                if (readLength >= bytesRead)
                    break;
                receive = receive[length..];
            }

            if (isConnected)
            {
                try
                {
                    clientSocket.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
                }
                catch (Exception)
                {
                    isConnected = false;
                    clientSocket.Disconnect(true);
                    Program.Log.Info("Pol Client disconnected.");
                }
            }
        }

        private void ParseMessage(string line)
        {
            // Split the message and checksum
            string message = line[..^4];
            string checksum = line.Substring(line.Length - 4, 4);

            // Verify Checksum
            string ourChecksum = SqCrypto.CheckSumToStr(SqCrypto.CalcCheckSum(message));
            if (!ourChecksum.Equals(checksum))
            {
                Program.Log.Info($"[{ConsoleName}] - 0x{Mg.MjKey(polId):X16} - received invalid checksum!");
            }

            // Handle Msg
            if (message[0] == ':')
            {
                string[] colonSplit = message.Split(':');
                string[] firstPartSplit = colonSplit[1].Split(' ');

                string source = firstPartSplit[0];
                string command = firstPartSplit[1];
                if (int.TryParse(command, out int replyCode))
                {
                    switch (replyCode)
                    {
                        case 001:
                            WelcomeConfirm(firstPartSplit[2]);
                            break;
                        case 300:
                            if (isAuthenticated && ReceiveMsgCallback != null)
                            {
                                ReceiveMsgCallback(source, message[message.LastIndexOf(firstPartSplit[3])..]);
                            }
                            else if (!isAdminData)
                            {
                                isAdminData = true;
                                StartHandshake();
                            }
                            else
                                InitCrypto(firstPartSplit[3]);
                            break;
                    }
                }
                else
                {
                    if (firstPartSplit[1].Equals("NOTICE"))
                    {
                        if (source.Length >= 9 && colonSplit.Length == 3)
                        {
                            source = source.Substring(0, 9);
                            ReceiveMsgCallback(SqCrypto.UnScramblePolId(source), colonSplit[2]);
                        }
                    }
                    else if (firstPartSplit[1].Equals("JOIN") && firstPartSplit.Length == 3)
                    {
                        string channelName = firstPartSplit[2];
                        JoinChannelCallback.Invoke(channelName);
                        JoinChannelCallback = null;
                        CurrentChannel = channelName;
                    }
                }
            }
            else
            {
                string[] colonSplit = message.Split(':');
                string[] firstPartSplit = colonSplit[1].Split(' ');
                string command = colonSplit[0].Trim();

                if (command.Equals("PING"))
                {
                    SendLine($"PONG :{colonSplit[1]}");
                }
            }
        }

        private int FindNextNewline(Span<byte> span)
        {
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] == '\n')
                    return i + 1;
            }
            return -1;
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {                
                clientSocket.EndConnect(ar);
                isConnected = true;
                Program.Log.Info($"[{ConsoleName}] - 0x{Mg.MjKey(polId):X16} - connected to POL-Auth");
                clientSocket.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
            }
            catch
            {
                Program.Log.Info($"[{ConsoleName}] - 0x{Mg.MjKey(polId):X16} - failed connection to POL-Auth");
            }
        }

        #endregion
    }
}
