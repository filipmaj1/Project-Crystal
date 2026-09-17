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
using System.Text;

namespace Crystal.Common.MiniGame
{
    public class MgPlayer
    {
        public ulong Id;
        public ulong RoomId;
        public int EnterTime;
        public int GameId;
        public int Level;
        public int Rating;
        public ushort Volume;
        public byte Domain; 
        public byte Class;
        public string Name;
        public byte[] GameData;

        public MgPlayer(ulong id, ulong roomId, int time, int gameId, int level, int rating, ushort volume, byte domain, byte classType, string charaName, byte[] gameData)
        {
            Id = id;
            RoomId = roomId;
            EnterTime = time;
            GameId = gameId;
            Level = level;
            Rating = rating;
            Volume = volume;
            Domain = domain;
            Class = classType;
            Name = charaName;

            GameData = new byte[0x20];
            Array.Copy(gameData, GameData, gameData.Length <= 0x20 ? gameData.Length : 0x20);
        }

        public void Update(MgPlayer player)
        {
            Id = player.Id;
            RoomId = player.RoomId;
            EnterTime = player.EnterTime;
            GameId = player.GameId;
            Level = player.Level;
            Rating = player.Rating;
            Volume = player.Volume;
            Domain = player.Domain;
            Class = player.Class;
            Name = player.Name;

            Array.Copy(GameData, player.GameData, 0x20);
        }

        public string ToUpdCmd()
        {
            string gameData = Encoding.ASCII.GetString(GameData).Replace("\0", "");
            return new MgPacketBuilder()
               .AddCommand("PD", Id, GameId, Domain, Volume, Name, Class, EnterTime, Level, Rating, RoomId, gameData)
               .Build();
        }
    }
}
