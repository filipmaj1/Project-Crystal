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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Crystal.Common;
using Crystal.POLProfile.DataObjects;
using Crystal.POLProfile.DataObjects.Pol;

namespace Crystal.POLProfile
{
    class Client
    {
        PolProServer Server;

        // Packet Handling
        private const int SEND_TIMEOUT_MS = 10000;
        public Socket ClientSocket;
        private IPAddress ClientIp;
        public int ClientPort;
        public byte[] Buffer = new byte[0xffff];
        public int LastPartialSize = 0;

        // Request Reading
        public enum ClientState { NewConnection, Ready, ReadObj };
        private ClientState CurrentPhase;
        private ushort Opcode = 0;
        private uint ObjSize = 0;
        private SqCrypto CurrentCrypto;
        private string PolId;
        private uint LoginTime;
        private PolIpAddress PolIpAddress;

        public Client(PolProServer server, Socket socket)
        {
            var endpoint = ((IPEndPoint)socket.RemoteEndPoint);

            Server = server;
            ClientSocket = socket;
            ClientSocket.SendTimeout = SEND_TIMEOUT_MS;
            ClientIp = endpoint.Address;
            ClientPort = endpoint.Port;
            CurrentPhase = ClientState.NewConnection;
        }

        public string GetAddress() => $"{ClientIp}:{ClientPort}";

        public void SetPolIpAddress(uint ip, ushort port) 
        {
            PolIpAddress = new()
            {
                IsValid = 1,
                Port = port,
                Ip = Utils.SwapEndian(ip),
                UnkMustBe1 = 1,
                LanguageCode = 2
            };
        }

        public byte[] GetPolIpAddressBytes()
        {
            byte[] bytes = new byte[0x14];
            MemoryMarshal.Write(bytes, in PolIpAddress);
            return bytes;
        }

        public SqCrypto GetCrypto() => CurrentCrypto;
        
        public IPAddress GetIp() => ClientIp;

        public int GetPort() => ClientPort;

        public uint GetLoginTime() => LoginTime;

        public string GetPolProData() => PolId;

        public ulong GetPolProId() => SqCrypto.PolProDataToPolId(PolId, 0, 0);

        public ClientState GetState() => CurrentPhase;

        public uint GetObjectSize() => ObjSize;

        public ushort GetRequestedOpcode() => Opcode;

        public void Disconnect()
        {
            try
            {
                ClientSocket.Shutdown(SocketShutdown.Both);
                ClientSocket.Disconnect(false);
            }
            catch (SocketException) { /* Already reset by the peer */ }        
            catch (ObjectDisposedException) { }
            finally
            {
                ClientSocket.Close();
            }
        }

        public void SetReady(AuthConnectionInfo authConnInfo)
        {
            PolId = authConnInfo.PolId;
            LoginTime = authConnInfo.LoginTime;
            CurrentCrypto = new SqCrypto(authConnInfo.BlowfishKey, authConnInfo.RsaKey1, authConnInfo.RsaKey2);
            CurrentPhase = ClientState.Ready;
        }

        public void StartObjectRead(ushort opcode, uint objectSize)
        {
            Opcode = opcode;
            ObjSize = objectSize;
            CurrentPhase = ClientState.ReadObj;
        }

        public void SendAnswer(byte[] obj = null, byte errorCode = 0)
        {
            byte[] header = new byte[0x18];
            byte[] checksumObj = null;

            //Build the response header
            using (MemoryStream memStream = new(header))
            using (BinaryWriter binWriter = new(memStream))
            {
                binWriter.Write((Byte)0x83);
                binWriter.Write((Byte)errorCode); //7E; File Not Found. EF; Bad checksum. 0x73: Max Groups. 0x74: Group name in use. 0xE4: Group name bad.
                binWriter.Write((UInt16)0);

                if (obj == null)
                    binWriter.Write((UInt32)0x0);
                else
                    binWriter.Write((UInt32)obj.Length + 4);

                binWriter.Write(Server.ServerIpNumeric);
            }

            //Checksum the obj if needed
            if (obj != null)
            {
                uint checksum = SqCrypto.CalcCheckSum(obj);
                byte[] checksumBytes = BitConverter.GetBytes(checksum);

                checksumObj = new byte[obj.Length + 4];

                Array.Copy(obj, 0, checksumObj, 0, obj.Length);
                Array.Copy(checksumBytes, 0, checksumObj, checksumObj.Length - 4, 4);
            }

            if (checksumObj != null)
                Program.Log.Debug($"Sending Object:\n{Utils.ByteArrayToHex(checksumObj)}");

            //Encrypt
            if (CurrentPhase != ClientState.NewConnection)
            {
                CurrentCrypto.SqCrypt64(header, header.Length, 1);
                if (checksumObj != null)
                    CurrentCrypto.SqCrypt64(checksumObj, checksumObj.Length, 0);
            }

            //Send
            ClientSocket.Send(header);
            if (checksumObj != null)
                ClientSocket.Send(checksumObj);
        }

        public void SendHandshakeAnswer(uint currentTime)
        {
            byte[] data = new byte[0x18];

            using (MemoryStream memStream = new(data))
            using (BinaryWriter binWriter = new(memStream))
            {
                binWriter.Write((UInt32)0x81);
                binWriter.Seek(0x14, SeekOrigin.Begin);
                binWriter.Write((UInt32)currentTime);
            }
            if (data != null)
                Program.Log.Debug($"Sending Data:\n{Utils.ByteArrayToHex(data)}");

            ClientSocket.Send(data);
        }
        
        public byte[] GenerateSELoginHash(uint loginTime, string passwordHash)
        {
            string password = $"{passwordHash}playonline";
            byte[] hash = SHA1.HashData(Encoding.UTF8.GetBytes(password));

            // Byte Array -> Hex String
            byte b;
            char[] c = new char[hash.Length * 2];
            for (int i = 0, j = 0; i < hash.Length; ++i, ++j)
            {
                b = (byte)(hash[i] >> 4);
                c[j] = (char)(b > 9 ? b + 0x37 + 0x20 : b + 0x30);

                b = (byte)(hash[i] & 0x0F);
                c[++j] = (char)(b > 9 ? b + 0x37 + 0x20 : b + 0x30);
            }
            string hash2 = new(c);

            loginTime -= loginTime % 0x3C;
            password = $"{hash2}{loginTime}";
            return SHA1.HashData(Encoding.UTF8.GetBytes(password));
        }

        public override string ToString()
        {
            if (PolId != null) 
                return PolId;
            else
                return GetAddress();
        }

        public int GetNextSearchIndex()
        {
            return 0;
        }
    }
}