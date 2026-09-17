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

using System.Runtime.InteropServices;

namespace Crystal.POLProfile.DataObjects.Pol
{
    [StructLayout(LayoutKind.Explicit, Size = 0x14)]
    public struct PolIpAddress
    {
        [FieldOffset(0x00)] public ushort IsValid;
        [FieldOffset(0x02)] public ushort Port;
        [FieldOffset(0x04)] public uint Ip;
        [FieldOffset(0x08)] public uint LanguageCode;
        [FieldOffset(0x0C)] public uint UnkMustBe1;
        [FieldOffset(0x10)] public uint Unk2;
    }
}
