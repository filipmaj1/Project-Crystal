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

using Crystal.TetraMaster.Packets;
using System;
using System.Runtime.InteropServices;
using static Crystal.TetraMaster.Packets.TmPacket;
using Crystal.Common.MiniGame;
using Crystal.Common.MiniGame.Packets;
using Crystal.TetraMaster.Models;
using Crystal.Common.PolFileSystem;
using System.Collections.Generic;

namespace Crystal.TetraMaster
{
    public class TmShopMenuServer
    {
        private readonly PolIrcClient PolShopMenuChannel;

        private List<Zone> ZoneList;
        private static PolFile ZoneFile;

        private readonly ulong PolId;
        private readonly byte Domain;
        private readonly ushort Volume;

        public TmShopMenuServer(ulong polId, byte domain, ushort volume)
        {
            PolId = polId;
            Domain = domain;
            Volume = volume;
            PolShopMenuChannel = new("Tm-ShopMenu", PolId, OnReceiveMessage);
        }

        public bool StartServer()
        {
            Program.Log.Info("!!!Tetra Master Shop Menu Server is starting!!!");

            // Load Zones
            ZoneFile = PolFileSys.OpenFile(TmConstants.BALANCER_POLID, TmConstants.BALANCER_VOLUME, TmConstants.BALANCER_DOMAIN, "b/g/ZL", MgConstants.MAX_ZONES, MgConstants.ZONE_SIZE, MgConstants.ZONE_DATA_OFFSET, MgConstants.ZONE_COUNT_OFFSET);
            ZoneFile.InitFile(0x848);
            ZoneList = Database.LoadZones();
            foreach (Zone zone in ZoneList)
                zone.Init();

            // Open Game Channel
            PolShopMenuChannel.Connect("127.0.0.1", 51241);
            while (!PolShopMenuChannel.IsAuthenticated())
            {
                if (PolShopMenuChannel.AuthenticationFailed())
                {
                    return false;
                }
            }
            return true;
        }

