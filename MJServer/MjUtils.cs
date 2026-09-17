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
using Org.BouncyCastle.Crypto;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Crystal.Mahjong
{
    internal class MjUtils
    {
        public static int MjsStrToPktSize(int strSize)
        {
            int temp = strSize * 6;
            if (temp < 0)
                temp += 7;
            return temp >> 3;
        }

        public static int MjsPktToStrSize(int pktSize)
        {
            return pktSize + (pktSize / 3) + 1;
        }

        public static byte[] MjsConvertPlayerDataToStr(byte[] buff)
        {
            byte[] result = new byte[0x20];
            int leftOver = 0;
            int bitsRead = 0;
        
            for (int i = 0; i < result.Length - 1; i++)
            {
                // Cut off anything above 3 bits; shift moves down 6 - 4 - 2 - 0
                leftOver &= 0x7;
                // We read every byte til 8 bits consumed
                int readIndx = bitsRead >> 3;

                int outChar = ((buff[readIndx] + (buff[readIndx + 1] << 8)) >> leftOver) & 0x3F; //Get the next 6 bits for a 16 bit block
                result[i] = (byte) (outChar + 0x20); // Make it ascii

                leftOver += 6;
                bitsRead += 6;
            }

            result[0x1F] = 0;
            
            return result;
        }

        public static byte[] MjsConvertStrToPlayerData(byte[] buff)
        {
            byte[] result = new byte[0x18];
            int j = 0;

            for (int i = 0; i < 0x17; i++)
            { 
                int readIn = j / 6;
                
                result[i] = (byte)((buff[readIn + 2] - 0x20 & 0x3f) * 0x1000 +
                               (buff[readIn] - 0x20 & 0x3f) + (buff[readIn + 1] - 0x20 & 0x3f) * 0x40 >> ((j % 6) & 0x1f));

                j += 8;
            }

            return result;
        }

        public static byte MjsConvertStrToPkt(byte[] buff, int indx)
        {
            int subIndx = indx % 3;
            indx += indx / 3;

            if (indx >= buff.Length - 1)
                return 0;

            if (subIndx == 2)
            {
                return (byte)(((buff[indx] & 3) << 6) | (buff[indx + 1] & 0x3f));
            }
            else if (subIndx == 1)
            {
                return (byte)(((buff[indx] & 0xF) << 4) | ((buff[indx + 1] & 0x3c) >> 2));
            }
            else
            {
                return (byte)(((buff[indx] & 0x3F) << 2) | ((buff[indx + 1] & 0x30) >> 4));
            }
        }

        public static void MjsConvertPktToStr(byte[] buff, byte nextByte, int indx)
        {
            int subIndx = indx % 3;
            indx = indx + indx / 03;

            if (subIndx == 2)
            {
                buff[indx] &= 0x3c;
                buff[indx] |= (byte)(((nextByte & 0xc0) >> 6) | 0x40);
                buff[indx + 1] = 0;
                buff[indx + 1] |= (byte)((nextByte & 0x3f) | 0x40);
            }
            else if (subIndx == 1)
            {
                buff[indx] &= 0x30;
                buff[indx] |= (byte)(((nextByte & 0xf0) >> 4) | 0x40);
                buff[indx + 1] &= 3;
                buff[indx + 1] |= (byte)(((nextByte & 0xf) << 2) | 0x40);
            }
            else if (subIndx == 0)
            {
                buff[indx] = 0;
                buff[indx] |= (byte)(((nextByte & 0xfc) >> 2) | 0x40);
                buff[indx + 1] &= 0xf;
                buff[indx + 1] |= (byte)(((nextByte & 0x3) << 4) | 0x40);
            }
        }
    }
}
