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
using System.Runtime.InteropServices;
using System.Text;

namespace Crystal.FrontMissionOnline.Network.UdpNet
{
    [StructLayout(LayoutKind.Sequential, Size = 0x28)]
    public unsafe struct UdpPacketHeader
    {
        public int SourceID;
        public ushort Size;
        public ushort Unknown;
        public byte Reserved;
        public byte UdpSysID;
        public byte MapType;
        public byte FragmentationRelated;
        private fixed byte MD5Buff[0x10];
        public ushort EndSeq;
        public ushort StartSeq;
        public ushort AckedSeq;
        public ushort MaxRecvCmds;
        public ushort CommandDataSize;
        public ushort Reserved2;

        public Span<byte> MD5
        {
            get
            {
                fixed (byte* ptr = MD5Buff)
                {
                    return new Span<byte>(ptr, 16);
                }
            }

            set
            {
                int len = value.Length;
                fixed (byte* ptr = &MD5Buff[0])
                {
                    value.CopyTo(new Span<byte>(ptr, len));
                }
            }
        }
    }
}
