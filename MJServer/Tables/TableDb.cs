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

namespace Crystal.Mahjong.Tables
{
    class TableDb
    {
        const string HOST = "127.0.0.1";
        const string PORT = "3306";
        const string DB_NAME = "tetra_master";
        const string USERNAME = "root";
        const string PASSWORD = "";
        
        public static TablePlayer LoadTablePlayer(ulong polId)
        {
            MySqlCommand cmd;
            TablePlayer result = null;

            using (MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", HOST, PORT, DB_NAME, USERNAME, PASSWORD)))
            {
                try
                {
                    conn.Open();
                    string query = @"
                        SELECT * FROM characters
                        LEFT JOIN characters_data ON characters.polId = characters_data.polId
                        LEFT JOIN characters_data_cards ON characters.polId = characters_data_cards.polId
                        WHERE characters.polId = @polId";
                    cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@polId", polId);
                    using MySqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        ulong charaId = Mg.MjKey(reader.GetUInt64("polId"));
                        string charaName = reader.GetString("characterName");
                        uint avgRank = reader.GetUInt32("avgRank");
                        uint money = reader.GetUInt32("money");
                        uint vsRating = reader.GetUInt32("vsRating");
                        result = new TablePlayer(charaId, charaName, avgRank, money, vsRating);
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

            return result;
        }

        public static bool SaveUserRulesAndRestrictions(ulong playerPolId, TableRules rules, TableRestrictions restrictions)
        {
            string query;
            MySqlCommand cmd;

            using MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", HOST, PORT, DB_NAME, USERNAME, PASSWORD));
            try
            {
                conn.Open();
                query = @"
                    UPDATE characters_options_rule_settings tet, characters_options_table_settings tab SET 
                    
                    WHERE 
                        tet.polId = @polId AND tab.polId = @polId;
                    ";
                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@polId", playerPolId);
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
