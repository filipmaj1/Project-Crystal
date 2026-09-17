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

using Crystal.Common.MiniGame;
using Crystal.Common.PolFileSystem;
using static Crystal.TetraMaster.TmConstants;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Crystal.Common.MiniGame.PolFileSystem.FileEntries;

namespace Crystal.TetraMaster.Models
{
    /// <summary>
    /// Defines a game "Zone" that groups together rooms, similar to how zones are used in POL Chat to group chatrooms.
    /// Each zone holds a list of room, and initializes and updates it's entry in the zone list file (ZL). It also inits
    /// the room list file (RL_%3d) for this zone.
    /// 
    /// This class does not process incoming packets.
    /// </summary>
    public class Zone
    {
        public readonly ushort Id;
        public readonly ushort FileIndex;
        private readonly byte[] StaticAsciiData = new byte[0x30];

        public List<Room> RoomList;

        private static PolFile ZoneListFile;
        private static PolFile RoomListFile;
        
        public Zone(ushort fileIndex, ushort id, string name, string langCode, byte platform, bool isEvent)
        {
            Id = id;
            FileIndex = fileIndex;

            // Stored in the file
            Utils.ClrASCII(StaticAsciiData);
            Utils.SetASCIIStr(StaticAsciiData, langCode, 0x0, 0x2);
            Utils.SetASCIINum(StaticAsciiData, platform, 0x2, 1);
            Utils.SetASCIINum(StaticAsciiData, 0, 0x3, 1);
            Utils.SetASCIINum(StaticAsciiData, isEvent ? 1UL : 0UL, 0x4, 1);
            Utils.SetASCIIStr(StaticAsciiData, name, 0x5, 0x1B);
            Utils.SetASCIIStr(StaticAsciiData, "gi000.pol.com", 0x20, 0x10);
        }

        public bool Init()
        {
            // Load Rooms
            RoomList = Database.LoadRooms(this);
            if (RoomList == null)
                return false;

            // Init the entry for this zone in the zone list file
            ZoneListFile = PolFileSys.OpenFile(BALANCER_POLID, BALANCER_VOLUME, BALANCER_DOMAIN, $"b/g/ZL", MgConstants.MAX_ZONES, MgConstants.ZONE_SIZE, MgConstants.ZONE_DATA_OFFSET, MgConstants.ZONE_COUNT_OFFSET);
            ZoneFileEntry zoneEntry = new()
            {
                NumPlayer = 0,
                NumRooms = (uint)RoomList.Count,
                RoomNum = FileIndex,
                GameData = StaticAsciiData
            };
            ReadOnlySpan<byte> data = MemoryMarshal.Cast<ZoneFileEntry, byte>(new ReadOnlySpan<ZoneFileEntry>(zoneEntry));
            ZoneListFile.InsertNewEntryAtPosition(FileIndex, data);

            // Init the room list file for this zone
            RoomListFile = PolFileSys.OpenFile(BALANCER_POLID, BALANCER_VOLUME, BALANCER_DOMAIN, $"b/g/RL{FileIndex:D3}", MgConstants.MAX_ROOMS, MgConstants.ROOM_SIZE, MgConstants.ROOM_DATA_OFFSET, MgConstants.ROOM_COUNT_OFFSET);
            RoomListFile.InitFile(0xC848);
            foreach (Room room in RoomList)
            {
                bool success = room.Init();
                if (!success)
                    return false;
            }

            return true;
        }

        public void UpdatePlayerCount()
        {
            int numPlayers = 0;
            for (int i = 0; i < RoomList.Count; i++)
                numPlayers += RoomList[i].NumPlayers;

            ZoneFileEntry zoneEntry = new()
            {
                NumPlayer = (uint)numPlayers,
                NumRooms = (uint)RoomList.Count,
                RoomNum = FileIndex,
                GameData = StaticAsciiData
            };

            ReadOnlySpan<byte> data = MemoryMarshal.Cast<ZoneFileEntry, byte>(new ReadOnlySpan<ZoneFileEntry>(zoneEntry));
            ZoneListFile.UpdateEntryAtPosition(FileIndex, data);
        }
    }
}
