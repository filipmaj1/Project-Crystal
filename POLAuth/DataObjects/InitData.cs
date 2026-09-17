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
using System;
using System.IO;
using System.Net;

namespace Crystal.POLAuth.DataObjects
{
    class InitData
    {
        public readonly uint ServerTime;
        public readonly uint IrcIp; // If 0, no redirect
        public readonly short IrcPort; // If 0, 10440
        public readonly uint ClientIp;
        public readonly short ClientPort;

        public InitData(uint serverTime, string ircIp, int ircPort, string clientIp, int clientPort)
        {
            byte[] ircIpBytes = IPAddress.Parse(ircIp).GetAddressBytes();
            byte[] clientIpBytes = IPAddress.Parse(clientIp).GetAddressBytes();
            Array.Reverse(ircIpBytes);
            Array.Reverse(clientIpBytes);

            this.ServerTime = serverTime;
            this.IrcIp = BitConverter.ToUInt32(ircIpBytes, 0);
            this.IrcPort = (short) ircPort;
            this.ClientIp = BitConverter.ToUInt32(clientIpBytes, 0);
            this.ClientPort = (short) clientPort;
        }

        public byte[] GetByes()
        {
            byte[] data = new byte[0x40];

            using (MemoryStream memStream = new(data))
            using (BinaryWriter binWriter = new(memStream))
            {
                binWriter.Write((UInt32)Utils.SwapEndian(ServerTime));
                binWriter.Write((UInt32)Utils.SwapEndian(ClientIp));
                binWriter.Write((UInt32)Utils.SwapEndian(IrcIp));
                binWriter.Write((UInt16)Utils.SwapEndian((ushort)IrcPort));
                binWriter.Write((UInt16)0);
                binWriter.Write((UInt16)0);
                binWriter.Write((UInt16)0);
                binWriter.Write((UInt16)Utils.SwapEndian((ushort)ClientPort));
                binWriter.Write((Byte)1);
                binWriter.Write((Byte)1);
            }

            return data;
        }

        public string GetBase32()
        {
            byte[] initInfoBytes = GetByes();
            return SqCrypto.EncodeBase32(initInfoBytes, initInfoBytes.Length);
        }
    }
}
