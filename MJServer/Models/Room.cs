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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Crystal.Common.MiniGame;
using Crystal.Common.MiniGame.Packets;
using Crystal.Common.MiniGame.PolFileSystem.FileEntries;
using Crystal.Common.PolFileSystem;
using static Crystal.Mahjong.MjConstants;
using Crystal.Mahjong.Packets;

namespace Crystal.Mahjong.Models
{
    public class Room
    {
        // Room Properties
        public readonly ulong Id;
        public readonly byte Domain;
        public readonly ushort Volume;

        public readonly ushort FileIndex;
        public readonly string IrcChannel;
        public readonly string Name;
        public readonly int MaxTables;
        public readonly int MaxPlayers;
        private byte[] GameData = new byte[0x80];

        // Table State
        private PolFile RoomListFile;
        private PolFile PlayerFile;
        private PolFile TableFile;
        private PolIrcClient IrcClient;
        private readonly List<MgPlayer> Users;
        public readonly Table[] Tables;

        // Update Tracker
        private const int UpdMaxQueue = 0x20;
        private int UpdCounter = 0;
        private int UpdQueueStart = 0;
        private Queue<string> UpdQueue = new(UpdMaxQueue);
        private object PtlLock = new object();

        private readonly Zone ParentZone;

        public int NumPlayers { get { return Users.Count; } }
        public int NumTables { get { return Tables.Length; } }
        public int NumClosedTables
        {
            get
            {
                int closedCount = 0;
                for (int i = 0; i < Tables.Length; i++)
                {
                    if (Tables[i].State != 0)
                        closedCount++;
                }
                return closedCount;
            }
        }

        public Room(Zone parentZone, byte domain, ushort volume, ulong roomId, string roomName, int maxTables, int maxPlayers)
        {
            // Setup Room
            Id = Mg.MjKey(SqCrypto.PolProDataToPolId($"MJ{roomId:D4}00", 0, 3));
            Domain = domain;
            Volume = volume;

            Name = roomName;
            IrcChannel = $"#ROOM{roomId:D4}00";
            IrcClient = new PolIrcClient($"Room:{Name}", Id, OnReceiveMsg);
            Users = new(maxPlayers);
            MaxPlayers = maxPlayers;
            
            FileIndex = parentZone.FileIndex;
            ParentZone = parentZone;

            ReadOnlySpan<byte> nameBytes = Mg.Encoding.GetBytes(Name);
            nameBytes.CopyTo(GameData);

            // Setup Tables
            Tables = new Table[maxTables];
            for (uint i = 0; i < maxTables; i++)
                Tables[i] = new Table(Id, i);

        }

