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
using MySqlConnector;
using System.Collections.Generic;
using Crystal.TetraMaster.Models;
using System.Data;
using Crystal.TetraMaster.Tables;
using Crystal.Common.MiniGame;

namespace Crystal.TetraMaster
{
    class Database
    {
        const string HOST = "127.0.0.1";
        const string PORT = "3306";
        const string DB_NAME = "tetra_master";
        const string USERNAME = "root";
        const string PASSWORD = "";
        
        public static GameDataFile? LoadPlayerGameData(ulong polId)
        {
            MySqlCommand cmd;
            GameDataFile? result = null;
            int step = 0;

            using (MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", HOST, PORT, DB_NAME, USERNAME, PASSWORD)))
            {
                try
                    {
                        conn.Open();
                        string query = @"
                            SELECT * FROM characters
                        LEFT JOIN characters_data ON characters.polId = characters_data.polId
                        LEFT JOIN characters_data_cards ON characters.polId = characters_data_cards.polId
                        LEFT JOIN characters_options ON characters.polId = characters_options.polId
                        LEFT JOIN characters_options_rule_settings ON characters.gameId = characters_options_rule_settings.gameId
                        LEFT JOIN characters_options_table_settings ON characters.gameId = characters_options_table_settings.gameId
                        WHERE characters.polId = @polId";
                    cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@polId", polId);
                    using MySqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        step++;
                        GameDataHeader header = new()
                        {
                            FileVersion = GameDataFile.FileVersion,
                            GameId = reader.GetUInt32("gameId"),
                            PolId = Mg.TmKey(polId),
                            CharaName = reader.GetString("characterName"),
                            HandleId = reader.GetUInt64("linkedHandleId")
                        };

                        step++;
                        GameDataStatsSection charaData = new()
                        {
                            AverageRank = reader.GetUInt32("avgRank"),
                            Money = reader.GetUInt32("money"),
                            CardPower = 1,
                            Guild = reader.GetByte("guild"),
                            NumCards = 5,
                            NumCards2 = 0,
                            PlayersBeaten = reader.GetUInt32("playersBeaten"),
                            BiggestPrize = reader.GetUInt32("biggestPrize"),
                            AveragePrize = reader.GetUInt32("averagePrize"),
                            GrandTotal = reader.GetUInt32("grandTotal"),
                            ConsecutiveWins = reader.GetUInt16("consecutiveWins"),
                            WinningStreak = reader.GetUInt32("winningStreak"),
                            UnkStat1 = reader.GetByte("unk"),
                            NameOfMemorableWin = reader.GetString("nameOfMemorableWin"),
                        };

                        step++;
                        GameDataOptionsSection options = new()
                        {
                            OptCardPlacement = reader.GetByte("cardPlacement"),
                            OptVibration = reader.GetByte("vibrationEnabled"),
                            OptSeVolume = reader.GetByte("seVolume"),
                            OptBgmVolume = reader.GetByte("bgmVolume"),
                            OptTradeAccRequire = reader.GetByte("acceptTradeRequests"),
                            OptDisplayRankings = reader.GetByte("hideRankingName"),
                            OptAutoMemberDisplay = reader.GetByte("autoDisplayMembers"),
                            OptAutoChatDisplay = reader.GetByte("autoDisplayChat"),
                            OptChatWindowSize = reader.GetByte("chatWindowSize"),
                            OptChatWindowTransparency = reader.GetByte("chatWindowTransparency"),
                            OptLinkHandles = reader.GetByte("linkHandles"),

                            TetRuleWager = reader.GetUInt32("wager"),
                            TetRuleDoubleUp = reader.GetByte("doubleUp"),
                            TetRuleSpecialTile = reader.GetByte("specialTile"),
                            TetRuleChanceBlock = reader.GetByte("chanceBlock"),
                            TetRuleRotatingBlock = reader.GetByte("rotatingBlock"),
                            TetRuleQuitMode = reader.GetByte("quitMode"),
                            TetRuleTimeLimit = reader.GetByte("timeLimit"),

                            TabObserveType = reader.GetByte("observeType"),
                            TabCardLevelUpper = reader.GetUInt32("doubleUp"),
                            TabCardLevelLower = reader.GetUInt32("specialTile"),
                            TabAUpper = reader.GetUInt32("chanceBlock"),
                            TabALower = reader.GetByte("rotatingBlock"),
                            TabComment = reader.GetUInt32("doubleWager"),
                            TabHasPassword = reader.GetByte("hasPassword"),
                            TabPassword = reader.GetString("tablePassword"),

                            DeckName1 = reader.GetString("deckName1"),
                            DeckName2 = reader.GetString("deckName2"),

                            BadgeIcon = reader.GetByte("badgeIcon"),
                            VsGames = reader.GetUInt16("vsGames"),
                            VsRating = reader.GetUInt32("vsRating"),
                            PrizePointsAquired = reader.GetUInt32("prizePoints")
                        };

                        // Read in the card data binary, if null we use the empty list
                        byte[] cardDataBuffer = new byte[0x2EE0];
                        if (!reader.IsDBNull("cardListBinary"))
                        {
                            long cardDataReadBytes = reader.GetBytes("cardListBinary", 0, cardDataBuffer, 0, cardDataBuffer.Length);
                            if (cardDataReadBytes != 0x2EE0)
                                throw new Exception("Did not load full card data binary.");
                        }

                        // Fake the cards
                        charaData.NumCards = 10;
                        for (int i = 0; i < charaData.NumCards; i++)
                        {
                            cardDataBuffer[(0xC * i)] = (byte)i; // Portrait
                            cardDataBuffer[(0xC * i) + 2] = 1; // Offense
                            cardDataBuffer[(0xC * i) + 3] = 1; // Type
                            cardDataBuffer[(0xC * i) + 4] = 3; // P Defense
                            cardDataBuffer[(0xC * i) + 5] = 3; // M Defense  
                            cardDataBuffer[(0xC * i) + 6] = 1; // ????
                            cardDataBuffer[(0xC * i) + 7] = 1; // Directions
                            cardDataBuffer[(0xC * i) + 8] = 0xFF; // Deck Index
                        }

                        step++;
                        result = new()
                        {
                            Header = header,
                            Stats = charaData,
                            Options = options,
                            CardData = cardDataBuffer
                        };

                        return result;
                    }
                }
                catch (Exception e)
                {
                    Program.Log.Error($"Failed to read character at step: {step}. E is:\n{e}");
                }
                finally
                {
                    conn.Dispose();
                }
            }

            return result;
        }

