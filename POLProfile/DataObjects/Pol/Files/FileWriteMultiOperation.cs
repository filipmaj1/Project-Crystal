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
using System.Runtime.InteropServices;
using System.Text;

namespace Crystal.POLProfile.DataObjects.Pol.Files
{
    [StructLayout(LayoutKind.Explicit, Size = SIZE)]
    unsafe struct FileWriteMultiOperation
    {
        public const int SIZE = 0x398;
        public const int MAX_TARGETS = 20;
        private const int HANDLE_NAME_SIZE = 0x10;

        [FieldOffset(0x000)] public byte NumTargets;
        [FieldOffset(0x008)] private fixed byte TargetHandleNums[MAX_TARGETS];
        [FieldOffset(0x01C)] private fixed byte TargetFlags[MAX_TARGETS];
        [FieldOffset(0x030)] private fixed byte TargetHandleNames[MAX_TARGETS * 0x10];
        [FieldOffset(0x170)] private fixed ulong TargetPolIds[MAX_TARGETS];
        [FieldOffset(0x210)] private fixed byte PathBuff[0x180];
        [FieldOffset(0x390)] public uint Offset;
        [FieldOffset(0x394)] public uint Length;

        public byte GetTargetHandleNum(int index)
        {
            fixed (byte* ptr = TargetHandleNums)
                return ptr[index];
        }

        public byte GetTargetFlag(int index)
        {
            fixed (byte* ptr = TargetFlags)
                return ptr[index];
        }

        public ulong GetTargetPolId(int index)
        {
            fixed (ulong* ptr = TargetPolIds)
                return ptr[index];
        }

        public string GetTargetHandleName(int index)
        {
            fixed (byte* ptr = TargetHandleNames)
                return ReadCString(ptr + index * HANDLE_NAME_SIZE, HANDLE_NAME_SIZE);
        }

        public string FilePath
        {
            get
            {
                fixed (byte* ptr = PathBuff)
                    return ReadCString(ptr, 0x180);
            }

            set
            {
                ReadOnlySpan<byte> name = Encoding.ASCII.GetBytes(value);
                int len = Math.Min(name.Length, 0x17F); // keep at least one null terminator
                fixed (byte* pName = PathBuff)
                {
                    name[..len].CopyTo(new Span<byte>(pName, len));
                    new Span<byte>(pName + len, 0x180 - len).Clear();
                }
            }
        }

        private static string ReadCString(byte* ptr, int maxLen)
        {
            string str = Encoding.ASCII.GetString(new ReadOnlySpan<byte>(ptr, maxLen));
            int nul = str.IndexOf('\0');
            return nul >= 0 ? str[..nul] : str;
        }
    }
}