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

namespace Crystal.FrontMissionOnline
{
    public class FmoCrypto
    {
        private readonly byte[] ScrambleBuff = new byte[0x100];
        private byte CurScrambleIndx = 0;
        private byte CurScrambleSum = 0;

        public FmoCrypto(byte[] key) 
        {
            // Init Scramble
            for (int i = 0; i < 0x100; i++)
                ScrambleBuff[i] = (byte)i;

            // Generate initial scramble table from key
            int currentIndex = 0;
            int keyIndex = 0;
            for (int i = 0; i < 0x100; i++)
            {
                byte scrambleByte = ScrambleBuff[i];
                currentIndex = ((currentIndex + (key[keyIndex++ % key.Length] & 0xFF) & 0xFF) + scrambleByte) & 0xFF;
                ScrambleBuff[i] = ScrambleBuff[currentIndex];
                ScrambleBuff[currentIndex] = scrambleByte;
            }
        }

        public void TransformBuff(Span<byte> buff, int length)
        {
            if (length > 0)
            {
                byte nextVal;

                for (int i = 0; i < length; i++)
                {
                    CurScrambleIndx = (byte)((++CurScrambleIndx) & 0xFF);
                    nextVal = ScrambleBuff[CurScrambleIndx];
                    CurScrambleSum += nextVal;

                    ScrambleBuff[CurScrambleIndx] = ScrambleBuff[CurScrambleSum];
                    ScrambleBuff[CurScrambleSum] = nextVal;

                    buff[i] ^= ScrambleBuff[(nextVal + ScrambleBuff[CurScrambleIndx]) & 0xFF];
                }
            }
        }
    }
}
