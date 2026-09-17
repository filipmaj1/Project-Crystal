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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Crystal.POLAuth.DataObjects
{
    public struct AdminData
    {
        private ushort Volume_;
        public byte Domain;
        private ushort Status_;
        public byte MessageId; // 0xDC to 0xFC + 0xFF
        public byte AccountNum;
        private ushort AccountYear_; //Only msg 0xDE uses this
        public byte AccountMonth;
        public byte AccountDay;
        private ulong masterPolId;

        public ushort Volume
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                return (ushort)(((Volume_ << 8) & 0xff00) |
                            ((Volume_ >> 8) & 0x00ff));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Volume_ = (ushort)(((value << 8) & 0xff00) |
                            ((value >> 8) & 0x00ff));
            }
        }

        public ushort Status
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (ushort)(((Status_ << 8) & 0xff00) |
                            ((Status_ >> 8) & 0x00ff));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Status_ = (ushort)(((value << 8) & 0xff00) |
                            ((value >> 8) & 0x00ff));
            }
        }

        public ushort AccountYear
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (ushort)(((AccountYear_ << 8) & 0xff00) |
                            ((AccountYear_ >> 8) & 0x00ff));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                AccountYear_ = (ushort)(((value << 8) & 0xff00) |
                            ((value >> 8) & 0x00ff));
            }
        }

        public ulong MasterPolId
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return 0x00000000000000FF & (masterPolId >> 56) |
                       0x000000000000FF00 & (masterPolId >> 40) |
                       0x0000000000FF0000 & (masterPolId >> 24) |
                       0x00000000FF000000 & (masterPolId >> 8) |
                       0x000000FF00000000 & (masterPolId << 8) |
                       0x0000FF0000000000 & (masterPolId << 24) |
                       0x00FF000000000000 & (masterPolId << 40) |
                       0xFF00000000000000 & (masterPolId << 56);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                masterPolId = 
                       0x00000000000000FF & (value >> 56) |
                       0x000000000000FF00 & (value >> 40) |
                       0x0000000000FF0000 & (value >> 24) |
                       0x00000000FF000000 & (value >> 8) |
                       0x000000FF00000000 & (value << 8) |
                       0x0000FF0000000000 & (value << 24) |
                       0x00FF000000000000 & (value << 40) |
                       0xFF00000000000000 & (value << 56);
            }
        }

        public string ToBase32()
        {
            byte[] adminDataByte = new byte[0x18];
            MemoryMarshal.Write(adminDataByte, in this);
            return SqCrypto.EncodeBase32(adminDataByte, adminDataByte.Length);
        }

        /*
         * --- Error Codes ---
         * 0xC9 - Bad Username
         * 0xCA - Bad Password
         * 0xCB - Tried three failed logins
         * 0xCC - Not working due to a version discrepancy
         * 0xCD - Network is busy generic error
         * 0xCE - Unknown error
         * 
         * Also the admin msg codes also work: 0xDC - 0xFC and 0xFF.
         * Use it for the "logout" msgs; ie being banned.
         */
        public static AdminData Error(byte code)
        {
            return  new AdminData { MessageId = code };
        }
    }
}