        private void OnReceiveMsg(string from, string data)
        {
            if (!data.Contains("DR"))
                Program.Log.Debug($"[{Name}] Got msg: {data}");

            MgPacket lobbyPacket = MgPacket.Parse(data);
            if (lobbyPacket == null)
                return;

            if (lobbyPacket.Type == PacketType.Game)
            {
                var gamePkt = MjPacket.FromIrcStr(lobbyPacket.GetGamePacketData());

                Program.Log.Debug($"Game Packet - Opcode: {gamePkt.Header.Command}.\n{Utils.ByteArrayToHex(gamePkt.Data)}");
            }
            else if (lobbyPacket.Type == PacketType.Lobby)
            {
                // Lobby Enter
                if (lobbyPacket.HasCmd("DE"))
                {
                    MgPlayer player = lobbyPacket.GetCmd("DE").GetPlayer();
                    PlayerUpdate(player, true);

                    string dsResponse = new MgPacketBuilder(PacketType.Lobby).AddCommand("DS", Id).Build();
                    IrcClient.Notice(from, dsResponse);
                }
                // PTL Update Data Request
                else if (lobbyPacket.HasCmd("DR"))
                {
                    lock (UpdQueue)
                    {
                        lobbyPacket.GetCmd("DR").GetParamNumber(0, out int clientUpdCounter);
                        int numUpdates = UpdCounter - clientUpdCounter;

                        // If client up to date, ignore
                        if (numUpdates <= 0)
                            return;
                        // If client is too far behind, reload the PTL file
                        else if (numUpdates > 20)
                        {
                            string doResponse = new MgPacketBuilder(PacketType.Lobby).AddCommand("DO").Build();
                            IrcClient.Notice(from, doResponse);
                        }
                        // Send all updates since sent client counter
                        else
                        {
                            int i = UpdQueueStart;
                            var builder = new MgPacketBuilder(PacketType.Lobby).AddCommand("DD").AddCommand("DN", numUpdates);
                            foreach (string update in UpdQueue)
                            {
                                if (i >= clientUpdCounter)
                                    builder.AddCommand("DC", i + 1).AddRaw(update);
                                i++;
                            }
                            string ddResponse = builder.Build();
                            IrcClient.Notice(from, ddResponse);
                            Program.Log.Debug($"UPD Request ({clientUpdCounter})({numUpdates}): {ddResponse}.");
                        }

                    }
                }
                // PTL Update, Player Clear
                else if (lobbyPacket.HasCmd("PC"))
                {
                    // Find our removed player
                    lobbyPacket.GetCmd("PC").GetParamId(0, out ulong playerId);
                    MgPlayer player = Users.Where((e) => e.Id == playerId).FirstOrDefault();

                    // Delete em
                    if (player != null)
                    {
                        Users.Remove(player);
                        PlayerDelete(player);
                    }
                }
                // PTL Update, Player Update
                else if (lobbyPacket.HasCmd("PD"))
                {
                    // Find our removed player
                    MgPlayer fromPacket = lobbyPacket.GetCmd("PD").GetPlayer();
                   // MgPlayer playerToUpdate = Users.Where((e) => e.Id == fromPacket.Id).FirstOrDefault();
                    //playerToUpdate.Update(fromPacket);
                    PlayerUpdate(fromPacket, false);

                    byte[] pData = MjUtils.MjsConvertStrToPlayerData(fromPacket.GameData);
                    Program.Log.Debug($"\n{Utils.ByteArrayToHex(pData)}");
                }
                // PTL Update, Table Update - (THIS IS SEND BY THE TABLE SERVER)
                else if (lobbyPacket.HasCmd("TD"))
                {
                    // Server sent us a table to update; relay to the room
                    MgTable updatedTable = lobbyPacket.GetCmd("TD").GetTable();
                    TableUpdate(updatedTable);
                }
            }
        }

        public bool Init()
        {
            // Init this room's entry into the roomlist file
            RoomListFile = PolFileSys.OpenFile(POLID_BALANCER, 0, 3, $"b/g/RL{FileIndex:D3}", MgConstants.MAX_ROOMS, MgConstants.ROOM_SIZE, MgConstants.ROOM_DATA_OFFSET, MgConstants.ROOM_COUNT_OFFSET);
            RoomFileEntry roomEntry = new()
            {
                Id = Id,
                NumPlayers = 0,
                NumPlayers2 = 0,
                NumClosedTables = 0,
                JangIsValid = 1,
                Domain = Domain,
                Volume = Volume,
                GameData = GameData,
                IrcChannel = IrcChannel
            };
            ReadOnlySpan<byte> data = MemoryMarshal.Cast<RoomFileEntry, byte>(new ReadOnlySpan<RoomFileEntry>(roomEntry));
            RoomListFile.InsertNewEntry(data);

            // Init the PTL file that stores Player + Table data. File is access is split across these two
            // file pointers.
            PlayerFile = PolFileSys.OpenFile(Mg.MjKey(Id), Volume, Domain, "b/g/PTL", MgConstants.MAX_PLAYERS, MgConstants.PLAYER_SIZE, MgConstants.PLAYER_DATA_OFFSET, MgConstants.PLAYER_COUNT_OFFSET);
            TableFile = PolFileSys.OpenFile(Mg.MjKey(Id), Volume, Domain, "b/g/PTL", MgConstants.MAX_TABLES, MgConstants.TABLE_SIZE, MgConstants.TABLE_DATA_OFFSET, MgConstants.TABLE_COUNT_OFFSET);
            PlayerFile.InitFile(0xC050);

            // Write all the tables into the PTL file
            for (int i = 0; i < Tables.Length; i++)
            {
                Table table = Tables[i];
                var tableEntry = TableFileEntry.Create(table.GetMgTable());
                var tableData = MemoryMarshal.Cast<TableFileEntry, byte>(new ReadOnlySpan<TableFileEntry>(tableEntry));
                TableFile.InsertNewEntry(tableData);
            }

            // Write the updBuff start val
            PolFile file = PolFileSys.OpenFileDirect(Mg.MjKey(Id), Volume, Domain, "b/g/PTL", out object fileLock);
            lock (fileLock)
            {
                var fileStream = file.GetDirectFileStream();
                fileStream.Seek(0x40, System.IO.SeekOrigin.Begin);
                fileStream.Write(BitConverter.GetBytes(0));
                fileStream.Close();
            }
            PolFileSys.CloseFile(file);

            // Start IRC Client
            IrcClient.Connect("127.0.0.1", MgConstants.MG_IRC_PORT);
            while (!IrcClient.IsAuthenticated()) ;
            
            for (int i = 0; i < Tables.Length; i++)
                Tables[i].Init();

            return true;
        }

