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
using System.Net;
using System.Net.Sockets;

namespace Crystal.POLPatch
{
    public class PatchServer
    {
        public const int BUFFER_SIZE        = 0xFFFF;
        public const int BACKLOG            = 100;

        private Socket ServerSocket;
        private volatile bool ShuttingDown = false;
        private readonly List<ClientConnection> ConnList = [];
        private readonly PacketProcessor PacketProcessor;
        private readonly PatchRetriever PatchRetriever;

        public readonly string ApplicationId;
        public readonly string ServerIP;
        public readonly int ServerPort;

        public PatchServer(int basePort, string rootDir, string serverIp, string applicationId, PatchPlatformContainer[] patches)
        {
            ServerIP = serverIp;
            if (int.TryParse(applicationId, out int appId))
                ServerPort = basePort + appId;
            else
                throw new ApplicationException("ApplicationId was not a number.");

            ApplicationId = applicationId;
            PatchRetriever = new PatchRetriever(rootDir, ServerIP, applicationId, patches);
            PacketProcessor = new PacketProcessor(PatchRetriever);
        }

        #region Socket Handling
        public bool Start()
        {
            IPEndPoint serverEndPoint = new(IPAddress.Parse("0.0.0.0"), ServerPort);
           
            try{
                ServerSocket = new Socket(serverEndPoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);                
            }
            catch (Exception e)
            {
                throw new ApplicationException("Error creating socket.", e);
            }
            try
            {
                ServerSocket.Bind(serverEndPoint);
                ServerSocket.Listen(BACKLOG);
            }
            catch (Exception e)
            {
                throw new ApplicationException("Error binding socket. Already running?", e);
            }
            try
            {               
                ServerSocket.BeginAccept(new AsyncCallback(AcceptCallback), ServerSocket);
            }
            catch (Exception e)
            {
                throw new ApplicationException("Error occured starting listeners.", e);
            }

            return true;
        }

        public void Stop()
        {
            Program.Log.Info("Server is shutting down...");
            ShuttingDown = true;
            lock (ConnList)
            {
                foreach (ClientConnection client in ConnList)
                    client.Disconnect();
                ConnList.Clear();
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
            catch (SocketException) {}
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

            ClientConnection conn = null;
            try
            {
                conn = new ClientConnection(clientSocket);
                lock (ConnList)
                {
                    ConnList.Add(conn);
                }
                //Queue recieving of data from the connection
                conn.Socket.BeginReceive(conn.Buffer, 0, conn.Buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), conn);

#if DEBUG_PKT
                Program.Log.Debug("{0} has connected.", conn);
#endif
            }
            catch (Exception)
            {
                clientSocket.Close();
                if (conn != null)
                {
                    lock (ConnList)
                    {
                        ConnList.Remove(conn);
                    }
                }
            }
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            ClientConnection conn = (ClientConnection)result.AsyncState;            

            try
            {
                int bytesRead = conn.Socket.EndReceive(result);

                if (bytesRead > 0)
                {
                    bytesRead += conn.LastPartialSize;
                    int offset = 0;

                    //Build packets until can no longer or out of data
                    while (true)
                    {
                        if (bytesRead - offset < BasePacket.BASEPACKET_SIZE)
                            break;

                        // Peek the declared size; reject garbage, wait on partial packets
                        uint packetSize = BitConverter.ToUInt32(conn.Buffer, offset);
                        if (packetSize < BasePacket.BASEPACKET_SIZE || packetSize > conn.Buffer.Length)
                        {
                            conn.SendError();
                            lock (ConnList)
                            {
                                ConnList.Remove(conn);
                            }
                            return;
                        }

                        if (bytesRead - offset < packetSize)
                            break; // Partial packet; wait for the rest

                        try
                        {
                            BasePacket packet = new(conn.Buffer, ref offset);
                            PacketProcessor.ProcessPacket(conn, packet);
                        }
                        catch (Exception)
                        {
                            conn.SendError();
                            lock (ConnList)
                            {
                                ConnList.Remove(conn);
                            }
                            return;
                        }
                    }

                    //Not all bytes consumed, transfer leftover to beginning
                    if (offset < bytesRead)
                        Array.Copy(conn.Buffer, offset, conn.Buffer, 0, bytesRead - offset);
                    conn.LastPartialSize = bytesRead - offset;

                    conn.Socket.BeginReceive(conn.Buffer, conn.LastPartialSize, conn.Buffer.Length - conn.LastPartialSize, SocketFlags.None, new AsyncCallback(ReceiveCallback), conn);
                }
                else
                {
                    lock (ConnList)
                    {
                        conn.Disconnect();
                        ConnList.Remove(conn);
                    }
                }
            }
            catch (SocketException)
            {
                lock (ConnList)
                {
                    conn.Disconnect();
                    ConnList.Remove(conn);
                }
            }
            catch (ObjectDisposedException)
            {
                // Socket was closed by a Disconnect racing this callback
                lock (ConnList)
                {
                    ConnList.Remove(conn);
                }
            }
        }      
        #endregion

    }
}
