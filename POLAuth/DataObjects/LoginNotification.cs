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
    class LoginNotification
    {
        private readonly ulong PolProId;
        private readonly byte NumPolMail;
        private readonly uint LastEmailUpdate;
        private readonly uint LastFriendListUpdate;
        private readonly uint LastHandleListUpdate;
        private readonly uint LastCharacterListUpdate;        

        public LoginNotification(ulong polProId, byte numPOLMail, uint lastEmailUpdate, uint lastFriendListUpdate, uint lastHandleListUpdate, uint lastCharacterListUpdate)
        {
            PolProId = polProId;
            NumPolMail = numPOLMail;
            LastEmailUpdate = lastEmailUpdate;
            LastFriendListUpdate = lastFriendListUpdate;
            LastHandleListUpdate = lastHandleListUpdate;
            LastCharacterListUpdate = lastCharacterListUpdate;
        }

        public byte[] GetByes()
        {
            byte[] data = new byte[0x48];

            using (MemoryStream memStream = new(data))
            using (BinaryWriter binWriter = new(memStream))
            {
                binWriter.Write((UInt64) SqCrypto.ChangeCryptPolId(PolProId, 0, SqCrypto.POL_CRYPTKEY_MESSAGE));
                binWriter.Write((UInt64) 0);
                binWriter.Write((Byte) 1); // PolPro is available
                binWriter.Write((Byte) NumPolMail);
                binWriter.Write((UInt16) 0);
                binWriter.Write((UInt32)LastEmailUpdate);
                binWriter.Write((Byte)0xFF); // Required CFLAG
                binWriter.Write((Byte)0x00);
                binWriter.Write((Byte)0x00);
                binWriter.Write((Byte)0x01); // Required CFLAG
                binWriter.Write((Byte)0xFF); // Required CFLAG
                binWriter.BaseStream.Seek(0x22, SeekOrigin.Begin);
                binWriter.Write((UInt16)0x01); // UDP Offset
                binWriter.Write((UInt32)LastFriendListUpdate);
                binWriter.Write((UInt32)LastHandleListUpdate);
                binWriter.Write((UInt32)LastCharacterListUpdate);
                binWriter.BaseStream.Seek(0x3E, SeekOrigin.Begin);
                binWriter.Write((UInt16)0xCF80);
                binWriter.Write((UInt16)0x0000); // Content ID is 0 on login
                binWriter.Write((UInt16)0x0001); // PolProRequest
            }

            return data;
        }

        public string GetBase64()
        {
            byte[] bytes = GetByes();
            return SqCrypto.EncodeBase64(bytes, bytes.Length, false);
        }
    }
}