        public static void UpdateInitData(ulong polId, int language, int guild)
        {
            string query;
            MySqlCommand cmd;

            using MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", HOST, PORT, DB_NAME, USERNAME, PASSWORD));
            try
            {
                conn.Open();
                query = "UPDATE characters SET language = @lang, guild = IF(@newGld != 0, @newGld, guild) WHERE polId = @polId";
                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@polId", polId);
                cmd.Parameters.AddWithValue("@lang", language);
                cmd.Parameters.AddWithValue("@newGld", guild);
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException e)
            {
                Program.Log.Error(e.ToString());
            }
            finally
            {
                conn.Dispose();
            }
        }

        public static void SetOptions(ulong polId)
        {

        }

        public static void SetDeckName(ulong polid, uint index, string name)
        {
        }


        public static List<Zone> LoadZones()
        {
            MySqlCommand cmd;
            List<Zone> zoneList = new();
            ushort index = 0;

            //Get From DB
            using (MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", HOST, PORT, DB_NAME, USERNAME, PASSWORD)))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT * FROM server_zones";
                    cmd = new MySqlCommand(query, conn);
                    using MySqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        ushort id = reader.GetUInt16("id");
                        string name = reader.GetString("name");
                        string langCode = reader.GetString("langCode");
                        bool isEvent = reader.GetBoolean("isEvent");
                        byte release = reader.GetByte("releaseCode");
                        byte platform = reader.GetByte("platformCode");

                        zoneList.Add(new(index++, id, name, langCode, platform, isEvent));
                    }
                }
                catch (MySqlException e)
                {
                    Program.Log.Error(e.ToString());
                }
                finally
                {
                    conn.Dispose();
                }
            }

