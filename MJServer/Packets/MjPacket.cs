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
using System.Runtime.InteropServices;
using System.Text;

namespace Crystal.Mahjong.Packets
{
    public class MjPacket
    {
        [StructLayout(LayoutKind.Sequential, Size = SIZE)]
        public struct MjPacketHeader
        {
            public ulong SourcePolId;
            public ulong TargetPolId;
            public ushort Size;
            public byte Command;
            public byte SequenceNum;
            public byte Unk2;
            public byte Unk3;
            public byte Unk4;
            public byte fifoChannel;

            public const int SIZE = 0x18;
        }
        
        public MjPacketHeader Header;
        public readonly byte[] Data;

        public MjPacket(ulong source, ulong target, MjConstants.Opcodes command, byte seq, byte[] data, byte fifoChan = 0)
        {
            Header = new()
            {
                SourcePolId = source,
                TargetPolId = target,
                Size = (ushort)(MjPacketHeader.SIZE + data.Length),
                Command = (byte)command,
                SequenceNum = seq,
                Unk2 = 0xFD,
                Unk3 = 0xFE,
                Unk4 = 0x00,
                fifoChannel = fifoChan
            };
            Data = data;
        }

        private MjPacket(MjPacketHeader header, byte[] data)
        {
            Header = header;
            Data = data;
        }

        public string ToIrcStr()
        {
            byte[] pktBytes = new byte[Header.Size];
            MemoryMarshal.Write(pktBytes, ref Header);
            Data.AsSpan().CopyTo(pktBytes.AsSpan()[MjPacketHeader.SIZE..]);

            int strSize = MjUtils.MjsPktToStrSize(pktBytes.Length);
            byte[] strBytes = new byte[strSize];

            for (int i = 0; i < pktBytes.Length; i++)
                MjUtils.MjsConvertPktToStr(strBytes, pktBytes[i], i);

            return MgPacketBuilder.Game("B" + Encoding.ASCII.GetString(strBytes));
        }

        public static MjPacket FromIrcStr(string str)
        {
            byte[] strBytes = Encoding.ASCII.GetBytes(str.Substring(1));
            int pktSize = MjUtils.MjsStrToPktSize(strBytes.Length);
            byte[] pktBytes = new byte[pktSize];

            for (int i = 0; i < pktBytes.Length; i++)
                pktBytes[i] = MjUtils.MjsConvertStrToPkt(strBytes, i);

            MjPacketHeader header = MemoryMarshal.Cast<byte, MjPacketHeader>(pktBytes.AsSpan())[0];

            return new(header, pktBytes.AsSpan()[MjPacketHeader.SIZE..].ToArray());
        }
    }


}
