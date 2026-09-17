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
using Crystal.POLAuth.DataObjects.Irc;
using System.Collections.Generic;
using System.Text;
using System.Data;
using Crystal.Common;
using System.IO;

namespace Crystal.POLAuth
{
    class Database
    {
        public static string DB_HOST = "127.0.0.1";
        public static string DB_PORT = "3306";
        public static string DB_NAME = "playonline";
        public static string DB_USERNAME = "root";
        public static string DB_PASSWORD = "";
        public static byte[] DB_PASSWORD_KEY = null;
        private const int PASSWORD_SIZE = 0x10;

        public static byte[] GetDecryptedPasswordForPolId(string polId)
        {
            //Get From DB
            using (MySqlConnection conn = new($"Server={DB_HOST}; Port={DB_PORT}; Database={DB_NAME}; UID={DB_USERNAME}; Password={DB_PASSWORD}"))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT password FROM accounts WHERE polId = @polId";
                    MySqlCommand cmd = new(query, conn);
                    cmd.Parameters.AddWithValue("@polId", polId);

                    using MySqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        byte[] encryptedPassword = new byte[PASSWORD_SIZE];
                        long bytesReceived = reader.GetBytes(reader.GetOrdinal("password"), 0, encryptedPassword, 0, encryptedPassword.Length);

                        if (DB_PASSWORD_KEY != null)
                        {
                            Blowfish bf = new Blowfish(DB_PASSWORD_KEY, 16);
                            bf.Decipher(encryptedPassword, 0, encryptedPassword.Length);
                        }

                        int strlen = 0;
                        for (strlen = 0; strlen < encryptedPassword.Length; strlen++)
                        {
                            if (encryptedPassword[strlen] == 0)
                                break;
                        }

                        byte[] decryptedPassword = new byte[strlen];
                        for (int i = 0; i < strlen; i++)
                            decryptedPassword[i] = encryptedPassword[i];
                        Array.Clear(encryptedPassword);

                        return decryptedPassword;
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
            return null;
        }

        private static byte[] EncryptPassword(string password)
        {
            byte[] passBytes = new byte[PASSWORD_SIZE];
            Encoding.ASCII.GetBytes(password, passBytes);
            if (DB_PASSWORD_KEY != null)
            { 
                Blowfish bf = new Blowfish(DB_PASSWORD_KEY, 16);
                bf.Encipher(passBytes, 0, PASSWORD_SIZE);
                return passBytes;
            }
            return passBytes;
        }