            return zoneList;
        }

        public static List<Room> LoadRooms(Zone zone)
        {
            MySqlCommand cmd;
            List<Room> roomList = new();

            //Get From DB
            using (MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", HOST, PORT, DB_NAME, USERNAME, PASSWORD)))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT * FROM server_rooms WHERE zoneId = @zoneId";
                    cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@zoneId", zone.Id);
                    using MySqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        ulong id = reader.GetUInt64("id");
                        string name = reader.GetString("name");
                        int maxTables = (int)reader.GetUInt32("maxTables");
                        int maxPlayers = (int)reader.GetUInt32("maxPlayers");

                        // Rule info
                        uint wager = reader.GetUInt32("wager");
                        bool doubleUp = reader.GetBoolean("doubleUp");
                        bool specialTiles = reader.GetBoolean("specialTiles");
                        bool chanceBlocks = reader.GetBoolean("chanceBlocks");
                        bool rotatingBlocks = reader.GetBoolean("rotatingBlocks");
                        byte quitMode = reader.GetByte("quitMode");
                        byte timerNum = reader.GetByte("timerNum");
                        TableRules rules = new(wager, doubleUp, specialTiles, chanceBlocks, rotatingBlocks, quitMode, timerNum);

                        bool canModifyRules = reader.GetBoolean("canSetRules");
                        bool vsOnly = reader.GetBoolean("vsOnly");
                        bool chatSuppressed = reader.GetBoolean("supressChat");

                        roomList.Add(new(zone, 2, 0, id, name, maxTables, maxPlayers, rules, canModifyRules, vsOnly, chatSuppressed));
                    }
                }
                catch (MySqlException e)
                {
                    Program.Log.Error(e.ToString());
                }
                finally
                {
                    conn.Dispose();
                }
            }

            return roomList;
        }

        public static bool SaveOptions(ulong playerPolId, int cardPlacement, int vibration, int seVolume, int bgmVolume, int acceptTradeRequests, int rankingNameHidden, int autoMemberDisplay, int autoChatDisplay, int numChatLines, int chatTransparency, int linkHandleId)
        {
            string query;
            MySqlCommand cmd;

            using MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", HOST, PORT, DB_NAME, USERNAME, PASSWORD));
            try
            {
                conn.Open();
                query = @"
                    UPDATE characters_options SET 
                        cardPlacement = @Ar,
                        vibrationEnabled = @Vi,
                        seVolume = @Se,
                        bgmVolume = @Bgm,
                        acceptTradeRequests = @Per,
                        hideRankingName = @Ran,
                        autoDisplayMembers = @CMD,
                        autoDisplayChat = @CAD,
                        chatWindowSize = @CL,
                        chatWindowTransparency = @CT,
                        linkHandles = @HNSS
                    WHERE 
                        polId = @polId";
                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@polId", playerPolId);
                cmd.Parameters.AddWithValue("@Ar", cardPlacement);
                cmd.Parameters.AddWithValue("@Vi", vibration);
                cmd.Parameters.AddWithValue("@Se", seVolume);
                cmd.Parameters.AddWithValue("@Bgm", bgmVolume);
                cmd.Parameters.AddWithValue("@Per", acceptTradeRequests);
                cmd.Parameters.AddWithValue("@Ran", rankingNameHidden);
                cmd.Parameters.AddWithValue("@CMD", autoMemberDisplay);
                cmd.Parameters.AddWithValue("@CAD", autoChatDisplay);
                cmd.Parameters.AddWithValue("@CL", numChatLines);
                cmd.Parameters.AddWithValue("@CT", chatTransparency);
                cmd.Parameters.AddWithValue("@HNSS", linkHandleId);
                cmd.ExecuteNonQuery();

                return true;
            }
            catch (MySqlException e)
            {
                Program.Log.Error(e.ToString());
            }
            finally
            {
                conn.Dispose();
            }
            return false;
        }
    }
}
