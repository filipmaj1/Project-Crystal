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
using System.Text.RegularExpressions;

namespace Crystal.POLProfile.PolDb
{
    public class PolBesColumnArgument
    {
        private static Regex SqlRegex = new(@"\bz_\w+", RegexOptions.Compiled);
        public readonly PolDbColumn Column;
        public readonly Operation Operation;
        public readonly object Value;

        public PolBesColumnArgument(PolDbColumn column, Operation operation, object value)
        {
            Column = column;
            Operation = operation;
            Value = value;
        }

        public string ToSqlWhere(PolDbTable tableDef)
        {
            if (Operation != Operation.Sql)
            {
                string column = tableDef.PibNameToSqlName(Column.PolBESName);
                string operation = OperationToSql(Operation);
                return $"{column} {operation} {Value}";
            }
            else
            {
                string sql = SqlRegex.Replace(ParsePolSql((string)Value), m =>
                tableDef.PibNameToSqlName(m.Value)
                    ?? throw new ArgumentException($"Unknown PIB column name: {m.Value}"));
                return sql;
            }
        }

        public string ToSqlSet(PolDbTable tableDef)
        {
            string column = tableDef.PibNameToSqlName(Column.PolBESName);
            if (Column.Type == Datatype.STRINGS)
                return $"{column} = '{Value}'";
            else
                return $"{column} = {Value}";
        }

        public override string ToString()
        {
            return $"{Column.PolBESName} {OperationToSql(Operation)} {Value}";
        }


        private static readonly Regex TruncRegex = new(@"\bTRUNC\b", RegexOptions.Compiled);
        private static readonly Regex HexToRawRegex = new(@"\bHEXTORAW\('([0-9A-Fa-f]+)'\)", RegexOptions.Compiled);
        private static readonly Regex EqAnyRegex = new(@"=\s*ANY\b", RegexOptions.Compiled);

        private static string ParsePolSql(string input)
        {
            int nextZero = input.IndexOf('\0');
            if (nextZero > 0)
                input = input[..nextZero];
            input = input.Replace(";", ""); //Remove semicolons, someone is sql injecting.
            input = input.Replace("\u0004", "'");
            input = TruncRegex.Replace(input, "TRUNCATE");
            input = HexToRawRegex.Replace(input, "0x$1");
            input = EqAnyRegex.Replace(input, " IN");
            return input;
        }

        private static string OperationToSql(Operation operation)
        {
            switch (operation)
            {
                case Operation.Equal:
                    return "=";
                case Operation.NotEqual:
                    return "!=";
                case Operation.Greater:
                    return ">";
                case Operation.GreaterEq:
                    return ">=";
                case Operation.Less:
                    return "<";
                case Operation.LessEq:
                    return "<=";
                default:
                    return null;
            }
        }
    }
}
