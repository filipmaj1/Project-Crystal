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
using Crystal.POLProfile.PolDb.Tables;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Crystal.POLProfile.PolDb
{
    public enum Datatype
    {
        CHARACTERS = 0,
        STRINGS = 1,
        BINARY = 2,
        SHORT16 = 3,
        USHORT16 = 4,
        INT32 = 5,
        UINT32 = 6,
        LONG64 = 7,
        ULONG64 = 8,
        PROFPTR = 9
    }

    public enum Operation : byte
    {
        Sql = 0,
        Equal = 1,
        NotEqual = 2,
        Greater = 3,
        GreaterEq = 4,
        Less = 5,
        LessEq = 6
    }

    public class PolBesDb
    {
        public static string DB_HOST = "127.0.0.1";
        public static string DB_PORT = "3306";
        public static string DB_NAME = "playonline";
        public static string DB_USERNAME = "root";
        public static string DB_PASSWORD = "";

        private static readonly Dictionary<ushort, PolDbTable> DBTables = new PolDbTable[]
        {
            // Games
            new FinalFantasyXiTable(),
            new TetraMasterTable(),
            new JanghourouTable(),
            new FrontMissionOnlineTable(),
            new DirgeOfCerberusTable(),
            new FantasyEarthTable(),
            // Pol Viewer
            new PlayOnlineProfileTable(),
            new ChatChannelTable(),
            new ChatChannelCountTable(),
            new ZoneCountTable(),
            new KbFaqTable(),
            new KbIndextable(),
        }.ToDictionary(table => table.ContentClass);

        public static byte[] SearchPolBES(string polProData, ushort contentClass, List<PolBesColumnArgument> where, out int numResults)
        {
            byte[] results = null;
            numResults = 0;

            // Does table exist?
            if (!DBTables.ContainsKey(contentClass))            
                return null;
            PolDbTable dbTable = DBTables[contentClass];

            // Get From DB
            using (MySqlConnection conn = new($"Server={DB_HOST}; Port={DB_PORT}; Database={DB_NAME}; UID={DB_USERNAME}; Password={DB_PASSWORD}"))
            {
                try
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand(dbTable.GetSelectQuery(polProData, where), conn);
                    using MySqlDataReader reader = cmd.ExecuteReader();
                    results = dbTable.ReadResults(reader, out numResults);
                    Program.Log.Info(Utils.ByteArrayToHex(results));
                }
                catch (MySqlException e)
                {
                    Program.Log.Error(e.ToString());
                    numResults = -1;
                }
                finally
                {
                    conn.Dispose();
                }
            }

            return results;
        }


        public static bool UpdatePolBES(string polProData, byte visibility, ushort contentClass, List<PolBesColumnArgument> updateArgs, List<PolBesColumnArgument> whereArgs)
        {
            // Does table exist?
            if (!DBTables.ContainsKey(contentClass))
            {
                return false;
            }

            // Update DB
            PolDbTable dbTable = DBTables[contentClass];
            using (MySqlConnection conn = new($"Server={DB_HOST}; Port={DB_PORT}; Database={DB_NAME}; UID={DB_USERNAME}; Password={DB_PASSWORD}; Allow User Variables=True"))
            {
                try
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand(dbTable.GetUpdateQuery(polProData, visibility, updateArgs, whereArgs), conn);
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
            }

            return true;
        }

        public static List<PolBesColumnArgument> ReadColumnArgument(ushort contentClass, int numParams, ReadOnlySpan<byte> span, out int bytesRead)
        {
            bytesRead = 0;
            List<PolBesColumnArgument> args = new();

            for (int i = 0; i < numParams; i++)
            {
                byte columnNumber = span[0];
                byte operation = span[1];
                ushort buffSize = MemoryMarshal.Read<ushort>(span[2..]);
                PolDbColumn column = operation != 0 ? DBTables[contentClass].GetColumn(columnNumber) : new PolDbColumn("SQL", "", Datatype.STRINGS);
                object value = GetValue(column.Type, span[8..]);

                span = span[(8 + buffSize)..];

                bytesRead += 8 + buffSize;

                args.Add(new PolBesColumnArgument(column, (Operation)operation, value));
            }

            return args;
        }

        private static object GetValue(Datatype dataType, ReadOnlySpan<byte> buffer)
        {
            switch (dataType)
            {
                case Datatype.CHARACTERS:
                case Datatype.STRINGS:
                    string str = Encoding.UTF8.GetString(buffer);
                    return str.Substring(0, str.IndexOf('\0'));
                case Datatype.BINARY:
                    return MemoryMarshal.Read<byte>(buffer);
                case Datatype.SHORT16:
                    return MemoryMarshal.Read<short>(buffer);
                case Datatype.USHORT16:
                    return MemoryMarshal.Read<ushort>(buffer);
                case Datatype.INT32:
                    return MemoryMarshal.Read<int>(buffer);
                case Datatype.UINT32:
                    return MemoryMarshal.Read<uint>(buffer);
                case Datatype.LONG64:
                    return MemoryMarshal.Read<long>(buffer);
                case Datatype.ULONG64:
                    return MemoryMarshal.Read<ulong>(buffer);
                default:
                    return null;
            }
        }
    }
}
