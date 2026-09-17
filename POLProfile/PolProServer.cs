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
using Crystal.POLProfile.DataObjects;
using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Net.Sockets;
using static Crystal.POLProfile.Packets.PacketBuilders;

namespace Crystal.POLProfile
{
    public class PolProServer
    {
        private const int BUFFER_SIZE = 0xFFFF;
        private const int BACKLOG = 100;

        // Connection
        public readonly string ServerIp;
        public readonly uint ServerIpNumeric;
        public readonly int ServerPort;
        private Socket ServerSocket;
        private volatile bool ShuttingDown = false;
        private readonly List<Client> ConnectionList = [];

        // PolPro
        private readonly string RootPath;
        private readonly NotifierClient NotifierClient;
        private readonly RequestHandler RequestHandler;
        private readonly Dictionary<byte, string> DomainToPath = [];

        public PolProServer(string ip, int port, string profilePath, string authIp, int authPort, string polId, string polPassword)
        {
            ServerIp = ip;
            ServerIpNumeric = Utils.SwapEndian(BitConverter.ToUInt32(IPAddress.Parse(ip).GetAddressBytes()));
            ServerPort = port;
            RootPath = profilePath;

            NotifierClient = new NotifierClient(authIp, authPort, polId, polPassword); //"NZPGFP7G"
            RequestHandler = new RequestHandler(this, NotifierClient);

            DomainToPath.Add(0x00, Path.Combine(RootPath, "playonline"));
            DomainToPath.Add(0x01, Path.Combine(RootPath, "finalfantasyxi"));
            DomainToPath.Add(0x02, Path.Combine(RootPath, "tetramaster"));
            DomainToPath.Add(0x03, Path.Combine(RootPath, "jangho"));
        }

        public void StartNotifier()
        {
            NotifierClient.StartNotifyLoop();
        }

