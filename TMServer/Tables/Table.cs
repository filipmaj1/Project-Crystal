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
using Crystal.TetraMaster.Packets;
using static Crystal.TetraMaster.Packets.TmPacket;
using Crystal.TetraMaster.TmGame;
using Crystal.Common.MiniGame;
using Crystal.Common.MiniGame.Packets;

namespace Crystal.TetraMaster.Tables
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
        public byte[] StaticAsciiData = new byte[0x40];

        // Table State
        public byte State;
        private ulong MasterPolId;
        private TablePlayer[] Members = new TablePlayer[MAX_TABLE_MEMBERS];
        private uint NumMembers = 0;

        // Table Settings
        private bool CanModifyRules;
        private TableRules ForcedRules;
        private TableRules Rules;
        private TableRestrictions Restrictions = new(1, 1, 999, 1, 100, 1, 0, null);

        // The Game
        Game CurrentGame = null;

        public ulong CreateTableId(ulong roomId, uint tableNumber)
        {
            ulong roomIdDecrypted = Mg.TmKey(roomId);
            char[] polIdData = SqCrypto.PolIdToPolProData(roomIdDecrypted).ToCharArray();
            char[] hex = (tableNumber + 1).ToString("X2").ToCharArray();
            polIdData[6] = hex[0];
            polIdData[7] = hex[1];
            ulong tableId = SqCrypto.PolProDataToPolId(new(polIdData), 0, 0);
            return Mg.TmKey(tableId);
        }

        public string CreateIrcChannel(ulong tableId)
        {
            ulong tableIdDecrypted = Mg.TmKey(tableId);
            string strPolData = SqCrypto.PolIdToPolProData(tableIdDecrypted);
            return $"#TABLE{strPolData.Substring(2)}";
        }

        public Table(ulong roomId, uint tableNumber, TableRules rules, bool canModifyRules)
        {
            Id = CreateTableId(roomId, tableNumber);
            Number = tableNumber;
            IrcChannel = CreateIrcChannel(Id);
            TableLobbyChannel = new PolIrcClient($"Tm-Table-{Number}", Id, OnReceiveMsg);

            if (rules != null)
            {
                CanModifyRules = false;
                Rules = rules;
            }
            else
                CanModifyRules = true;

            ForcedRules = rules;

            ParentRoomIrcId = SqCrypto.PolIdToPolProData(Mg.TmKey(roomId));

            Utils.ClrASCII(StaticAsciiData);
            ResetTable();
            UpdateTable();
        }

        public bool Init()
        {
            // Start IRC Client
            TableLobbyChannel.Connect("127.0.0.1", MgConstants.MG_IRC_PORT);
            return true;
        }

        public void ResetTable()
        {
            MasterPolId = 0;
            State = 0;
            Rules = ForcedRules;
            Restrictions = new(1, 1, 999, 1, 100, 1, 0, null);
        }

        private void OnReceiveMsg(string from, string data)
        {
            MgPacket mgPacket = MgPacket.Parse(data);

            if (mgPacket == null)
                return;

            // Is initializing user?
            if (mgPacket.Type == PacketType.Game)
            {
                TmPacket packet = new(mgPacket.GetGamePacketData());
                ulong tableIdDecrypted = Mg.TmKey(Id);
                string strPolData = SqCrypto.PolIdToPolProData(tableIdDecrypted);
                Program.Log.Info($"Table {strPolData} got msg: {data}");
                    
                //Game Opcodes
                if (packet.CatCode == 0x43 && CurrentGame != null)
                    CurrentGame.ReceivePacket(from, packet);

                // Table enter request
                if (packet.HasCommand("GameEN"))
                {
                    /*
                     * Errors:
                     * -32872: You cannot make a reservation, since all reservations were cancelled.
                     * -32871: Cannot make a reserveration.
                     * -32870: You cannot make a reservation, since this table is full.
                     * -32869: Already registered.
                     */

                    // Grab User
                    TmGameCommand init = packet.GetCommand("GameEN");
                    init.GetParamId("NN", 0, out ulong playerPolId);
                    playerPolId = Mg.TmKey(playerPolId);
                    init.GetParamNumber("HID", 0, out int hid);
                    init.GetParamNumber("Dm", 0, out int domain);
                    init.GetParamNumber("Vol", 0, out int volume);
                    string charaName = init.GetParamHexStr("CN", 0);
                    string handleName = init.GetParamHexStr("HN", 0);
                    init.GetParamNumber("L", 0, out int language);

                    // Setup User and Update
                    int answer;
                    TablePlayer enteringPlayer = TableDb.LoadTablePlayer(playerPolId);
                    if (enteringPlayer != null)
                        answer = AddPlayer(enteringPlayer);
                    else
                        answer = -32871;

                    // Response
                    string response = new TmPacketBuilder(0x41, 0x02, 0x00)
                        .Command(new TmGameCommandBuilder("GameEA")
                            .Param("EN", answer).End()
                        ).Build();
                    TableLobbyChannel.Notice(from, response);

                    UpdateTable();
                }
                // Table enter COM request
                else if (packet.HasCommand("GameENC"))
                {
                    /*
                     * Errors:
                     * -32872: You cannot make a reservation, since all reservations were cancelled.
                     * -32871: Cannot make a reserveration.
                     * -32870: You cannot make a reservation, since this table is full.
                     * -32869: Already registered.
                     */

                    // Grab User
                    TmGameCommand init = packet.GetCommand("GameENC");
                    init.GetParamId("NN", 0, out ulong playerPolId);
                    playerPolId = Mg.TmKey(playerPolId);
                    init.GetParamNumber("HID", 0, out int hid);
                    init.GetParamNumber("Dm", 0, out int domain);
                    init.GetParamNumber("Vol", 0, out int volume);
                    string charaName = init.GetParamHexStr("CN", 0);
                    string handleName = init.GetParamHexStr("HN", 0);
                    init.GetParamNumber("L", 0, out int language);

                    // Setup User and Update
                    int answer;
                    TablePlayer enteringPlayer = TableDb.LoadTablePlayer(playerPolId);
                    if (enteringPlayer != null)
                        answer = AddPlayer(enteringPlayer);
                    else
                        answer = -32871;

                    if (answer == 1)
                    {
                        if (CurrentGame != null)
                            return;
                        CurrentGame = new Game(this, true, NumMembers);
                    }

                    // Response
                    string response = new TmPacketBuilder(0x41, 0x13, 0x00)
                        .Command(new TmGameCommandBuilder("GameECA")
                            .Param("EN", answer).End()
                        ).Build();
                    TableLobbyChannel.Notice(from, response);

                    UpdateTable();
                }
                else if (packet.HasCommand("CheckJoinTable"))
                {
                    string response = new TmPacketBuilder(0x2E, 0x00, 0x00)
                        .Command(new TmGameCommandBuilder("CheckJoinTable_Ans")
                            .Param("Join", 0)
                            .Param("Mem", 1)
                            .End()
                        ).Build();
                    TableLobbyChannel.Notice(from, response);
                }
                // The master has started the game
                else if (packet.HasCommand("GameReady"))
                {
                    if (CurrentGame != null)
                        return;
                    CurrentGame = new Game(this, false, NumMembers);

                    for (int i = 1; i < NumMembers; i++)
                    {
                        var target = SqCrypto.PolIdToPolProData(Mg.TmKey(Members[i].Id));
                        var startGameMsg = new TmPacketBuilder(0x41, 0x07, 0x00).Build();
                        TableLobbyChannel.Notice(target, startGameMsg);
                    }
                }
                // Enter game request (all players send this)
                else if (packet.HasCommand("Req"))
                {
                    if (CurrentGame == null)
                        return;

                    /* 
                     * Errors:
                     * -32872: Could not start game as the reservation was cancelled.
                     * -32871: Could not start game.
                     * -32870: Could not start game as this table is full.
                     * -32869: Could not start game.
                     */
                    // Enter Game Request
                    TmGameCommand reqPacket = packet.GetCommand("Req");
                    reqPacket.GetParamId("NN", 0, out ulong playerId);
                    reqPacket.GetParamNumber("HID", 0, out int handleId);
                    reqPacket.GetParamNumber("Dm", 0, out int domain);
                    reqPacket.GetParamNumber("Vol", 0, out int volume);
                    string charaName = reqPacket.GetParamHexStr("CN", 0);
                    string handleName = reqPacket.GetParamHexStr("HN", 0);

                    int result = CurrentGame.AddPlayer(playerId, domain, volume, handleId, charaName, handleName);

                    // Response codes
                    int answer = 1;
                    if (result == -1)
                        answer = -32870;
                    else if (result < 0)
                        answer = -32871;

                    // Build and send the PLAYACK packet
                    string resultMsg;
                    var ackCommand = new TmGameCommandBuilder("PLAYACK")
                        .Param("EN", answer)
                        .End();
                    if (answer == 1)
                    {
                        resultMsg = new TmPacketBuilder(0x02, 0x00, 0x00)
                            .Command(ackCommand)
                            .Command(GetTetCommand())
                            .Command(GetTabCommand())
                            .Command(GetMLCommand())
                            .Build();
                    }
                    else
                    {
                        resultMsg = new TmPacketBuilder(0x02, 0x00, 0x00)
                            .Command(ackCommand).Build();
                    }

                    TableLobbyChannel.Notice(from, resultMsg);
                }
                // Client setup itself and is ready to wait
                else if (packet.HasCommand("GameOK"))
                {
                    var resultMsg = new TmPacketBuilder(0x41, 0x9, 0x00)
                        .Command(new TmGameCommandBuilder("GameWa")
                            .Param("Error", 0)
                            .End())
                        .Build();

                    TableLobbyChannel.Notice(from, resultMsg);
                }
                // Table set rules and restrictions
                else if (packet.HasCommand("Tet") && packet.HasCommand("Tab"))
                {
                    // Rules
                    TmGameCommand tet = packet.GetCommand("Tet");
                    tet.GetParamNumber("bm", 0, out int wager);
                    tet.GetParamNumber("du", 0, out int doubleUp);
                    tet.GetParamNumber("st", 0, out int specialTile);
                    tet.GetParamNumber("cb", 0, out int chanceBlock);
                    tet.GetParamNumber("ca", 0, out int rotatingBlock);
                    tet.GetParamNumber("gs", 0, out int quitType);
                    tet.GetParamNumber("tl", 0, out int timerId);
                    Rules = new((uint)wager, doubleUp == 1, specialTile == 1, chanceBlock == 1, rotatingBlock == 1, (byte)quitType, (byte)timerId);

                    // Restrictions
                    TmGameCommand tab = packet.GetCommand("Tab");
                    tab.GetParamNumber("in", 0, out int observeMode);
                    tab.GetParamNumber("lu", 0, out int cardLevelUpper);
                    tab.GetParamNumber("ll", 0, out int cardLevelLower);
                    tab.GetParamNumber("au", 0, out int unkUpper);
                    tab.GetParamNumber("al", 0, out int unkLower);
                    tab.GetParamNumber("co", 0, out int commentId);
                    tab.GetParamNumber("pa", 0, out int pwdEnable);
                    string password = null;
                    if (pwdEnable == 1)
                        password = tab.GetParamHexStr("pw", 0);
                     Restrictions = new((byte)observeMode, (uint)cardLevelLower, (uint)cardLevelUpper, (uint)unkLower, (uint)unkUpper, (byte)commentId, (byte)pwdEnable, password);

                    SendRulesAndRestrictions(from);
                    UpdateTable();
                }
                // Table member list request
                else if (packet.HasCommand("GameM"))
                {
                    TmGameCommand memberListReq = packet.GetCommand("GameM");
                    memberListReq.GetParamId("ID", 0, out ulong requestingPlayerId);
                    SendMemberList(from, requestingPlayerId);
                }
                // Table rule request
                else if (packet.HasCommand("Rule"))
                {
                    SendRulesAndRestrictions(from);
                }
                // Table exit request
                else if (packet.HasCommand("GameQT"))
                {
                    /*
                     * Errors:
                     * -32869: ??? doesn't seem to do anything
                     * -32871: 
                     */

                    // Get the id of the quitting player
                    TmGameCommand quitReq = packet.GetCommand("GameQT");
                    bool isKicked = quitReq.GetParamId("ID", 0, out ulong kickedPlayer);
                    int quitAnswer;

                    if (!isKicked)
                    {
                        ulong requestingPlayerId = Mg.TmKey(SqCrypto.PolProDataToPolId(from, 0, 0));
                        quitAnswer = RemovePlayer(requestingPlayerId);
                    }
                    else
                    {
                        string targetCharName = null;
                        for (int i = 1; i < NumMembers; i++)
                            targetCharName = Members[i]?.Name;
                        SendKickMessageToTable(kickedPlayer, Members[0].Name, targetCharName);
                        quitAnswer = RemovePlayer(kickedPlayer);
                    }

                    // Response
                    UpdateTable();
                    string response = new TmPacketBuilder(0x41, 0x06, 0x00)
                        .Command(new TmGameCommandBuilder("GameQA")
                            .Param("QT", quitAnswer).End()
                        ).Build();
                    TableLobbyChannel.Notice(from, response);
                }
                // Ping/Pong
                else if (packet.HasCommand("Pong"))
                {
                    TmGameCommand pong = packet.GetCommand("Pong");
                    pong.GetParamNumber("Cnt", 0, out int count);
                    pong.GetParamNumber("GM", 0, out int game);
                }
            }
        }

        private void UpdateTable()
        {
            // Setting Up -> Standing By
            if (State == 0 && Rules != null && Restrictions != null)
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

            UpdateStaticASCII();
            SendMemberListToTable();
            if (TableLobbyChannel.IsReady())
                SendUpdateMsgToRoom();
        }

        private void UpdateStaticASCII()
        {
            Utils.SetASCIINum(StaticAsciiData, Number, 0, 4);
            Utils.SetASCIINum(StaticAsciiData, IsReady() ? 1UL : 0UL, 4, 1);
            Utils.SetASCIINum(StaticAsciiData, NumMembers, 5, 1);
            Utils.SetASCIIStr(StaticAsciiData, Restrictions?.Password ?? "********", 6, 0x9);
            Utils.SetASCIINum(StaticAsciiData, MasterPolId, 0xE, 0x10);
            Utils.SetASCIINum(StaticAsciiData, Rules?.Wager ?? 0, 0x1E, 0x4);
            Utils.SetASCIINum(StaticAsciiData, Rules?.GetBitfield() ?? 0, 0x22, 0x2);
            Utils.SetASCIINum(StaticAsciiData, Rules?.QuitMode ?? 0, 0x24, 0x1);
            Utils.SetASCIINum(StaticAsciiData, (ulong)(((Restrictions?.HasPassword ?? 0) << 2) | (Restrictions?.ObserveMode ?? 1)), 0x25, 0x1);
            Utils.SetASCIINum(StaticAsciiData, Restrictions?.CommentId ?? 1, 0x26, 0x1);
            Utils.SetASCIINum(StaticAsciiData, Restrictions?.CardPowerMax ?? 999, 0x27, 0x5);
            Utils.SetASCIINum(StaticAsciiData, Restrictions?.CardPowerMin ?? 0, 0x2C, 0x5);
        }

        private bool IsReady()
        {
            return NumMembers > 1 && MasterPolId != 0;
        }

        public void SendMemberListToTable()
        {
            for (int i = 0; i < NumMembers; i++)
            {
                string target = SqCrypto.PolIdToPolProData(Mg.TmKey(Members[i].Id));
                SendMemberList(target);
            }
        }

        private void SendMemberList(string target, ulong requestingPlayerId = 0)
        {
            TmPacketBuilder resPacket = new(0x41, 0x04, 0x00);
            TmGameCommand gMLCmd = GetMLCommand(requestingPlayerId);
            resPacket.Command(gMLCmd);
            TableLobbyChannel.Notice(target, resPacket.Build());
        }

        private void SendRulesAndRestrictions(string target)
        {
            string response = new TmPacketBuilder(0x13, 0x06, 0x00)
                .Command(GetTetCommand())
                .Command(GetTabCommand())
                .Build();
            TableLobbyChannel.Notice(target, response);
        }

        private void SendKickMessageToTable(ulong kickedId, string masterName, string kickedName)
        {
            for (int i = 0; i < NumMembers; i++)
            {
                string target = SqCrypto.PolIdToPolProData(Mg.TmKey(Members[i].Id));
                string kickMsg = new TmPacketBuilder(0x41, 0x0C, 0x00)
                            .Command(new TmGameCommandBuilder("GameKick")
                                .Param("Kick", kickedId)
                                .ParamHexStr("FN", masterName)
                                .ParamHexStr("QN", kickedName)
                                .End()
                            ).Build();
                TableLobbyChannel.Notice(target, kickMsg);
            }
        }

        private void SendGameOwnerToNewMaster(string oldMaster)
        {
            string target = SqCrypto.PolIdToPolProData(Mg.TmKey(Members[0].Id));
            string gameOwnerMsg = new TmPacketBuilder(0x1A, 0x00, 0x00)
                        .Command(new TmGameCommandBuilder("GameOwner")
                            .Param("NN", Members[0].Id)
                            .ParamHexStr("CN", Members[0].Name)
                            .ParamHexStr("QN", oldMaster)
                            .End()
                        ).Build();
            TableLobbyChannel.Notice(target, gameOwnerMsg);
        }

        public void Test()
        {
            string target = SqCrypto.PolIdToPolProData(Mg.TmKey(Members[1].Id));
            string testMsg = new TmPacketBuilder(0x41, 0x07, 0x00).Build();
            //string testMsg = new TmPacketBuilder(0x2b, 0x00, 0x00).Build();
            TableLobbyChannel.Notice(target, testMsg);
        }

        private TmGameCommand GetTetCommand()
        {
            return new TmGameCommandBuilder("Tet")
                            .Param("in", Restrictions.ObserveMode)
                            .Param("lu", Restrictions.CardPowerMax)
                            .Param("ll", Restrictions.CardPowerMin)
                            .Param("au", Restrictions.UnkMax)
                            .Param("al", Restrictions.UnkMin)
                            .Param("co", Restrictions.CommentId)
                            .Param("pa", Restrictions.HasPassword)
                            .Param("pw", Restrictions.Password)
                            .End();
        }

        private TmGameCommand GetTabCommand()
        {
            return new TmGameCommandBuilder("Tab")
                            .Param("in", Restrictions.ObserveMode)
                            .Param("lu", Restrictions.CardPowerMax)
                            .Param("ll", Restrictions.CardPowerMin)
                            .Param("au", Restrictions.UnkMax)
                            .Param("al", Restrictions.UnkMin)
                            .Param("co", Restrictions.CommentId)
                            .Param("pa", Restrictions.HasPassword)
                            .Param("pw", Restrictions.Password)
                            .End();
        }

        private TmGameCommand GetMLCommand(ulong targetPlayerId = 0)
        {
            TmGameCommandBuilder gameMlCommand = new TmGameCommandBuilder("GameML");

            if (targetPlayerId != 0)
                gameMlCommand.Param("ID", targetPlayerId);
            gameMlCommand.Param("Num", NumMembers);

            string[] names = new string[NumMembers];
            object[] ids = new object[NumMembers];
            object[] pos = new object[NumMembers];
            object[] lvs = new object[NumMembers];
            object[] rks = new object[NumMembers];
            object[] handles = new object[NumMembers];

            for (int i = 0; i < NumMembers; i++)
            {
                names[i] = Members[i].Name;
                ids[i] = Members[i].Id;
                pos[i] = 1;
                lvs[i] = 1;
                rks[i] = 1;
                handles[i] = "BLAHBLAH";
            }

            gameMlCommand.ParamHexStr("MLNN", names)
                         .Param("MLID", ids)
                         .Param("Po", pos)
                         .Param("Lv", lvs)
                         .Param("Rk", rks)
                         .Param("HN", handles);

            return gameMlCommand.End();
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

                if (oldMaster != null && NumMembers > 0)
                    SendGameOwnerToNewMaster(oldMaster.Name);
            }

            return 1;
        }

        private void SendUpdateMsgToRoom()
        {
            //string updateMsg = new MgPacketBuilder(MgPacket.PacketType.Lobby).AddTableDataCommand(this).Build();
            //TableLobbyChannel.Notice(ParentRoomIrcId, updateMsg);
        }

        public MgTable GetMgTable()
        {
            return new(Id, 0, 0, 0, State, IrcChannel, StaticAsciiData);
        }
    }
}
