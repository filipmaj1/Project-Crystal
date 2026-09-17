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

using MySqlConnector;
using System;

namespace Crystal.FrontMissionOnline
{
    class Database
    {
        const string HOST = "127.0.0.1";
        const string PORT = "3306";
        const string DB_NAME_POL = "playonline";
        const string DB_NAME = "front_mission_online_server";
        const string USERNAME = "root";
        const string PASSWORD = "";

        public static byte[]? GetPlayonlineRandomValue(byte[] authHash)
        {
            using MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", HOST, PORT, DB_NAME_POL, USERNAME, PASSWORD));
            try
            {
                conn.Open();
                MySqlCommand cmd = new("SELECT polRandomValueBinary FROM sessions WHERE polContentAuthHash = @authHash", conn);
                cmd.Parameters.AddWithValue("@authHash", authHash);

                using MySqlDataReader Reader = cmd.ExecuteReader();
                while (Reader.Read())
                {
                    byte[] randomValue = new byte[0x14];
                    long bytesRead = Reader.GetBytes("polRandomValueBinary", 0, randomValue, 0, 0x10);
                    randomValue[0x10] = 0x49;
                    randomValue[0x11] = 0x4A;
                    randomValue[0x12] = 0x4B;
                    randomValue[0x13] = 0x4C;
                    if (bytesRead == 0x10)
                        return randomValue;
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
            return null;
        }

        public static void CreateGameSession(uint sessionId, byte[] encryptionKey)
        {
            string query;
            MySqlCommand cmd;

            using MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", HOST, PORT, DB_NAME, USERNAME, PASSWORD));
            try
            {
                conn.Open();

                query = @"
                    INSERT INTO sessions (sessionId, encryptionKey) 
                    VALUES (@sessionId, @key) 
                    ON DUPLICATE KEY UPDATE encryptionKey=@key;
                    ";

                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@sessionId", sessionId);
                cmd.Parameters.AddWithValue("@key", encryptionKey);

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

        public static bool GetGameSession(uint sessionId, out byte[] outEncryptionKey)
        {
            outEncryptionKey = Array.Empty<byte>();

            using MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", HOST, PORT, DB_NAME, USERNAME, PASSWORD));
            try
            {
                conn.Open();
                MySqlCommand cmd = new("SELECT encryptionKey FROM sessions WHERE sessionId = @sessionId", conn);
                cmd.Parameters.AddWithValue("@sessionId", sessionId);

                using MySqlDataReader Reader = cmd.ExecuteReader();
                while (Reader.Read())
                {
                    byte[] encKey = new byte[0x14];
                    long bytesRead = Reader.GetBytes("encryptionKey", 0, encKey, 0, 0x14);

                    if (bytesRead == 0x14)
                    {
                        outEncryptionKey = encKey;
                        return true;
                    }
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
            return false;
        }
    }
}
