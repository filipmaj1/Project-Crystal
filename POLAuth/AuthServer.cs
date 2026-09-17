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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Crystal.POLAuth
{
    public class AuthServer
    {
        public const int AUTHENTICATION_PORT = 51240;
        public const string COMM_PIPE_NAME = "crystal_auth.comm";
        private const int BACKLOG = 100;
        
        // Server Info
        public readonly string ServerIp;
        public readonly ushort ServerPort;
        public readonly string ServerName;

        // Connection
        private Socket ServerSocket;
        private volatile bool IsAlive = false;

        // Thread
        private Thread ServerProcThread;
        private uint LastPingTime = Utils.UnixTimeStampUTC();

        // Pol IRC
        public Dictionary<Socket, Client> IrcClients = [];
        public Dictionary<string, Channel> IrcChannels { get; } = [];
        private readonly MessageHandler IrcMessageHandler;

        // PolPro
        public readonly string PolProId;
        public readonly string PolProPassword;
        public readonly string? PolProIpAddress;
        private Client PolProNotifyClient;

        // Other
        public readonly PolAuthAdminControl AdminControl = new();

        public AuthServer(string ip, string name, string polProId, string polProPassword, string polProIpAddress)
        {
            ServerIp = ip;
            ServerPort = AUTHENTICATION_PORT;
            ServerName = name;
            PolProId = polProId;
            PolProPassword = polProPassword;
            PolProIpAddress = polProIpAddress;
            IrcMessageHandler = new(this);
        }

        public bool StartServer(bool isMainPolAuth = false)
        {
            // Only the main POL Auth server touches this. MG Servers do not.
            if (isMainPolAuth) {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Program.Log.Info("Clearing sessions table...");
                Database.ClearSessions();
                Program.Log.Info("Clearing chatroom table...");
                Database.ClearChatrooms();
                Program.Log.Info("Creating permanent chatrooms...");
                List<Tuple<string, string, ushort>> permaChannels = Database.GetPermanentChatroomChannels();
                foreach (Tuple<string, string, ushort> channelAndTopicAndLimit in permaChannels)
                {
                    Channel chann = new(this, channelAndTopicAndLimit.Item1, true, channelAndTopicAndLimit.Item3);
                    chann.SetTopic(null, channelAndTopicAndLimit.Item2);
                    IrcChannels.Add(channelAndTopicAndLimit.Item1, chann);
                }
                Console.ForegroundColor = ConsoleColor.Gray;
            }

            IPEndPoint serverEndPoint = new(IPAddress.Parse("0.0.0.0"), ServerPort);
            try
            {
                ServerSocket = new Socket(serverEndPoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                ServerSocket.Bind(serverEndPoint);
                ServerSocket.Listen(BACKLOG);

                Console.ForegroundColor = ConsoleColor.Yellow;
                Program.Log.Info("Server socket created...");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            catch (Exception e)
            {
                throw new ApplicationException("Could not Create socket, check to make sure not duplicating port", e);
            }

            IsAlive = true;

            ServerSocket.BeginAccept(new AsyncCallback(AcceptCallback), ServerSocket);

            ServerProcThread = new Thread(MainLoop);
            ServerProcThread.Start();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Program.Log.Info("Server thread has started.");
            Console.ForegroundColor = ConsoleColor.Gray;

            return true;
        }

        public void StopServer()
        {
            Program.Log.Info("Server thread stopping...");
            IsAlive = false;
            ServerProcThread.Join();
            Program.Log.Info("Server thread has stopped.");
        }
        public Channel FindChannel(string name)
        {
            return IrcChannels.Values.Where(channel => channel.Name != null && channel.Name.Equals(name)).FirstOrDefault();
        }

        public Client FindClient(string nick)
        {
            lock (IrcClients)
            {
                return IrcClients.Values.Where(client => client.GetNick() != null && client.GetNick().Equals(nick)).FirstOrDefault();
            }
        }

        public void SendLineToAll(string line)
        {
            List<Client> clients;
            lock (IrcClients)
            {
                clients = new List<Client>(IrcClients.Values);
            }
            foreach (Client client in clients)
            {
                client.SendLine(line, client.IsEncrypted());
            }
        }

        private void MainLoop()
        {
            List<Socket> socketsToRemove = [];

            while (IsAlive)
            {
                List<Socket> socketList;
                List<Socket> errList;

                //Poll the sockets for disconnects
                lock (IrcClients)
                {
                    foreach (Socket s in IrcClients.Keys)
                    {
                        if ((s.Poll(0, SelectMode.SelectRead) && (s.Available == 0)) || !s.Connected)
                            socketsToRemove.Add(s);
                    }

                    socketList = new List<Socket>(IrcClients.Keys);
                    errList = new List<Socket>(IrcClients.Keys);
                }

                if (socketList.Count == 0)
                {
                    Thread.Sleep(50); // Nothing to poll; don't spin a core
                    continue;
                }

                try
                {
                    Socket.Select(socketList, null, errList, 100_000); // Arg is microseconds: 100ms
                }
                catch (ObjectDisposedException)
                {
                    continue; // A snapshotted socket was closed mid-iteration; re-snapshot
                }

                // Remove errored sockets and process opened ones
                socketsToRemove.AddRange(errList);
                foreach (Socket s in socketList)
                {
                    try
                    {
                        Client client;
                        lock (IrcClients)
                        {
                            if (!IrcClients.TryGetValue(s, out client))
                                continue;
                        }

                        int bytesRead = s.Receive(client.RecvBuffer, client.LastPartialSize, client.RecvBuffer.Length - client.LastPartialSize, SocketFlags.None);

                        if (bytesRead > 0)
                        {
                            bytesRead += client.LastPartialSize;
                            Span<byte> receive = client.RecvBuffer.AsSpan()[..bytesRead];

                            while (true)
                            {
                                // Get next msg length
                                int length = FindNextNewline(receive);
                                if (length == -1)
                                    break;

                                // Grab message and decrypt
                                if (client.IsEncrypted())
                                    client.GetCrypto().SqCrypt64(receive[..length], length, 1);

                                // Verify checksum before turning into a string:
                                // ShiftJIS is lossy for some client byte sequences, so it can't happen after decoding
                                var line = receive[..(length - 2)];
                                int msgResult = -1;
                                if (SqCrypto.VerifyCheckSum(line))
                                {
                                    string message = Encoding.GetEncoding("shift_jis").GetString(line[..^4]);
                                    msgResult = IrcMessageHandler.ParseMessage(client, message);
                                }
                                else
                                    Program.Log.Info($"Received invalid checksum from {client}, kicking them.");

                                // Kick bad clients
                                if (msgResult != 0 && !client.GetIsBot())
                                {
                                    client.Disconnect();
                                    socketsToRemove.Add(s);
                                    receive = Span<byte>.Empty;
                                    break;
                                }

                                // Parse more data
                                receive = receive[length..];
                            }

                            // Keep any partial line (still encrypted) for the next read
                            receive.CopyTo(client.RecvBuffer);
                            client.LastPartialSize = receive.Length;

                            // Data fills the whole buffer, line can never complete; drop the client
                            if (client.LastPartialSize == client.RecvBuffer.Length)
                            {
                                client.LastPartialSize = 0;
                                client.Disconnect();
                                socketsToRemove.Add(s);
                            }
                        }
                    }
                    catch (SocketException)
                    {
                        socketsToRemove.Add(s);
                    }
                    catch (ObjectDisposedException)
                    {
                        socketsToRemove.Add(s);
                    }
                    catch (Exception e)
                    {
                        Program.Log.Trace(e, "Unhandled error processing an IRC client");
                        socketsToRemove.Add(s);
                    }
                }

                //Ping all connected clients every 30s
                uint curTime = Utils.UnixTimeStampUTC();
                if (curTime - LastPingTime > 30)
                {
                    LastPingTime = curTime;
                    List<Client> clientsToPing;
                    lock (IrcClients)
                    {
                        clientsToPing = new List<Client>(IrcClients.Values);
                    }
                    foreach (Client c in clientsToPing)
                    {
                        IrcReplies.Ping(c, "Za9ABLN");
                    }
                }

                //Clean up all removed sockets
                foreach (Socket s in socketsToRemove)
                {
                    Client disconnectedClient;
                    lock (IrcClients)
                    {
                        if (!IrcClients.TryGetValue(s, out disconnectedClient))
                            continue;

                        //Remove socket
                        IrcClients.Remove(s);
                    }

                    if (disconnectedClient.GetNick() != null)
                    {
                        disconnectedClient.Disconnect();
                    }

                    Program.Log.Info($"{disconnectedClient} has disconnected...");
                    s.Close();
                }

                socketsToRemove.Clear();
            }

            lock (IrcClients)
            {
                foreach (Socket s in IrcClients.Keys)
                {
                    IrcClients[s].Disconnect();
                    s.Close();
                }
                IrcClients.Clear();
            }

            ServerSocket.Close();
            GC.Collect();
        }

        private void AcceptCallback(IAsyncResult result)
        {
            // Try to accept the connection
            Socket clientSocket = null;
            try
            {
                clientSocket = ServerSocket.EndAccept(result);
            }
            catch (ObjectDisposedException) { return; } // Listen socket closed; server is shutting down
            catch (SocketException) { }                 // Pending connection reset before accept completed; drop it
            finally
            {
                if (IsAlive)
                {
                    try { ServerSocket.BeginAccept(new AsyncCallback(AcceptCallback), ServerSocket); }
                    catch (ObjectDisposedException) { }
                }
            }

            if (clientSocket == null)
                return;

            try
            {
                Client client = new(this, clientSocket);

                lock (IrcClients)
                {
                    // Server has already shutdown, close this socket.
                    if (!IsAlive)
                    {
                        clientSocket.Close();
                        return;
                    }
                    IrcClients[clientSocket] = client;
                }

                client.DoInit();
            }
            catch (Exception e)
            {
                // Client died between accept and init
                Program.Log.Trace(e.Message);
                lock (IrcClients)
                {
                    IrcClients.Remove(clientSocket);
                }
                clientSocket.Close();
            }
        }

        public void SetNotifyClient(Client client)
        {
            PolProNotifyClient = client;
        }

        public Client GetNotifyClient()
        {
            return PolProNotifyClient;
        }

        public List<string> GetOnlineUserNames()
        {
            lock (IrcClients)
            {
                return IrcClients.Values.Select(c => c.ToString()).ToList();
            }
        }

        public Client CheckForSameSession(Client client)
        {
            lock (IrcClients)
            {
                return IrcClients.Values.Where(c => !c.Equals(client) && (c.GetPolID()?.Equals(client.GetPolID() ?? "") ?? false)).FirstOrDefault();
            }
        }

        private static int FindNextNewline(Span<byte> span)
        {
            for (int i = 0; i < span.Length-1; i++)
            {
                if (span[i] == '\r' && span[i+1] == '\n')
                    return i + 2;
            }
            return -1;
        }
    }
    
}