        public static void CreateAccountSession(Client client)
        {
            string query;
            MySqlCommand cmd;

            using MySqlConnection conn = new($"Server={DB_HOST}; Port={DB_PORT}; Database={DB_NAME}; UID={DB_USERNAME}; Password={DB_PASSWORD}");
            try
            {
                conn.Open();

                query = @"
                    REPLACE INTO sessions (clientIp, clientPort, polId, rkey1, rkey2, blowkey, loginTime) 
                    VALUES (@ip, @port, @polId, @rsaKey1, @rsaKey2, @blowKey, @loginTime)
                    ";

                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ip", client.Ip);
                cmd.Parameters.AddWithValue("@port", client.Port);
                cmd.Parameters.AddWithValue("@polId", client.GetPolID());
                cmd.Parameters.AddWithValue("@rsaKey1", client.GetCrypto().GetRKey1());
                cmd.Parameters.AddWithValue("@rsaKey2", client.GetCrypto().GetRKey2());
                cmd.Parameters.AddWithValue("@blowKey", client.GetCrypto().GetBlowKeyUInt64());
                cmd.Parameters.AddWithValue("@loginTime", client.GetLoginTime());

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

        public static void DeleteAccountSession(Client client)
        {
            string query;
            MySqlCommand cmd;

            using MySqlConnection conn = new($"Server={DB_HOST}; Port={DB_PORT}; Database={DB_NAME}; UID={DB_USERNAME}; Password={DB_PASSWORD}");
            try
            {
                conn.Open();
                query = @"
                    DELETE FROM sessions
                    WHERE polId = @polId AND clientPort = @port;
                    ";

                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@polId", client.GetPolID());
                cmd.Parameters.AddWithValue("@port", client.Port);
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

        // Clears the session list and sets all status rows to offline.
        public static void ClearSessions()
        {
            string query;
            MySqlCommand cmd;

            using MySqlConnection conn = new($"Server={DB_HOST}; Port={DB_PORT}; Database={DB_NAME}; UID={DB_USERNAME}; Password={DB_PASSWORD}");
            try
            {
                conn.Open();

                query = "DELETE FROM sessions; UPDATE status SET isOnline = 0";

                cmd = new MySqlCommand(query, conn);
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Program.Log.Error(e.ToString());
                Program.Log.Error(e.ToString());
            }
            finally
            {
                conn.Dispose();
            }
        }

        // Taken from PolProfile. Gets a list of PolId/CreationPosition pairs for all online
        // players attached to the current handle's FL.
        public static List<Tuple<string, byte>> GetOnlineFriendsPolIds(string polid)
        {
            List<Tuple<string, byte>> friendPolIdAndPositionList = new();
            string query;
            MySqlCommand cmd;

            //Get From DB
            using (MySqlConnection conn = new($"Server={DB_HOST}; Port={DB_PORT}; Database={DB_NAME}; UID={DB_USERNAME}; Password={DB_PASSWORD}"))
            {
                try
                {
                    conn.Open();
                    query = @"
                    SELECT 
                       friendlist.ownerPolId,
                       friendlist.creationPosition
                    FROM friendlist
                    INNER JOIN status ON status.polId = friendlist.friendPolId
                    INNER JOIN handles ON handles.id = friendlist.friendHandleId
                    WHERE friendPolId = @polId AND status.activeHandleId = handles.id AND status.isOnline = 1
                    ";
                    cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@polId", polid);
                    using MySqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        string polId = reader.GetString("ownerPolId");
                        byte creationPosition = reader.GetByte("creationPosition");
                        friendPolIdAndPositionList.Add(new Tuple<string, byte>(polId, creationPosition));
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

            return friendPolIdAndPositionList;
        }

        // Chatroom DB Updates
        public static void CreateChatroom(string channel)
        {
            string query;
            MySqlCommand cmd;

            using MySqlConnection conn = new($"Server={DB_HOST}; Port={DB_PORT}; Database={DB_NAME}; UID={DB_USERNAME}; Password={DB_PASSWORD}");
            try
            {
                conn.Open();

                query = @"
                    REPLACE INTO chatrooms (channel, usersCurrent) 
                    VALUES (@channel, 1)
                    ";

                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@channel", channel);

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

        public static bool CreateChatroomPerm(string channel, string chatroomName, ushort usersMax, ushort memberCode, ushort purposeCode, ushort languageCode, ushort zoneCode)
        {
            bool result = true;
            string query;
            MySqlCommand cmd;

            using MySqlConnection conn = new($"Server={DB_HOST}; Port={DB_PORT}; Database={DB_NAME}; UID={DB_USERNAME}; Password={DB_PASSWORD}");
            try
            {
                conn.Open();

                query = @"
                    INSERT INTO chatrooms (channel, chatroomName, usersMax, permanent, memberCode, purposeCode, languageCode, zoneCode) 
                    VALUES (@channel, @chatroomName, @usersMax, 1, @memberCode, @purposeCode, @languageCode, @zoneCode)
                    ";

                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@channel", channel);
                cmd.Parameters.AddWithValue("@chatroomName", chatroomName);
                cmd.Parameters.AddWithValue("@usersMax", usersMax);
                cmd.Parameters.AddWithValue("@memberCode", memberCode);
                cmd.Parameters.AddWithValue("@purposeCode", purposeCode);
                cmd.Parameters.AddWithValue("@languageCode", languageCode);
                cmd.Parameters.AddWithValue("@zoneCode", zoneCode);

                cmd.ExecuteNonQuery();
            }
            catch (MySqlException e)
            {
                Program.Log.Error(e.ToString());
                result = false;
            }
            finally
            {
                conn.Dispose();
            }
            return result;
        }

        public static List<Tuple<string, string, ushort>> GetPermanentChatroomChannels()
        {
            List<Tuple<string, string, ushort>> channels = [];
            string query;
            MySqlCommand cmd;

            //Get From DB
            using (MySqlConnection conn = new($"Server={DB_HOST}; Port={DB_PORT}; Database={DB_NAME}; UID={DB_USERNAME}; Password={DB_PASSWORD}"))
            {
                try
                {
                    conn.Open();
                    query = @"
                    SELECT 
                       id,
                       chatroomName,
                       zoneCode,
                       usersMax,
                       memberCode,
                       purposeCode,
                       languageCode
                    FROM chatrooms_permanent
                    ORDER BY chatroomName ASC
                    ";
                    cmd = new MySqlCommand(query, conn);
                    using MySqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        uint id = reader.GetUInt16("id");
                        string chatroomName = reader.GetString("chatroomName");
                        ushort zoneCode = reader.GetUInt16("zoneCode");
                        ushort usersMax = (ushort) (reader.GetUInt16("usersMax") + 1);
                        ushort memberCode = reader.GetUInt16("memberCode");
                        ushort purposeCode = reader.GetUInt16("purposeCode");
                        ushort languageCode = reader.GetUInt16("languageCode");

                        // Build the channel
                        if (id != 0)
                            id--;
                        int langCode = 0;
                        if (languageCode == 301)
                            langCode = 1;
                        string channel = $"#{langCode:D2}CP{"ZYOTYU"}{id:D6}";

                        // Build the topic
                        byte[] zoneStruct = new byte[9];
                        using (MemoryStream memStream = new(zoneStruct))
                        using (BinaryWriter writer = new(memStream))
                        {
                            writer.Write(zoneCode);      // 0x00  Zone Code
                            writer.Write(memberCode);    // 0x02  Category Code 1
                            writer.Write(purposeCode);   // 0x04  Category Code 2
                            writer.Write(languageCode);  // 0x06  Category Code 3
                            writer.Write((byte)0);       // 0x08  trailing byte
                        }
                        string topic = SqCrypto.EncodeBase64(zoneStruct, zoneStruct.Length, false) + chatroomName;

                        if (CreateChatroomPerm(channel, chatroomName, usersMax, memberCode, purposeCode, languageCode, zoneCode))
                            channels.Add(new(channel, topic, usersMax));
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

            return channels;
        }

        public static void ClearChatrooms()
        {
            string query;
            MySqlCommand cmd;

            using MySqlConnection conn = new($"Server={DB_HOST}; Port={DB_PORT}; Database={DB_NAME}; UID={DB_USERNAME}; Password={DB_PASSWORD}");
            try
            {
                conn.Open();
                query = @"DELETE FROM chatrooms";

                cmd = new MySqlCommand(query, conn);
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

        public static void DeleteChatroom(string channel)
        {
            string query;
            MySqlCommand cmd;

            using MySqlConnection conn = new($"Server={DB_HOST}; Port={DB_PORT}; Database={DB_NAME}; UID={DB_USERNAME}; Password={DB_PASSWORD}");
            try
            {
                conn.Open();
                query = @"
                    DELETE FROM chatrooms
                    WHERE channel = @channelName AND permanent = 0;
                    ";

                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@channelName", channel);
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

        public static void UpdateChatroomHasPassword(string channel, bool hasPwd)
        {
            string query;
            MySqlCommand cmd;

            using MySqlConnection conn = new($"Server={DB_HOST}; Port={DB_PORT}; Database={DB_NAME}; UID={DB_USERNAME}; Password={DB_PASSWORD}");
            try
            {
                conn.Open();
                query = @$"
                    UPDATE chatrooms
                    SET hasPassword = @hasPassword
                    WHERE channel = @channelName;
                    ";

                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@channelName", channel);
                cmd.Parameters.AddWithValue("@hasPassword", hasPwd);
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

        public static void UpdateChatroomTopicData(string channel, string topic)
        {
            string query;
            MySqlCommand cmd;

            // Read in topic data
            byte[] topicData = SqCrypto.DecodeBase64(topic[0..12], 12);
            if (topicData.Length != 9)
                return;
            ushort zoneId = BitConverter.ToUInt16(topicData);
            ushort cate1 = BitConverter.ToUInt16(topicData, 2);
            ushort cate2 = BitConverter.ToUInt16(topicData, 4);
            ushort cate3 = BitConverter.ToUInt16(topicData, 6);
            string chatroomName = topic[12..];

            // Set
            using MySqlConnection conn = new($"Server={DB_HOST}; Port={DB_PORT}; Database={DB_NAME}; UID={DB_USERNAME}; Password={DB_PASSWORD}");
            try
            {
                conn.Open();
                query = @$"
                    UPDATE chatrooms
                    SET chatroomName = @topic, zoneCode = @zone, memberCode = @member, purposeCode = @purpose, languageCode = @language
                    WHERE channel = @channelName;
                    ";

                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@channelName", channel);
                cmd.Parameters.AddWithValue("@topic", chatroomName);
                cmd.Parameters.AddWithValue("@zone", zoneId);
                cmd.Parameters.AddWithValue("@member", cate1);
                cmd.Parameters.AddWithValue("@purpose", cate2);
                cmd.Parameters.AddWithValue("@language", cate3);
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

        public static void UpdateChatroomMaxPerson(string channel, int max)
        {
            string query;
            MySqlCommand cmd;

            using MySqlConnection conn = new($"Server={DB_HOST}; Port={DB_PORT}; Database={DB_NAME}; UID={DB_USERNAME}; Password={DB_PASSWORD}");
            try
            {
                conn.Open();
                query = @$"
                    UPDATE chatrooms
                    SET usersMax = @usersMax
                    WHERE channel = @channelName;
                    ";

                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@channelName", channel);
                cmd.Parameters.AddWithValue("@usersMax", max);
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

        public static void UpdateChatroomPersonCount(string channel, bool increment)
        {
            string query;
            MySqlCommand cmd;

            using MySqlConnection conn = new($"Server={DB_HOST}; Port={DB_PORT}; Database={DB_NAME}; UID={DB_USERNAME}; Password={DB_PASSWORD}");
            try
            {
                conn.Open();
                query = @$"
                    UPDATE chatrooms
                    SET usersCurrent = usersCurrent {(increment ? '+' : '-')} 1
                    WHERE channel = @channelName;
                    ";

                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@channelName", channel);
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
            
        public static List<string> GetUserListForOutput()
        {
            string query;
            MySqlCommand cmd;
            using (MySqlConnection conn = new($"Server={DB_HOST}; Port={DB_PORT}; Database={DB_NAME}; UID={DB_USERNAME}; Password={DB_PASSWORD}"))
            {
                try
                {
                    StringBuilder builder = new();
                    conn.Open();

                    query = @"
                    SELECT polId, email
                    FROM accounts
                    ";

                    // Get General Info
                    cmd = new MySqlCommand(query, conn);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        List<string> lines = [];
                        lines.Add($"{"POL ID",-10} {"Email",-35} Online Status");
                        while (reader.Read())
                        {
                            string polId = reader.GetString("polId");
                            string email = reader.GetString("email");
                            if (string.IsNullOrWhiteSpace(email))
                                email = "None";
                            lines.Add($"{polId,-10} {email,-35} OFFLINE");
                        }
                        return lines;
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
            return null;
        }

        public static string CreateAccount(string email, string password, string forcedId = "")
        {
            string idStr = !string.IsNullOrEmpty(forcedId) ? $"'{forcedId}'" : "GenerateNewPolId()";
            using (MySqlConnection conn = new($"Server={DB_HOST}; Port={DB_PORT}; Database={DB_NAME}; UID={DB_USERNAME}; Password={DB_PASSWORD}; AllowUserVariables=true"))
            {
                try
                {
                    conn.Open();
                    string query = $@"
                    SET @newId = {idStr};

                    INSERT INTO accounts (polId, email, password) 
                    VALUES (@newId, @email, @password);

                    SELECT @newId;
                    ";
                    MySqlCommand cmd = new(query, conn);
                    cmd.Parameters.AddWithValue("@email", email);

                    byte[] encryptedPassword = EncryptPassword(password);
                    cmd.Parameters.AddWithValue("@password", encryptedPassword);
                    object result = cmd.ExecuteScalar();
                    Array.Clear(encryptedPassword);

                    return (string) result;
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
            return null;
        }

        public static bool DeleteAccount(string polId, out bool couldNotFindPolId)
        {
            couldNotFindPolId = false;
            using (MySqlConnection conn = new($"Server={DB_HOST}; Port={DB_PORT}; Database={DB_NAME}; UID={DB_USERNAME}; Password={DB_PASSWORD}"))
            {
                try
                {
                    conn.Open();
                    string query = @"
                    DELETE FROM accounts
                    WHERE polId = @polId
                    ";
                    MySqlCommand cmd = new(query, conn);
                    cmd.Parameters.AddWithValue("@polId", polId);
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected == 0)
                    {
                        couldNotFindPolId = true;
                        return false;
                    }
                    else
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
            }
            return false;
        }

        public static bool SetAccountPassword(string polId, string newPassword, out bool couldNotFindPolId)
        {
            couldNotFindPolId = false;
            using (MySqlConnection conn = new($"Server={DB_HOST}; Port={DB_PORT}; Database={DB_NAME}; UID={DB_USERNAME}; Password={DB_PASSWORD}"))
            {
                try
                {
                    conn.Open();
                    string query = @"
                    UPDATE 
                       accounts
                    SET password = @newPassword
                    WHERE polId = @polId
                    ";
                    MySqlCommand cmd = new(query, conn);

                    byte[] encryptedPassword = EncryptPassword(newPassword);
                    cmd.Parameters.AddWithValue("@polId", polId);
                    cmd.Parameters.Add("@newPassword", MySqlDbType.Blob).Value = encryptedPassword;

                    int rowsAffected = cmd.ExecuteNonQuery();
                    Array.Clear(encryptedPassword);

                    if (rowsAffected == 0)
                    {
                        couldNotFindPolId = true;
                        return false;
                    }
                    else
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
            }
            return false;
        }

        public static bool SetAccountAdminData(string polId, ushort adminData, out bool couldNotFindPolId)
        {
            couldNotFindPolId = false;
            using (MySqlConnection conn = new($"Server={DB_HOST}; Port={DB_PORT}; Database={DB_NAME}; UID={DB_USERNAME}; Password={DB_PASSWORD}"))
            {
                try
                {
                    conn.Open();
                    string query = @"
                    UPDATE 
                       accounts
                    SET adminData = @adminData
                    WHERE polId = @polid
                    ";
                    MySqlCommand cmd = new(query, conn);
                    cmd.Parameters.AddWithValue("@polId", polId);
                    cmd.Parameters.AddWithValue("@adminData", adminData);
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected == 0)
                    {
                        couldNotFindPolId = true;
                        return false;
                    }
                    else
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
            }
            return false;
        }

        public static ulong CreateContentId(string polId, ushort gameId, out bool couldNotFindPolId)
        {
            couldNotFindPolId = false;
            using (MySqlConnection conn = new($"Server={DB_HOST}; Port={DB_PORT}; Database={DB_NAME}; UID={DB_USERNAME}; Password={DB_PASSWORD}"))
            {
                try
                {
                    conn.Open();
                    string query = @"
                    INSERT INTO characters (polId, creationPosition, customPosition, contentClass)
                    SELECT 
                        @polId, 
                        COALESCE(MAX(creationPosition), 0) + 1, 
                        COALESCE(MAX(creationPosition), 0) + 1, 
                        @contentClass
                    FROM characters
                    WHERE polId = @polId;

                    SELECT LAST_INSERT_ID();
                    ";
                    MySqlCommand cmd = new(query, conn);
                    cmd.Parameters.AddWithValue("@polId", polId);
                    cmd.Parameters.AddWithValue("@contentClass", gameId);
                    object result = cmd.ExecuteScalar();

                    if (result != null)
                    {
                        ulong insertedId = Convert.ToUInt64(result);
                        return insertedId;
                    }
                    return 0;
                }
                catch (MySqlException e)
                {
                    if (e.Number == 1452)
                    {
                        couldNotFindPolId = true;
                        return 0;
                    }
                }
                finally
                {
                    conn.Dispose();
                }
            }
            return 0;
        }

        public static bool DeleteContentId(ulong contentId, out bool couldNotFindContentId)
        {
            couldNotFindContentId = false;
            using (MySqlConnection conn = new($"Server={DB_HOST}; Port={DB_PORT}; Database={DB_NAME}; UID={DB_USERNAME}; Password={DB_PASSWORD}"))
            {
                try
                {
                    conn.Open();
                    string query = @"
                    DELETE FROM characters
                    WHERE id = @contentId
                    ";
                    MySqlCommand cmd = new(query, conn);
                    cmd.Parameters.AddWithValue("@contentId", contentId);
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected == 0)
                    {
                        couldNotFindContentId = true;
                        return false;
                    }
                    else
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
            }
            return false;
        }

        public static bool SetContentIdPaid(ulong contentId, bool isPaid, out bool couldNotFindContentId)
        {
            couldNotFindContentId = false;
            using (MySqlConnection conn = new($"Server={DB_HOST}; Port={DB_PORT}; Database={DB_NAME}; UID={DB_USERNAME}; Password={DB_PASSWORD}"))
            {
                try
                {
                    conn.Open();
                    string query = @"
                    UPDATE 
                       characters
                    SET isPaid = @isPaid
                    WHERE id = @contentId
                    ";
                    MySqlCommand cmd = new(query, conn);
                    cmd.Parameters.AddWithValue("@contentId", contentId);
                    cmd.Parameters.AddWithValue("@isPaid", isPaid);
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected == 0)
                    {
                        couldNotFindContentId = true;
                        return false;
                    }
                    else
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
            }
            return false;
        }

        public static string GetUserInfoAndBuildString(string polId, out bool couldNotFindPolId)
        {
            couldNotFindPolId = false;
            string query;
            MySqlCommand cmd;
            using (MySqlConnection conn = new($"Server={DB_HOST}; Port={DB_PORT}; Database={DB_NAME}; UID={DB_USERNAME}; Password={DB_PASSWORD}"))
            {
                try
                {
                    StringBuilder builder = new();
                    conn.Open();

                    query = @"
                    SELECT
                        a.adminData, 
                        s.loginTime, 
                        s.clientIp, 
                        s.clientPort,
                        st.lastLoginTime, 
                        st.onlineStatus, 
                        st.activeHandleId, 
                        st.activeCharacterId, 
                        st.currentContentId
                    FROM accounts a
                    LEFT JOIN sessions s ON a.polId = s.polId
                    LEFT JOIN status st ON a.polId = st.polId
                    WHERE a.polId = @polId;
                    ";

                    // Get General Info
                    cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@polId", polId);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            ushort adminData = reader.GetUInt16("adminData");
                            byte status = reader.GetByte("onlineStatus");
                            ulong handleId = reader.GetUInt64("activeHandleId");
                            ulong contentId = reader.GetUInt64("activeCharacterId");
                            ushort currentContentClass = reader.GetUInt16("currentContentId");
                            DateTime lastLoginTime = DateTimeOffset.FromUnixTimeSeconds(reader.GetUInt32("lastLoginTime")).LocalDateTime;

                            string clientIp = "";
                            uint clientPort = 0;
                            DateTime? loginTime = null;
                            string authHash = null;
                            string randValue = null;

                            if (!reader.IsDBNull(reader.GetOrdinal("clientIp")))
                            {
                                clientIp = reader.GetString("clientIp");
                                clientPort = reader.GetUInt32("clientPort");
                                loginTime = DateTimeOffset.FromUnixTimeSeconds(reader.GetUInt32("loginTime")).LocalDateTime;

                                byte[] contentAuthHashBytes = reader.GetFieldValue<byte[]>("polRandomValueBinary");
                                byte[] randValBytes = reader.GetFieldValue<byte[]>("polContentAuthHash");

                                authHash = Convert.ToHexString(contentAuthHashBytes);
                                randValue = Convert.ToHexString(randValBytes);
                            }


                            builder.AppendLine($"--- Account Info ---");
                            builder.AppendLine($"  POL ID: {polId.ToUpper()}");
                            builder.AppendLine($"  Last Login: {lastLoginTime:MMMM dd, yyyy hh:mm tt}");
                            builder.AppendLine($"  Active Handle: {handleId} (0x{handleId:X})");
                            builder.AppendLine($"  Active Character: {handleId} (0x{handleId:X})");
                            builder.AppendLine($"  Active Content Class: {currentContentClass} ({PolAuthAdminControl.GetGameIdName(currentContentClass)})");
                            builder.AppendLine($"  Online Status: {status} ({PolAuthAdminControl.GetOnlineStatusName(status)})");

                            if (!string.IsNullOrEmpty(clientIp))
                            {
                                builder.AppendLine($"  Connection Status: ***ONLINE***");
                                builder.AppendLine($"    Login Time: {loginTime:MMMM dd, yyyy hh:mm tt}");
                                builder.AppendLine($"    Client Ip: {clientIp}");
                                builder.AppendLine($"    Client Port: {clientPort}");
                                builder.AppendLine($"    Content Auth Hash: {authHash}");
                                builder.AppendLine($"    Random Value: {randValue}");
                            }
                            else
                                builder.AppendLine($"  Connection Status: ***OFFLINE***");
                        }
                        else
                        {
                            couldNotFindPolId = true;
                            return null;
                        }
                    }

                    builder.AppendLine($"\n--- Content Ids/Characters ---");

                    // Get Content Ids
                    query = @"
                    SELECT
                        id, 
                        subId,
                        gameId,
                        name,
                        info
                    FROM characters
                    WHERE polId = @polId;
                    ";
                    cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@polId", polId);
                    int indx = 0;
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ulong id = reader.GetUInt64("id");
                            uint subId = reader.GetUInt32("subId");
                            ushort gameId = reader.GetUInt16("gameId");
                            string name = reader.GetString("name");
                            string info = reader.GetString("info");

                            builder.AppendLine($"{indx++})");
                            builder.AppendLine($"  Id: {id} (0x{id:X})");
                            builder.AppendLine($"  SubId: {subId} (0x{subId:X})");
                            builder.AppendLine($"  Game Id: {PolAuthAdminControl.GetGameIdName(gameId)}");
                            builder.AppendLine($"  Name: {name}");
                            builder.AppendLine($"  Info: {info}");
                        }
                    }

                    // Finished!!!
                    return builder.ToString();
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

            return null;
        }
    }
}