        private void OnReceiveMessage(string from, string data)
        {
            MgPacket mgPacket = MgPacket.Parse(data);
            if (mgPacket == null)
                return;

            Program.Log.Debug($"Received Msg [{from}]: {data}");

            // Is initializing user?
            if (mgPacket.Type == PacketType.Game)
            {
                TmPacket packet = new(mgPacket.GetGamePacketData());
                if (packet.HasCommand("Init"))
                {
                    // Load params
                    TmGameCommand init = packet.GetCommand("Init");
                    init.GetParamId("NN", 0, out ulong playerPolId);
                    init.GetParamNumber("HID", 0, out int hid);
                    init.GetParamNumber("Dm", 0, out int domain);
                    init.GetParamNumber("Vol", 0, out int volume);
                    string charaName = init.GetParamHexStr("CN", 0);
                    init.GetParamId("GID", 0, out ulong gameId);
                    init.GetParamNumber("L", 0, out int language);
                    init.GetParamNumber("GLD", 0, out int guild);

                    playerPolId = Mg.TmKey(playerPolId);

                    // Write the gamedatafile
                    // Gotta shim this here:
                    // Load player from DB and refresh the player data file. This would normally happen somewhere else.
                    LoadAndWriteGameDataFile(playerPolId);

                    // Update Database
                    Database.UpdateInitData(playerPolId, language, guild);

                    // Get player

                    // Response
                    string initAns = new TmPacketBuilder(0x81, 0x00, 0x00)
                        .Command(new TmGameCommandBuilder("InitAns")
                            .Param("Ans", 1).End()
                        ).Build();
                    PolShopMenuChannel.Notice(from, initAns);
                    return;
                }
                // Save game options
                else if (packet.HasCommand("Opt"))
                {
                    // Read in options and save to db
                    TmGameCommand optCommand = packet.GetCommand("Opt");
                    optCommand.GetParamId("NN", 0, out ulong playerPolId);
                    playerPolId = Mg.TmKey(playerPolId);

                    optCommand.GetParamNumber("HID", 0, out int hid);
                    optCommand.GetParamNumber("Dm", 0, out int domain);
                    optCommand.GetParamNumber("Vol", 0, out int volume);
                    string charaName = optCommand.GetParamHexStr("CN", 0);

                    optCommand.GetParamNumber("Ar", 0, out int cardPlacement);
                    optCommand.GetParamNumber("Cu", 0, out int unk);
                    optCommand.GetParamNumber("Vi", 0, out int vibration);
                    optCommand.GetParamNumber("Se", 0, out int seVolume);
                    optCommand.GetParamNumber("Bgm", 0, out int bgmVolume);
                    optCommand.GetParamNumber("Per", 0, out int acceptTradeRequests);
                    optCommand.GetParamNumber("Ran", 0, out int rankingNameHidden);
                    optCommand.GetParamNumber("CMD", 0, out int autoMemberDisplay);
                    optCommand.GetParamNumber("CAD", 0, out int autoChatDisplay);
                    optCommand.GetParamNumber("CL", 0, out int numChatLines);
                    optCommand.GetParamNumber("CT", 0, out int chatTransparency);
                    optCommand.GetParamNumber("HNSS", 0, out int linkHandleId);

                    bool saveResult = Database.SaveOptions(playerPolId, cardPlacement, vibration, seVolume, bgmVolume, acceptTradeRequests, rankingNameHidden, autoMemberDisplay, autoChatDisplay, numChatLines, chatTransparency, linkHandleId);

                    // Response
                    int responseCode;
                    if (saveResult)
                        responseCode = 1;
                    else
                        responseCode = -0x100;

                    string initAns = new TmPacketBuilder(0x85, 0x00, 0x00)
                        .Command(new TmGameCommandBuilder("OptAns")
                            .Param("Ans", responseCode).End()
                        ).Build();
                    PolShopMenuChannel.Notice(from, initAns);
                }
                // Enter Shop Server
                else if (packet.HasCommand("ShReq"))
                {
                    // Read in options and save to db
                    TmGameCommand optCommand = packet.GetCommand("ShReq");
                    optCommand.GetParamId("NN", 0, out ulong playerPolId);
                    playerPolId = Mg.TmKey(playerPolId);

                    optCommand.GetParamNumber("HID", 0, out int hid);
                    optCommand.GetParamNumber("Dm", 0, out int domain);
                    optCommand.GetParamNumber("Vol", 0, out int volume);
                    string charaName = optCommand.GetParamHexStr("CN", 0);

                    // Response
                    string shEnter = new TmPacketBuilder(0xB2, 0x13, 0x00)
                        .Command(new TmGameCommandBuilder("EInit")
                            .Param("N", 0)
                            .Param("RA", 1)
                            .Param("M", 1)
                            .Param("CP", 1)
                            .Param("S", 0)
                            .Param("RT", 1)
                            .Param("Stat", 1)
                            .End()
                        ).Build();
                    PolShopMenuChannel.Notice(from, shEnter);
                }
                // Quit Shop Server
                else if (packet.HasCommand("Quit"))
                {
                    // Read in params
                    TmGameCommand optCommand = packet.GetCommand("Quit");
                    optCommand.GetParamNumber("No", 0, out int number);

                    // Response
                    string shQuit = new TmPacketBuilder(0xB2, 0x17, 0x0)
                        .Command(new TmGameCommandBuilder("Quit")
                            .Param("N", 0)
                            .Param("D", 0)
                            .Param("B", 0)
                            .End()
                        ).Build();
                    PolShopMenuChannel.Notice(from, shQuit);
                }
                // Buy Card
                else if (packet.HasCommand("Buy"))
                {
                    TmGameCommand buyCommand = packet.GetCommand("Buy");
                    buyCommand.GetParamNumber("No", 0, out int cardNumber);

                    // Response
                    string cardMsg = new TmPacketBuilder(0xB2, 0x14, 0x00)
                        .Command(new TmGameCommandBuilder("Card").Param("S", 6).End())
                        .Command(new TmGameCommandBuilder("N0").Param("D", 1, 1, 1, 1, 1, 1, 8, 8).End())
                        .Command(new TmGameCommandBuilder("N1").Param("D", 2, 1, 1, 1, 1, 1, 8, 8).End())
                        .Command(new TmGameCommandBuilder("N2").Param("D", 3, 1, 1, 1, 1, 1, 8, 8).End())
                        .Command(new TmGameCommandBuilder("N3").Param("D", 4, 1, 1, 1, 1, 1, 8, 8).End())
                        .Command(new TmGameCommandBuilder("N4").Param("D", 5, 1, 1, 1, 1, 1, 8, 8).End())
                        .Command(new TmGameCommandBuilder("N5").Param("D", 6, 1, 1, 1, 1, 1, 8, 8).End())
                        .Build();
                    PolShopMenuChannel.Notice(from, cardMsg);
                }
                // Sell Card
                else if (packet.HasCommand("Sell"))
                {
                    TmGameCommand sellCommand = packet.GetCommand("Sell");
                    sellCommand.GetParamNumber("C", 0, out int numSoldCards);

                    // Go through every card
                    for (int i = 0; i < numSoldCards; i++)
                    {
                        TmGameCommand cardEditData = packet.GetCommand($"{i}");
                        cardEditData.GetParamNumber("D", 0, out int cardPortrait);
                        cardEditData.GetParamNumber("D", 1, out int cardOffense);
                        cardEditData.GetParamNumber("D", 2, out int cardType);
                        cardEditData.GetParamNumber("D", 3, out int cardPDefense);
                        cardEditData.GetParamNumber("D", 4, out int cardMDefense);
                        cardEditData.GetParamNumber("D", 5, out int cardDirectionBitfield);
                    }
                }
                // Card Data Editing
                else if (packet.HasCommand("Decks"))
                {
                    // Are there any card updates?
                    TmGameCommand decksCommand = packet.GetCommand("Decks");
                    decksCommand.GetParamNumber("C", 0, out int numEditedCards);

                    // Go through every card and set
                    for (int i = 0; i < numEditedCards; i++)
                    {
                        TmGameCommand cardEditData = packet.GetCommand($"{i}");
                        cardEditData.GetParamNumber("D", 0, out int cardPortrait);
                        cardEditData.GetParamNumber("D", 1, out int cardOffense);
                        cardEditData.GetParamNumber("D", 2, out int cardType);
                        cardEditData.GetParamNumber("D", 3, out int cardPDefense);
                        cardEditData.GetParamNumber("D", 4, out int cardMDefense);
                        cardEditData.GetParamNumber("D", 5, out int cardDirectionBitfield);
                        if (cardEditData.GetParamNumber("D", 6, out int cardDeckPosition))
                        {

                        }
                    }

                    // Did the deck set names change?
                    if (packet.HasCommand("DN1"))
                    {
                        TmGameCommand deckName1Command = packet.GetCommand("DN1");
                        string deckName1 = deckName1Command.GetParamHexStr("S", 0);

                    }
                    if (packet.HasCommand("DN2"))
                    {
                        TmGameCommand deckName2Command = packet.GetCommand("DN2");
                        string deckName2 = deckName2Command.GetParamHexStr("S", 0);
                    }
                }

                //B2002702@Dead=/N=?
            }
        }

        public void LoadAndWriteGameDataFile(ulong polId)
        {
            GameDataFile? dataFile = Database.LoadPlayerGameData(polId);
            if (dataFile != null)
                WriteGameDataFile(polId, (GameDataFile)dataFile);
        }

        private void WriteGameDataFile(ulong polId, GameDataFile gamedata)
        {
            byte[] outBuff = new byte[GameDataFile.SIZE];
            Span<byte> gameDataSpan = outBuff.AsSpan();
            MemoryMarshal.Write(gameDataSpan, ref gamedata);

            PolFile file = PolFileSys.OpenFileDirect(polId, 0, 0, "U/g/TM0DataFile", out object fileLock);
            lock (fileLock)
            {
                var fileStream = file.GetDirectFileStream();
                fileStream.Write(gameDataSpan);
                fileStream.Close();
            }
            PolFileSys.CloseFile(file);
        }

        public void Test()
        {
            ZoneList[0].RoomList[0].Tables[0].Test();
        }

    }
}
