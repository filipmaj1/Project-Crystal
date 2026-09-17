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
using Crystal.Common.Notification.Payload;
using Crystal.POLProfile.DataObjects;
using Crystal.POLProfile.DataObjects.Pol;
using Crystal.POLProfile.DataObjects.Pol.Character;
using Crystal.POLProfile.DataObjects.Pol.Friend;
using Crystal.POLProfile.DataObjects.Pol.Group;
using Crystal.POLProfile.DataObjects.Pol.Handle;
using Crystal.POLProfile.DataObjects.Pol.Status;
using Crystal.POLProfile.Packets.Receive;
using Crystal.POLProfile.PolDb;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;

namespace Crystal.POLProfile
{
    class Database
    {
        public static string DB_HOST = "127.0.0.1";
        public static string DB_PORT = "3306";
        public static string DB_NAME = "playonline";
        public static string DB_USERNAME = "root";
        public static string DB_PASSWORD = "";

        #region Account
        public static AuthConnectionInfo GetAccountSession(string ip, int port)
        {
            using MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD));
            try
            {
                conn.Open();
                MySqlCommand cmd = new("SELECT sessions.polId AS polId, activeHandleId AS handleId, blowkey, rkey1, rkey2, loginTime FROM sessions LEFT JOIN status ON status.polId = sessions.polId WHERE clientIp = @ip AND clientPort = @port", conn);
                cmd.Parameters.AddWithValue("@ip", ip);
                cmd.Parameters.AddWithValue("@port", port);
                using MySqlDataReader Reader = cmd.ExecuteReader();
                while (Reader.Read())
                {
                    string polId = Reader.GetString("polId");
                    ulong currentHandleId = !Reader.IsDBNull(Reader.GetOrdinal("handleId")) ? Reader.GetUInt64("handleId") : 0;
                    ulong blowfishKey = Reader.GetUInt64("blowkey");
                    uint rkey1 = Reader.GetUInt32("rkey1");
                    uint rkey2 = Reader.GetUInt32("rkey2");
                    uint loginTime = Reader.GetUInt32("loginTime");
                    return new AuthConnectionInfo(polId, currentHandleId, BitConverter.GetBytes(blowfishKey), rkey1, rkey2, loginTime);
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
        #endregion

        #region Handles
        public static ulong GetHandleIdByPosition(string polProData, byte creationPosition)
        {
            ulong handleId = 0;
            using (MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD)))
            {
                try
                {
                    conn.Open();
                    MySqlCommand cmd = new("SELECT id FROM handles WHERE polId = @polId AND creationPosition = @creationPosition", conn);
                    cmd.Parameters.AddWithValue("@polId", polProData);
                    cmd.Parameters.AddWithValue("@creationPosition", creationPosition);
                    object result = cmd.ExecuteScalar();
                    if (result != null)
                        handleId = Convert.ToUInt64(result);
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
            return handleId;
        }

        public static List<HandleData> LoadHandleList(string polId)
        {
            string query;
            MySqlCommand cmd;

            List<HandleData> handles = new();

            //Get From DB
            using (MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD)))
            {
                try
                {
                    conn.Open();
                    query = @"
                        SELECT 
                            handles.id,
                            handles.creationPosition,
                            handles.customPosition,
                            handles.name, 
                            poldb_profiles.portrait,
                            handles.comment 
                        FROM handles 
                        LEFT JOIN poldb_profiles ON handles.id = poldb_profiles.handleId
                        WHERE handles.polId = @polId 
                        ORDER BY handles.creationPosition DESC
                    ";
                    cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@polId", polId);
                    using MySqlDataReader Reader = cmd.ExecuteReader();
                    while (Reader.Read())
                    {
                        handles.Add(HandleData.FromSql(Reader));
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

            return handles;
        }

        public static void UpdateHandlePositions(string polId, Span<byte> positionBlock)
        {
            string query;
            MySqlCommand cmd;

            using MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD));
            try
            {
                conn.Open();
                query = "UPDATE handles SET customPosition = @newPosition WHERE polId = @polId AND creationPosition = @creationPosition";
                cmd = new MySqlCommand(query, conn);

                using var transaction = conn.BeginTransaction();
                cmd.Transaction = transaction;
                for (int i = 0; i < 0x40; i++)
                {
                    cmd.Parameters.AddWithValue("@polId", polId);
                    cmd.Parameters.AddWithValue("@creationPosition", i);
                    cmd.Parameters.AddWithValue("@newPosition", positionBlock[i]);
                    cmd.ExecuteNonQuery();
                    cmd.Parameters.Clear();
                }
                transaction.Commit();
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

        public static ulong StoreHandle(string polId, HandleUpdate update)
        {
            string query;
            MySqlCommand cmd;
            ulong modified = ulong.MaxValue;

            using (MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD)))
            {
                try
                {
                    conn.Open();
                    query = "INSERT INTO handles (polId, creationPosition, customPosition, name) VALUES (@polId, @creationPosition, @creationPosition, @newName) ON DUPLICATE KEY UPDATE name = @newName";
                    cmd = new MySqlCommand(query, conn);

                    cmd.Parameters.AddWithValue("@polId", polId);
                    cmd.Parameters.AddWithValue("@creationPosition", update.CreationPosition);
                    cmd.Parameters.AddWithValue("@newName", update.Name);
                    cmd.ExecuteNonQuery();
                    modified = (ulong)cmd.LastInsertedId;
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

            return modified;
        }

        public static void DeleteHandle(string polId, byte creationPosition)
        {
            MySqlCommand cmd;

            using MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD));
            try
            {
                string query = @"
                        DELETE FROM handles 
                        WHERE polId = @polId AND creationPosition = @creationPosition
                    ";
                conn.Open();
                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@polId", polId);
                cmd.Parameters.AddWithValue("@creationPosition", creationPosition);
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
        #endregion

        #region Characters
        public static List<CharacterData> LoadCharacterList(string polId)
        {
            string query;
            MySqlCommand cmd;

            List<CharacterData> characters = new();

            //Get From DB
            using (MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD)))
            {
                try
                {
                    conn.Open();
                    query = $"SELECT id, subId, contentClass, name, info, creationPosition, customPosition, linkPosition, handleCreationPosition FROM characters WHERE polId = @polId ORDER BY creationPosition ASC";
                    cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@polId", polId);
                    using MySqlDataReader Reader = cmd.ExecuteReader();
                    while (Reader.Read())
                    {
                        characters.Add(CharacterData.FromSql(Reader));
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

            return characters;
        }

        public static void UpdateCharacterOrder(string polId, ReadOnlySpan<byte> orderUpdates)
        {
            string query;
            MySqlCommand cmd;

            using MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD));
            try
            {
                conn.Open();
                query = "UPDATE characters SET customPosition = @newPosition WHERE polId = @polId AND creationPosition = @creationPosition";
                cmd = new MySqlCommand(query, conn);

                var transaction = conn.BeginTransaction();
                cmd.Transaction = transaction;
                for (int i = 0; i < 0x40; i++)
                {
                    cmd.Parameters.AddWithValue("@polId", polId);
                    cmd.Parameters.AddWithValue("@creationPosition", i);
                    cmd.Parameters.AddWithValue("@newPosition", orderUpdates[i]);
                    cmd.ExecuteNonQuery();
                    cmd.Parameters.Clear();
                }
                transaction.Commit();
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

        public static void UpdateCharacterLinks(string polId, ReadOnlySpan<CharacterUpdate> characterUpdates)
        {
            string query;
            MySqlCommand cmd;

            using MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD));
            try
            {
                conn.Open();
                query = @"
                    UPDATE characters 
                    SET 
                        handleCreationPosition = @handlePosition,
                        linkPosition = @linkPosition
                    WHERE polId = @polId AND characters.creationPosition = @creationPosition";
                cmd = new MySqlCommand(query, conn);

                int creationPosition = -1;
                var transaction = conn.BeginTransaction();
                cmd.Transaction = transaction;

                foreach (CharacterUpdate charaUpdate in characterUpdates)
                {
                    creationPosition++;
                    if (charaUpdate.Mode == 0)
                        continue;

                    cmd.Parameters.AddWithValue("@polId", polId);
                    cmd.Parameters.AddWithValue("@creationPosition", creationPosition);
                    cmd.Parameters.AddWithValue("@handlePosition", charaUpdate.Mode == 1 ? charaUpdate.HandleNumber : 0xFF);
                    cmd.Parameters.AddWithValue("@linkPosition", charaUpdate.Mode == 1 ? charaUpdate.CharacterIndexOfHandleNameList : 0x0);

                    cmd.ExecuteNonQuery();
                    cmd.Parameters.Clear();
                }

                transaction.Commit();
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
        #endregion

        #region Friend List
        public static List<FriendData> LoadFriendList(string polProData)
        {
            List<FriendData> friendDataList = new();
            string query;
            MySqlCommand cmd;

            //Get From DB
            using (MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD)))
            {
                try
                {
                    conn.Open();
                    query = @"
                        SELECT 
                            friendPolId AS polProId,
                            friendHandleId AS handleId,
                            handles.creationPosition AS handlePosition,
                            friendlist.name,
                            friendlist.creationPosition,
                            friendlist.customPosition,
                            friendlist.blacklist,
                            friendlist.temp,
                            friendlist.level,
                            friendlist.grp
                        FROM friendlist 
                        LEFT JOIN handles ON handles.id = friendlist.friendHandleId
                        WHERE ownerPolId = @polId
                    ";

                    cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@polId", polProData);
                    using MySqlDataReader Reader = cmd.ExecuteReader();
                    while (Reader.Read())
                    {
                        friendDataList.Add(FriendData.FromSql(Reader));
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

            return friendDataList;
        }

        public static FriendData LoadSpecificFriend(string polProData, byte num)
        {
            string query;
            MySqlCommand cmd;

            //Get From DB
            using (MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD)))
            {
                try
                {
                    conn.Open();
                    query = @"
                        SELECT 
                            friendPolId AS polProId,
                            friendHandleId AS handleId,
                            handles.creationPosition AS handlePosition,
                            friendlist.name,
                            friendlist.creationPosition,
                            friendlist.customPosition,
                            friendlist.blacklist,
                            friendlist.temp,
                            friendlist.level,
                            friendlist.grp
                        FROM friendlist 
                        LEFT JOIN handles ON handles.id = friendlist.friendHandleId
                        WHERE ownerPolId = @polId AND friendlist.creationPosition = @creationPosition
                    ";

                    cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@polId", polProData);
                    cmd.Parameters.AddWithValue("@creationPosition", num);
                    using MySqlDataReader Reader = cmd.ExecuteReader();
                    while (Reader.Read())
                    {
                        return FriendData.FromSql(Reader);
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

            FriendData badFriend = new FriendData();
            badFriend.IsValid = false;
            return badFriend;
        }

        public static bool StoreFriend(string polProData, FriendData friend)
        {
            string query;
            MySqlCommand cmd = null;

            using MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD));
            try
            {
                conn.Open();
                query = @"                         
                            INSERT INTO friendlist
                            (ownerPolId, friendPolId, name, creationPosition, customPosition, blacklist, temp, level, grp, friendHandleId)
                            VALUES 
                            (@ownerPolId, @friendPolId, @name, @creationPosition, @customPosition, @blacklist, @temp, @level, @group, (SELECT id FROM handles WHERE polId = @friendPolId AND creationPosition = @friendHandleNumber)) 
                            ON DUPLICATE KEY UPDATE 
                            blacklist = @blacklist, temp = @temp, level = @level, grp = @group, name = @name
                    ";
                cmd = new MySqlCommand(query, conn, conn.BeginTransaction());

                cmd.Parameters.AddWithValue("@ownerPolId", polProData);
                cmd.Parameters.AddWithValue("@friendPolId", friend.PolProData);
                cmd.Parameters.AddWithValue("@friendHandleNumber", friend.HandlePosition);
                cmd.Parameters.AddWithValue("@name", friend.Name);
                cmd.Parameters.AddWithValue("@creationPosition", friend.CreationPosition);
                cmd.Parameters.AddWithValue("@customPosition", friend.CustomPosition);
                cmd.Parameters.AddWithValue("@blacklist", friend.IsBlackList);
                cmd.Parameters.AddWithValue("@temp", friend.Temporary);
                cmd.Parameters.AddWithValue("@level", friend.Level);
                cmd.Parameters.AddWithValue("@group", friend.Group);

                cmd.ExecuteNonQuery();

                // Committing a real entry is what confirms the link, so the other side stops
                // being provisional. Storing a TEMPORARY entry must not do this - a friend
                // request arrives as one, and promoting it would broadcast the requester as
                // online before the request was ever accepted. My own row already carries
                // whatever my client sent in @temp.
                if (!friend.Temporary && !friend.IsBlackList)
                {
                    cmd.CommandText = @"
                            UPDATE friendlist SET temp = 0
                            WHERE ownerPolId = @friendPolId AND friendPolId = @ownerPolId
                    ";

                    cmd.ExecuteNonQuery();
                }

                cmd.Transaction.Commit();
            }
            catch (MySqlException e)
            {
                cmd.Transaction.Rollback();
                Program.Log.Error(e.ToString());
                return false;
            }
            finally
            {
                conn.Dispose();
            }
            return true;
        }

        public static bool DeleteFriend(string polProData, FriendData fdata)
        {
            string query;
            MySqlCommand cmd;

            using MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD));
            try
            {
                conn.Open();
                query = @"
                        DELETE FROM friendlist 
                        WHERE ownerPolId = @ownerPolId AND creationPosition = @creationPosition
                    ";
                cmd = new MySqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@ownerPolId", polProData);
                cmd.Parameters.AddWithValue("@friendPolId", fdata.PolProData);
                cmd.Parameters.AddWithValue("@creationPosition", fdata.CreationPosition);

                cmd.ExecuteNonQuery();
            }
            catch (MySqlException e)
            {
                Program.Log.Error(e.ToString());
                return false;
            }
            finally
            {
                conn.Dispose();
            }
            return true;
        }
        
        public static bool RemoveTempFlag(string ownerPolProData, string friendPolProData)
        {
            string query;
            MySqlCommand cmd;

            using MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD));
            try
            {
                conn.Open();
                query = @"                         
                            UPDATE friendlist 
                            SET temp = 0 
                            WHERE ownerPolId = @ownerPolId AND friendPolId = @friendPolId
                    ";
                cmd = new MySqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@ownerPolId", ownerPolProData);
                cmd.Parameters.AddWithValue("@friendPolId", friendPolProData);

                cmd.ExecuteNonQuery();
            }
            catch (MySqlException e)
            {
                Program.Log.Error(e.ToString());
                return false;
            }
            finally
            {
                conn.Dispose();
            }
            return true;
        }

