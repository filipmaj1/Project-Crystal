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
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Crystal.FrontMissionOnline
{
    class PacketProcessor
    {
        private static uint SessionIdCounter = 1;
        private readonly Server Server;

        public PacketProcessor(Server server)
        {
            Server = server;
        }

        public int ProcessPacket(Client client, ushort commandId, uint sessionId, byte[] payload)
        {
            if (commandId != 0x198)
                Program.Log.Debug($"\nGot TCP Pkt - CommandId: 0x{commandId:X2} - sessionId: 0x{sessionId:X4}. \n--- Packet Data ---\n{Utils.ByteArrayToHex(payload)}");
            byte[] data;

            switch (commandId)
            {
                // POL Authentication Code
                case 0x321:
                    { 
                    if (Server.ServerPort == 61300)
                    {
                        SqCrypto.DecryptAuthPassword(payload, out byte[] authHash, out uint ip, out ushort port);
                        byte[]? encryptionKey = Database.GetPlayonlineRandomValue(authHash);
                        uint newId = SessionIdCounter++;
                        Database.CreateGameSession(newId, encryptionKey);

                        Program.Log.Info($"Set encryption key to: {BitConverter.ToString(encryptionKey).Replace("-", "")}");

                        data = File.ReadAllBytes("./322_61300");
                        MemoryMarshal.Write(data.AsSpan()[0xC..], ref newId);
                        client.SendPacket(0x322, sessionId, data);
                    }
                    break;
                    }
                //
                case 0x15B:
                    {
                        uint gameId = BitConverter.ToUInt32(payload, 0);
                        if (Database.GetGameSession(gameId, out byte[] encryptionKey))
                        {
                            client.StartEncryption(encryptionKey);
                            client.SendPacket(0x001, sessionId, isEncrypted: false);
                        }
                        else
                            client.SendPacket(0x000, sessionId, null);
                        break;
                    }
                // Set server time
                case 0x13B:
                    {
                        data = new byte[0x8];
                        ulong unixTimestamp = Utils.MilisUnixTimeStampUTC(DateTime.UtcNow);
                        uint sTime = (uint)(unixTimestamp / 1000);
                        uint msTime = (uint)((unixTimestamp % 1000) * 1000);

                        MemoryMarshal.Write(data, ref sTime);
                        MemoryMarshal.Write(data.AsSpan()[4..], ref msTime);
                        client.SendPacket(0x13C, sessionId, data);
                        break;
                    }
                // Ping
                case 0x198:
                    {
                        data = new byte[0x10];
                        ulong unixTimestamp = Utils.MilisUnixTimeStampUTC(DateTime.UtcNow);
                        uint sTime = (uint)(unixTimestamp / 1000);
                        uint msTime = (uint)((unixTimestamp % 1000) * 1000);
                        uint lagSTime = sTime;
                        uint lagMsTime = msTime;

                        MemoryMarshal.Write(data, ref sTime);
                        MemoryMarshal.Write(data.AsSpan()[0x4..], ref msTime);
                        MemoryMarshal.Write(data.AsSpan()[0x8..], ref lagSTime);
                        MemoryMarshal.Write(data.AsSpan()[0xC..], ref lagMsTime);

                        client.SendPacket(0x199, sessionId, data);
                        break;
                    }
                // Create Character
                case 0x19C:
                    data = File.ReadAllBytes("./19D_startCreateChara");
                    client.SendPacket(0x19D, sessionId, data);
                    break;
                // Create Character - Confirm Name
                case 0x177:
                    client.SendPacket(0x001, sessionId);
                    break;
                // Create Character - Submit
                case 0x13E:
                    client.SendPacket(0x001, sessionId);
                    break;
                // Delete Character
                case 0x13F:
                    client.SendPacket(0x001, sessionId);
                    break;
                // Change Nations
                case 0x1AB:
                    client.SendPacket(0x001, sessionId);
                    break;
                // Change Nations Submit
                case 0x1AA:
                    client.SendPacket(0x001, sessionId);
                    break;
                // Get Character Data
                case 0x12E:
                    data = File.ReadAllBytes("./12F_charaData");
                    client.SendPacket(0x12F, sessionId, data);
                    break;
                case 0x130:
                    data = File.ReadAllBytes("./packets/test");
                    client.SendPacket(0x14a, sessionId, data);
                    break;
                case 0x154: // Query Groups
                    data = File.ReadAllBytes("./packets/query_groups");
                    client.SendPacket(0x155, sessionId, data);
                    break;
                case 0x132: // Query Items
                    data = File.ReadAllBytes("./packets/query_items");
                    client.SendPacket(0x133, sessionId, data);
                    break;
                case 0x165: // All Setup
                    data = File.ReadAllBytes("./packets/all_setup");
                    client.SendPacket(0x166, sessionId, data);
                    break;
                case 0x150: // Login Area
                    data = File.ReadAllBytes("./packets/login_area");
                    client.SendPacket(0x153, sessionId, data);
                    break;
                case 0x16D: // Change Area
                    data = File.ReadAllBytes("./packets/login_area");
                    client.SendPacket(0x153, sessionId, data);
                    break;
                case 0x16E: // Get Areas
                    data = File.ReadAllBytes("./packets/16f_lobbylist");
                    client.SendPacket(0x16f, sessionId, data);
                    break;
                case 0x182: // Get PlayTime
                    data = File.ReadAllBytes("./packets/time");
                    client.SendPacket(0x183, sessionId, data);
                    break;
                case 0x152: // Logout?
                    client.SendPacket(0x001, sessionId);
                    break;
            }

            return 1;
        }
    }
}
