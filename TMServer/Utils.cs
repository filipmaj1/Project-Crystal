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

namespace Crystal.TetraMaster
{
    internal class Utils
    {
        public static ulong GetASCIINum(byte[] asciiInfo, int offset, int numDigits)
        {
            ulong finalNum = 0;
            ulong multiplier = (ulong)1 << ((numDigits - 1) * 0x4);
            for (int i = 0; i < numDigits; i++)
            {
                finalNum += ((byte)(asciiInfo[offset + i] + 0xBF) * multiplier);
                multiplier >>= 0x4;
            }
            return finalNum;
        }

        public static void ClrASCII(byte[] asciiInfo)
        {
            for (int i = 0; i < asciiInfo.Length; i++)
                asciiInfo[i] = 0x41;
            asciiInfo[asciiInfo.Length - 1] = 0;
        }

        public static void SetASCIINum(byte[] asciiInfo, ulong value, int offset, int numDigits)
        {
            ulong divisor = (ulong)1 << ((numDigits - 1) * 0x4);
            ulong remaining = value;
            for (int i = 0; i < numDigits; i++)
            {
                asciiInfo[offset + i] = (byte)((((remaining / divisor) & 0xFF) - 0xBF) & 0xFF);
                remaining -= remaining & (0xFUL << (numDigits-i-1)*4);
                divisor >>= 0x4;
            }
        }

        public static void SetASCIIStr(byte[] asciiInfo, string value, int offset, int maxStrSize)
        {
            byte[] strBytes = Encoding.ASCII.GetBytes(value);
            int copyLen = strBytes.Length <= maxStrSize ? strBytes.Length : maxStrSize;
            Array.Copy(strBytes, 0, asciiInfo, offset, copyLen);
            for (int i = copyLen; i < maxStrSize; i++)
                asciiInfo[offset + i] = 0;
        }
    }
}
