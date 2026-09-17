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
using System.Text;
using System.Text.RegularExpressions;

namespace Crystal.Common.MiniGame.Packets
{
    public enum PacketType { Game, Lobby, Auction, Rank, Profile }

    public class MgPacket
    {
        public readonly PacketType Type;
        private static readonly Regex FindCmds = new(@"<(?<name>[A-Z][A-Z])>\a([ -~]+)(\u0006[ -~]+)*\a", RegexOptions.Compiled);

        private readonly string PacketString;
        private readonly Dictionary<string, MgCommand> Cmds;

        public MgPacket(PacketType type, string data)
        {
            Type = type;
            PacketString = data;

            // If it's a game pkt, just return the string for the server to parse
            if (type != PacketType.Game)
            {
                // Find all cmds
                MatchCollection matches = FindCmds.Matches(data);
                Cmds = new(matches.Count);
                for (int i = 0; i < matches.Count; i++)
                {
                    Match match = matches[i];
                    Cmds[match.Groups["name"].Value] = new MgCommand(match.Value);
                }
            }
        }

        public static MgPacket Parse(string incoming)
        {
            // Verify Packet
            if (incoming.Length < 5)
                return null;
            if (incoming[0] != 'G')
                return null;
            if (!incoming.Substring(1, 3).Equals(Mg.GameCode))
                return null;

            // Check/Assign type
            PacketType type;
            if (incoming[4] == 'G')
                type = PacketType.Game;
            else if (incoming[4] == 'L')
                type = PacketType.Lobby;
            else if (incoming[4] == 'A')
                type = PacketType.Auction;
            else if (incoming[4] == 'R')
                type = PacketType.Rank;
            else if (incoming[4] == 'P')
                type = PacketType.Profile;
            else
                return null;

            // All good, continue
            return new MgPacket(type, incoming[5..]);
        }

        public string GetGamePacketData()
        {
            if (Type == PacketType.Game)
                return PacketString;
            return null;
        }

        public bool HasCmd(string name)
        {
            if (Type == PacketType.Game)
                return false;

            return Cmds.ContainsKey(name);
        }

        public MgCommand GetCmd(string name)
        {
            if (Type == PacketType.Game)
                return null;

            if (Cmds.TryGetValue(name, out MgCommand value))
                return value;
            return null;
        }

    }

    public class MgCommand
    {
        private readonly string Name;
        private readonly string[] Params;

        public MgCommand(string data)
        {
            int startPos = data.IndexOf('\a') + 1;
            int endPos = data.LastIndexOf('\a');

            // Check if valid
            if (startPos == -1 || endPos == -1 || startPos == endPos)
                throw new MgInvalidPacketException("Could not find a start or end code.");

            // Get name
            Name = data.Substring(1, 2);

            // Split out the params
            string sub = data.Substring(startPos, endPos - startPos);
            Params = sub.Split('\u0006');
        }

        public string GetName()
        {
            return Name;
        }

        public string GetParamStr(int index)
        {
            return Params[index];
        }

        public string GetParamHexStr(int index)
        {
            string strVal = Params[index];
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

        public bool GetParamId(int index, out ulong result)
        {
            result = 0;
            string strVal = Params[index];
            if (!strVal.StartsWith("0x") && !strVal.StartsWith("0X"))
                return false;
            strVal = strVal[2..];
            if (strVal != null && strVal.Length == 2 * 8)
            {
                int c = 0;
                byte[] byteArray = new byte[8];
                for (int i = byteArray.Length - 1; i >= 0; i--)
                {
                    byteArray[c++] = (byte)((GetHexVal(strVal[i << 1]) << 4) + GetHexVal(strVal[(i << 1) + 1]));
                }
                result = BitConverter.ToUInt64(byteArray);
                return true;
            }
            return false;
        }

        public bool GetParamNumber(int index, out int result)
        {
            result = 0;
            string strVal = Params[index];
            if (strVal != null)
            {
                if (!int.TryParse(strVal, out int val))
                    return false;
                result = val;
                return true;
            }
            return false;
        }

        public MgPlayer GetPlayer()
        {
            GetParamId(0, out ulong id);
            GetParamNumber(1, out int gameId);
            GetParamNumber(2, out int domain);
            GetParamNumber(3, out int volume);
            string charaName = GetParamStr(4);
            GetParamNumber(5, out int classType);
            GetParamNumber(6, out int unixTime);
            GetParamNumber(7, out int cardLevel);
            GetParamNumber(8, out int vsRating);
            GetParamId(9, out ulong roomId);
            string gameData = GetParamStr(10);

            return new(id, roomId, unixTime, gameId, cardLevel, vsRating, (ushort)volume, (byte)domain, (byte)classType, charaName, Encoding.ASCII.GetBytes(gameData));
        }

        public MgTable GetTable()
        {
            string ircChannel = GetParamStr(0);
            GetParamId(2, out ulong id);
            GetParamNumber(3, out int state);
            string gamedata = GetParamStr(6);
            byte[] gameDataBytes = Encoding.ASCII.GetBytes(gamedata);

            return new(id, 0, 0, 0, (uint)state, ircChannel, gameDataBytes);
        }

        private static int GetHexVal(char hex)
        {
            int val = hex;
            return val - (val < 58 ? 48 : 55);
        }

        public override string ToString()
        {
            string strPacket = $"<{Name}>\a";
            for (int i = 0; i < Params.Length; i++)
                strPacket += $"{(i != 0 ? "\u0006" : "")}{Params[i]}";
            strPacket += "\a";
            return strPacket;
        }
    }
}
