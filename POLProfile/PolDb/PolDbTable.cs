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
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace Crystal.POLProfile.PolDb
{
    public abstract class PolDbTable
    {
        public readonly ushort ContentClass;
        public readonly string SqlTableName;
        private readonly int PibSize;

        // FriendData (0xA8) + NotifyStatusData (0x8), the shape ProfileHeader.Write emits
        private const int ProfHeadSize = 0xB0;
        private readonly PolDbColumn[] Columns;
        private readonly Dictionary<string, PolDbColumn> Pib2SqlName;

        public PolDbTable(ushort contentClass, string sqlTableName, PolDbColumn[] columns, int pibSize)
        {
            ContentClass = contentClass;
            SqlTableName = sqlTableName;
            Columns = columns;
            PibSize = pibSize;
            Pib2SqlName = columns.ToDictionary(col => col.PolBESName);
        }

        public PolDbColumn GetColumn(byte index) => Columns[index];

        public string PibNameToSqlName(string pibName) => Pib2SqlName.GetValueOrDefault(pibName).CrystalDbName;

        public bool HasProfHead() => Columns.Where((col) => col.PolBESName.Equals("z_phead")).Any();

        public bool IsHandleIdKey() => Columns.Where((col) => col.PolBESName.Equals("z_hid")).Any();

        public bool IsContentIdKey() => Columns.Where((col) => col.PolBESName.Equals("z_ctid")).Any();

        public virtual string GetSelectQuery(string polProData, List<PolBesColumnArgument> whereCols) => 
            $"SELECT {SelectList()} FROM {SqlTableName} WHERE {WhereClause(polProData, whereCols)}";

        public virtual string GetUpdateQuery(string polProData, byte visibility, List<PolBesColumnArgument> updateCols, List<PolBesColumnArgument> whereCols) =>
            $"UPDATE {SqlTableName} SET {SetClause(visibility, updateCols)} WHERE {WhereClause(polProData, whereCols)}{OwnerScope(polProData)}";

        protected virtual string OwnerScope(string polProData) =>
            IsHandleIdKey() ? $" AND {PibNameToSqlName("z_hid")} IN (SELECT id FROM handles WHERE polId = '{polProData}')" : "";

        protected virtual string SelectList() => string.Join(", ", Columns.Where(col => col.CrystalDbName.Length != 0).Select(col => col.CrystalDbName));
        protected virtual string WhereClause(string polProData, List<PolBesColumnArgument> whereCols)  => string.Join(" AND ", whereCols.Select(col => col.ToSqlWhere(this)));
        protected virtual string SetClause(byte visibility, List<PolBesColumnArgument> updateCols) => string.Join(", ", updateCols.Select(col => col.ToSqlSet(this)));

        public byte[] ReadResults(MySqlDataReader reader, out int numResults)
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            numResults = 0;
            while (reader.Read())
            {
                int resultStart = (int) stream.Position;

                // Read SQL data
                byte visibility = ContentClass == 1000 ? reader.GetByte("visibility") : (byte) 3; // Games are always 3
                object[] row = new object[Columns.Length];
                for (int i = 0; i < Columns.Length; i++)
                {
                    if (Columns[i].CrystalDbName.Length != 0) // Skip undefined cols
                        row[i] = ReadSql(reader, Columns[i]);
                }

                // If we need it, get profheader info
                ProfileHeader profhead = null;
                if (HasProfHead())
                {
                    if (IsHandleIdKey())
                    {
                        ulong handleId = reader.GetUInt64(PibNameToSqlName("z_hid"));
                        profhead = Database.GetProfHeader(handleId: handleId);
                    } 
                    else if (IsContentIdKey())
                    {
                        ulong contentId = reader.GetUInt64(PibNameToSqlName("z_ctid"));
                        profhead = Database.GetProfHeader(contentId: contentId);
                    }
                }

                // Write to result data
                writer.Write(visibility);
                for (int i = 0; i < Columns.Length; i++)
                {
                    if (Columns[i].CrystalDbName.Length == 0) // Write 0 for undefined                    
                        WriteEntry(writer, Columns[i], 0);
                    else
                        WriteEntry(writer, Columns[i], row[i]);
                }
                writer.Seek(resultStart + PibSize, SeekOrigin.Begin);

                // Append Profile Header if this has one
                if (HasProfHead())
                {
                    if (profhead != null)
                    {
                        profhead.Write(writer);
                    }
                    else
                    {
                        writer.Write(new byte[ProfHeadSize]); //Empty
                    }
                }

                numResults++;
            }

            return stream.ToArray();
        }

        private object ReadSql(MySqlDataReader reader, PolDbColumn colDef) => colDef.Type switch
        {
            Datatype.PROFPTR => (ulong) PibSize,
            Datatype.BINARY => reader.GetByte(colDef.CrystalDbName),
            Datatype.STRINGS or Datatype.CHARACTERS => reader.GetString(colDef.CrystalDbName),
            Datatype.SHORT16 => reader.GetInt16(colDef.CrystalDbName),
            Datatype.USHORT16 => reader.GetUInt16(colDef.CrystalDbName),
            Datatype.INT32 => reader.GetInt32(colDef.CrystalDbName),
            Datatype.UINT32 => reader.GetUInt32(colDef.CrystalDbName),
            Datatype.LONG64 => reader.GetInt64(colDef.CrystalDbName),
            Datatype.ULONG64 => reader.GetUInt64(colDef.CrystalDbName),
            _ => null
        };

        private void WriteEntry(BinaryWriter writer, PolDbColumn colDef, object value)
        {
            writer.Write((byte)3);
            switch (colDef.Type)
            {
                case Datatype.PROFPTR:
                    writer.Write((ulong)PibSize);
                    break;
                case Datatype.BINARY:
                    writer.Write((byte)value);
                    break;
                case Datatype.STRINGS:
                case Datatype.CHARACTERS:
                    if (value is int && (int)value == 0)
                    {
                        writer.Seek(colDef.StringLength, SeekOrigin.Current);
                        break;
                    }
                    byte[] str = Encoding.GetEncoding("shift_jis").GetBytes((string)value);
                    int written = Math.Min(str.Length, colDef.StringLength);
                    writer.Write(str, 0, written);
                    writer.Seek(colDef.StringLength - written, SeekOrigin.Current);
                    break;
                case Datatype.SHORT16:
                    writer.Write((short)value);
                    break;
                case Datatype.USHORT16:
                    writer.Write((ushort)value);
                    break;
                case Datatype.INT32:
                    writer.Write((int)value);
                    break;
                case Datatype.UINT32:
                    writer.Write((uint)value);
                    break;
                case Datatype.LONG64:
                    writer.Write((long)value);
                    break;
                case Datatype.ULONG64:
                    writer.Write((ulong)value);
                    break;
                default:
                    throw new InvalidOperationException($"Column {colDef.PolBESName} has non-serializable type {colDef.Type}");
            }
        }
    }
}
