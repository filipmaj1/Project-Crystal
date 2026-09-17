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
    public class MgTable
    {
        public ulong Id;
        public uint Unk1;
        public uint NumMembers;
        public uint IsValidUnk;
        public uint State;
        public string IrcChannel;
        public byte[] GameData;

        public MgTable(ulong id, uint unk1, uint numMembers, uint isValidUnk, uint state, string ircChannel, byte[] gameData)
        {
            Id = id;
            Unk1 = unk1;
            NumMembers = numMembers;
            IsValidUnk = isValidUnk;
            State = state;
            IrcChannel = ircChannel;

            GameData = new byte[0x40];
            Array.Copy(gameData, GameData, gameData.Length <= 0x40 ? gameData.Length : 0x40);
        }

        public string ToUpdCmd()
        {
            string gameData = Encoding.ASCII.GetString(GameData).Replace("\0", "");
            return new MgPacketBuilder()
               .AddCommand("TD", IrcChannel, IsValidUnk, Id, State, Unk1, NumMembers, gameData)
               .Build();
        }
    }
}
