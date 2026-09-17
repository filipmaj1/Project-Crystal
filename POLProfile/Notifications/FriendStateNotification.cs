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
using System.Text;

namespace Crystal.POLProfile.Notifications
{
    class FriendStateNotification : Notification
    {
        public enum UpdateType { All, Status, Group, Portrait, Comment }

        readonly UpdateType updateType;

        readonly string polIdData;
        readonly bool isOnline;
        readonly byte status;
        readonly ushort contentID;
        readonly uint displayPicID;
        readonly string comment;
        readonly byte position;

        public FriendStateNotification(UpdateType updateType, string polIdData, byte position, bool isOnline, byte status, ushort contentID, uint displayPicID = 0, string comment = null)
        {
            this.updateType = updateType;
            this.polIdData = polIdData;
            this.position = position;
            this.isOnline = isOnline;
            this.status = status;
            this.contentID = contentID;
            this.displayPicID = displayPicID;
            this.comment = comment;
        }

        public override string GetNoticeMsg()
        {
            byte[] packet = new byte[0x100];
            using (MemoryStream memStream = new(packet))
            using (BinaryWriter binWriter = new(memStream))
            {
                binWriter.Write((UInt64)SqCrypto.ChangeCryptPolId(SqCrypto.PolProDataToPolId(polIdData, 0, 0), 0, SqCrypto.POL_CRYPTKEY_MESSAGE));
                binWriter.Write((UInt64)0);
                binWriter.Write((byte)(isOnline ? 1 : 0));
                
                // This HAS to map to the status byte somehow... they couldn't have just made two enums ?!?!?
                if (!isOnline || status == 5)
                    binWriter.Write((Byte)1);
                else if (status == 2)
                    binWriter.Write((Byte)3);
                else
                    binWriter.Write((Byte)2);

                binWriter.Seek(0x14, SeekOrigin.Begin);
                binWriter.Write((UInt16)contentID);

                binWriter.Seek(0x18, SeekOrigin.Begin);
                binWriter.Write((Byte)1);
                binWriter.Write((Byte)1);

                binWriter.Seek(0x1C, SeekOrigin.Begin);
                binWriter.Write((Byte)position);

                binWriter.Seek(0x34, SeekOrigin.Begin);
                binWriter.Write((UInt32)Utils.UnixTimeStampUTC());

                ushort payloadSize = 0;

                switch (updateType)
                {
                    case UpdateType.All:
                        payloadSize = 0xA1;
                        break;
                    case UpdateType.Status:
                        payloadSize = 8;
                        break;
                    case UpdateType.Portrait:
                        payloadSize = 8;
                        break;
                    case UpdateType.Comment:
                        payloadSize = 0x68;
                        break;
                }

                binWriter.Write((UInt32)payloadSize);

                binWriter.Seek(0x3E, SeekOrigin.Begin);
                binWriter.Write((UInt16)0xCF80);

                binWriter.Seek(0x42, SeekOrigin.Begin);
                binWriter.Write((Byte)0x1);

                binWriter.Seek(0x48, SeekOrigin.Begin);
                if (updateType == UpdateType.Status)
                {
                    binWriter.Write((UInt64)0x01);
                    binWriter.Write((UInt64)0x0);
                }
                else if (updateType == UpdateType.Portrait)
                {
                    binWriter.Write((UInt64)0x04);
                    binWriter.Write((UInt64)displayPicID);
                }
                else if (updateType == UpdateType.Comment)
                {
                    binWriter.Write((UInt64)0x20);
                    binWriter.Write(Encoding.Unicode.GetBytes(comment), 0, Encoding.Unicode.GetByteCount(comment) >= 0x64 ? 0x64 : Encoding.Unicode.GetByteCount(comment));
                }
                else if (updateType == UpdateType.All)
                {
                    binWriter.Write((UInt64)0x25);
                    binWriter.Write((UInt64)displayPicID);
                    binWriter.Write((UInt64)0x20);
                    binWriter.Write(Encoding.Unicode.GetBytes(comment), 0, Encoding.Unicode.GetByteCount(comment) >= 0x64 ? 0x64 : Encoding.Unicode.GetByteCount(comment));
                }               
            }

            return SqCrypto.EncodeBase64(packet, packet.Length, false);
        }
    }
}