        #region Socket Handling
        public bool StartServer()
        {
            IPEndPoint serverEndPoint = new(IPAddress.Parse("0.0.0.0"), ServerPort);

            try {
                ServerSocket = new(serverEndPoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            }
            catch (Exception e)
            {
                throw new ApplicationException("Could not Create socket, check to make sure not duplicating port", e);
            }
            try
            {
                ServerSocket.Bind(serverEndPoint);
                ServerSocket.Listen(BACKLOG);
            }
            catch (Exception e)
            {
                throw new ApplicationException("Error occured while binding socket, check inner exception", e);
            }
            try
            {
                ServerSocket.BeginAccept(new AsyncCallback(AcceptCallback), ServerSocket);
            }
            catch (Exception e)
            {
                throw new ApplicationException("Error occured starting listeners, check inner exception", e);
            }

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Program.Log.Info($"Profile Server has started @ {ServerIp}:{ServerPort}");
            Console.ForegroundColor = ConsoleColor.Gray;

            return true;
        }

        public void StopServer()
        {
            Program.Log.Info("Server is shutting down...");
            ShuttingDown = true;
            NotifierClient.StopNotifyLoop();
            lock (ConnectionList)
            {
                foreach (Client client in ConnectionList)
                    client.Disconnect();
                ConnectionList.Clear();
            }
            ServerSocket.Close();
            Program.Log.Info("Server has shutdown.");
        }

        private void AcceptCallback(IAsyncResult result)
        {
            // Try to accept the connection
            Socket clientSocket = null;
            try
            {
                clientSocket = ((Socket)result.AsyncState).EndAccept(result);
            }
            catch (ObjectDisposedException) { return; }
            catch (SocketException e)
            { Program.Log.Trace(e.Message); }
            finally
            {
                if (!ShuttingDown)
                {
                    try { ServerSocket.BeginAccept(new AsyncCallback(AcceptCallback), ServerSocket); }
                    catch (ObjectDisposedException) { }
                }
            }

            if (clientSocket == null)
                return;

            Client conn = new(this, clientSocket)
            {
                Buffer = new byte[BUFFER_SIZE]
            };

            lock (ConnectionList)
            {
                ConnectionList.Add(conn);
            }

            // Start receiving from client
            try
            {
                //Queue receiving of data from the connection
                conn.ClientSocket.BeginReceive(conn.Buffer, 0, conn.Buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), conn);
                Program.Log.Debug($"Connection {(conn.ClientSocket.RemoteEndPoint as IPEndPoint).Address}:{(conn.ClientSocket.RemoteEndPoint as IPEndPoint).Port} has connected.");
            }
            catch (Exception e)
            {
                // Client socket failed
                if (conn.ClientSocket != null)
                {
                    conn.ClientSocket.Close();
                    lock (ConnectionList)
                    {
                        ConnectionList.Remove(conn);
                    }
                }
                Program.Log.Trace(e.Message);
            }
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            Client client = (Client)result.AsyncState;

            try
            {
                int bytesRead = client.ClientSocket.EndReceive(result);

                if (bytesRead > 0)
                {
                    bytesRead += client.LastPartialSize;
                    int offset = 0;
                    bool needMore = false;

                    while (offset < bytesRead && !needMore)
                    {
                        byte[] packetData = new byte[0x28];
                        switch (client.GetState())
                        {
                            case Client.ClientState.NewConnection:
                                if (bytesRead >= offset + 0x28)
                                {
                                    Array.Copy(client.Buffer, offset, packetData, 0, 0x28);
                                    offset += 0x28;

                                    Packet packet = ParsePacket(packetData);

                                    // If this is a handshake packet, we are unencrypted and need to retrieve the client's
                                    // auth connection info to init blowfish.
                                    if (packet is HandshakePacket)
                                    {
                                        HandshakePacket hsPacket = (HandshakePacket)packet;
                                        string ipAsString = new IPAddress(hsPacket.ip).ToString();

                                        AuthConnectionInfo connInfo = Database.GetAccountSession(ipAsString, hsPacket.port);
                                        if (connInfo != null)
                                        {
                                            client.SendHandshakeAnswer(Utils.UnixTimeStampUTC());
                                            client.SetPolIpAddress(hsPacket.ip, hsPacket.port);
                                            client.SetReady(connInfo);
                                        }
                                        else
                                        {
                                            client.SendAnswer(errorCode: 0xEF);
                                            Program.Log.Warn("Client tried to connect with unknown port.");
                                            client.Disconnect();
                                        }
                                    }
                                    else
                                        client.Disconnect();
                                }
                                else
                                    needMore = true; // Partial packet; wait for the rest
                                break;
                            case Client.ClientState.Ready:
                                if (bytesRead >= offset + 0x28)
                                {
                                    Array.Copy(client.Buffer, offset, packetData, 0, 0x28);
                                    offset += 0x28;

                                    client.GetCrypto().SqCrypt64(packetData, 0x28, 1);

                                    Packet packet = ParsePacket(packetData);

                                    // We are encrypted, handle the request or switch to the object reading state.
                                    if (packet is RequestPacket)
                                    {
                                        RequestPacket reqPacket = (RequestPacket)packet;

                                        if (reqPacket.objectSize == 0)
                                            RequestHandler.HandleRequest(client, reqPacket.opcode);
                                        else if (reqPacket.objectSize > (uint)client.Buffer.Length)
                                        {
                                            Program.Log.Warn($"Client {client.GetAddress()} declared an object of {reqPacket.objectSize} bytes (max {client.Buffer.Length}); disconnecting.");
                                            client.SendAnswer(errorCode: 0xEF);
                                            client.Disconnect();
                                        }
                                        else
                                            client.StartObjectRead(reqPacket.opcode, reqPacket.objectSize);
                                    }
                                    else
                                        client.Disconnect();
                                }
                                else
                                    needMore = true; // Partial packet; wait for the rest
                                break;
                            case Client.ClientState.ReadObj:
                                uint objSize = client.GetObjectSize();
                                if (bytesRead >= offset + objSize)
                                {
                                    // Decrypt the incoming object and handle the request.
                                    byte[] objectData = new byte[objSize];
                                    Array.Copy(client.Buffer, offset, objectData, 0, objSize);
                                    offset += (int)objSize;

                                    client.GetCrypto().SqCrypt64(objectData, objectData.Length, 0);

                                    RequestHandler.HandleRequest(client, client.GetRequestedOpcode(), objectData);
                                }
                                else
                                    needMore = true;
                                break;
                        }
                    }

                    // Not all bytes consumed, transfer leftover to beginning
                    if (offset < bytesRead)
                        Array.Copy(client.Buffer, offset, client.Buffer, 0, bytesRead - offset);

                    client.LastPartialSize = bytesRead - offset;

                    if (offset < bytesRead)
                        // Need offset since not all bytes consumed
                        client.ClientSocket.BeginReceive(client.Buffer, bytesRead - offset, client.Buffer.Length - (bytesRead - offset), SocketFlags.None, new AsyncCallback(ReceiveCallback), client);
                    else
                        // All bytes consumed, full buffer available
                        client.ClientSocket.BeginReceive(client.Buffer, 0, client.Buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), client);
                }
                else
                {
                    Program.Log.Debug($"{client.GetAddress()} has disconnected.");

                    lock (ConnectionList)
                    {
                        client.Disconnect();
                        ConnectionList.Remove(client);
                    }
                }
            }
            catch (SocketException e)
            {
                Program.Log.Trace(e.Message);

                lock (ConnectionList)
                {
                    client.Disconnect();
                    ConnectionList.Remove(client);
                }
            }
            catch (ObjectDisposedException)
            {
                lock (ConnectionList)
                {
                    ConnectionList.Remove(client);
                }
            }
            catch (Exception e)
            {
                Program.Log.Error(e, $"Unhandled error processing client {client.GetAddress()}: {e.Message}");
                lock (ConnectionList)
                {
                    client.Disconnect();
                    ConnectionList.Remove(client);
                }
            }
        }

        public string GetPathFromDomain(byte domain)
        {
            return DomainToPath[domain] ?? null;
        }
        #endregion

    }
}
