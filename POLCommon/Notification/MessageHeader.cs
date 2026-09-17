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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Crystal.Common.Notification
{
    public enum MessageType : byte
    {
        NormalMessage           = 0x00,
        FriendRequest           = 0x01,
        NewsMessage             = 0x02,
        Knock                   = 0x03,
        PrivateChatStart        = 0x04,
        PrivateChatEnd          = 0x05,
        ThreeFaceUpPoint        = 0x06,
        ThreeFaceCircle         = 0x07,
        ThreeFaceCancel         = 0x08,
        FriendAccept            = 0x09,
        FriendDeclined          = 0x0A,
        FriendDeclined2         = 0x0B,
        FriendDeclinedCustom    = 0x0C,
        GmKnock                 = 0x0D,
        GroupInvite             = 0x0E,
        GroupAccept             = 0x0F,
        GroupDecline            = 0x10,
        GroupMessage            = 0x11,
        GroupRemoved            = 0x12,
        GroupDisband            = 0x13,
        SystemMessage           = 0x1E,
        StatusNotification      = 0x1F      // Not a mailbox message. The client's message classifier drops these up front, they are the friend/group status notifies.
    }

    [StructLayout(LayoutKind.Explicit, Size = SIZE)]
    public unsafe struct MessageHeader
    {
        [FieldOffset(0x00)]
        [MarshalAs(UnmanagedType.U8)]
        public ulong SourcePolProId;
        [FieldOffset(0x08)]
        [MarshalAs(UnmanagedType.U8)]
        public ulong TargetPolProId;
        [FieldOffset(0x10)]
        public fixed byte SenderBuff[0x10];
        [FieldOffset(0x10)]
        public NotifyStatusData FriendStatus;
        [FieldOffset(0x18)]
        [MarshalAs(UnmanagedType.U1)]
        public byte SourceHandleNum;
        [FieldOffset(0x19)]
        [MarshalAs(UnmanagedType.U1)]
        public byte ControlFlag1;
        [FieldOffset(0x1A)]
        [MarshalAs(UnmanagedType.U1)]
        public byte ControlFlag2;
        [FieldOffset(0x1B)]
        [MarshalAs(UnmanagedType.U1)]
        public byte ControlFlag3;
        [FieldOffset(0x1C)]
        [MarshalAs(UnmanagedType.U4)]
        public uint FriendPosition;
        [FieldOffset(0x20)]
        public fixed byte SubjectBuff[0x10];
        [FieldOffset(0x30)]
        [MarshalAs(UnmanagedType.U4)]
        public uint SequenceNumber;
        [FieldOffset(0x34)]
        [MarshalAs(UnmanagedType.U4)]
        public uint Timestamp;
        [FieldOffset(0x38)]
        [MarshalAs(UnmanagedType.U4)]
        public uint PayloadStringSize;
        [FieldOffset(0x3C)]
        [MarshalAs(UnmanagedType.U1)]
        public byte SenderHandleNumber;
        [FieldOffset(0x3D)]
        [MarshalAs(UnmanagedType.U1)]
        public byte ReceiverHandleNumber;
        [FieldOffset(0x3E)]
        [MarshalAs(UnmanagedType.U2)]
        public ushort Bitfield1;
        [FieldOffset(0x40)]
        [MarshalAs(UnmanagedType.U2)]
        public ushort ContentId;
        [FieldOffset(0x42)]
        [MarshalAs(UnmanagedType.U2)]
        public ushort Bitfield2;

        const int SIZE = 0x48;

        public string Sender
        {
            get
            {
                fixed (byte* ptr = &SenderBuff[0])
                {
                    string str = Encoding.ASCII.GetString(new ReadOnlySpan<byte>(ptr, 0xF));
                    return str[..str.IndexOf('\0')];
                }
            }

            set
            {
                ReadOnlySpan<byte> sender = Encoding.ASCII.GetBytes(value);
                int len = sender.Length <= 0xF ? sender.Length : 0xF;
                fixed (byte* pName = &SenderBuff[0])
                {
                    sender.CopyTo(new Span<byte>(pName, len));
                }
            }
        }

        public string Subject
        {
            get
            {
                fixed (byte* ptr = &SubjectBuff[0])
                {
                    string str = Encoding.ASCII.GetString(new ReadOnlySpan<byte>(ptr, 0xF));
                    return str[..str.IndexOf('\0')];
                }
            }

            set
            {
                ReadOnlySpan<byte> subject = Encoding.ASCII.GetBytes(value);
                int len = subject.Length <= 0xF ? subject.Length : 0xF;
                fixed (byte* pSubject = &SubjectBuff[0])
                {
                    subject.CopyTo(new Span<byte>(pSubject, len));
                }
            }
        }


        public byte DataType
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (byte)(Bitfield1 & 0x7F);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Bitfield1 = (ushort)(Bitfield1 & 0xFF80 | (value & 0x7F));
            }
        }

        public MessageType Type
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (MessageType)((Bitfield1 >> 7) & 0x1F);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Bitfield1 = (ushort)(Bitfield1 & 0xF07F | (((byte)value & 0x1F) << 7));
            }
        }


        public byte Code
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (byte)((Bitfield1 >> 12) & 0x3);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Bitfield1 = (ushort)(Bitfield1 & 0xCFFF | ((value & 0x3) << 12));
            }
        }


        public bool IsOnline
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (Bitfield1 & 0x4000) != 0;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Bitfield1 = value ? (ushort)(Bitfield1 | 0x4000) : (ushort)(Bitfield1 & 0xBFFF);
            }
        }

        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (Bitfield1 & 0x8000) != 0;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Bitfield1 = value ? (ushort)(Bitfield1 | 0x8000) : (ushort)(Bitfield1 & 0x7FFF);
            }
        }

        public bool IsPolProRequest
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (Bitfield2 & 1) != 0;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Bitfield2 = value ? (ushort)(Bitfield2 | 0x1) : (ushort)(Bitfield2 & 0xFFFE);
            }
        }

        public byte Language
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (byte)((Bitfield2 >> 1) & 0x7F);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Bitfield2 = (ushort)(Bitfield2 & 0xFF01 | ((value & 0x7F) << 1));
            }
        }

        public bool NavigatorLock
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (Bitfield2 & 0x0100) != 0;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Bitfield2 = value ? (ushort)(Bitfield2 | 0x0100) : (ushort)(Bitfield2 & 0xFEFF);
            }
        }

        public bool DoNotReply
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (Bitfield2 & 0x200) != 0;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Bitfield2 = value ? (ushort)(Bitfield2 | 0x0200) : (ushort)(Bitfield2 & 0xFDFF);
            }
        }

        public byte SRStatus
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (byte)((Bitfield2 >> 10) & 0x3);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Bitfield2 = (ushort)(Bitfield2 & 0xCFFF | ((value & 0x3) << 10));
            }
        }

        public bool TitleSummaryOverFlow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (Bitfield2 & 0x4000) != 0;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Bitfield2 = value ? (ushort)(Bitfield2 | 0x4000) : (ushort)(Bitfield2 & 0xBFFF);
            }
        }

        public bool NonUnicastMessage
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (Bitfield2 & 0x8000) != 0;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Bitfield2 = value ? (ushort)(Bitfield2 | 0x8000) : (ushort)(Bitfield2 & 0x7FFF);
            }
        }

        public static MessageHeader FromBytes(byte[] msgHeader)
        {
            IntPtr ptr = Marshal.AllocHGlobal(SIZE);
            Marshal.Copy(msgHeader, 0, ptr, SIZE);
            MessageHeader dataStruct = (MessageHeader)Marshal.PtrToStructure(ptr, typeof(MessageHeader));
            Marshal.FreeHGlobal(ptr);
            return dataStruct;
        }

        public static MessageHeader FromB64(string b64)
        {
            byte[] read = SqCrypto.DecodeBase64(b64, 0x60);
            IntPtr ptr = Marshal.AllocHGlobal(SIZE);
            Marshal.Copy(read, 0, ptr, SIZE);
            MessageHeader dataStruct = (MessageHeader)Marshal.PtrToStructure(ptr, typeof(MessageHeader));
            Marshal.FreeHGlobal(ptr);
            return dataStruct;
        }

        public static string ToB64(MessageHeader header)
        {
            Span<byte> headerBytes = new byte[SIZE];
            MemoryMarshal.Write(headerBytes, ref header);
            return SqCrypto.EncodeBase64(headerBytes.ToArray(), 0x48, false);
        }

        public static byte[] ToBytes(MessageHeader header)
        {
            Span<byte> headerBytes = new byte[SIZE];
            MemoryMarshal.Write(headerBytes, ref header);
            return headerBytes.ToArray();
        }

        public static MessageHeader Copy(MessageHeader header)
        {
            return (MessageHeader) header.MemberwiseClone();
        }

        public override string ToString()
        {
            string source = SqCrypto.PolIdToPolProData(SourcePolProId);
            string target = SqCrypto.PolIdToPolProData(TargetPolProId);
            return $"Source: {source}, Target: {target}, Seq: {SequenceNumber}, Time: 0x{Timestamp:X}, ContentId: {ContentId}, Language: {Language}, Status: {FriendStatus.IsLoginAndReceiveMsg}, {FriendStatus.Status}, {FriendStatus.ActiveCharacterBits}, {FriendStatus.ContentClass}";
        }
    }
}
