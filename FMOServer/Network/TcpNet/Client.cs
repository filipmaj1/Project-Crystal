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
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Crypto.Tls;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace Crystal.FrontMissionOnline.Network.TcpNet
{
    class Client
    {
        // Connection
        private Socket Socket;
        private bool Disconnected = false;
        private Server Server;
        private FmoCrypto? SendEncryption = null;
        private FmoCrypto? ReceiveEncryption = null;
        private string IpAddress;
        private int Port;

        // Traffic Buffer
        private byte[] ReceiveBuffer = new byte [0xFFFF];
        private int ReceiveLength = 0;
        private int ReceiveParsed = 0;

        public Client(Server server, Socket socket)
        {
            Socket = socket;
            Server = server;

            var endpoint = Socket.RemoteEndPoint as IPEndPoint;
            IpAddress = endpoint.Address.ToString();
            Port = endpoint.Port;
        }

        public string GetAddress() => $"{IpAddress}:{Port}";

        public void Disconnect()
        {
            if (Disconnected)
                return;

            Disconnected = true;

            //Close if need be
            if (Socket.Available != 0)
            {
                Socket.Shutdown(SocketShutdown.Both);
                Socket.Disconnect(false);
            }
        }

        public string GetIp() => IpAddress;

        public int GetIpAsNumber()
        {
            byte[] ipBytes = IPAddress.Parse(GetIp()).GetAddressBytes();
            Array.Reverse(ipBytes);
            return BitConverter.ToInt32(ipBytes, 0);
        }

        public int GetPort() => Port;

        public override string ToString()
        {
            return GetAddress();
        }

        public int ReceiveData()
        {
            int received = Socket.Receive(ReceiveBuffer, ReceiveLength, ReceiveBuffer.Length - ReceiveLength, SocketFlags.None);
            ReceiveLength += received;
            return received;
        }

        public void FinishReceive()
        {
            Array.Copy(ReceiveBuffer, ReceiveParsed, ReceiveBuffer, 0, ReceiveLength - ReceiveParsed);
            ReceiveLength -= ReceiveParsed;
            ReceiveParsed = 0;
        }

        public void StartEncryption(byte[] key)
        {
            SendEncryption = new(key);
            ReceiveEncryption = new(key);
        }

        public bool IsEncrypted
        {
            get => SendEncryption != null;
        }

        public int SendPacket(ushort commandId, uint sessionId, byte[]? data = null, ushort errorId = 0, bool isEncrypted = true)
        {
            byte[] pktBytes = MakePacket(commandId: commandId,
                                         sessionId: sessionId,
                                         errorId: errorId,
                                         data: data,
                                         plaintext: !isEncrypted);
            //Program.Log.Debug($"SendPkt:\n{Utils.ByteArrayToHex(MakePacket(commandId: commandId,
            //                             sessionId: sessionId,
            //                             errorId: errorId,
            //                             data: data,
            //                             plaintext: true))}");
            return Socket.Send(pktBytes);
        }

        public int DebugSendPacket(string path)
        {
            byte[] data;
            try
            { 
                data = File.ReadAllBytes(path);
            }
            catch (Exception)
            {
                Program.Log.Debug("File not found.");
                return 0;
            }

            return SendPacket(0x183, 0x101e, data);
        }

        public int GetPacket(out ushort outCommandId, out uint outSessionId, out byte[] outData, out bool outChecksumValid)
        {
            int bytesParsed = ParsePacket(ReceiveBuffer, ReceiveParsed, ReceiveLength, out ushort commandId, out uint sessionId, out byte[]? data, out bool checksumValid);
            ReceiveParsed += bytesParsed;
            outCommandId = commandId;
            outSessionId = sessionId;
            outChecksumValid = checksumValid;
            outData = data;
            return bytesParsed;
        }

        /* PACKET HEADER (0x14 in size)
         * 0x00: Size
         * 0x02: Flags
         * 0x04: Checksum
         * 0x06: CommandId
         * 0x08: ErrorId
         * 0x10: SessionId
         * 0x14: Data
        */
        private int ParsePacket(byte[] buffer, int offset, int maxLength, out ushort outCommandId, out uint outSessionId, out byte[] outData, out bool outChecksumValid)
        {
            outCommandId = 0;
            outSessionId = 0;
            outChecksumValid = true;
            outData = Array.Empty<byte>();

            // Is the header here?
            if (maxLength - offset < PacketHeader.SIZE)
                return 0;

            var pktSpan = new Span<byte>(buffer)[offset..];

            // Is the full packet here?
            ushort packetSize = MemoryMarshal.Read<ushort>(pktSpan);
            ushort flags = MemoryMarshal.Read<ushort>(pktSpan[2..]);

            if (maxLength - offset < packetSize)
                return 0;

            // Decrypt if needed
            if ((flags & 0x0100) != 0)
            {
                ReceiveEncryption?.TransformBuff(pktSpan[4..], packetSize - 4);
            }

            // Get the header
            var castedSpan = MemoryMarshal.Cast<byte, PacketHeader>(pktSpan);
            if (castedSpan == null || castedSpan.Length == 0)
                return 0;
            PacketHeader header = castedSpan[0];

            // Verify checksum if needed
            if (header.HasChecksum)
            {
                ushort pktChecksum = header.Checksum;
                ushort calcChecksum = 0;
                buffer[offset + 4] = 0;
                buffer[offset + 5] = 0;

                for (int i = 0; i < header.Size; i++)
                    calcChecksum += pktSpan[i];
                if (calcChecksum != pktChecksum)
                    outChecksumValid = false;
            }
                                        
            // Return all the details
            outCommandId = header.CommandId;
            outSessionId = header.SessionId;
            outData = pktSpan.Slice(offset + PacketHeader.SIZE, header.Size - PacketHeader.SIZE).ToArray();
            return header.Size;
        }

        private byte[] MakePacket(ushort commandId, uint sessionId, byte[]? data, ushort errorId = 0, bool plaintext = false, bool doChecksum = false)
        {
            // Init header
            PacketHeader header = new()
            {
                Size = (ushort)(PacketHeader.SIZE + (data?.Length ?? 0)),
                IsEncrypted = IsEncrypted && !plaintext,
                HasChecksum = doChecksum,
                Checksum = 0x00,
                CommandId = commandId,
                ErrorId = errorId,
                SessionId = sessionId
            };

            byte[] packet = new byte[header.Size];
            Span<byte> packetSpan = new(packet);
            MemoryMarshal.Write(packetSpan, ref header);
            data?.AsSpan().CopyTo(packetSpan[PacketHeader.SIZE..]);

            // Calc checksum
            if (doChecksum)
            {
                ushort checksum = 0;
                for (int i = 0; i < packet.Length; i++)
                    checksum += packet[i];
                header.Checksum = checksum;
                MemoryMarshal.Write(packetSpan, ref header);
            }

            // Encrypt
            if (!plaintext)
                SendEncryption?.TransformBuff(packet.AsSpan()[4..], header.Size - 4);

            return packet;
        }
    }
}