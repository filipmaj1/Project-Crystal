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
using Crystal.TetraMaster.Packets;
using Crystal.TetraMaster.Tables;
using System;
using System.Collections.Generic;
using System.Threading;
using static Crystal.TetraMaster.Packets.TmPacket;

namespace Crystal.TetraMaster.TmGame
{
    public class Game
    {
        enum State
        {
            Unknown,
            InitWaitPlayers,
            ComSelect,
            CardSelect,
            StartGame,
            PlayNewRound,
            PlayCardSelect,
            PlayBattleSelect,
            PlayBattle,
            PlayEnd,
            WaitEndDecision
        }

        enum BoardSize
        {
            Size4x4,
            Size5x5
        }

        private readonly Thread GameThread;

        // Game Info
        Table ParentTable;
        bool ComGame = false;
        uint NumPlayers;

        GamePlayer[] PlayerList = new GamePlayer[3];
        Dictionary<string, GamePlayer> PolId2Player = new();
        uint CurrentNumPlayers = 0;

        private Random Random = new Random();
        TablePlayer[] ObserverList;
        BoardSize CurrentBoardSize;
        TableRules GameRules;
        State GameState = State.InitWaitPlayers;

        // Gameplay
        byte CurrentTurn;
        byte CurrentPlayerIndx;
        
        public Game(Table parent, bool isComGame, uint numPlayers)
        {
            ParentTable = parent;
            ComGame = isComGame;
            NumPlayers = numPlayers;


            if (isComGame)
                GameThread = new(new ThreadStart(ComGameLoop));
            else
                GameThread = new(new ThreadStart(GameLoop));
            GameThread.Start();
        }

        private void GameLoop()
        {
            while (GameThread.IsAlive) { 
                switch (GameState)
                {
                    case State.InitWaitPlayers:
                        if (CurrentNumPlayers == NumPlayers)
                        {
                            SendInitDone();
                            if (ComGame)
                                GameState = State.ComSelect;
                            else
                                GameState = State.CardSelect;
                        }
                        break;
                    case State.ComSelect:
                        break;
                    case State.CardSelect:
                        bool ready = true;
                        for (int i = 0; i < CurrentNumPlayers; i++)
                        {
                            if (!PlayerList[i].CardSelectDone)
                            {
                                ready = false;
                                break;
                            }
                        }
                        if (ready)
                            GameState = State.StartGame;
                        break;
                    case State.StartGame:
                        StartGame();
                        break;
                }
                Thread.Sleep(1000);
            }
        }

        private void ComGameLoop()
        {
            while (GameThread.IsAlive)
            {
                switch (GameState)
                {
                    case State.InitWaitPlayers:
                        if (CurrentNumPlayers == 1)
                            SendInitDone();
                        break;
                    case State.ComSelect:
                        break;
                    case State.CardSelect:
                        bool ready = true;
                        for (int i = 0; i < CurrentNumPlayers; i++)
                        {
                            if (!PlayerList[i].CardSelectDone)
                            {
                                ready = false;
                                break;
                            }
                        }
                        if (ready)
                            GameState = State.StartGame;
                        break;
                    case State.StartGame:
                        StartGame();
                        break;
                }
                Thread.Sleep(1000);
            }
        }

        private void SendInitDone()
        {
            if (ComGame)
            {
                string target = SqCrypto.PolIdToPolProData(Mg.TmKey(PlayerList[0].Id));
                string vsComGameInit = new TmPacketBuilder(0x43, 0x02, 0x00)
                            .Command(new TmGameCommandBuilder("ComGameInit")
                            .Param("RA", 10)
                            .Param("M", 10000)
                            .Param("CP", 500)
                            .Param("S", 1)
                            .Param("R", 1, 1, 1, 1, 1, 1, 0)
                            .End()
                            ).Build();
                ParentTable.TableLobbyChannel.Notice(target, vsComGameInit);
                
            }
            else
            {
                for (int i = 0; i < CurrentNumPlayers; i++)
                {
                    string target = SqCrypto.PolIdToPolProData(Mg.TmKey(PlayerList[i].Id));
                    string vsGameInit = new TmPacketBuilder(0x43, 0x01, 0x00)
                                .Command(new TmGameCommandBuilder("VsGameInit")
                                .Param("N", CurrentNumPlayers)
                                .Param("ID", 0x12, 0x13)
                                .ParamHexStr("CN", "Test1", "Test2")
                                .Param("R", 1, 2, 3, 4, 5, 6)
                                .End()
                                ).Build();
                    ParentTable.TableLobbyChannel.Notice(target, vsGameInit);
                }
            }

        }