        public static List<DbFriendPolIdAndPosition> GetFriendPolIdAndNum(string polid, string friendPolProData = null, bool requireMutual = true, byte? handleNum = null)
        {
            List<DbFriendPolIdAndPosition> result = new();
            string query;
            MySqlCommand cmd;

            //Get From DB
            using (MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD)))
            {
                try
                {
                    conn.Open();
                    // A friend entry names one of my handles. Normally that is the one I am on,
                    // but a handle switch has to speak for the handle being left behind.
                    string myHandleId = handleNum == null ? "status.activeHandleId"
                        : "(SELECT id FROM handles WHERE polId = @polId AND creationPosition = @handleNum)";
                    query = @$"
                    SELECT
                      friendlist.ownerPolId,
                      friendlist.creationPosition
                    FROM friendlist
                    INNER JOIN status ON status.polId = @polId
                    WHERE friendlist.friendPolId = @polId AND friendlist.friendHandleId = {myHandleId}
                    ";

                    // Narrow it to one friend instead of everyone who has me listed
                    if (friendPolProData != null)
                        query += " AND friendlist.ownerPolId = @friendPolId";

                    // Only a live mutual link earns status. If they dropped me, blacklisted me, or
                    // are only a temp entry, they stop hearing about me. Matches the rule
                    // GetAllFriendStatusForLogin applies to the friend list the client loads.
                    if (requireMutual)
                        query += @"
                            AND friendlist.blacklist = 0
                            AND friendlist.temp = 0
                            AND EXISTS (
                                SELECT 1 FROM friendlist AS Mine
                                WHERE Mine.ownerPolId  = @polId
                                  AND Mine.friendPolId = friendlist.ownerPolId
                                  AND Mine.blacklist   = 0
                                  AND Mine.temp        = 0
                                  AND Mine.friendHandleId = (SELECT activeHandleId FROM status AS TheirStatus
                                                             WHERE TheirStatus.polId = friendlist.ownerPolId)
                            )";

                    cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@polId", polid);
                    if (handleNum != null)
                        cmd.Parameters.AddWithValue("@handleNum", handleNum);
                    if (friendPolProData != null)
                        cmd.Parameters.AddWithValue("@friendPolId", friendPolProData);
                    using MySqlDataReader Reader = cmd.ExecuteReader();
                    while (Reader.Read())
                    {
                        string polId = Reader.GetString("ownerPolId");
                        byte creationPosition = Reader.GetByte("creationPosition");
                        result.Add(new DbFriendPolIdAndPosition(polId, creationPosition));
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

        public static List<DbFriendStatusResult> GetAllFriendStatusForLogin(string polProData)
        {
            string query;
            MySqlCommand cmd;
            List<DbFriendStatusResult> statusResults = new();

            //Get From DB
            using (MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD)))
            {
                try
                {
                    conn.Open();
                    query = @$"
                        SELECT
                           Friend.ownerPolId AS polId,
                           Friend.creationPosition AS friendFLNum,
                           Myself.creationPosition AS myFLNum,
                           friendHandle.creationPosition AS activeHandleNumber,
                           IFNULL(status.isOnline AND status.activeHandleId = Myself.friendHandleId, 0) AS isOnline,
                           IFNULL(status.hasActiveCharacter AND status.activeHandleId = Myself.friendHandleId, 0) AS hasActiveCharacter,
                           IF(status.activeHandleId = Myself.friendHandleId, characters.creationPosition, NULL) AS activeCharacterNumber,
                           IFNULL(IF(status.activeHandleId = Myself.friendHandleId, status.currentContentClass, 0), 0) AS currentContentClass,
                           IFNULL(status.canReceiveMsgs, 0) AS canReceiveMsgs,
                           IFNULL(status.onlineStatus, 0) AS onlineStatus,
                           IFNULL(poldb_profiles.portrait, 0) AS portrait,
                           IFNULL(friendHandle.comment, '') AS comment
                        FROM friendlist AS Friend
                           JOIN friendlist AS Myself ON Myself.ownerPolId  = Friend.friendPolId AND
                                                        Myself.friendPolId = Friend.ownerPolId AND
                                                        Myself.blacklist   = 0 AND
                                                        Myself.temp        = 0
                           JOIN handles AS friendHandle ON friendHandle.id = Myself.friendHandleId
                           LEFT JOIN poldb_profiles ON poldb_profiles.handleId = Myself.friendHandleId
                           LEFT JOIN status     ON status.polId    = Friend.ownerPolId
                           LEFT JOIN characters ON characters.id   = status.activeCharacterId
                        WHERE
                           Friend.friendPolId = @polProId AND
                           Friend.blacklist = 0 AND
                           Friend.temp = 0 AND
                           Friend.friendHandleId = (SELECT activeHandleId FROM status AS MyStatus WHERE MyStatus.polId = Friend.friendPolId);
                    ";
                    cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@polProId", polProData);
                    using MySqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        string polIdData = reader.GetString("polId");
                        byte otherFriendlistPosition = reader.GetByte("friendFLNum");
                        byte myFriendListPosition = reader.GetByte("myFLNum");
                        byte activeHandleNum = !reader.IsDBNull(reader.GetOrdinal("activeHandleNumber")) ? reader.GetByte("activeHandleNumber") : (byte) 0xFF;
                        bool hasActiveCharacter = reader.GetBoolean("hasActiveCharacter");
                        byte currentActiveCharacter = !reader.IsDBNull(reader.GetOrdinal("activeCharacterNumber")) ? reader.GetByte("activeCharacterNumber") : (byte) 0xFF;
                        bool isOnline = reader.GetBoolean("isOnline");
                        bool canReceiveMsgs = reader.GetBoolean("canReceiveMsgs");
                        byte onlineStatus = reader.GetByte("onlineStatus");
                        ushort currentContentClass = reader.GetUInt16("currentContentClass");

                        StatusData status = new()
                        {
                            ActiveHandleNumber = activeHandleNum,
                            HasActiveCharacter = (byte)(hasActiveCharacter ? 1 : 0),
                            ActiveCharacterIndex = currentActiveCharacter,
                            CanReceiveOnlineMessage = (byte)(isOnline ? 1 : 0x0),
                            CurrentContentsClass = currentContentClass,
                            OpenStat = (byte)(isOnline ? onlineStatus : 0),
                            FriendAuthMode = 0,
                            LastLoginTime = 0,
                            CurrentLoginTime = 0
                        };

                        uint portraitId = reader.GetUInt32("portrait");
                        string comment = reader.GetString("comment");
                        byte[] payload = new FriendPayloadBuilder(true).Comment(comment).DisplayPic(portraitId).BuildPayload();

                        statusResults.Add(new DbFriendStatusResult(status, polIdData, myFriendListPosition, otherFriendlistPosition, payload));
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

            return statusResults;
        }
        #endregion

        #region Status
        public static Tuple<uint, string> GetCommentAndDisplayPicId(string polId, byte? handleNum = null)
        {
            string query;
            MySqlCommand cmd;

            //Get From DB
            using MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD));
            try
            {
                conn.Open();
                // Portrait and comment belong to a handle, so a notice speaking for one has to name
                // it. Editing a handle you are not currently on would otherwise broadcast the
                // active handle's picture against the edited handle's number.
                string handleFilter = handleNum == null
                    ? "handles.id = (SELECT activeHandleId FROM status WHERE status.polId = @polId)"
                    : "handles.creationPosition = @handleNum";

                query = @$"
                    SELECT
                        IFNULL(poldb_profiles.portrait, 0) AS portrait,
                        IFNULL(handles.comment, '') AS comment
                    FROM handles
                    LEFT JOIN poldb_profiles ON poldb_profiles.handleId = handles.id
                    WHERE handles.polId = @polId AND {handleFilter}
                    ";
                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@polId", polId);
                if (handleNum != null)
                    cmd.Parameters.AddWithValue("@handleNum", handleNum);
                using MySqlDataReader Reader = cmd.ExecuteReader();
                while (Reader.Read())
                {
                    uint portraitId = Reader.GetUInt32("portrait");
                    string comment = Reader.GetString("comment");

                    return new Tuple<uint, string>(portraitId, comment);
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

        public static DbFriendStatusResult GetStatus(string polProData)
        {
            string query;
            MySqlCommand cmd;

            //Get From DB
            using (MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD)))
            {
                try
                {
                    conn.Open();
                    query = @"
                    SELECT 
                        handles.creationPosition AS activeHandleNumber,
                        status.hasActiveCharacter,
                        characters.creationPosition AS activeCharacterNumber,
                        status.isOnline,
                        status.canReceiveMsgs,
                        status.onlineStatus,
                        status.currentContentClass,
                        status.lastLoginTime,
                        sessions.loginTime
                    FROM status
                        LEFT JOIN sessions   ON sessions.polId  = status.polId
                        LEFT JOIN handles    ON handles.id      = status.activeHandleId
                        LEFT JOIN characters ON characters.id   = status.activeCharacterId
                    WHERE status.polId = @polId
                    ";
                    cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@polId", polProData);
                    using MySqlDataReader Reader = cmd.ExecuteReader();
                    while (Reader.Read())
                    {
                        byte activeHandleId = !Reader.IsDBNull(Reader.GetOrdinal("activeHandleNumber")) ? Reader.GetByte("activeHandleNumber") : (byte) 0xFF;
                        bool isOnline = Reader.GetBoolean("isOnline");
                        byte onlineStatus = Reader.GetByte("onlineStatus");
                        bool canReceiveMsgs = Reader.GetBoolean("canReceiveMsgs");
                        bool hasActiveCharacter = Reader.GetBoolean("hasActiveCharacter");
                        byte currentActiveCharacter = !Reader.IsDBNull(Reader.GetOrdinal("activeCharacterNumber")) ? Reader.GetByte("activeCharacterNumber") : (byte)0xFF;
                        ushort currentContentClass = Reader.GetUInt16("currentContentClass");
                        uint lastLoginTime = Reader.GetUInt32("lastLoginTime");
                        uint loginTime = !Reader.IsDBNull("loginTime") ? Reader.GetUInt32("loginTime") : 0;

                        StatusData status = new()
                        {
                            ActiveHandleNumber = activeHandleId,
                            HasActiveCharacter = (byte) (hasActiveCharacter ? 1 : 0),
                            ActiveCharacterIndex = currentActiveCharacter,
                            CanReceiveOnlineMessage = (byte)(isOnline ? (canReceiveMsgs ? 0x1 : 0x2) : 0x0),
                            CurrentContentsClass = currentContentClass,
                            OpenStat = (byte)(isOnline ? onlineStatus : 0),
                            FriendAuthMode = 0,
                            LastLoginTime = lastLoginTime,
                            CurrentLoginTime = loginTime
                        };

                        return new DbFriendStatusResult(status, "", 0, 0, null);
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

        public static void SaveContentsAuth(string polId, byte[] randomValue, byte[] authHash)
        {
            string query;
            MySqlCommand cmd;

            using MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD));
            try
            {
                conn.Open();
                query = @"
                    UPDATE sessions                    
                    SET
	                    polRandomValueBinary    = @randomValueBinary,
                        polContentAuthHash      = @authHash
                    WHERE polId = @polId
                    ";

                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@polId", polId);
                cmd.Parameters.Add("@randomValueBinary", MySqlDbType.VarBinary).Value = randomValue;
                cmd.Parameters.Add("@authHash", MySqlDbType.VarBinary).Value = authHash;
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

        public static void UpdateStatus(string polId, StatusInfo request)
        {
            string query;
            MySqlCommand cmd;

            using MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD));
            try
            {
                conn.Open();
                query = @"
                    UPDATE status
                    INNER JOIN handles ON handles.polId = status.polId
                    SET
	                    activeHandleId = handles.id,
                        currentContentClass = @currentContentClass,
                        onlineStatus = @onlineStatus
                    WHERE status.polId = @polId AND handles.creationPosition = @activeHandleNumber;
                    ";

                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@polId", polId);
                cmd.Parameters.AddWithValue("@activeHandleNumber", request.ActiveHandleNumber);
                cmd.Parameters.AddWithValue("@currentContentClass", request.CurrentContentsClass);
                cmd.Parameters.AddWithValue("@onlineStatus", request.OpenStat);
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

        public static void UpdateComment(string polId, string comment)
        {
            string query;
            MySqlCommand cmd;

            using MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD));
            try
            {
                conn.Open();
                // The 0x403 request carries no handle number, so it always means the one I am on.
                // Without the id match this wrote the comment onto every handle of the account.
                query = @"
                    UPDATE handles SET comment = @comment
                    WHERE polId = @polId
                      AND id = (SELECT activeHandleId FROM status WHERE status.polId = @polId)";
                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@polId", polId);
                cmd.Parameters.AddWithValue("@comment", comment);
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
        #endregion

        #region Groups
        public static List<DbGroupMemberStatusResult> GetAllGroupMemberStatuses(ulong groupId)
        {
            List<DbGroupMemberStatusResult> members = new();
            string query;
            MySqlCommand cmd;

            using (MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD)))
            {
                try
                {
                    conn.Open();
                    query = @"
                SELECT
                    group_members.polId,
                    group_members.rank,
                    group_members.comment,
                    group_members.onlineStatus AS groupOnlineStatus,
                    handles.id AS handleId,
                    handles.creationPosition,
                    handles.name,
                    IFNULL(poldb_profiles.portrait, 0) AS portrait,
                    IF(status.activeHandleId = group_members.handleId, status.isOnline, 0) AS isOnline,
                    IFNULL(status.canReceiveMsgs, 0) AS canReceiveMsgs,
                    IFNULL(status.hasActiveCharacter, 0) AS hasActiveCharacter,
                    IFNULL(status.currentContentClass, 0) AS currentContentClass
                FROM group_members
                JOIN handles ON handles.id = group_members.handleId
                LEFT JOIN status ON status.polId = group_members.polId
                LEFT JOIN poldb_profiles ON poldb_profiles.handleId = group_members.handleId
                WHERE group_members.groupId = @groupId
                ORDER BY group_members.joinDate
            ";
                    cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@groupId", groupId);
                    using MySqlDataReader Reader = cmd.ExecuteReader();
                    while (Reader.Read())
                    {
                        bool isOnline = Reader.GetBoolean("isOnline");
                        bool canReceiveMsgs = Reader.GetBoolean("canReceiveMsgs");

                        StatusData status = new()
                        {
                            ActiveHandleNumber = Reader.GetByte("creationPosition"),
                            HasActiveCharacter = (byte)(isOnline && Reader.GetBoolean("hasActiveCharacter") ? 1 : 0),
                            CanReceiveOnlineMessage = (byte)(isOnline ? (canReceiveMsgs ? 0x1 : 0x2) : 0x0),
                            CurrentContentsClass = isOnline ? Reader.GetUInt16("currentContentClass") : (ushort)0,
                            OpenStat = (byte)(isOnline ? Reader.GetByte("groupOnlineStatus") : 0),
                        };

                        status.OpenStat++;

                        members.Add(new DbGroupMemberStatusResult(
                            Reader.GetString("polId"),
                            Reader.GetUInt64("handleId"),
                            Reader.GetByte("creationPosition"),
                            Reader.GetString("name"),
                            Reader.GetByte("rank"),
                            Reader.GetString("comment"),
                            Reader.GetUInt32("portrait"),
                            status));
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

            return members;
        }

        public static DbGroupMemberStatusResult GetGroupMemberStatus(string polProData, ulong groupId, byte? handleNum = null)
        {
            string query;
            MySqlCommand cmd;

            using (MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD)))
            {
                try
                {
                    conn.Open();
                    // A status notice speaks for one handle and names it - a handle switch has to
                    // speak for the handle being left, whose row no longer matches
                    // status.activeHandleId. Everything else wants my membership whichever handle
                    // holds it, since the group list is account-wide.
                    string myMembership = handleNum == null ? "group_members.polId = status.polId"
                        : "group_members.handleId = (SELECT id FROM handles WHERE polId = @polId AND creationPosition = @handleNum)";
                    query = @$"
                        SELECT
                            group_members.groupId,
                            group_members.polId,
                            group_members.rank,
                            group_members.comment,
                            group_members.onlineStatus AS groupOnlineStatus,
                            handles.id AS handleId,
                            handles.creationPosition,
                            handles.name,
                            IFNULL(poldb_profiles.portrait, 0) AS portrait,
                            IF(status.activeHandleId = group_members.handleId, status.isOnline, 0) AS isOnline,
                            status.canReceiveMsgs,
                            status.hasActiveCharacter,
                            status.currentContentClass
                        FROM status
                        JOIN group_members ON {myMembership} AND group_members.groupId = @groupId
                        JOIN handles ON handles.id = group_members.handleId
                        LEFT JOIN poldb_profiles ON poldb_profiles.handleId = group_members.handleId
                        WHERE status.polId = @polId
                    ";
                    cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@polId", polProData);
                    cmd.Parameters.AddWithValue("@groupId", groupId);
                    if (handleNum != null)
                        cmd.Parameters.AddWithValue("@handleNum", handleNum);
                    using MySqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        bool isOnline = reader.GetBoolean("isOnline");
                        bool canReceiveMsgs = reader.GetBoolean("canReceiveMsgs");

                        StatusData status = new()
                        {
                            ActiveHandleNumber = reader.GetByte("creationPosition"),
                            HasActiveCharacter = (byte)(isOnline && reader.GetBoolean("hasActiveCharacter") ? 1 : 0),
                            CanReceiveOnlineMessage = (byte)(isOnline ? (canReceiveMsgs ? 0x1 : 0x2) : 0x0),
                            CurrentContentsClass = isOnline ? reader.GetUInt16("currentContentClass") : (ushort)0,
                            OpenStat = (byte)(isOnline ? reader.GetByte("groupOnlineStatus") : 0)
                        };

                        return new DbGroupMemberStatusResult(
                            reader.GetString("polId"),
                            reader.GetUInt64("handleId"),
                            reader.GetByte("creationPosition"),
                            reader.GetString("name"),
                            reader.GetByte("rank"),
                            reader.GetString("comment"),
                            reader.GetUInt32("portrait"),
                            status);
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

        public static List<GroupMemberNotifyTarget> GetGroupMemberNotifyTargets(string polProData, ulong groupId, byte? handleNum = null)
        {
            List<GroupMemberNotifyTarget> result = new();
            string query;
            MySqlCommand cmd;

            using (MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD)))
            {
                try
                {
                    conn.Open();
                    // A status notice speaks for one handle and names it - a handle switch has to
                    // speak for the handle being left, whose row no longer matches
                    // status.activeHandleId. Everything else wants my membership whichever handle
                    // holds it, since the group list is account-wide.
                    string myMembership = handleNum == null ? "myMembership.polId = status.polId"
                        : "myMembership.handleId = (SELECT id FROM handles WHERE polId = @polId AND creationPosition = @handleNum)";
                    query = @$"
                        SELECT
                            members.groupId,
                            members.polId,
                            activeHandle.creationPosition AS activeHandleNum
                        FROM status
                        JOIN group_members myMembership ON {myMembership}
                        JOIN group_members members ON members.groupId = myMembership.groupId
                        JOIN status targetStatus ON targetStatus.polId = members.polId
                        JOIN handles activeHandle ON activeHandle.id = targetStatus.activeHandleId
                        WHERE status.polId = @polId AND myMembership.groupId = @groupId AND targetStatus.isOnline = 1
                    ";
                    cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@polId", polProData);
                    cmd.Parameters.AddWithValue("@groupId", groupId);
                    if (handleNum != null)
                        cmd.Parameters.AddWithValue("@handleNum", handleNum);
                    using MySqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        result.Add(new GroupMemberNotifyTarget(
                            reader.GetUInt64("groupId"),
                            reader.GetString("polId"),
                            reader.GetByte("activeHandleNum")));
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

        /*
         * Groups an account holds across every one of its handles. The client only has four group
         * slots for the whole account, so the cap has to be counted this way, not per handle.
        */
        public static long GetGroupCount(string polProData)
        {
            long count = 0;

            using (MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD)))
            {
                try
                {
                    conn.Open();
                    MySqlCommand cmd = new("SELECT COUNT(*) FROM group_members WHERE polId = @polId", conn);
                    cmd.Parameters.AddWithValue("@polId", polProData);
                    count = Convert.ToInt64(cmd.ExecuteScalar());
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

            return count;
        }

        public static long CreateGroup(string polProData, string name)
        {
            string query1, query2;
            MySqlCommand cmd;
            long id;

            if (GetGroupCount(polProData) >= GroupSettings.MAX_GROUPS)
                return -3;

            query1 = @"
                        INSERT INTO groups
                        (name)
                        VALUES 
                        (@name)
                    ";

            query2 = @"                         
                        INSERT INTO group_members 
                        (groupId, polId, handleId, rank)
                        VALUES
                        (@groupId, @polId, (SELECT activeHandleId FROM status WHERE polId = @polId), 5);
                    ";

            using (MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD)))
            {
                try
                {
                    conn.Open();
                    cmd = new MySqlCommand(query1, conn);
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.ExecuteNonQuery();
                    id = cmd.LastInsertedId;
                    cmd.CommandText = query2;
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@groupId", id);
                    cmd.Parameters.AddWithValue("@polId", polProData);
                    cmd.ExecuteNonQuery();

                }
                catch (MySqlException e)
                {
                    switch(e.Number)
                    {
                        case 1062:
                            return -2;
                        default:
                            Program.Log.Error(e.ToString());
                            return -1;
                    }
                }
                finally
                {
                    conn.Dispose();
                }
            }

            return id;
        }

        public static bool DeleteGroup(ulong groupId)
        {
            MySqlCommand cmd;

            using MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD));
            try
            {
                string query = @"
                        DELETE FROM groups 
                        WHERE id = @groupId
                    ";
                conn.Open();
                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@groupId", groupId);
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException e)
            {
                Program.Log.Error(e.ToString());
                return false;
            }
            finally
            {
                conn.Dispose();
            }
            return true;
        }

        /*
         * My rank in a group, whichever of my handles holds the membership. MAX so that any
         * legacy duplicate rows resolve to the most permissive one rather than at random.
        */
        public static byte GetGroupMemberRank(ulong groupId, string polProData)
        {
            byte rank = 0;
            using (MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD)))
            {
                try
                {
                    conn.Open();
                    MySqlCommand cmd = new(@"
                        SELECT MAX(group_members.rank) FROM group_members
                        WHERE group_members.polId = @polId AND group_members.groupId = @groupId", conn);
                    cmd.Parameters.AddWithValue("@polId", polProData);
                    cmd.Parameters.AddWithValue("@groupId", groupId);
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        rank = Convert.ToByte(result);
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
            return rank;
        }

        public static bool UpdateGroupMember(ulong groupId, ulong memberPolProId, byte handlePosition, byte rank)
        {
            string query;
            MySqlCommand cmd;
            int rowsChanged = 0;
            bool isDelete = rank == GroupMember.RANK_REMOVED;

            if (rank > GroupMember.RANK_MASTER)
                return false;

            using (MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD)))
            {
                try
                {
                    conn.Open();
                    if (isDelete)
                    {
                        query = @"
                            DELETE group_members FROM group_members
                            JOIN handles ON handles.id = group_members.handleId
                            WHERE group_members.groupId = @groupId
                              AND group_members.polId = @polId
                              AND handles.creationPosition = @handlePosition
                        ";
                    }
                    else
                    {
                        query = @"
                            INSERT INTO group_members (groupId, polId, handleId, `rank`)
                            SELECT @groupId, handles.polId, handles.id, @rank
                            FROM handles
                            WHERE handles.polId = @polId AND handles.creationPosition = @handlePosition
                            ON DUPLICATE KEY UPDATE `rank` = @rank
                        ";
                    }
                    cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@groupId", groupId);
                    cmd.Parameters.AddWithValue("@polId", SqCrypto.PolIdToPolProData(memberPolProId));
                    cmd.Parameters.AddWithValue("@handlePosition", handlePosition);
                    if (!isDelete)
                        cmd.Parameters.AddWithValue("@rank", rank);
                    rowsChanged = cmd.ExecuteNonQuery();
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

            return rowsChanged > 0;
        }

        public static bool SetGroupSettings(string polProData, ulong id, string comment, byte onlineStatus, byte handlePosition)
        {
            string query;
            MySqlCommand cmd;
            int rowsChanged = 0;

            using (MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD)))
            {
                try
                {
                    conn.Open();
                    query = @"
                        UPDATE group_members
                        JOIN handles ON handles.id = group_members.handleId
                        SET group_members.comment = @comment,
                            group_members.onlineStatus = @onlineStatus
                        WHERE group_members.groupId = @groupId
                          AND group_members.polId = @polId
                          AND handles.creationPosition = @handlePosition
                    ";
                    cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@polId", polProData);
                    cmd.Parameters.AddWithValue("@handlePosition", handlePosition);
                    cmd.Parameters.AddWithValue("@groupId", id);
                    cmd.Parameters.AddWithValue("@comment", comment);
                    cmd.Parameters.AddWithValue("@onlineStatus", onlineStatus);
                    rowsChanged = cmd.ExecuteNonQuery();
                }
                catch (MySqlException e)
                {
                    Program.Log.Error(e.ToString());
                    return false;
                }
                finally
                {
                    conn.Dispose();
                }
            }

            return rowsChanged > 0;
        }

        public static List<GroupSettings> GetGroups(string polProData, out byte[] groupCounts)
        {
            List<GroupSettings> groupDataList = new();
            string query;
            MySqlCommand cmd;

            groupCounts = new byte[4];

            //Get From DB
            using (MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD)))
            {
                try
                {
                    conn.Open();
                    query = @"
                        SELECT
                            myMembership.groupId,
                            groupInfo.name AS groupName,
                            myMembership.onlineStatus AS myOnlineStatus,
                            myMembership.comment AS myComment,
                            myMembership.rank AS myRank,
                            myHandle.creationPosition AS myHandlePosition,
                            (SELECT COUNT(*) FROM group_members allMembers WHERE allMembers.groupId = myMembership.groupId) AS numMembers
                        FROM group_members myMembership
                        JOIN handles myHandle ON myHandle.id = myMembership.handleId
                        JOIN groups groupInfo ON groupInfo.id = myMembership.groupId
                        WHERE myMembership.polId = @polId
                        ORDER BY myMembership.joinDate
                        LIMIT 4
                    ";

                    cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@polId", polProData);
                    using MySqlDataReader Reader = cmd.ExecuteReader();
                    int indx = 0;
                    while (Reader.Read())
                    {
                        groupDataList.Add(GroupSettings.FromSql(Reader));
                        groupCounts[indx++] = (byte)Reader.GetInt32("numMembers");
                        if (indx >= 4)
                            break;
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

            return groupDataList;
        }

        /*
         * Every group the account holds, or only the ones belonging to one handle. Status notices
         * speak for a single handle and pass one; roster loads cover the whole list and do not.
        */
        public static ulong[] GetGroupIds(string polProData, byte? handleNum = null)
        {
            List<ulong> groupIdList = new();
            string query;
            MySqlCommand cmd;

            //Get From DB
            using (MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD)))
            {
                try
                {
                    conn.Open();
                    query = @"
                        SELECT
                            myMembership.groupId
                        FROM group_members myMembership
                        JOIN handles ON handles.id = myMembership.handleId
                        WHERE myMembership.polId = @polId
                    ";

                    if (handleNum != null)
                        query += " AND handles.creationPosition = @handleNum";

                    query += @"
                        ORDER BY myMembership.joinDate
                        LIMIT 4
                    ";

                    cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@polId", polProData);
                    if (handleNum != null)
                        cmd.Parameters.AddWithValue("@handleNum", handleNum);
                    using MySqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                        groupIdList.Add(reader.GetUInt64("groupId"));
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

            return groupIdList.ToArray();
        }

        public static List<GroupMember> GetGroupMembers(ulong[] groupIds)
        {
            List<GroupMember> memberList = new();
            string query;
            MySqlCommand cmd;

            using (MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD)))
            {
                try
                {
                    conn.Open();
                    query = @"
                        SELECT 
                            group_members.groupId,
                            group_members.polId,
                            group_members.rank,
                            handles.id AS handleId,
                            handles.creationPosition,
                            handles.name
                        FROM group_members
                        JOIN handles ON handles.id = group_members.handleId
                        WHERE FIND_IN_SET(groupId, @ids)
                        ORDER BY FIND_IN_SET(groupId, @ids), joinDate
                        ";

                    cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@ids", string.Join(",", groupIds));
                    using MySqlDataReader Reader = cmd.ExecuteReader();
                    while (Reader.Read())
                    {
                        memberList.Add(GroupMember.FromSql(Reader));
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

            return memberList;
        }
        /*
        public static List<Tuple<MessageHeader, byte[]>> GetInitialGroupListStatusNotifications(string polProData, ulong groupId)
        {
            string query;
            MySqlCommand cmd;

            List<Tuple<MessageHeader, byte[]>> notifications = new();

            //Get From DB
            using (MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", HOST, PORT, DB_NAME, USERNAME, PASSWORD)))
            {
                try
                {
                    conn.Open();
                    query = @"
                        SELECT 
                            status.polId, 
                            friendlist.creationPosition,
                            IF(status.activeHandleId = handles.id, status.isOnline, 0) AS isOnline, 
                            status.onlineStatus, 
                            status.currentContentClass,
                            poldb_profiles.portrait,
                            handles.comment
                        FROM friendlist
                        LEFT JOIN status ON status.polId = friendlist.friendPolId
                        LEFT JOIN handles ON handles.id = friendlist.friendHandleId
                        LEFT JOIN poldb_profiles ON poldb_profiles.handleId = friendlist.friendHandleId
                        WHERE friendlist.ownerPolId = @polId AND friendlist.blacklist = 0
                        ORDER BY friendlist.creationPosition DESC
                    ";
                    cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@polId", polProData);
                    using MySqlDataReader Reader = cmd.ExecuteReader();
                    while (Reader.Read())
                    {
                        string polIdData = Reader.GetString("polId");
                        byte position = Reader.GetByte("creationPosition");
                        bool isOnline = Reader.GetBoolean("isOnline");
                        byte status = Reader.GetByte("onlineStatus");
                        ushort contentID = Reader.GetUInt16("currentContentClass");
                        uint portraitId = Reader.GetUInt32("portrait");
                        string comment = Reader.GetString("comment");

                        NotifyStatusData statusData = new()
                        {
                            IsOnline = isOnline,
                            OnlineStatus = status,
                            Position = position,
                            ContentId = contentID,
                            ControlFlag1 = 0,
                            ControlFlag2 = 1,
                            ControlFlag3 = 0,
                            ControlFlag4 = 0
                        };
                        MessageHeader header = new()
                        {
                            SourcePolProId = SqCrypto.PolProDataToPolId(polProData, 0, 0),
                            StatusData = statusData,
                            ContentId = contentID,
                        };
                        header.StatusData = statusData;

                        byte[] payload = new FriendPayloadBuilder(true).Comment(comment).DisplayPic(portrait).BuildPayload();

                        notifications.Add(new Tuple<MessageHeader, byte[]>(header, payload));
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

            return notifications;
        }*/
        #endregion

        #region Profiles and Search
        public static ProfileHeader GetProfHeader(ulong handleId = 0, ulong contentId = 0)
        {
            if (handleId == 0 && contentId == 0)
                return null;

            ProfileHeader profHeader = null;
            List<CharacterPrimitive> charaPrims = [];
            string polProData = null;
            byte handleNum = 0;
            string handleName = null;

            using (MySqlConnection conn = new(String.Format("Server={0}; Port={1}; Database={2}; UID={3}; Password={4}", DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD)))
            {
                try
                {
                    conn.Open();

                    // Resolve the owning handle if a character id was provided
                    if (handleId == 0)
                    {
                        MySqlCommand resolveCmd = new(
                            "SELECT handleId FROM characters_primitives WHERE contentId = @contentId", conn);
                        resolveCmd.Parameters.AddWithValue("@contentId", contentId);

                        object resolved = resolveCmd.ExecuteScalar();
                        if (resolved == null)
                            return null; // unknown character
                        handleId = Convert.ToUInt64(resolved);
                    }

                    MySqlCommand handleCmd = new(
                        "SELECT polId, creationPosition, name FROM handles WHERE id = @handleId", conn);
                    handleCmd.Parameters.AddWithValue("@handleId", handleId);

                    using (MySqlDataReader handleReader = handleCmd.ExecuteReader())
                    {
                        if (!handleReader.Read())
                            return null; // unknown handle

                        polProData = handleReader.GetString("polId");
                        handleNum = handleReader.GetByte("creationPosition");
                        handleName = handleReader.GetString("name");
                    }

                    // Get Character Primitives
                    string query = @"
                        SELECT * FROM characters_primitives
                        WHERE handleId = @handleId
                        ORDER BY attachOrder
                        ";

                    MySqlCommand cmd = new(query, conn);
                    cmd.Parameters.AddWithValue("@handleId", handleId);
                    using MySqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        charaPrims.Add(new CharacterPrimitive()
                        {
                            IsValid = 1,
                            AttachOrder = reader.GetByte("attachOrder"),
                            ContentsClass = reader.GetUInt16("contentClass"),
                            ContentsSubUserId = reader.GetUInt32("contentSubId"),
                            ContentsId = reader.GetUInt64("contentId")
                        });
                    }

                    FriendData friendData = FriendData.CreateStranger(SqCrypto.PolProDataToPolId(polProData, 0, 0), handleId, handleNum, handleName, charaPrims);
                    profHeader = new ProfileHeader(friendData, new() { });
                }
                catch (MySqlException e)
                {
                    Program.Log.Error(e.ToString());
                }
            }

            return profHeader;
        }
        #endregion
    }
}
