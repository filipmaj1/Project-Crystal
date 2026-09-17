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

namespace Crystal.POLAuth.DataObjects
{
    class FriendStateOfflineNotification
    {
        private readonly string PolIdData;
        private readonly byte Position;

        public FriendStateOfflineNotification(string polIdData, byte position)
        {
            this.PolIdData = polIdData;
            this.Position = position;
        }

        public string GetNoticeMsg()
        {
            byte[] packet = new byte[0x100];
            using (MemoryStream memStream = new(packet))
            using (BinaryWriter binWriter = new(memStream))
            {
                binWriter.Write((UInt64)SqCrypto.ChangeCryptPolId(SqCrypto.PolProDataToPolId(PolIdData, 0, 0), 0, SqCrypto.POL_CRYPTKEY_MESSAGE));
                binWriter.Write((UInt64)0);
                binWriter.Write((Byte)(0));
                
                binWriter.Write((Byte)0);

                binWriter.Seek(0x14, SeekOrigin.Begin);
                binWriter.Write((UInt16)0);

                binWriter.Seek(0x18, SeekOrigin.Begin);
                binWriter.Write((Byte)1);
                binWriter.Write((Byte)1);

                binWriter.Seek(0x1C, SeekOrigin.Begin);
                binWriter.Write((Byte)Position);

                binWriter.Seek(0x34, SeekOrigin.Begin);
                binWriter.Write((UInt32)Utils.UnixTimeStampUTC());

                binWriter.Write((UInt32)8);

                binWriter.Seek(0x3E, SeekOrigin.Begin);
                binWriter.Write((UInt16)0xCF80);

                binWriter.Seek(0x42, SeekOrigin.Begin);
                binWriter.Write((Byte)0x1);

                binWriter.Seek(0x48, SeekOrigin.Begin);

                binWriter.Write((UInt64)0x01);
                binWriter.Write((UInt64)0x0);
                     
            }

            return SqCrypto.EncodeBase64(packet, packet.Length, false);
        }
    }
}
