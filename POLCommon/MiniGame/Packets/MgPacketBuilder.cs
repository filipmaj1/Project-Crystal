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
using System.Text;

namespace Crystal.Common.MiniGame.Packets
{
    public class MgPacketBuilder
    {
        private string StrPacket;

        public static string Game(string data)
        {
            return new MgPacketBuilder(PacketType.Game).AddRaw(data).Build();
        }

        public MgPacketBuilder(PacketType type)
        {
            string typeCode = "";

            if (type == PacketType.Game)
                typeCode = "G";
            else if (type == PacketType.Lobby)
                typeCode = "L";
            else if (type == PacketType.Auction)
                typeCode = "A";
            else if (type == PacketType.Rank)
                typeCode = "R";
            else if (type == PacketType.Profile)
                typeCode = "P";

            StrPacket = $"G{Mg.GameCode}{typeCode}";
        }

        public MgPacketBuilder()
        {
            StrPacket = "";
        }

        public MgPacketBuilder AddCommand(string name, params object[] value)
        {
            StrPacket += $"<{name}>\a";

            for (int i = 0; i < value.Length; i++)
            {
                if (i != 0)
                    StrPacket += "\u0006";

                object curVal = value[i];

                // Number
                if (curVal is uint)
                    StrPacket += ((uint)curVal).ToString();
                else if (curVal is int)
                    StrPacket += ((int)curVal).ToString();
                else if (curVal is ushort)
                    StrPacket += ((ushort)curVal).ToString();
                else if (curVal is byte)
                    StrPacket += ((byte)curVal).ToString();
                // String
                if (curVal is string)
                    StrPacket += ((string)curVal).Replace("\0", "");
                // ID
                if (curVal is long)
                    StrPacket += string.Format("0x{0:X16}", (ulong)(long)curVal);
                if (curVal is ulong)
                    StrPacket += string.Format("0x{0:X16}", (ulong)curVal);
                // Byte Array
                if (curVal is byte[])
                    StrPacket += BitConverter.ToString((byte[])curVal).Replace("-", "");
            }

            StrPacket += $"\a";

            return this;
        }

        public MgPacketBuilder AddPlayer(MgPlayer player)
        {
            return AddCommand("PD", player.Id, player.GameId, player.Domain, player.Volume, player.Name, 2, player.EnterTime, player.Level, player.Rating, player.RoomId, player.GameData);
        }

        public MgPacketBuilder AddTable(MgTable table)
        {
            return AddCommand("TD", table.IrcChannel, 0, table.Id, table.State, 0, 0, Encoding.ASCII.GetString(table.GameData).Replace("\0", ""));
        }

        public MgPacketBuilder AddRaw(string raw)
        {
            StrPacket += raw;
            return this;
        }

        public string Build()
        {
            return StrPacket;
        }
    }
}
