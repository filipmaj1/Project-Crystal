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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Crystal.Common.PolClient
{
    public class PolIrcClient
    {
        private string PolId;
        private string Password;

        private Socket ClientSocket;
        private byte[] ReceiveBuffer = new byte[0xFFFF];
        public  SqCrypto CurrentCrypto;
        private SqRSAKeyPair RsaKeys;
        private string InitInfoBase32;

        private bool isConnected = false;
        private bool isAdminData = false;
        private bool isCryptoReady = false;
        private bool isAuthenticated = false;
        private bool AuthError = false;

        private Action<string> ReceiveMsgCallback = null;
        private static readonly char[] Separator = ['\r', '\n'];

        public PolIrcClient(string polId, string password, Action<string> receiveMsgCallback = null)
        {
            PolId = polId;
            Password = password;
            ReceiveMsgCallback = receiveMsgCallback;
        }

        private void Print(string str)
        {
            //Console.WriteLine(str);
        }

        public bool Connect(string host, int port)
        {
            AuthError = false;

            if (!IPAddress.TryParse(host, out IPAddress address))
            {
                IPHostEntry ipHostInfo = Dns.GetHostEntry(host);
                address = ipHostInfo.AddressList[0];
            }
            IPEndPoint remoteEP = new(address, port);
            ClientSocket = new Socket(address.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            try
            {
                ClientSocket.Connect(remoteEP);
            }
            catch (Exception)
            {
                isConnected = false;
                return false;
            }

            isConnected = true;
            ClientSocket.BeginReceive(ReceiveBuffer, 0, ReceiveBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);

            return true;
        }      

        public void Disconnect(bool remoteDisconnected = false)
        {
            isConnected = isCryptoReady = isAuthenticated = isAdminData = false;
            CurrentCrypto = null;
            ClientSocket.Close();
        }

        public bool IsConnected()
        {
            return isConnected;
        }

        public bool IsCryptoReady()
        {
            return isCryptoReady;
        }

        public bool IsAuthenticated()
        {
            return isAuthenticated;
        }

        public bool AuthenticationFailed()
        {
            return AuthError;
        }

        public bool WaitAuthentication(int timeoutMS)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (!IsAuthenticated())
            {
                if (AuthenticationFailed() || sw.ElapsedMilliseconds >= timeoutMS)
                    return false;
                Thread.Sleep(100);
            }

            return true;
        }

        public uint GetMyIp()
        {
            if (ClientSocket == null || !ClientSocket.Connected)
                return 0xFFFFFFFF;
            var clientConnection = (IPEndPoint) ClientSocket.LocalEndPoint;
            return Utils.SwapEndian(BitConverter.ToUInt32(clientConnection.Address.GetAddressBytes()));
        }

        public uint GetMyPort()
        {
            if (ClientSocket == null || !ClientSocket.Connected)
                return 0xFFFFFFFF;
            var clientConnection = (IPEndPoint)ClientSocket.LocalEndPoint;
            return (uint) clientConnection.Port;
        }

        public void Notice(string target, string message)
        {
            SendLine($"NOTICE {SqCrypto.ScramblePolId(target)} :{message}");
        }

        private void SendLine(string message, bool encryptMessage = true)
        {
            string checksum = SqCrypto.CheckSumToStr(SqCrypto.CalcCheckSum(message));
            string finalMessage = $"{message}{checksum}\r\n";

            Print($"Sending{(encryptMessage && CurrentCrypto != null ? " (Encrypted)" : "")}: {finalMessage}");

            byte[] result = Encoding.ASCII.GetBytes(finalMessage);
            if (encryptMessage && CurrentCrypto != null)
                CurrentCrypto.SqCrypt64(result, result.Length, 1);

            try
            {
                ClientSocket.Send(result);
            } catch (Exception)
            {
                Disconnect();
            }
        }

        #region Crypto

        private void StartHandshake()
        {
            RsaKeys = SqCrypto.RSAGenerate();
            Print($"{PolId} - POL-Auth handshake started");
            byte[] modulus = RsaKeys.Public.Modulus.ToByteArrayUnsigned();
            Array.Reverse(modulus);
            string rsaB32 = SqCrypto.EncodeBase64(modulus, modulus.Length, true);
            SendLine($"USER x 8 * :{rsaB32}");
        }

        private void InitCrypto(string serverMsg)
        {
            byte[] result = SqCrypto.DecodeBase64(serverMsg, serverMsg.Length);
            byte[] cleanedResult = new byte[32];
            Array.Copy(result, cleanedResult, 32);
            
            byte[] blowfishKey = SqCrypto.RSADecrypt(RsaKeys.Private, cleanedResult);
            byte[] modulus = RsaKeys.Public.Modulus.ToByteArrayUnsigned();
            Array.Reverse(modulus);
            Array.Reverse(blowfishKey);
            CurrentCrypto = new SqCrypto(blowfishKey, modulus);
            isCryptoReady = true;

            string passwordMD5 = SqCrypto.GeneratePOLPasswordMD5(InitInfoBase32, Encoding.ASCII.GetBytes(Password));

            SendLine($"NICK :{SqCrypto.ScramblePolId(PolId, true)}:{passwordMD5}:BotBotBot");
        }

        private void WelcomeConfirm(string msg)
        {
            if (msg.Equals("Gucci"))
            {
                isAuthenticated = true;
                Print($"{PolId} - POL-Auth handshake complete");
            }
            else
            {
                isAuthenticated = false;
                AuthError = true;
                Print($"{PolId} - POL-Auth handshake failed");
                Disconnect();
            }
        }
        #endregion

        #region Sockets
        private void ReceiveCallback(IAsyncResult result)
        {
            int bytesRead;
            try
            {
                bytesRead = ClientSocket.EndReceive(result);
            }
            catch (Exception)
            {
                Disconnect(remoteDisconnected: true);
                return;
            }

            if (bytesRead == 0)
            {
                Disconnect(remoteDisconnected: true);
                return;
            }

            if (isCryptoReady)
            {
                CurrentCrypto.SqCrypt64(ReceiveBuffer, bytesRead, 1);
            }

            foreach (string line in Encoding.ASCII.GetString(ReceiveBuffer, 0, bytesRead).Split(Separator, StringSplitOptions.RemoveEmptyEntries))
            {
                // Split the message and checksum
                string message = line[..^4];
                string checksum = line.Substring(line.Length - 4, 4);

                // Verify Checksum
                string ourChecksum = SqCrypto.CheckSumToStr(SqCrypto.CalcCheckSum(message));
                if (!ourChecksum.Equals(checksum))
                {
                    Print($"{PolId} - received invalid checksum!");
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
                                    ReceiveMsgCallback(message[message.LastIndexOf(firstPartSplit[3])..]);
                                }
                                else if (!isAdminData)
                                {
                                    isAdminData = true;
                                    InitInfoBase32 = firstPartSplit[3];
                                    StartHandshake();
                                }
                                else
                                    InitCrypto(firstPartSplit[3]);
                                break;
                        }
                    }
                    else
                    {
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

            if (isConnected)
            {
                try
                {
                    ClientSocket.BeginReceive(ReceiveBuffer, 0, ReceiveBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
                }
                catch (Exception)
                {
                    isConnected = false;
                    ClientSocket.Disconnect(true);
                    Print("Pol Client disconnected.");
                }
            }
        }
#endregion
    }
}
