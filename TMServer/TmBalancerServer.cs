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

using Crystal.POLAuth;
using System;
using System.Runtime.InteropServices;
using Crystal.TetraMaster.Packets;
using Crystal.Common.MiniGame;
using Crystal.Common.MiniGame.Packets;
using Crystal.Common.PolFileSystem;
using Crystal.TetraMaster.Models;
using static Crystal.TetraMaster.TmConstants;

namespace Crystal.TetraMaster
{
    internal class TmBalancerServer
    { 
        private readonly PolIrcClient PolGameChannel;
        private readonly PolIrcClient PolProfileChannel;

        public TmBalancerServer()
        {
            PolGameChannel = new("Tm-Balancer", Mg.TmKey(TmConstants.BALANCER_POLID), OnReceiveMessage);
            PolProfileChannel = new("Tm-Profile", Mg.TmKey(TmConstants.PROFILE_POLID), OnReceiveMessageProfile);
        }

        public void StartServer()
        {
            Program.Log.Info("!!!Tetra Master Load Balancer is starting!!!");
            Program.Log.Info("Connecting notify interface client...");
            PolGameChannel.Connect("127.0.0.1", MgConstants.MG_IRC_PORT);
            PolProfileChannel.Connect("127.0.0.1", MgConstants.MG_IRC_PORT);

            while (!PolGameChannel.IsAuthenticated())
            {
                if (PolGameChannel.AuthenticationFailed())
                {
                    Program.Log.Error("The game irc client could not connect to the auth server. Check settings!!!");
                    Environment.Exit(1);
                }
            }

            while (!PolProfileChannel.IsAuthenticated())
            {
                if (PolProfileChannel.AuthenticationFailed())
                {
                    Program.Log.Error("The profile irc client could not connect to the auth server. Check settings!!!");
                    Environment.Exit(1);
                }
            }
        }

        private void OnReceiveMessage(string from, string data)
        {
            MgPacket mgPacket = MgPacket.Parse(data);
            if (mgPacket == null)
                return;

            Program.Log.Debug($"Received Msg [{from}]: {data}");

            if (mgPacket.Type == PacketType.Game)
            {
                TmPacket packet = new(mgPacket.GetGamePacketData());
                // TM is requesting POL DB configuration
                if (packet.HasCommand("TeachDV"))
                {
                    // Set the POL sharding info for Tetra Master's various servers
                    string teachDVAns = new TmPacketBuilder(0xE2, 0x00, 0x00)
                        .Command(new TmGameCommandBuilder("TeachDVAns")
                            .Param("D", BALANCER_DOMAIN)
                            .Param("V", BALANCER_VOLUME)
                            .Param("AC", AUCTION_DOMAIN, AUCTION_VOLUME)
                            .Param("RK", RANK_DOMAIN, RANK_VOLUME)
                            .Param("Shm", Mg.TmKey(SHOPMENU_POLID), SHOPMENU_DOMAIN, SHOPMENU_VOLUME)
                            .End()
                        ).Build();
                    PolGameChannel.Notice(from, teachDVAns);

                    return;
                }
            }
        }

        private void OnReceiveMessageProfile(string from, string data)
        {
            Program.Log.Debug($"Received Msg [{from}]: {data}");

            MgPacket mgPacket = MgPacket.Parse(data);
            if (mgPacket == null)
                return;

            // Character Pool Request
            if (mgPacket.HasCmd("CR"))
            {
                MgCommand cmdCR = mgPacket.GetCmd("CR");
                MgCommand cmdAN = mgPacket.GetCmd("AN");
                MgCommand cmdCI = mgPacket.GetCmd("CI");

                if (cmdAN != null && cmdCI != null)
                {
                    cmdCR.GetParamId(0, out ulong id);
                    string characterName = cmdAN.GetParamStr(0);
                    cmdCI.GetParamNumber(0, out int someNumber);
                    string cardLevelString = cmdCI.GetParamStr(1);

                    string crResponse = new MgPacketBuilder(PacketType.Profile)
                    .AddCommand("CS", 1)
                    .Build();

                    PolProfileChannel.Notice(from, crResponse);
                }
            }
        }

        public void LoadAndWriteGameDataFile(ulong polId)
        {
            GameDataFile? dataFile = Database.LoadPlayerGameData(polId);
            if (dataFile != null)
                WriteGameDataFile(polId, (GameDataFile)dataFile);
        }

        private void WriteGameDataFile(ulong polId, GameDataFile gamedata)
        {
            byte[] outBuff = new byte[GameDataFile.SIZE];
            Span<byte> gameDataSpan = outBuff.AsSpan();
            MemoryMarshal.Write(gameDataSpan, ref gamedata);
           
            PolFile file = PolFileSys.OpenFileDirect(polId, 0, 0, "U/g/TM0DataFile", out object fileLock);
            lock (fileLock)
            {
                var fileStream = file.GetDirectFileStream();
                fileStream.Write(gameDataSpan);
                fileStream.Close();
            }
            PolFileSys.CloseFile(file);
        }
    }
}
