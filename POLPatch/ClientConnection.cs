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
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Crystal.POLPatch.Packets.Send;

namespace Crystal.POLPatch
{
    class ClientConnection
    {
        private const int SEND_TIMEOUT_MS = 10000;
        public Socket Socket;
        public byte[] Buffer = new byte[0xffff];
        public BlockingCollection<BasePacket> SendPacketQueue = new(100);
        public int LastPartialSize = 0;

        public readonly string IpAddress;
        public readonly int Port;

        public ClientConnection(Socket socket)
        {
            this.Socket = socket;
            this.Socket.SendTimeout = SEND_TIMEOUT_MS;
            var endpoint = (socket.RemoteEndPoint as IPEndPoint);
            IpAddress = endpoint.Address.ToString();
            Port = endpoint.Port;
        }

        public void QueuePacket(BasePacket packet)
        {
#if DEBUG_SHOW_PACKETS
            Program.Log.Debug($"Sent packet to {this}:\n");
            packet.DebugPrintPacket();
#endif
            if (SendPacketQueue.Count == SendPacketQueue.BoundedCapacity - 1)
                FlushQueuedSendPackets();

            SendPacketQueue.Add(packet);
        }

        public void FlushQueuedSendPackets()
        {
            if (!Socket.Connected)
                return;

            while (SendPacketQueue.Count > 0)
            {
                BasePacket packet = SendPacketQueue.Take();
                byte[] packetBytes = packet.GetPacketBytes();
                try {
                    Socket.Send(packetBytes);
                }
                catch(Exception e)
                {
                    Program.Log.Error(e, $"[{this}] => Disconnected unexpectedly");
                    break;
                }
            }
        }

        public void SendError()
        {
            ErrorResponse response = new();
            QueuePacket(response.GetDataBytes());
            FlushQueuedSendPackets();
            Disconnect();
        }

        public void Disconnect()
        {
            try
            {
                Socket.Shutdown(SocketShutdown.Both);
                Socket.Disconnect(false);
            }
            catch (SocketException) { }        // Already reset by the peer
            catch (ObjectDisposedException) { }
            finally
            {
                Socket.Close();
            }
        }

        public override string ToString()
        {
            return $"{IpAddress}";
        }
    }
}
