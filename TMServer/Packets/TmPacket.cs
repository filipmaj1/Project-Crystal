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

using Crystal.Common.MiniGame.Packets;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Crystal.TetraMaster.Packets
{
    public class TmPacket
    {
        private static readonly Regex FindOpcodes = new(@"^(?<catCode>[0-9A-F]{2})00(?<opCode>[0-9A-F]{2})(?<phaseCode>[0-9A-F]{2})@", RegexOptions.Compiled);
        private static readonly Regex FindCmds = new(@"@(?<name>\w+)\=(?<value>(?:\/[\w\s*!@#$%^&]+\=\w+(?:\|[\w\s*!@#$%^&]+)*)*)", RegexOptions.Compiled);

        private readonly string Data;
        public readonly byte CatCode, OpCode, PhaseCode;
        private readonly Dictionary<string, TmGameCommand> Cmds;

        public TmPacket(string data) 
        {
            Data = data;

            // Get opcodes
            MatchCollection opcodeMatch = FindOpcodes.Matches(Data);
            if (opcodeMatch.Count == 1)
            {
                CatCode = byte.Parse(opcodeMatch[0].Groups["catCode"].Value.ToString(), NumberStyles.HexNumber);
                OpCode = byte.Parse(opcodeMatch[0].Groups["opCode"].Value.ToString(), NumberStyles.HexNumber);
                PhaseCode = byte.Parse(opcodeMatch[0].Groups["phaseCode"].Value.ToString(), NumberStyles.HexNumber);
            }
            else
                throw new MgInvalidPacketException("Malformed packet; no opcodes.");

            // Find all msgs
            MatchCollection matches = FindCmds.Matches(Data);
            Cmds = new();
            for (int i = 0; i < matches.Count; i++)
            {
                Match match = matches[i];
                Cmds[match.Groups["name"].Value] = new TmGameCommand(match.Value);
            }
        }

        public bool HasCommand(string name)
        {
            return Cmds.ContainsKey(name);
        }

        public TmGameCommand GetCommand(string name)
        {
            if (Cmds.TryGetValue(name, out TmGameCommand value))
                return value;
            return null;
        }

        public class TmGameCommand
        {
            private static readonly Regex FindParams = new(@"\/(?<name>\w+)=(?<value>[\w\s*!@#$%^&]+(?:\|[\w\s*!@#$%^&]+)*)", RegexOptions.Compiled);
            private readonly string Data;

            public TmGameCommand(string data)
            {
                Data = data;
            }

            private string GetParam(string param, int index)
            {
                MatchCollection matches = FindParams.Matches(Data);

                foreach (Match match in matches)
                {
                    if (match.Groups.ContainsKey("name") && match.Groups["name"].Value.Equals(param))
                    {
                        if (match.Groups.ContainsKey("value"))
                        {
                            string[] paramValueList = match.Groups["value"].Value.Split('|');
                            if (index < paramValueList.Length)
                                return paramValueList[index];
                        }
                    }
                }

                return null;
            }

            public string GetName()
            {
                return Data[1..Data.IndexOf("=")];
            }

            public string GetParamStr(string param, int index)
            {
                return GetParam(param, index);
            }

            public string GetParamHexStr(string param, int index)
            {
                string strVal = GetParam(param, index);
                if (strVal != null && strVal.Length % 2 == 0)
                {
                    int c = 0;
                    char[] charArray = new char[strVal.Length / 2];
                    for (int i = 0; i < charArray.Length; i++)
                    {
                        charArray[c++] = (char)((GetHexVal(strVal[i << 1]) << 4) + GetHexVal(strVal[(i << 1) + 1]));
                    }
                    return new string(charArray);
                }
                return null;
            }

            public bool GetParamId(string param, int index, out ulong result)
            {
                result = 0;
                string strVal = GetParam(param, index);
                if (strVal != null && strVal.Length == 2 * 8)
                {
                    int c = 0;
                    byte[] byteArray = new byte[8];
                    for (int i = 0; i < byteArray.Length; i++)
                    {
                        byteArray[7 - c++] = (byte)((GetHexVal(strVal[i << 1]) << 4) + GetHexVal(strVal[(i << 1) + 1]));
                    }
                    result = BitConverter.ToUInt64(byteArray);
                    return true;
                }
                return false;
            }

            public bool GetParamNumber(string param, int index, out int result)
            {
                result = 0;
                string strVal = GetParam(param, index);
                if (strVal != null)
                {
                    if (!int.TryParse(strVal, out int val))
                        return false;
                    result = val;
                    return true;
                }
                return false;
            }

            private static int GetHexVal(char hex)
            {
                int val = hex;
                return val - (val < 58 ? 48 : 55);
            }

            public override string ToString()
            {
                return Data;
            }
        }
    }

    
}
