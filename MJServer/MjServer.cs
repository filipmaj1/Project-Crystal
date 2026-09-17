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
using Crystal.Common.MiniGame;
using Crystal.Common.MiniGame.Packets;
using Crystal.Common.PolFileSystem;
using Crystal.Mahjong.Models;
using Crystal.Mahjong.Packets;
using Crystal.Mahjong.Packets.GamePackets;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Crystal.Mahjong
{
    public class MjServer
    {
        public const int JANG_IRC_PORT = 51241;

        private static MjServer Self;

        readonly POLAuth.AuthServer PolIrcServer = new("192.168.0.147", JANG_IRC_PORT, "pol-1048-51241.pol.com");
        readonly PolIrcClient BalancerChannel;
        readonly PolIrcClient RankChannel;
        readonly PolIrcClient ProfileChannel;

        private List<Zone> ZoneList;
        private static PolFile ZoneFile;

        public MjServer()
        {
            var p = MgPacket.Parse("GMJSGBe@@@@@@@@ArXTya[VT@@B@APC}\u007f@E@uA{tc[c@Ul@@@@@@@@@@@");
            var mj = MjPacket.FromIrcStr(p.GetGamePacketData());

            ulong blah = SqCrypto.PolProDataToPolId("US1M3A0C1", 0, 0);


            Self = this;
            PolFileSys.Init(".\\profile_data");
            BalancerChannel = new("Mj-Balancer", Mg.MjKey(MjConstants.POLID_BALANCER), OnReceiveMessage);
            ProfileChannel = new("Mj-Profile", Mg.MjKey(MjConstants.POLID_PROFILE), OnReceiveMessageProfile);
            RankChannel = new("Mj-Rank", Mg.MjKey(MjConstants.POLID_RANK), OnReceiveMessageRank);
        }

        public void StartServer()
        {
            Program.Log.Info("Starting POL Auth server for MiniGame...");
            PolIrcServer.StartServer(false);
            Program.Log.Info("Connecting notify interface client...");
            BalancerChannel.Connect("127.0.0.1", JANG_IRC_PORT);
            ProfileChannel.Connect("127.0.0.1", JANG_IRC_PORT);
            RankChannel.Connect("127.0.0.1", JANG_IRC_PORT);

            // Open Game Channel
            while (!BalancerChannel.IsAuthenticated())
            {
                if (BalancerChannel.AuthenticationFailed())
                {
                    Program.Log.Error("The balancer irc client could not connect to the auth server. Check settings!!!");
                    Environment.Exit(1);
                }
            }

            while (!ProfileChannel.IsAuthenticated())
            {
                if (ProfileChannel.AuthenticationFailed())
                {
                    Program.Log.Error("The profile irc client could not connect to the auth server. Check settings!!!");
                    Environment.Exit(1);
                }
            }

            while (!RankChannel.IsAuthenticated())
            {
                if (RankChannel.AuthenticationFailed())
                {
                    Program.Log.Error("The test irc client could not connect to the auth server. Check settings!!!");
                    Environment.Exit(1);
                }
            }

            // Load Zones
            ZoneFile = PolFileSys.OpenFile(MjConstants.POLID_BALANCER, 0, 3, "b/g/ZL", MgConstants.MAX_ZONES, MgConstants.ZONE_SIZE, MgConstants.ZONE_DATA_OFFSET, MgConstants.ZONE_COUNT_OFFSET);
            ZoneFile.InitFile(0x848);
            ZoneList = Database.LoadZones();
            foreach (Zone zone in ZoneList)
                zone.Init();
        }

        private void OnReceiveMessage(string from, string data)
        {
            MgPacket mgPacket = MgPacket.Parse(data);
            if (mgPacket == null)
                return;

            // Is initializing user?
            if (mgPacket.Type == PacketType.Game)
            {
                var gamePkt = MjPacket.FromIrcStr(mgPacket.GetGamePacketData());

                Program.Log.Debug($"Game Packet - Opcode: 0x{gamePkt.Header.Command:X}({Enum.GetName(typeof(MjConstants.Opcodes), (MjConstants.Opcodes)gamePkt.Header.Command)}), Sequence: {gamePkt.Header.SequenceNum}, From: {from}.\n{Utils.ByteArrayToHex(gamePkt.Data)}");

                // MJ is requesting POL DB configuration
                if (gamePkt.Header.Command == (byte)MjConstants.Opcodes.MjGETLNDV)
                {
                    GetLnDvAck lnDvAck = new()
                    {
                        ProfileChannelId = Mg.MjKey(MjConstants.POLID_PROFILE),
                        RankChannelId = Mg.MjKey(MjConstants.POLID_RANK),
                        LobbyChannelId = Mg.MjKey(MjConstants.POLID_BALANCER),
                        LobbyEventChannelId = Mg.MjKey(MjConstants.POLID_BALANCER),
                        Version = 0x20020603,
                        LobbyVolume = 0,
                        LobbyDomain = 3
                    };

                    byte[] lnDvAckBytes = new byte[GetLnDvAck.SIZE];
                    MemoryMarshal.Write(lnDvAckBytes, ref lnDvAck);

                    MjPacket responsePkt = new(
                        source: 0, 
                        target: 0, 
                        command: MjConstants.Opcodes.MjGETLNDVACK,
                        seq: gamePkt.Header.SequenceNum,
                        data: lnDvAckBytes);

                    BalancerChannel.Notice(from, responsePkt.ToIrcStr());
                }
                else if (gamePkt.Header.Command == (byte)MjConstants.Opcodes.MjCHECKSAVEDATA)
                {
                    GetCheckSaveDataAck checkSaveDataAck = new()
                    {
                        Answer = 1
                    };
                    byte[] checkSaveDataAckBytes = new byte[GetCheckSaveDataAck.SIZE];
                    MemoryMarshal.Write(checkSaveDataAckBytes, ref checkSaveDataAck);

                    MjPacket responsePkt = new(
                        source: 0,
                        target: 0,
                        command: MjConstants.Opcodes.MjCHECKSAVEDATAACK,
                        seq: gamePkt.Header.SequenceNum,
                        data: checkSaveDataAckBytes);

                    BalancerChannel.Notice(from, responsePkt.ToIrcStr());
                }
                else if (gamePkt.Header.Command == (byte) MjConstants.Opcodes.EmISEVENT)
                {
                    IsEventAck isEventAck = new()
                    {
                        Result = 1,
                        Result2 = 1,
                        Unknown = 1,
                        Unk = "TEEEEEST"
                    };
                    byte[] isEventAckBytes = new byte[IsEventAck.SIZE];
                    MemoryMarshal.Write(isEventAckBytes, ref isEventAck);

                    MjPacket responsePkt = new(
                        0,
                        target: Mg.MjKey(SqCrypto.PolProDataToPolId(from, 0, 0)),
                        command: MjConstants.Opcodes.EmISEVENTACK,
                        fifoChan: 6,
                        seq: gamePkt.Header.SequenceNum,
                        data: isEventAckBytes);

                    BalancerChannel.Notice(from, responsePkt.ToIrcStr());
                }
            }
        }

        private void OnReceiveMessageProfile(string from, string data)
        {
            Program.Log.Debug($"Received Msg [{from}]: {data}");

            if (data.StartsWith("OFFLINE"))
                return;

            MgPacket packet = MgPacket.Parse(data);

            // Character Pool Request
            if (packet.HasCmd("CR"))
            {
                MgCommand cmdCR = packet.GetCmd("CR");
                MgCommand cmdAN = packet.GetCmd("AN");
                MgCommand cmdCI = packet.GetCmd("CI");

                if (cmdAN != null && cmdCI != null)
                {
                    cmdCR.GetParamId(0, out ulong id);
                    string characterName = cmdAN.GetParamStr(0);
                    cmdCI.GetParamNumber(0, out int someNumber);
                    string cardLevelString = cmdCI.GetParamStr(1);

                    string crResponse = new MgPacketBuilder(PacketType.Profile)
                    .AddCommand("CS", 1)
                    .Build();

                    ProfileChannel.Notice(from, crResponse);
                }
            } 
            else if (packet.HasCmd("PG"))
            {
                MgCommand cmdPG = packet.GetCmd("PG");
                cmdPG.GetParamId(0, out ulong contentId);
                cmdPG.GetParamNumber(1, out int subId);

                string poResponse = new MgPacketBuilder(PacketType.Profile)
                    .AddCommand("PO", (ulong)6466, 288, "Jang2", 4, 1, 0, 1, 2, 1, "TEST MORE")
                    .Build();

                ProfileChannel.Notice(from, poResponse);
            }
        }

        private void OnReceiveMessageRank(string from, string data)
        {
            Program.Log.Debug($"Received Msg [{from}]: {data}");

            if (data.StartsWith("OFFLINE"))
                return;

            MgPacket packet = MgPacket.Parse(data);

            // Character Pool Request
            if (packet.HasCmd("RR"))
            { 
                MgCommand cmdRR = packet.GetCmd("RR");
                cmdRR.GetParamNumber(0, out int unknown);
                MgCommand cmdPI = packet.GetCmd("PI");
                cmdRR.GetParamId(0, out ulong polId);
                //cmdRR.GetParamNumber(1, out int unk1);
                //cmdRR.GetParamNumber(2, out int unk2);
                //cmdRR.GetParamNumber(3, out int unk3);

                string rfResponse = new MgPacketBuilder(PacketType.Rank)
                    .AddCommand("RF", "b/g/MJSRankFile")
                    .AddCommand("LN", 1)
                    .Build();

                RankChannel.Notice(from, rfResponse);
            }
        }

        private void OnReceiveMessageLobby(string from, string data)
        {
            Program.Log.Debug($"Received Msg [{from}]: {data}");

            if (data.StartsWith("OFFLINE"))
                return;

            MgPacket packet = MgPacket.Parse(data);

        }

        public void SendLineToAll(string v)
        {
            BalancerChannel.Notice("WXYZ1234", v);
            PolIrcServer.SendLineToAll(v);
        }

        public static MjServer Get()
        {
            return Self;
        }
    }
}
