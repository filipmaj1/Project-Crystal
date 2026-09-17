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

extern alias Crypt;
using Crypt.Org.BouncyCastle.Crypto;
using Crypt.Org.BouncyCastle.Crypto.Encodings;
using Crypt.Org.BouncyCastle.Crypto.Engines;
using Crypt.Org.BouncyCastle.Crypto.Generators;
using Crypt.Org.BouncyCastle.Crypto.Parameters;
using Crypt.Org.BouncyCastle.Crypto.Prng;
using Crypt.Org.BouncyCastle.Math;
using Crypt.Org.BouncyCastle.Security;
using System;
using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Crystal.Common
{
    public class SqCrypto
    {
		// Find these in polcore.dll
        public const ulong POL_CRYPTKEY_MESSAGE = 0x0;
        public const ulong POL_CRYPTKEY_CHAT = 0x0;
        public const ulong POL_CRYPTKEY_PML = 0x0;

        private static readonly byte[] b32EncodeTable =
        {
            0x4e,0x34,0x33,0x4f, 0x56,0x48,0x42,0x4a, 0x31,0x59,0x32,0x43, 0x30,0x57,0x53,0x58,
            0x45,0x44,0x35,0x51, 0x46,0x49,0x4c,0x52, 0x5a,0x4d,0x55,0x54, 0x41,0x50,0x47,0x4b
        };

        private static readonly byte[] b32DecodeTable =
        {
            0xff,0xff,0xff,0xff, 0xff,0xff,0xff,0xff, 0xff,0xff,0xff,0xff, 0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff, 0xff,0xff,0xff,0xff, 0xff,0xff,0xff,0xff, 0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff, 0xff,0xff,0xff,0xff, 0xff,0xff,0xff,0xff, 0xff,0xff,0xff,0xff,
            0x0c,0x08,0x0a,0x02, 0x01,0x12,0xff,0xff, 0xff,0xff,0xff,0xff, 0xff,0xff,0xff,0xff,
            0xff,0x1c,0x06,0x0b, 0x11,0x10,0x14,0x1e, 0x05,0x15,0x07,0x1f, 0x16,0x19,0x00,0x03,
            0x1d,0x13,0x17,0x0e, 0x1b,0x1a,0x04,0x0d, 0x0f,0x09,0x18,0x00
        };

        private static readonly byte[] b64EncodeTable =
        {
            0x54,0x53,0x47,0x38, 0x49,0x6e,0x63,0x57, 0x33,0x48,0x46,0x4b, 0x6f,0x6b,0x4f,0x67,
            0x37,0x39,0x71,0x7a, 0x65,0x43,0x6d,0x5a, 0x73,0x32,0x79,0x42, 0x59,0x45,0x51,0x56,
            0x41,0x55,0x78,0x52, 0x35,0x72,0x62,0x77, 0x69,0x34,0x50,0x40, 0x6a,0x4d,0x44,0x4c,
            0x74,0x70,0x76,0x61, 0x64,0x30,0x66,0x5f, 0x4a,0x31,0x68,0x6c, 0x4e,0x36,0x75,0x58
        };

        private static readonly byte[] b64DecodeTable =
        {
            0x40,0xff,0xff,0xff, 0xff,0xff,0xff,0xff, 0xff,0xff,0xff,0xff, 0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff, 0xff,0xff,0xff,0xff, 0xff,0xff,0xff,0xff, 0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff, 0xff,0xff,0xff,0xff, 0xff,0xff,0xff,0xff, 0xff,0xff,0xff,0xff,
            0x35,0x39,0x19,0x08, 0x29,0x24,0x3d,0x10, 0x03,0x11,0xff,0xff, 0xff,0xff,0xff,0xff,
            0x2B,0x20,0x1B,0x15, 0x2E,0x1D,0x0A,0x02, 0x09,0x04,0x38,0x0B, 0x2F,0x2D,0x3C,0x0E,
            0x2A,0x1E,0x23,0x01, 0x00,0x21,0x1F,0x07, 0x3F,0x1C,0x17,0xFF, 0xFF,0xFF,0xFF,0x37,
            0xFF,0x33,0x26,0x06, 0x34,0x14,0x36,0x0F, 0x3A,0x28,0x2C,0x0D, 0x3B,0x16,0x05,0x0C,
            0x31,0x12,0x25,0x18, 0x30,0x3E,0x32,0x27, 0x22,0x1A,0x13,0x00
        };

        private static readonly byte[] sqCharacterToPolIdMap = {
            0xD8, 0x35, 0xE8, 0xD4, 0x66, 0x82, 0x64, 0x98, 0xD9, 0xA8, 0x87, 0x75, 0x65, 0x70, 0x5A, 0x8A,
            0x3F, 0x62, 0x80, 0x29, 0x44, 0xDE, 0x7C, 0xA5, 0x89, 0x4E, 0x57, 0x59, 0xD3, 0x51, 0xAD, 0xAC,
            0x86, 0x95, 0x80, 0xEC, 0x17, 0xE4, 0x85, 0xF1, 0x8C, 0x0C, 0x66, 0xF1, 0x7C, 0xC0, 0x7C, 0xBB,
            0x1F, 0x20, 0x1C, 0x1B, 0x1E, 0x23, 0x1A, 0x21, 0x1D, 0x22, 0x22, 0xFC, 0xE4, 0x66, 0xDA, 0x61,
            0x0B, 0x03, 0x0F, 0x13, 0x0C, 0x00, 0x01, 0x0A, 0x16, 0x14, 0x07, 0x02, 0x11, 0x06, 0x09, 0x04,
            0x12, 0x10, 0x15, 0x0D, 0x0B, 0x19, 0x08, 0x0E, 0x18, 0x05, 0x17, 0x00
        };

        private static readonly byte[] sqPolIdToCharacterMap =
        {
            0x45, 0x46, 0x4b, 0x41, 0x4f, 0x59, 0x4d, 0x4a, 0x56, 0x4e, 0x47, 0x54, 0x44, 0x53, 0x57, 0x42,
            0x51, 0x4c, 0x50, 0x43, 0x49, 0x52, 0x48, 0x5a, 0x58, 0x55, 0x36, 0x33, 0x32, 0x38, 0x34, 0x30,
            0x31, 0x37, 0x39, 0x35
        };


        Blowfish blowfish;
        uint[] clientPublicKey;
        uint lastXl = 0, lastXr = 0;
        uint cryptCallNum = 0;

        byte[] blowKey;
        uint rKey1, rKey2;

        public SqCrypto(byte[] blowfishKey, byte[] clientPublicKey)
        {
            blowfish = InitPOLBlowfish(blowfishKey);
            this.clientPublicKey = new uint[] { BitConverter.ToUInt32(clientPublicKey, 0), BitConverter.ToUInt32(clientPublicKey, 4) };

            blowKey = blowfishKey;
            rKey1 = BitConverter.ToUInt32(clientPublicKey, 0);
            rKey2 = BitConverter.ToUInt32(clientPublicKey, 4);
        }

        public SqCrypto(byte[] blowfishKey, uint rKey1, uint rKey2)
        {
            blowfish = InitPOLBlowfish(blowfishKey);
            this.clientPublicKey = new uint[] { rKey1, rKey2 };

            blowKey = blowfishKey;
            this.rKey1 = rKey1;
            this.rKey2 = rKey2;
        }

        private Blowfish InitPOLBlowfish(byte[] key)
        {
            //Generate hash buffer to make the P,S values
            byte[] hashBuffer = new byte[0x1000];
            Array.Copy(key, 0, hashBuffer, 0, key.Length);
            int currentLength = 8;

            MD5Hash md5 = new();
            while (currentLength < 0x1000)
            {
                byte[] hash = new byte[0x10];
                md5.BlockUpdate(hashBuffer, 0, currentLength);
                md5.DoFinal(hash, 0, true);
                Array.Copy(hash, 0, hashBuffer, currentLength, 0x1000 - currentLength < 0x10 ? 0x1000 - currentLength : 0x10);
                currentLength += 0x10;
            }

            //Generate P table
            byte[] P_Table = new byte[0x12 * 4];
            int offset = 0;
            for (int i = 0; i < 0x12; i++)
            {
                P_Table[offset + 0] = hashBuffer[i + 3];
                P_Table[offset + 1] = hashBuffer[i + 2];
                P_Table[offset + 2] = hashBuffer[i + 1];
                P_Table[offset + 3] = hashBuffer[i + 0];
                offset += 4;
            }

            //Generate S table
            byte[] S_Table = new byte[0x1000];
            offset = 0;
            for (int i = 0; i < 0x1000; i += 4)
            {
                S_Table[offset + 0] = hashBuffer[i + 3];
                S_Table[offset + 1] = hashBuffer[i + 2];
                S_Table[offset + 2] = hashBuffer[i + 1];
                S_Table[offset + 3] = hashBuffer[i + 0];
                offset += 4;
            }

            return new Blowfish(hashBuffer, 16, P_Table, S_Table);
        }

        public void SqCrypt64(Span<byte> buff, int size, int flag)
        {
            uint a, xl, xr;

            if (flag == 0)
            {
                a = cryptCallNum;
                xl = lastXl;
                xr = lastXr;
            }
            else
            {
                a = 0;
                xl = (uint)clientPublicKey[0];
                xr = (uint)clientPublicKey[1];
            }

            for (int i = 0; i < size; i++)
            {
                if ((a &= 7) == 0)
                    blowfish.BlowfishEncipher(ref xl, ref xr);

                byte nextValue = buff[i];
                long shiftResult = ((long)xr << 0x20 | (long)xl) >> (int)a * 8;
                byte xoredValue = (byte)((byte)(shiftResult & 0xFF) ^ nextValue);

                //Skip linebreaks when decoding/encoding irc
                if (nextValue == 0x0D || nextValue == 0x0A || xoredValue == 0x0D || xoredValue == 0x0A)
                    buff[i] = nextValue;
                else
                    buff[i] = xoredValue;

                a++;
            }

            lastXl = xl;
            lastXr = xr;
            cryptCallNum = a;
        }

        public byte[] GetBlowKey()
        {
            return blowKey;
        }

        public ulong GetBlowKeyUInt64()
        {
            return Utils.SwapEndian(BitConverter.ToUInt64(blowKey, 0));
        }

        public uint GetRKey1()
        {
            return rKey1;
        }

        public uint GetRKey2()
        {
            return rKey2;
        }

        public static byte[] RSAEncrypt(byte[] publicKey, byte[] input)
        {
            Array.Reverse(publicKey);
            BigInteger pkey = new(1, publicKey, publicKey.Length - 32, 32);
            Array.Reverse(publicKey);
            BigInteger exp = new(1, new byte[] { 0x00, 0x00, 0xFF, 0xFF });

            RsaKeyParameters pubParameters = new(false, pkey, exp);
            IAsymmetricBlockCipher eng = new Pkcs1Encoding(new RsaEngine());
            eng.Init(true, pubParameters);
            return eng.ProcessBlock(input, 0, input.Length);
        }

        public static byte[] RSADecrypt(RsaKeyParameters privateKey, byte[] input)
        {
            IAsymmetricBlockCipher eng = new Pkcs1Encoding(new RsaEngine());
            eng.Init(false, privateKey);
            return eng.ProcessBlock(input, 0, input.Length);
        }

        public static SqRSAKeyPair RSAGenerate()
        {
            BigInteger exp = new(1, new byte[] { 0x00, 0x00, 0xFF, 0xFF });
            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(new RsaKeyGenerationParameters(exp, new SecureRandom(), 256, 8));

            var keyPair = keyPairGenerator.GenerateKeyPair();

            RsaKeyParameters privateKey = (RsaKeyParameters)keyPair.Private;
            RsaKeyParameters publicKey = (RsaKeyParameters)keyPair.Public;

            return new SqRSAKeyPair(privateKey, publicKey);
        }

        public static string EncodeBase32(byte[] input, int size)
        {
            byte[] output = new byte[8 * ((size + 4) / 5)];   // 8 chars per 5-byte group, rounded up
            int dataCntr = 0;
            int inCntr = 0;

            if (size >= 5)
            {
                for (int i = 0; i < size; i += 5)
                {
                    if (size < i + 5)
                        break;
                    output[dataCntr++] = b32EncodeTable[input[i] >> 3];
                    output[dataCntr++] = b32EncodeTable[(input[i] & 7) << 2 | (input[i + 1] >> 6)];
                    output[dataCntr++] = b32EncodeTable[(input[i + 1] >> 1) & 0x1f];
                    output[dataCntr++] = b32EncodeTable[(input[i + 1] & 1) << 4 | (input[i + 2] >> 4)];
                    output[dataCntr++] = b32EncodeTable[(input[i + 2] & 0xf) << 1 | (input[i + 3] >> 7)];
                    output[dataCntr++] = b32EncodeTable[(input[i + 3] >> 2) & 0x1f];
                    output[dataCntr++] = b32EncodeTable[(input[i + 3] & 3) << 3 | (input[i + 4] >> 5)];
                    output[dataCntr++] = b32EncodeTable[input[i + 4] & 0x1f];
                    inCntr += 5;
                }
            }

            if (inCntr < size)
            {
                int leftover = (int)size - inCntr;
                byte[] tempInput = new byte[5];
                Array.Copy(input, inCntr, tempInput, 0, leftover);
                output[dataCntr++] = b32EncodeTable[tempInput[0] >> 3];
                output[dataCntr++] = b32EncodeTable[(tempInput[0] & 7) << 2 | (tempInput[1] >> 6)];
                output[dataCntr++] = b32EncodeTable[(tempInput[1] >> 1) & 0x1f];
                output[dataCntr++] = b32EncodeTable[(tempInput[1] & 1) << 4 | (tempInput[2] >> 4)];
                output[dataCntr++] = b32EncodeTable[(tempInput[2] & 0xf) << 1 | (tempInput[3] >> 7)];
                output[dataCntr++] = b32EncodeTable[(tempInput[3] >> 2) & 0x1f];
                output[dataCntr++] = b32EncodeTable[(tempInput[3] & 3) << 3 | (tempInput[4] >> 5)];
                output[dataCntr++] = b32EncodeTable[tempInput[4] & 0x1f];
            }

            return Encoding.ASCII.GetString(output).TrimEnd(new char[] { '\0' });
        }

        public static byte[] DecodeBase32(string input, int size)
        {
            byte[] strChar = Encoding.ASCII.GetBytes(input);
            byte[] data = new byte[(8 * size / 5) + 5];
            int dataCntr = 0;

            for (int i = 0; i < size; i += 8)
            {
                byte c0 = b32DecodeTable[strChar[i + 0]];
                byte c1 = b32DecodeTable[strChar[i + 1]];
                byte c2 = b32DecodeTable[strChar[i + 2]];
                byte c3 = b32DecodeTable[strChar[i + 3]];
                byte c4 = b32DecodeTable[strChar[i + 4]];
                byte c5 = b32DecodeTable[strChar[i + 5]];
                byte c6 = b32DecodeTable[strChar[i + 6]];
                byte c7 = b32DecodeTable[strChar[i + 7]];

                data[dataCntr + 0] = (byte)(c0 << 3 | c1 >> 2);
                data[dataCntr + 1] = (byte)((c2 | c1 << 5) << 1 | c3 >> 4);
                data[dataCntr + 2] = (byte)(c4 >> 1 | c3 << 4);
                data[dataCntr + 3] = (byte)((c5 | c4 << 5) << 2 | c6 >> 3);
                data[dataCntr + 4] = (byte)(c7 | c6 << 5);

                dataCntr += 5;
            }

            return data;
        }

        public static string EncodeBase64(byte[] input, int size, bool padding)
        {
            byte[] data = new byte[(4 * size / 3) + 3];
            int dataCntr = 0;
            int inCntr = 0;

            if (size >= 3)
            {
                for (int i = 0; i < size; i += 3)
                {
                    if (size < i + 3)
                        break;

                    int concat =
                        input[i + 0] << 16 |
                        input[i + 1] << 08 |
                        input[i + 2];

                    data[dataCntr++] = b64EncodeTable[concat >> 0x12 & 0x3F];
                    data[dataCntr++] = b64EncodeTable[concat >> 0x0c & 0x3F];
                    data[dataCntr++] = b64EncodeTable[concat >> 0x06 & 0x3F];
                    data[dataCntr++] = b64EncodeTable[concat & 0x3F];
                    inCntr += 3;
                }
            }

            if (inCntr < size)
            {
                int leftover = (int)size - inCntr;
                int concat = 0;

                // Pack the leftover bytes into the high end of the 24-bit group,
                // then zero-fill the missing byte(s): 1 byte -> concat << 16, 2 bytes -> concat << 8.
                for (int i = 0; i < leftover; i++)
                    concat = (concat << 8) | input[inCntr + i];
                for (int i = leftover; i < 3; i++)
                    concat <<= 8;

                data[dataCntr++] = b64EncodeTable[concat >> 0x12 & 0x3F];
                data[dataCntr++] = b64EncodeTable[concat >> 0x0c & 0x3F];
                data[dataCntr++] = b64EncodeTable[concat >> 0x06 & 0x3F];
                data[dataCntr++] = b64EncodeTable[concat & 0x3F];

                // Drop the base64 chars that carry no data so TrimEnd removes them:
                // 1 input byte -> 2 chars, 2 input bytes -> 3 chars.
                if (padding && leftover == 1)
                    data[dataCntr - 2] = 0;
                if (padding)
                    data[dataCntr - 1] = 0;
            }

            return Encoding.ASCII.GetString(data).TrimEnd(new char[] { '\0' });
        }

        public static byte[] DecodeBase64(string input, int size)
        {
            byte[] data = Encoding.ASCII.GetBytes(input);

            if (data is null)
                throw new ArgumentNullException(nameof(data));

            if (data.Length == 0)
                throw new ArgumentException(nameof(data));

            if (size <= 0)
                throw new ArgumentOutOfRangeException(nameof(size));

            int count;
            int value;
            int bufferPtr;
            int dataPtr;
            int count2;
            byte[] tempBuffer;
            byte[] buffer = new byte[3 * ((size + 3) / 4)];   // 3 bytes per 4-char group, rounded up

            count = 0;

            if (size >= 4)
            {
                bufferPtr = 0;
                dataPtr = 0;

                while (true)
                {
                    count += 4;

                    byte byte3 = (byte)(dataPtr + 3 < size ? b64DecodeTable[data[dataPtr + 3]] : 0);
                    byte byte2 = (byte)(dataPtr + 2 < size ? b64DecodeTable[data[dataPtr + 2]] : 0);
                    byte byte1 = (byte)(dataPtr + 1 < size ? b64DecodeTable[data[dataPtr + 1]] : 0);
                    byte byte0 = (byte)(dataPtr < size ? b64DecodeTable[data[dataPtr]] : 0);

                    value = byte3 | ((byte2 | ((byte1 | (byte0 << 6)) << 6)) << 6);

                    buffer[bufferPtr + 2] = (byte)value;
                    buffer[bufferPtr] = (byte)(value >> 16);
                    buffer[bufferPtr + 1] = (byte)(value >> 8);

                    if (count >= size)
                        break;

                    dataPtr += 4;
                    bufferPtr += 3;
                }

                if (count > size)
                {
                    count2 = count - 4;
                    tempBuffer = new byte[] { 0x54, 0x54, 0x54, 0x54 };

                    if (count2 < size)
                        Buffer.BlockCopy(data, dataPtr, tempBuffer, 0, size - count2);

                    value = b64DecodeTable[tempBuffer[3]] | ((b64DecodeTable[tempBuffer[2]] | ((b64DecodeTable[tempBuffer[1]] | (b64DecodeTable[tempBuffer[0]] << 6)) << 6)) << 6);

                    buffer[bufferPtr + 2] = (byte)value;
                    buffer[bufferPtr] = (byte)(value >> 16);
                    buffer[bufferPtr + 1] = (byte)(value >> 8);
                }
            }
            else
            {
                tempBuffer = new byte[] { 0x00, 0x00, 0x00, 0x00 };

                if (size > 0)
                    Buffer.BlockCopy(data, 0, tempBuffer, 0, size);

                value = b64DecodeTable[tempBuffer[3]] | ((b64DecodeTable[tempBuffer[2]] | ((b64DecodeTable[tempBuffer[1]] | (b64DecodeTable[tempBuffer[0]] << 6)) << 6)) << 6);

                buffer[0] = (byte)(value >> 16);
                buffer[1] = (byte)(value >> 8);
                buffer[2] = (byte)value;
            }

            return buffer;
        }

        public static string ScramblePolId(string polIdData, bool isBot = false)
        {
            if (polIdData == null || polIdData.Length != 8)
                return null;

            // Convert to PolProId
            ulong polProId = PolProDataToPolId(polIdData, 0, 0) | 0xC00000000000;

            // Scramble
            for (int i = 4; i >= 0; i--)
            {
                ulong shifter = (ulong)0xFF << (i * 8);
                ulong polProIdShifted = polProId >> 8;
                polProIdShifted &= shifter;
                polProId ^= polProIdShifted ;
            }

            // Convert back to PolProData
            string scrambledPolIdData = PolIdToPolProData(polProId, scrambleMask: true);

            // Prepend a 'U'
            StringBuilder builder = new(scrambledPolIdData);
            builder.Insert(0, isBot ? 'P' : 'U');

            return builder.ToString();
        }

        public static string UnScramblePolId(string polIdData)
        {
            if (polIdData == null || polIdData.Length != 9)
                return null;

            polIdData = polIdData[1..];

            // Convert to PolProId
            ulong polProId = PolProDataToPolId(polIdData, 0, 0) | 0xC00000000000;

            // UnScramble
            ulong shifter = 0xFF00;
            for (int i = 0; i < 5; ++i)
            {
                polProId ^= (polProId & shifter) >> 8;
                shifter <<= 8;
            }

            // Convert back to PolProData
            return PolIdToPolProData(polProId, scrambleMask: true);
        }

        public static string EncodePolPassword(string toEncode, byte xor = 0xFF)
        {
            toEncode += "\0";
            byte[] input = Encoding.ASCII.GetBytes(toEncode);
            byte[] output = new byte[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                byte fromEnd = input[input.Length - i - 1];
                if ((i & 1) != 0)
                    fromEnd = (byte)(fromEnd ^ xor);
                output[i] = (byte)(fromEnd >> (8 - (i & 7) & 0x1f) | fromEnd << (i & 7));
            }
            return Encoding.ASCII.GetString(output);
        }

        public static string GeneratePOLPasswordMD5(string adminDataBase32, byte[] passwordBytes)
        {
            byte[] base32Bytes = Encoding.ASCII.GetBytes(adminDataBase32);
            byte[] resultHash = new byte[0x10];

            MD5Hash md5 = new();

            for (int i = 0; i < base32Bytes.Length; i++)
                md5.BlockUpdate(new byte[] { base32Bytes[i] }, 0, 1);

            for (int i = 0; i < passwordBytes.Length; i++)
                md5.BlockUpdate(new byte[] { passwordBytes[i] }, 0, 1);

            md5.DoFinal(resultHash, 0);

            Array.Clear(passwordBytes);

            return BitConverter.ToString(resultHash).Replace("-", "").ToLower();
        }

        public static byte[] GenerateSEPasswordSha1(string password, int calendarTime)
        {
            byte[] resultHash = null;

                var hash = SHA1.HashData(Encoding.ASCII.GetBytes(password));
                string sha1AsString = BitConverter.ToString(hash).Replace("-", "").ToLower() + "playonline";
                hash = SHA1.HashData(Encoding.ASCII.GetBytes(sha1AsString));

                string calendarTimeAsString = calendarTime.ToString();

                sha1AsString = BitConverter.ToString(hash).Replace("-", "").ToLower() + calendarTimeAsString;
                resultHash = SHA1.HashData(Encoding.ASCII.GetBytes(sha1AsString));
           
            return resultHash;
        }

        public static uint CalcCheckSum(string input)
        {
            byte[] bytesIn = Encoding.GetEncoding("shift_jis").GetBytes(input);
            return CalcCheckSum(bytesIn, bytesIn.Length);
        }

        public static uint CalcCheckSum(byte[] input)
        {
            return CalcCheckSum(input, input.Length);
        }

        public static uint CalcCheckSum(byte[] input, int length)
        {
            int i = 0;
            uint checksum = 0;

            while (i <= length - 4)
            {
                uint val = BitConverter.ToUInt32(input, i);
                checksum += val;
                i += 4;
            }

            uint checksum2 = 0;
            while (i < length)
                checksum2 = checksum2 >> 8 | (uint)input[i++] << 24;

            return checksum + checksum2;
        }

        public static byte[] CheckSumToBytes(uint checksum)
        {
            byte[] byteResult = new byte[4];

            byteResult[0] = (byte)(((checksum >> 0x1A) & 0x3F) + 0x3F);
            byteResult[1] = (byte)(((checksum >> 0x14) & 0x3F) + 0x3F);
            byteResult[2] = (byte)(((checksum >> 0x0E) & 0x3F) + 0x3F);
            byteResult[3] = (byte)(((checksum >> 0x08) & 0x3F) + 0x3F);

            return byteResult;
        }

        // Payload = message bytes with the 4 checksum bytes still appended
        public static bool VerifyCheckSum(ReadOnlySpan<byte> payload)
        {
            return payload.Length >= 4 &&
                   payload[^4..].SequenceEqual(CheckSumToBytes(CalcCheckSum(payload[..^4].ToArray())));
        }

        public static string CheckSumToStr(uint checksum)
        {
            return Encoding.GetEncoding("shift_jis").GetString(CheckSumToBytes(checksum));
        }

        public static ulong CreateBaseKeyForPolProEncryption(string polIdData)
        {
            if (polIdData.Length != 8)
                return 0;

            byte[] polIdWork = Encoding.ASCII.GetBytes(polIdData);

            //Loop 1
            for (int i = 0; i < 7; i++)
            {
                byte val1 = polIdWork[i];
                byte val2 = polIdWork[i + 1];
                byte result = (byte)(val1 ^ val2);

                if (result != 2)
                    polIdWork[i + 1] = (byte)(result | 0x80);
                else
                    polIdWork[i + 1] = result;
            }

            //Loop 2
            ulong loop2Result = 123456789012345;
            uint counter = 1;
            for (int i = 0; i < 7; i++)
            {
                byte val1 = polIdWork[i];
                byte val2 = polIdWork[i + 1];
                loop2Result = (ulong)(val1 * val2) * loop2Result;
                counter += val2;
            }

            //Loop 3
            uint loop3Result = 0;
            for (int i = 0; i < 8; i++)
            {
                byte val = polIdWork[i];
                if (val >= 0x30)
                {
                    for (int j = val - 0x30; j > 0; j--)
                        loop3Result = ((loop3Result * 0x425f0cbd) + 0x7f4f) & 0xFFFFFFFF;
                }
            }

            return (loop2Result + counter) ^ loop3Result;
        }

        public static ulong PolProDataToPolId(string polIdData, ushort volume, byte domain)
        {
            if (polIdData.Length != 8)
                return 0;

            byte[] polData = Encoding.ASCII.GetBytes(polIdData);

            ulong polId = sqCharacterToPolIdMap[polData[0]];
            for (int i = 1; i < 8; i++)
            {
                uint val = sqCharacterToPolIdMap[polData[i]];
                polId = (polId * 0x24) + val;
            }
            
            ulong extra = (ulong) (((volume & 0xFFFF) << 7) | (domain & 0x7F & 0xFF));

            return polId | (extra << 41);
        }

        public static string PolIdToPolProData(ulong polProId, bool scrambleMask = false)
        {
            return PolIdToPolProData(polProId, out _, out _, scrambleMask);
        }

        public static string PolIdToPolProData(ulong polProId, out ushort volume, out byte domain, bool scrambleMask = false)
        {
            byte[] polProData = new byte[8];
            uint extras = (uint)(polProId >> 41);
            volume = (ushort) (extras >> 7);
            domain = (byte) (extras & 0x7F);

            if (scrambleMask)
                polProId &= 0x3FFFFFFFFFF;
            else
                polProId &= 0x1FFFFFFFFFF;

            ulong value = polProId;
            for (int i = 7; i >= 0; i--)
            {
                ulong lastValue = value;
                value /= 0x24;
                polProData[i] = sqPolIdToCharacterMap[lastValue - (value * 0x24)];
            }

            return Encoding.ASCII.GetString(polProData);
        }

        public static ulong EncryptPolId(string polIdData, ulong baseKey)
        {
            if (polIdData.Length != 8)
                return 0;

            ulong result = PolProDataToPolId(polIdData, 0, 0);
            ulong encrypted = result ^ baseKey;
            return encrypted;
        }

        public static ulong ChangeCryptPolId(ulong polProId, ulong baseKey, ulong newKey)
        {
            return polProId ^ baseKey ^ newKey;
        }

        public static string DecryptPolId(ulong encryptedPolProId, ulong baseKey)
        {
            ulong decryptedPolId = encryptedPolProId ^ baseKey;
            return PolIdToPolProData(decryptedPolId, out _, out _);
        }        

        public static byte[] VolumeDomainMerge(byte[] polProIdEncrypted, ulong baseKey, ushort volume, byte domain)
        {
            ulong polProIdEncryptedAsUInt64 = BitConverter.ToUInt64(polProIdEncrypted, 0);
            ulong encrypted = polProIdEncryptedAsUInt64 ^ baseKey;
            return BitConverter.GetBytes(encrypted);
        }

        public static byte [] PolGenerateContentsAuthenticationPassword(byte[] internetAddress, byte[] polRandomValueBinary, uint loginTime, ulong blowfishKey, byte volume)
        {
            byte[] authPassword = new byte[0x48];

            byte[] hashCode = PolGenerateContentsAuthHashCode(polRandomValueBinary, loginTime, blowfishKey);
            Array.Copy(hashCode, 0, authPassword, 0x18, 0x10);
            Array.Copy(internetAddress, 0, authPassword, 0x4, internetAddress.Length);

            authPassword[0] = (byte) (authPassword[37] ^ authPassword[27]);
            authPassword[1] = 0;

            SqCrypto.SqFileEncryption fileEncryption = new();
            fileEncryption.Init(0x0); // Find this in polcore.dll
            fileEncryption.Encrypt(authPassword, 0x28);

            Array.Copy(authPassword, 0, authPassword, 4, 0x30);

            for (int i = 0x5; i < 0x34; i++)
                authPassword[i] ^= authPassword[i - 1];

            authPassword[0] = (byte)(authPassword[14] ^ volume);
            authPassword[1] = (byte)(authPassword[12] ^ authPassword[6]);
            authPassword[2] = (byte)(authPassword[10] ^ authPassword[8]);
            authPassword[3] = (byte)(authPassword[13] ^ authPassword[9]);

            return authPassword;
        }

        public static bool DecryptAuthPassword(byte[] authPassword, out byte[] outAuthHash, out uint outClientIp, out ushort outClientPort)
        {
            outAuthHash = null;
            outClientIp = 0;
            outClientPort = 0;

            if (authPassword == null)
                return false;

            // Verify
            if (authPassword[1] != (authPassword[6] ^ authPassword[12]) ||
                authPassword[2] != (authPassword[8] ^ authPassword[10]) ||
                authPassword[3] != (authPassword[9] ^ authPassword[13]))
                return false;
            byte checksumByte = authPassword[14];

            // Decode
            for (int i = 51; i > 4; --i)
                authPassword[i] ^= authPassword[i - 1];

            byte[] decrypted = new byte[0x30];
            Array.Copy(authPassword, 4, decrypted, 0, 0x30);

            // Decrypt
            SqFileEncryption sqDecryptFile = new(); 
            sqDecryptFile.Init(0x0); // Find this in polcore.dll
            sqDecryptFile.Decrypt(decrypted, 0x30);

            // Verify 2
            if (authPassword[0] != (decrypted[1] ^ checksumByte) ||
                decrypted[0] != (decrypted[27] ^ decrypted[37]))
                return false;

            // Copy data
            byte[] authHash = new byte[0x10];
            Array.Copy(decrypted, 0x18, authHash, 0, 0x10);

            outAuthHash = authHash;
            outClientPort = BitConverter.ToUInt16(decrypted, 0x6);
            outClientIp = BitConverter.ToUInt32(decrypted, 0x8);

            return true;
        }

        public static byte[] PolGenerateContentsAuthHashCode(byte[] polRandomValueBinary, uint loginTime, ulong blowfishKey)
        {
            byte[] md5Result = new byte[0x10];
            byte[] workBuff = new byte[0x1C];
            Array.Copy(polRandomValueBinary, workBuff, 0x10);

            for (uint i = 0; i < 4; i++)
            {
                workBuff[0x10 + i] = (byte)(loginTime & 0xFF);
                loginTime >>= 8;
            }

            for (uint i = 0; i < 8; i++)
            {
                workBuff[0x14 + i] = (byte) (blowfishKey & 0xFF);
                blowfishKey >>= 8;
            }

            MD5Hash md5 = new();
            Console.WriteLine(BitConverter.ToString(workBuff));
            md5.BlockUpdate(workBuff, 0, 0x1C);
            md5.DoFinal(md5Result, 0, false);

            return md5Result;
        }

        public static void PolCacheDecrypt(Span<byte> file, uint key1 = 0x6dd0293, uint key2 = 0x683071b, uint chunkSize = 0x7FF8)
        {
            uint xor1 = key1;
            uint xor2 = key2;
            uint tmp1;
            uint tmp2;
            int j = 0;
            int chunkScanner = 0;

            uint checksum1 = 0, checksum2 = 0;
            bool checksumGood = true;

            var inFile = MemoryMarshal.Cast<byte, uint>(file);

            for (int i = 0; i < (file.Length/4) - 2; i+=2)
            {
                if (chunkScanner >= chunkSize)
                {
                    if (!(checksum1 == (inFile[i] ^ xor1) && checksum2 == (inFile[i + 1] ^ xor2)))
                        checksumGood = false;

                    xor1 = key1;
                    xor2 = key2;
                    checksum1 = checksum2 = 0;
                    chunkScanner = 0;
                    continue;
                }

                inFile[j] = inFile[i] ^ xor1;
                inFile[j+1] = inFile[i+1] ^ xor2;
                checksum1 ^= inFile[j];
                checksum2 ^= inFile[j+1];
                chunkScanner += 8;
                j +=2;

                tmp1 = xor1 >> 0xd;
                tmp2 = ~xor2 >> 0xd | xor1 << 0x13;

                xor1 = tmp2 + 0x6dd0293U;
                xor2 = (xor2 << 0x13 | tmp1) + 0x683071bU + (0xf922fd6cU < tmp2 ? 1U : 0U);
            }

            int lastChecksumPos = (file.Length / 4) - 2;
            if (!(checksum1 == (inFile[lastChecksumPos] ^ xor1) && checksum2 == (inFile[lastChecksumPos + 1] ^ xor2)))
                checksumGood = false;
        }

        public class SqFileEncryption
        {
            private ulong[] SBox = new ulong[32];
            private uint BytesRead, CheckSum;

            private static byte[] EncodeTable = {
                0x88, 0x89, 0x8a, 0x8b, 0x8c, 0x8d, 0x8e, 0x8f, 0x90, 0x91, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97,
                0x98, 0x99, 0x9a, 0x9b, 0x9c, 0x9d, 0x9e, 0x9f, 0xa0, 0xa1, 0xa2, 0xa3, 0xa4, 0xa5, 0xa6, 0xa7,
                0xa8, 0xa9, 0xaa, 0xab, 0xac, 0xad, 0xae, 0xaf, 0xb0, 0xb1, 0xb2, 0xb3, 0xb4, 0xb5, 0xb6, 0xb7,
                0xb8, 0xb9, 0xba, 0xbb, 0xbc, 0xbd, 0xbe, 0xbf, 0xc0, 0xc1, 0xc2, 0xc3, 0xc4, 0xc5, 0xc6, 0xc7,
                0xc8, 0xc9, 0xca, 0xcb, 0xcc, 0xcd, 0xce, 0xcf, 0xd0, 0xd1, 0xd2, 0xd3, 0xd4, 0xd5, 0xd6, 0xd7,
                0xd8, 0xd9, 0xda, 0xdb, 0xdc, 0xdd, 0xde, 0xdf, 0xe0, 0xe1, 0xe2, 0xe3, 0xe4, 0xe5, 0xe6, 0xe7,
                0xe8, 0xe9, 0xea, 0xeb, 0xec, 0xed, 0xee, 0xef, 0xf0, 0xf1, 0xf2, 0xf3, 0xf4, 0xf5, 0xf6, 0xf7,
                0xf8, 0xf9, 0xfa, 0xfb, 0xfc, 0xfd, 0xfe, 0xff, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
                0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
                0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27,
                0x28, 0x29, 0x2a, 0x2b, 0x2c, 0x2d, 0x2e, 0x2f, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37,
                0x38, 0x39, 0x3a, 0x3b, 0x3c, 0x3d, 0x3e, 0x3f, 0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47,
                0x48, 0x49, 0x4a, 0x4b, 0x4c, 0x4d, 0x4e, 0x4f, 0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57,
                0x58, 0x59, 0x5a, 0x5b, 0x5c, 0x5d, 0x5e, 0x5f, 0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67,
                0x68, 0x69, 0x6a, 0x6b, 0x6c, 0x6d, 0x6e, 0x6f, 0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77,
                0x78, 0x79, 0x7a, 0x7b, 0x7c, 0x7d, 0x7e, 0x7f, 0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87,
                0x78, 0x79, 0x7a, 0x7b, 0x7c, 0x7d, 0x7e, 0x7f, 0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87
            };

            private static byte[] DecodeTable = {
                0x78, 0x79, 0x7a, 0x7b, 0x7c, 0x7d, 0x7e, 0x7f, 0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87,
                0x88, 0x89, 0x8a, 0x8b, 0x8c, 0x8d, 0x8e, 0x8f, 0x90, 0x91, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97,
                0x98, 0x99, 0x9a, 0x9b, 0x9c, 0x9d, 0x9e, 0x9f, 0xa0, 0xa1, 0xa2, 0xa3, 0xa4, 0xa5, 0xa6, 0xa7,
                0xa8, 0xa9, 0xaa, 0xab, 0xac, 0xad, 0xae, 0xaf, 0xb0, 0xb1, 0xb2, 0xb3, 0xb4, 0xb5, 0xb6, 0xb7,
                0xb8, 0xb9, 0xba, 0xbb, 0xbc, 0xbd, 0xbe, 0xbf, 0xc0, 0xc1, 0xc2, 0xc3, 0xc4, 0xc5, 0xc6, 0xc7,
                0xc8, 0xc9, 0xca, 0xcb, 0xcc, 0xcd, 0xce, 0xcf, 0xd0, 0xd1, 0xd2, 0xd3, 0xd4, 0xd5, 0xd6, 0xd7,
                0xd8, 0xd9, 0xda, 0xdb, 0xdc, 0xdd, 0xde, 0xdf, 0xe0, 0xe1, 0xe2, 0xe3, 0xe4, 0xe5, 0xe6, 0xe7,
                0xe8, 0xe9, 0xea, 0xeb, 0xec, 0xed, 0xee, 0xef, 0xf0, 0xf1, 0xf2, 0xf3, 0xf4, 0xf5, 0xf6, 0xf7,
                0xf8, 0xf9, 0xfa, 0xfb, 0xfc, 0xfd, 0xfe, 0xff, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
                0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
                0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27,
                0x28, 0x29, 0x2a, 0x2b, 0x2c, 0x2d, 0x2e, 0x2f, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37,
                0x38, 0x39, 0x3a, 0x3b, 0x3c, 0x3d, 0x3e, 0x3f, 0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47,
                0x48, 0x49, 0x4a, 0x4b, 0x4c, 0x4d, 0x4e, 0x4f, 0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57,
                0x58, 0x59, 0x5a, 0x5b, 0x5c, 0x5d, 0x5e, 0x5f, 0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67,
                0x68, 0x69, 0x6a, 0x6b, 0x6c, 0x6d, 0x6e, 0x6f, 0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77
            };

            public void Init(ulong seed)
            {
                // Setup Key
                uint seedHI = (uint)(seed >> 32);
                uint seedLO = (uint)(seed & 0xFFFFFFFF);
                seedLO = Utils.RotateLeft32(seedLO, 0x08);
                seedHI = Utils.RotateRight32(seedHI, 0x10);
                seed = ((ulong)seedLO << 32) | seedHI;

                byte[] bytes = BitConverter.GetBytes(seed);
                bytes[0] += 0x45;
                for (int i = 1; i < 8; i++)
                {
                    byte tmp = (byte)(bytes[i] + bytes[i - 1] - 0x2c);
                    bytes[i] = (byte)(bytes[i - 1] << 2 ^ tmp ^ 0x45);
                }

                // Build Table
                SBox[0] = BitConverter.ToUInt64(bytes, 0);
                for (int i = 0x1; i < SBox.Length; i++)
                    SBox[i] = SBox[i - 1] * 5;

                // Zero these two
                BytesRead = CheckSum = 0;
            }

            public uint Encrypt(byte[] src, int size)
            {
                return Encrypt(src, src, size);
            }

            public uint Encrypt(byte[] src, byte[] dst, int size)
            {
                BytesRead = CheckSum = 0;

                int remainderSize = size & 0x7;
                EncryptDataInner(src, 0, dst, 0, size - remainderSize);

                // If this was not divisible by 8, encrypt the last bytes.
                if (remainderSize != 0)
                {
                    ulong remainingBytes = 0;
                    for (int i = remainderSize; i >= 0; i--)
                        remainingBytes = (remainingBytes << 8) | src[size - remainderSize + i];
                    EncryptDataInner(BitConverter.GetBytes(remainingBytes), 0, dst, size - remainderSize, 8);
                }

                // Append the size and checksum at the end if room               
                int dataEnd = remainderSize == 0 ? size : size - remainderSize + 8;
                if (dst.Length >= dataEnd + 8)
                {
                    ulong sizeAndChecksum = ((ulong)CheckSum << 32) | BytesRead;
                    EncryptDataInner(BitConverter.GetBytes(sizeAndChecksum), 0, dst, dataEnd, 8);
                }

                return BytesRead;
            }

            private void EncryptDataInner(byte[] src, int srcOffset, byte[] dst, int dstOffset, int size)
            {
                for (int i = 0; i < size / 8; i++)
                {
                    int dataOffset = i * 8;
                    CheckSum += (uint)(src[srcOffset + dataOffset] + src[srcOffset + dataOffset + 4]);
                    FirstEncrypt(src, srcOffset + dataOffset, dst, dstOffset + dataOffset);
                    SecondEncrypt(dst, dstOffset + dataOffset, dst, dstOffset + dataOffset);
                    BytesRead += 8;
                }
            }

            private void FirstEncrypt(byte[] src, int srcOffset, byte[] dst, int dstOffset)
            {
                ulong data = BitConverter.ToUInt64(src, srcOffset);
                data = (data >> 32) | (data << 32);

                ulong shiftedSize = ((ulong)BytesRead << 0xA) | BytesRead;
                shiftedSize = (shiftedSize << 0xA) | BytesRead;
                shiftedSize = (shiftedSize << 0xA) | BytesRead;
                shiftedSize += 0xA1652347;

                data ^= SBox[(BytesRead >> 3) & 0x1F];
                data ^= shiftedSize;
                data += SBox[(BytesRead >> 3) & 0x1F];

                Array.Copy(BitConverter.GetBytes(data), 0, dst, dstOffset, 8);
            }

            private void SecondEncrypt(byte[] src, int srcOffset, byte[] dst, int dstOffset)
            {
                byte xorByte1 = (byte) ((BytesRead >> 3) ^ 0x45);
                for (int i = 0; i < 8; i++)
                {
                    byte xorByte2 = src[srcOffset + i];
                    for (int j = 0; j < 8; j++)
                    {
                        // Bitmath to extract the byte from the SBox since it's ulongs. BytesRead/8, then
                        // shift by j bytes (*8 bits) and cut off the rest.
                        byte sboxVal = (byte)((SBox[(BytesRead >> 3) & 0x1F] >> (j * 8)) & 0xFF);
                        xorByte2 = EncodeTable[(sboxVal + xorByte2) & 0xFF];
                    }
                    dst[dstOffset + i] = xorByte1 = (byte) (xorByte1 ^ xorByte2);
                }
            }

            public int Decrypt(byte[] src, int size)
            {
                return Decrypt(src, src, size);
            }

            public int Decrypt(byte[] src, byte[] dst, int size)
            {
                if ((size & ~7) == 0)
                    return -1;

                if ((size & ~7) != size)
                    return -1;

                BytesRead = CheckSum = 0;

                DecryptDataInner(src, 0, dst, 0, size - 8);
                
                uint dataChecksum = CheckSum;

                DecryptDataInner(src, size - 8, dst, size - 8, 8);

                if (dataChecksum == BitConverter.ToUInt32(dst, size - 4))
                    return BitConverter.ToInt32(dst, size - 8);

                return -1;
            }

            private void DecryptDataInner(byte[] src, int srcOffset, byte[] dst, int dstOffset, int size)
            {
                for (int i = 0; i < size / 8; i++)
                {
                    int dataOffset = i * 8;                    
                    SecondDecrypt(src, srcOffset + dataOffset, dst, dstOffset + dataOffset);
                    FirstDecrypt(dst, dstOffset + dataOffset, dst, dstOffset + dataOffset);
                    CheckSum += (uint)(dst[dstOffset + dataOffset] + dst[dstOffset + dataOffset + 4]);
                    BytesRead += 8;
                }
            }

            private void FirstDecrypt(byte[] src, int srcOffset, byte[] dst, int dstOffset)
            {             
                ulong data = BitConverter.ToUInt64(src, srcOffset);

                ulong shiftedSize = ((ulong)BytesRead << 0xA) | BytesRead;
                shiftedSize = (shiftedSize << 0xA) | BytesRead;
                shiftedSize = (shiftedSize << 0xA) | BytesRead;
                shiftedSize += 0xA1652347;

                data -= SBox[(BytesRead >> 3) & 0x1F];
                data ^= shiftedSize;
                data ^= SBox[(BytesRead >> 3) & 0x1F];
                
                data = (data >> 32) | (data << 32);
                Array.Copy(BitConverter.GetBytes(data), 0, dst, dstOffset, 8);
            }

            private void SecondDecrypt(byte[] src, int srcOffset, byte[] dst, int dstOffset)
            {
                byte xorByte1 = (byte)(((BytesRead >> 3) ^ 0x45) & 0xFF);

                for (int i = 0; i < 8; i++)
                {
                    byte xorByte2 = (byte) (src[srcOffset + i] ^ xorByte1);

                    for (int j = 0; j < 8; j++)
                    {
                        // Bitmath to extract the byte from the SBox since it's ulongs. BytesRead/8, then
                        // shift by j bytes (*8 bits) and cut off the rest.
                        byte sboxVal = (byte)((SBox[(BytesRead >> 3) & 0x1F] >> (j * 8)) & 0xFF);
                        xorByte2 = (byte) (DecodeTable[xorByte2] - sboxVal);
                    }

                    byte temp = src[srcOffset + i];                    
                    dst[dstOffset + i] = xorByte2;
                    xorByte1 = temp;
                }
            }
        }
    }
}