        public void PlayerUpdate(MgPlayer player, bool isNew)
        {
            // Add player to the list
            if (isNew)
                Users.Add(player);

            var playerEntry = PlayerFileEntry.Create(player);
            var data = MemoryMarshal.Cast<PlayerFileEntry, byte>(new ReadOnlySpan<PlayerFileEntry>(playerEntry));
            if (isNew)
                PlayerFile.InsertNewEntry(data);
            else
                PlayerFile.UpdateEntryWithId(player.Id, data);

            // Add to the update list
            var pdUpdCommand = player.ToUpdCmd();
            AddUpd(pdUpdCommand);

            UpdateFiles();
        }

        public void PlayerDelete(MgPlayer player)
        {
            // Remove player from list
            Users.Remove(player);
            PlayerFile.DeleteEntryWithId(player.Id);

            // Add to the update list
            var pcUpdCommand = new MgPacketBuilder()
                .AddCommand("PC", player.Id)
                .Build();
            AddUpd(pcUpdCommand);

            UpdateFiles();
        }

        public void TableUpdate(MgTable table)
        {
            // Update file entry
            var tableEntry = TableFileEntry.Create(table);
            var data = MemoryMarshal.Cast<TableFileEntry, byte>(new ReadOnlySpan<TableFileEntry>(tableEntry));
            TableFile.UpdateEntryWithId(table.Id, data);

            // Add to the update list
            var tdUpdCommand = table.ToUpdCmd();
            AddUpd(tdUpdCommand);

            UpdateFiles();
        }

        public void AddUpd(string msg)
        {
            lock (UpdQueue)
            {
                if (UpdQueue.Count >= UpdMaxQueue)
                    UpdQueue.Dequeue();
                UpdQueue.Enqueue(msg);
                UpdCounter++;
                if (UpdCounter >= UpdMaxQueue)
                    UpdQueueStart++;

                lock (PtlLock)
                { 
                    PolFile file = PolFileSys.OpenFileDirect(Mg.MjKey(Id), Volume, Domain, "b/g/PTL", out object fileLock);

                    lock (fileLock)
                    {
                        var fileStream = file.GetDirectFileStream();
                        fileStream.Seek(0x40, System.IO.SeekOrigin.Begin);
                        fileStream.Write(BitConverter.GetBytes(UpdCounter));
                        fileStream.Close();
                    }

                    PolFileSys.CloseFile(file);
                }

                Program.Log.Debug($"UPD Msg Added ({UpdQueueStart})({UpdCounter}): {msg}.");
            }
        }

        public void UpdateFiles()
        {
            // Update this room's file entry
            RoomFileEntry roomEntry = new()
            {
                Id = Id,
                NumPlayers = (uint)NumPlayers,
                NumPlayers2 = 0,
                NumClosedTables = (uint)NumClosedTables,
                JangIsValid = 1,
                Domain = Domain,
                Volume = Volume,
                GameData = GameData,
                IrcChannel = IrcChannel
            };
            ReadOnlySpan<byte> data = MemoryMarshal.Cast<RoomFileEntry, byte>(new ReadOnlySpan<RoomFileEntry>(roomEntry));
            RoomListFile.UpdateEntryWithId(Id, data);

            // Tell the zone to do the same
            ParentZone.UpdatePlayerCount();
        }
    }
}
