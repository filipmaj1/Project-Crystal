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
using Crystal.POLPatch.Packets.Receive;
using Crystal.POLPatch.Packets.Send;

namespace Crystal.POLPatch
{
    class PacketProcessor(PatchRetriever Retriever)
    {
        public void ProcessPacket(ClientConnection client, BasePacket packet)
        {
#if DEBUG
            Program.Log.Debug($"Received packet from {client}:\n");
            packet.DebugPrintPacket();
#endif

            switch (packet.header.Opcode)
            {
                case StatusRequest.OPCODE:
                    {
                        StatusRequest request = new(packet.data);
                        StatusResponse response = Retriever.GetStatusResponsePacket(request.HardwareId, request.ApplicationId, client.IpAddress);

                        if (response != null)
                        {
                            client.QueuePacket(response.GetDataBytes());
                            client.FlushQueuedSendPackets();

                            Program.Log.Info($"[{client}][{request.HardwareId}{request.ApplicationId}] => Get Status");
                        }
                        else
                            client.SendError();

                        break;
                    }
                case DownloadRequest.OPCODE:
                    {
                        DownloadRequest request = new(packet.data);
                        DownloadResponse response = Retriever.GetDownloadPacket(request.HardwareId, request.ApplicationId, request.Path, request.ChunkCounter, request.ChunkSize);

                        if (response != null)
                        {
                            client.QueuePacket(response.GetDataBytes());
                            client.FlushQueuedSendPackets();

                            Program.Log.Info($"[{client}][{request.HardwareId}{request.ApplicationId}] => Download[{request.ChunkCounter}]: \"{request.Path}\"");
                        }
                        else
                        {
                            Program.Log.Error($"[{client}][{request.HardwareId}{request.ApplicationId}] => MISSING: {request.Path}!");
                            client.SendError();
                        }

                        break;
                    }
                case AskNewestRequest.OPCODE:
                    {
                        AskNewestRequest request = new(packet.data);
                        AskNewestResponse response = Retriever.GetNewestPacket(request.HardwareId, request.ApplicationId, request.VersionData, client.IpAddress);

                        if (response != null)
                        {
                            client.QueuePacket(response.GetDataBytes());
                            client.FlushQueuedSendPackets();

                            Program.Log.Info($"[{client}][{request.HardwareId}{request.ApplicationId}] => Latest Version - \"{request.VersionString}\"");
                        }
                        else
                        {
                            Program.Log.Info($"[{client}][{request.HardwareId}{request.ApplicationId}] => Failed to retrieve latest.");
                            client.SendError();
                        }

                        break;
                    }
                case ShutdownRequest.OPCODE:
                    {
                        client.Disconnect();
                        break;
                    }
                default:                    
                    break;
            }
        }
    }
}
