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
using Crystal.Common.MiniGame;
using Crystal.Common.MiniGame.Packets;
using Crystal.Mahjong.Packets;
using Org.BouncyCastle.Asn1.IsisMtt.X509;
using System.ComponentModel.Design;
using System.Text;

namespace Crystal.Mahjong.Tables
{
    public class Table
    {
        const int MAX_TABLE_MEMBERS = 3;

        // Table Properties
        public readonly ulong Id;
        private readonly uint Number;
        public readonly string IrcChannel;
        private readonly string ParentRoomIrcId;
        public readonly PolIrcClient TableLobbyChannel;
        public byte[] GameData = new byte[0x40];

        // Table User Properties
        private byte ReservationTimer = 10;
        private bool CanSpectate = true;
        private bool HasPassword = false;
        private bool RulesSetByMaster = false;
        private byte CommentId = 1;
        private byte Restrictions = 0;

        // Table State
        public byte State;
        private ulong MasterPolId;
        private TablePlayer[] Members = new TablePlayer[MAX_TABLE_MEMBERS];
        private uint NumMembers = 0;

        public ulong CreateTableId(ulong roomId, uint tableNumber)
        {
            ulong roomIdDecrypted = Mg.MjKey(roomId);
            char[] polIdData = SqCrypto.PolIdToPolProData(roomIdDecrypted).ToCharArray();
            char[] hex = (tableNumber + 1).ToString("X2").ToCharArray();
            polIdData[6] = hex[0];
            polIdData[7] = hex[1];
            ulong tableId = SqCrypto.PolProDataToPolId(new(polIdData), 0, 0);
            return Mg.MjKey(tableId);
        }

        public string CreateIrcChannel(ulong tableId)
        {
            ulong tableIdDecrypted = Mg.MjKey(tableId);
            string strPolData = SqCrypto.PolIdToPolProData(tableIdDecrypted);
            return $"#TABLE{strPolData.Substring(2)}";
        }

        public Table(ulong roomId, uint tableNumber)
        {
            Id = CreateTableId(roomId, tableNumber);
            Number = tableNumber;
            IrcChannel = CreateIrcChannel(Id);
            TableLobbyChannel = new PolIrcClient($"Tm-Table-{Number}", Id, OnReceiveMsg);

            ParentRoomIrcId = SqCrypto.PolIdToPolProData(Mg.MjKey(roomId));

            ResetTable();
            UpdateTable();
        }

        public bool Init()
        {
            // Start IRC Client
            TableLobbyChannel.Connect("127.0.0.1", MjServer.JANG_IRC_PORT);
            return true;
        }

        public void ResetTable()
        {
            MasterPolId = 0;
            State = 0;
        }

        private void OnReceiveMsg(string from, string data)
        {
            Program.Log.Debug($"Received Msg [{from}]: {data}");
            
            MgPacket mgPacket = MgPacket.Parse(data);
            if (mgPacket == null)
                return;

            // Is initializing user?
            if (mgPacket.Type == PacketType.Game)
            {
                MjPacket packet = MjPacket.FromIrcStr(mgPacket.GetGamePacketData());

                Program.Log.Debug($"Game Packet - Opcode: {packet.Header.Command}.\n{Utils.ByteArrayToHex(packet.Data)}");
            }
        }

        private void UpdateTable()
        {
            // Setting Up -> Standing By
            if (State == 0)
                State = 1;

            // Check master
            lock (Members)
            {
                if (NumMembers != 0 && MasterPolId != Members[0].Id)
                    MasterPolId = Members[0].Id;
                else if (NumMembers == 0)
                    MasterPolId = 0;
            }

            // Reset table if empty
            if (NumMembers == 0)
                ResetTable();

            UpdateGameDataBuffer();
            SendMemberListToTable();
            if (TableLobbyChannel.IsReady())
                SendUpdateMsgToRoom();
        }

        private void UpdateGameDataBuffer()
        {
            string gameDataStr = $"{Number:D5},{0:D5},{3:D3},{0:D3},{ReservationTimer:D3},{(CanSpectate ? 1 : 0):D1},{0:D2},{0:D2},{0:D1},{(HasPassword ? 1 : 0):D1},{(RulesSetByMaster ? 1 : 0):D1},{CommentId:D1},{0:D3},{Restrictions:D1}";
            Encoding.ASCII.GetBytes(gameDataStr).CopyTo(GameData, 0);
        }

        private bool IsReady()
        {
            return NumMembers > 1 && MasterPolId != 0;
        }

        public void SendMemberListToTable()
        {
            for (int i = 0; i < NumMembers; i++)
            {
                //string target = SqCrypto.PolIdToPolProData(MgUtils.MjKey(Members[i].Id));
                //SendMemberList(target);
            }
        }

        private int AddPlayer(TablePlayer player)
        {
            lock (Members)
            {
                // Max Check
                if (NumMembers == 3)
                    return -32870;

                // Weird case?
                if (NumMembers == 0 && State != 0)
                    return -32872;

                // Player Null Check
                if (player == null)
                    return -32871;

                // Check if Player already here
                for (int i = 0; i < NumMembers; i++)
                {
                    if (Members[i].Id == player.Id)
                        return -32869;
                }

                // Add Player and return Good Answer
                Members[NumMembers] = player;
                if (NumMembers++ == 0)
                    return 1; // Init table
                else
                    return 2; // Already existing table
            }
        }

        private int RemovePlayer(ulong id)
        {
            lock (Members)
            {
                // Sanity Check
                if (NumMembers == 0)
                    return -1;

                // Find player and set null. Once done, move members up.
                TablePlayer oldMaster = null;

                bool deleted = false;
                for (int i = 0; i < NumMembers; i++)
                {
                    // Finding player
                    if (!deleted && Members[i].Id == id)
                    {
                        // Player was master
                        if (i == 0)
                            oldMaster = Members[i];
                        Members[i] = null;
                        deleted = true;
                        continue;
                    }

                    // Moving up
                    if (deleted)
                    {
                        Members[i - 1] = Members[i];
                        Members[i] = null;
                    }
                }
                NumMembers--;

               // if (oldMaster != null && NumMembers > 0)
                    //SendGameOwnerToNewMaster(oldMaster.Name);
            }

            return 1;
        }

        private void SendUpdateMsgToRoom()
        {
            //string updateMsg = new TmMgPacketBuilder(PacketType.Lobby).AddTableDataCommand(this).Build();
            //TableLobbyChannel.Notice(ParentRoomIrcId, updateMsg);
        }

        public MgTable GetMgTable()
        {
            return new(Id, 0, 0, 0, State, IrcChannel, GameData);
        }
    }
}
