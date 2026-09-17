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
using System.Collections.Generic;
using Crystal.Mahjong.Models;

namespace Crystal.Mahjong
{
    class Database
    {
        const string HOST = "127.0.0.1";
        const string PORT = "3306";
        const string DB_NAME = "jangho";
        const string USERNAME = "root";
        const string PASSWORD = "";

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
                        string irc = reader.GetString("irc");

                        zoneList.Add(new(index++, id, name, irc));
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

                        roomList.Add(new(zone, 3, 0, id, name, maxTables, maxPlayers));
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
    }
}
