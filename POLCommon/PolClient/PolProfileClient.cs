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
using System.IO;
using System.Net.Sockets;
using System.Net;
using Crystal.Common.PolClient.PolProfile;
using System.Runtime.InteropServices;

namespace Crystal.Common.PolClient
{
    public class PolProfileClient
    {
        public enum PolProState
        {
            Idle,
            Opening,
            Sending,
            Receiving,
            Closing,
        }

        public enum PolProResult
        {
            Success,
            NetError,
            HandshakeError,
            BadResponse,
            BadChecksum,
            Unknown,
        }

        // Client Info
        private readonly string Host;
        private readonly int Port;
        private readonly uint MyIp;
        private readonly uint MyPort;

        // Connection
        private Socket Socket;
        private SqCrypto Crypto;
        private PolProState CurrentState = PolProState.Idle;

        public PolProState GetCurrentState()
        {
            return CurrentState;
        }

        public PolProfileClient(string host, int port, uint myAuthIp, uint myAuthPort, byte[] blowfishKey, uint rKey1, uint rKey2)
        {
            Host = host;
            Port = port;
            MyIp = myAuthIp;
            MyPort = myAuthPort;
            Crypto = new SqCrypto(blowfishKey, rKey1, rKey2);
        }

        public int ReadFile(ulong polProId, string filename, byte[] readBuffer, int srcOffset, int dstOffset, uint length)
        {
            FileOperation fileOp = new()
            {
                HandleNum1 = 0,
                HandleNum2 = 0,
                PolProId = polProId,
                FilePath = filename,
                Offset = (uint) srcOffset,
                Length = length,
            };

            byte[] sendObj = new byte[FileOperation.SIZE_READ];
            MemoryMarshal.Write(sendObj.AsSpan(), fileOp);

            if (OpenPolProfile() != PolProResult.Success) 
                return -1;
            if (SendRequest(0x300, sendObj) != PolProResult.Success) 
                return -1;
            if (RecvAnswer(out byte errorCode, out byte[] recvObj) != PolProResult.Success)
            {
                ClosePolProfile();
                return -1;
            }
            ClosePolProfile();

            if (errorCode == 0 && recvObj != null)
                recvObj.AsSpan().CopyTo(readBuffer.AsSpan(dstOffset, recvObj.Length));

            return errorCode;
        }

        public int WriteFile(ulong polProId, string filename, byte[] buffer, int srcOffset, uint dstOffset, int length)
        {
            FileOperation fileOp = new()
            {
                HandleNum1 = 0,
                HandleNum2 = 0,
                PolProId = polProId,
                FilePath = filename,
                Offset = dstOffset,
                Length = (uint) length,
            };

            byte[] sendObj = new byte[FileOperation.SIZE_WRITE + length];
            MemoryMarshal.Write(sendObj.AsSpan(), fileOp);
            buffer.AsSpan().Slice(srcOffset, (int)length).CopyTo(sendObj.AsSpan()[FileOperation.SIZE_WRITE..]);

            if (OpenPolProfile() != PolProResult.Success)
                return -1;
            if (SendRequest(0x301, sendObj) != PolProResult.Success)
                return -1;
            if (RecvAnswer(out byte errorCode) != PolProResult.Success)
            {
                ClosePolProfile();
                return -1;
            }
            ClosePolProfile();

            return errorCode;
        }

        public int DeleteFile(ulong polProId, string filename)
        {
            FileOperation fileOp = new()
            {
                HandleNum1 = 0,
                HandleNum2 = 0,
                PolProId = polProId,
                FilePath = filename,
            };

            byte[] sendObj = new byte[FileOperation.SIZE_DELETE];
            MemoryMarshal.Write(sendObj.AsSpan(), fileOp);

            if (OpenPolProfile() != PolProResult.Success)
                return -1;
            if (SendRequest(0x302, sendObj) != PolProResult.Success)
                return -1;
            if (RecvAnswer(out byte errorCode) != PolProResult.Success)
            {
                ClosePolProfile();
                return -1;
            }
            ClosePolProfile();

            return errorCode;
        }

        private PolProResult OpenPolProfile()
        {
            CurrentState = PolProState.Opening;

            try
            {
                Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Socket.Connect(IPAddress.Parse(Host), Port);
            }
            catch (SocketException)
            {
                ClosePolProfile();
                return PolProResult.NetError;
            }

            // Send handshake packet
            byte[] handshake = new byte[0x28];
            using (var ms = new MemoryStream(handshake))
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write((uint)0x00);           // Type: Handshake
                bw.Write((ushort)0x01);         // Unknown
                bw.Write((ushort)MyPort);       // Port
                bw.Write(MyIp);                 // IP Address
            }
            try
            {
                Socket.Send(handshake);
            }
            catch (SocketException)
            {
                ClosePolProfile();
                return PolProResult.NetError;
            }

