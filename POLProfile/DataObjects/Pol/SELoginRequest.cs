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

namespace Crystal.POLProfile.DataObjects.Pol
{

    public unsafe struct SELoginRequest
    {
        private byte HasOtpByte;
        private fixed byte UsernameBuff[0x11];
        private fixed byte OneTimePwdBuff[0xE];
        public fixed byte PwdHashBuff[0x14];

        public const int SIZE = 0xA8;

        public bool HasOtp
        {
            get
            {
                return HasOtpByte == 2;
            }
        }

        public string Username
        {
            get
            {
                fixed (byte* ptr = &UsernameBuff[0])
                {
                    string str = Encoding.ASCII.GetString(new ReadOnlySpan<byte>(ptr, 0x11));
                    return str[..(str.Contains('\0') ? str.IndexOf('\0') : str.Length)];
                }
            }
        }

        public string OneTimePwd
        {
            get
            {
                fixed (byte* ptr = &OneTimePwdBuff[0])
                {
                    string str = Encoding.ASCII.GetString(new ReadOnlySpan<byte>(ptr, 0x7));
                    return str[..(str.Contains('\0') ? str.IndexOf('\0') : str.Length)];
                }
            }
        }

        public override string ToString()
        {
            return $"{Username}:{(HasOtp ? OneTimePwd : "NO")}";
        }
    }
}
