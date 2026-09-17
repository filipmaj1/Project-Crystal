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
using System.Threading;

namespace Crystal.FrontMissionOnline
{
    class Server
    {
        private const int BACKLOG = 100;

        public readonly string ServerIp;
        public readonly ushort ServerPort;
        private bool IsAlive = false;
        private Socket? ServerSocket;
        private Thread? ServerLoopThread;

        private readonly Dictionary<Socket, Client> Clients = new();
        readonly PacketProcessor PacketProcessor;

        public Server(string ip, ushort port)
        {
            ServerIp = ip;
            ServerPort = port;
            PacketProcessor = new PacketProcessor(this);
        }

        public void StopServer()
        {
            IsAlive = false;
        }

        public void StartServer()
        {
            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(ServerIp), ServerPort);
            try
            {
                ServerSocket = new Socket(serverEndPoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                ServerSocket.Bind(serverEndPoint);
                ServerSocket.Listen(BACKLOG);

                Program.Log.Info($"Server socket created on {ServerPort}...");
            }
            catch (Exception e)
            {
                throw new ApplicationException("Could not Create socket, check to make sure not duplicating port", e);
            }

            IsAlive = true;

            ServerSocket.BeginAccept(new AsyncCallback(AcceptCallback), ServerSocket);

            ServerLoopThread = new Thread(MainLoop);
            ServerLoopThread.Start();

            Program.Log.Info("Server thread has started...");
        }

        public void DebugSendToAll(ushort cmdId, uint sessionId, string path)
        {
            foreach (Client c in Clients.Values)
            {
                c.DebugSendPacket(cmdId, sessionId, path);
            }
        }

        public void Test()
        {
            foreach (Client c in Clients.Values)
            {
                for (int i = 0x150; i < 0x160; i++)
                {
                    Program.Log.Info($"{i}"); 
                    c.SendPacket((byte)i, 1, new byte[0x100]);
                }
            }
        }

        public ushort GetPort()
        {
            return ServerPort;
        }

        public string GetIp()
        {
            return ServerIp;
        }

        private void MainLoop()
        {
            List<Socket> socketsToRemove = new List<Socket>();

            while (IsAlive)
            {
                var socketList = new List<Socket>(Clients.Keys);

                if (socketList.Count == 0)
                    continue;

                Socket.Select(socketList, null, null, 1000);
                foreach (Socket s in socketList)
                {
                    // Poll
                    if ((s.Poll(1000, SelectMode.SelectRead) && (s.Available == 0)) || !s.Connected)
                    {
                        socketsToRemove.Add(s);
                        continue;
                    }

                    try
                    {
                        // This round's client
                        Client client = Clients[s];

                        // Receive and process packets
                        int bytesReceived = client.ReceiveData();
                        while (true)
                        {
                            int bytesParsed = client.GetPacket(out ushort commandId, out uint sessionId, out byte[] pktData, out bool checksumValid);
                            if (bytesParsed == 0)
                                break;

                            if (checksumValid)
                                PacketProcessor.ProcessPacket(client, commandId, sessionId, pktData);
                            else
                                Program.Log.Info($"{client} sent a packet with a bad checksum.");
                        }
                        client.FinishReceive();
                    }
                    catch (SocketException)
                    {
                        socketsToRemove.Add(s);
                    }
                }

                //Clean up all removed sockets
                foreach (Socket s in socketsToRemove)
                {
                    if (!Clients.ContainsKey(s))
                        continue;

                    Client disconnectedClient = Clients[s];

                    disconnectedClient.Disconnect();

                    Program.Log.Info("Socket disconnected: " + Clients[s].GetAddress());

                    //Remove socket
                    Clients.Remove(s);
                }

                socketsToRemove.Clear();
            }

            foreach (Socket s in Clients.Keys)
                Clients[s].Disconnect();

            ServerSocket?.Close(5);

            Clients.Clear();
            GC.Collect();
        }

        private void AcceptCallback(IAsyncResult result)
        {
            Socket? serverSocket = result.AsyncState as Socket;
            Socket? s = serverSocket!.EndAccept(result);
            Client client = new(this, s);

            if (!Clients.ContainsKey(s))
                Clients.Add(s, client);
            else
            {
                Clients.Remove(s);
                Clients.Add(s, client);
            }

            serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), serverSocket);
        }

        private static bool IsSocketConnected(Socket s)
        {
            return !((s.Poll(1000, SelectMode.SelectRead) && (s.Available == 0)) || !s.Connected);
        }
    }

}