            // Receive handshake answer (0x18 bytes, unencrypted)
            byte[] answer = new byte[0x18];
            int received = 0;
            while (received < 0x18)
            {
                try
                {
                    int n = Socket.Receive(answer, received, 0x18 - received, SocketFlags.None);
                    if (n == 0)
                    {
                        ClosePolProfile();
                        return PolProResult.NetError;
                    }
                    received += n;
                }
                catch (SocketException)
                {
                    ClosePolProfile();
                    return PolProResult.NetError;
                }
            }

            // Handshake rejected
            if (answer[0] != 0x81)
            {
                ClosePolProfile();
                return PolProResult.HandshakeError;
            }

            return PolProResult.Success;
        }

        private PolProResult SendRequest(ushort opcode, byte[] obj)
        {
            CurrentState = PolProState.Sending;

            byte[] header = new byte[0x28];
            using (var ms = new MemoryStream(header))
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write((byte)0x02);                       // Type: Request
                bw.Write((byte)(opcode >> 8));              // Opcode high byte
                bw.Write((ushort)(opcode & 0xFF));          // Opcode low 2 bytes
                bw.Write((uint)obj.Length);                 // Object size
                bw.BaseStream.Seek(0x18, SeekOrigin.Begin);
            }

            try
            {
                // Encrypt header with flag=1 (resets cipher state)
                Crypto.SqCrypt64(header, header.Length, 1);
                Socket.Send(header);

                // Encrypt object with flag=0 (continues cipher state)
                Crypto.SqCrypt64(obj, obj.Length, 0);
                Socket.Send(obj);
            }
            catch (SocketException)
            {
                ClosePolProfile();
                return PolProResult.NetError;
            }

            return PolProResult.Success;
        }

        private PolProResult RecvAnswer(out byte errorCode)
        {
            PolProResult result = RecvAnswer(out byte error, out byte[] obj);
            errorCode = error;
            return result;
        }

        private PolProResult RecvAnswer(out byte errorCode, out byte[] obj)
        {
            errorCode = 0;
            obj = null;
            CurrentState = PolProState.Receiving;

            // Receive answer (0x18 bytes, encrypted)
            byte[] answer = new byte[0x18];
            int received = 0;
            while (received < 0x18)
            {
                try
                {
                    int n = Socket.Receive(answer, received, 0x18 - received, SocketFlags.None);
                    if (n == 0)
                    {
                        ClosePolProfile();
                        return PolProResult.NetError;
                    }
                    received += n;
                }
                catch (SocketException)
                {
                    ClosePolProfile();
                    return PolProResult.NetError;
                }
            }
            Crypto.SqCrypt64(answer, answer.Length, 1);

            // Parse answer
            using var ms = new MemoryStream(answer);
            using var br = new BinaryReader(ms);
            
            byte type = br.ReadByte();      // Has to be 0x83
            errorCode = br.ReadByte();      // Error Code
            br.ReadUInt16();                // Unknown
            uint objLen  = br.ReadUInt32(); // Object Length
            
            // Has to be a 0x83 type and no error
            if (type != 0x83)
            {
                ClosePolProfile();
                return PolProResult.BadResponse;
            }

            // If obj data is after this, receive it
            if (objLen != 0)
            {
                received = 0;
                byte[] recvObj = new byte[objLen];
                while (received < recvObj.Length)
                {
                    try
                    {
                        int n = Socket.Receive(recvObj, received, recvObj.Length - received, SocketFlags.None);
                        if (n == 0)
                        {
                            ClosePolProfile();
                            return PolProResult.NetError;
                        }
                        received += n;
                    }
                    catch (SocketException)
                    {
                        ClosePolProfile();
                        return PolProResult.NetError;
                    }
                }
                Crypto.SqCrypt64(recvObj, recvObj.Length, 0);
                obj = recvObj[0..(recvObj.Length - 4)];

                // Verify checksum
                uint receivedChecksum = BitConverter.ToUInt32(recvObj[(recvObj.Length - 4)..]);
                uint testChecksum = SqCrypto.CalcCheckSum(obj);
                if (receivedChecksum != testChecksum)
                {
                    obj = null;
                    return PolProResult.BadChecksum;
                }                 
            }

            // Done!
            return PolProResult.Success;
        }

        private void ClosePolProfile()
        {
            if (Socket == null)
                return;

            CurrentState = PolProState.Closing;
            try
            {
                Socket?.Shutdown(SocketShutdown.Both);
                Socket?.Close();
            }
            catch { }
            Socket = null;
            CurrentState = PolProState.Idle;
        }
    }
}