        private void StartGame()
        {
            if (CurrentNumPlayers == 2)
            {
            }

            int startingPlayer = Random.Next() % (int)CurrentNumPlayers;
            int numBlocks = 3;
            int numEvents = 3;

            for (int i = 0; i < CurrentNumPlayers; i++)
            {
                string target = SqCrypto.PolIdToPolProData(Mg.TmKey(PlayerList[i].Id));
                string startGame = new TmPacketBuilder(0x43, 0x08, 0x00)
                            .Command(new TmGameCommandBuilder("StartData")
                            .Param("S", startingPlayer)
                            .Param("B", numBlocks)
                            .Param("E", numEvents)
                            .End()
                            ).Build();
                ParentTable.TableLobbyChannel.Notice(target, startGame);
            }
        }

        public int AddPlayer(ulong id, int domain, int volume, int handleId, string charaName, string handleName)
        {
            lock (this) { 
                if (NumPlayers > 3)
                    return -1;

                GamePlayer player = new(id, handleId, charaName, handleName);
                    //TableDb.LoadGamePlayer(id, domain, volume);
                if (player == null)
                    return -2;

                PlayerList[CurrentNumPlayers++] = player;
                PolId2Player[SqCrypto.PolIdToPolProData(Mg.TmKey(id))] = player;
            }

            return 1;
        }

        public void ReceivePacket(string from, TmPacket packet)
        {
            switch (GameState)
            {
                case State.InitWaitPlayers:
                    break;
                case State.ComSelect:
                    break;
                case State.CardSelect:
                    if (packet.HasCommand("CardSelect"))
                    {
                        var player = PolId2Player[from];
                        PlayerSelectedCards(player, packet);
                    }    
                    break;
            }
        }

        private void PlayerSelectedCards(GamePlayer player, TmPacket packet)
        {
            // Load all the cards
            TmGameCommand cardSelect = packet.GetCommand("CardSelect");
            cardSelect.GetParamNumber("C", 0, out int numCards);
            for (int i = 0; i < numCards; i++)
            {
                TmGameCommand card = packet.GetCommand($"N{i}");
                card.GetParamNumber("D", 0, out int cardData0);
                card.GetParamNumber("D", 1, out int cardData1);
                card.GetParamNumber("D", 2, out int cardData2);
                card.GetParamNumber("D", 3, out int cardData3);
                card.GetParamNumber("D", 4, out int cardData4);
                card.GetParamNumber("D", 5, out int cardData5);
                card.GetParamNumber("D", 6, out int cardData6);
                card.GetParamNumber("D", 7, out int cardData7);
            }

            // Inform other players cards were selected
            for (int i = 0; i < CurrentNumPlayers; i++)
            {
                string target = SqCrypto.PolIdToPolProData(Mg.TmKey(PlayerList[i].Id));
                string cardSelectSend = new TmPacketBuilder(0x43, 0x07, 0x00)
                            .Command(new TmGameCommandBuilder("CardSelect")
                            .Param("Ans", PlayerList[0].CardSelectDone ? 1 : 0, PlayerList[1].CardSelectDone ? 1 : 0)
                            .Param("Ok", PlayerList[i].Id == player.Id ? 1 : 0)
                            .End()
                            ).Build();
                ParentTable.TableLobbyChannel.Notice(target, cardSelectSend);
            }
        }

        public void UpdateBoard()
        {

        }
    }
}
