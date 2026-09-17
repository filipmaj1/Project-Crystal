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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using static Crystal.Common.FmoBlowfish;

namespace Crystal.FrontMissionOnline
{
    [SuppressUnmanagedCodeSecurity]
    public static unsafe class FrontMissionOnlineDll
    {
        public static readonly delegate* unmanaged[Cdecl]<byte*, void> fmoCryptInit;
        public static readonly delegate* unmanaged[Cdecl]<byte*, byte*, uint, uint, void> fmoCryptSetKey;
        public static readonly delegate* unmanaged[Cdecl]<byte*, byte*, uint, void> fmoCryptTransform;


        public static readonly delegate* unmanaged[Cdecl]<BlowfishContext*, byte*, int, BlowfishContext*> fmoBlowfishInit;
        public static readonly delegate* unmanaged[Cdecl]<BlowfishContext*, byte*, int, void> fmoBlowfishEncrypt;
        public static readonly delegate* unmanaged[Cdecl]<BlowfishContext*, byte*, int, void> fmoBlowfishDecrypt;

        static FrontMissionOnlineDll()
        {
            nint handle = NativeLibrary.Load("FrontMissionOnline.dll");

            fmoCryptInit = (delegate* unmanaged[Cdecl]<byte*, void>)(handle + 0x1A3BA0);
            fmoCryptSetKey = (delegate* unmanaged[Cdecl]<byte*, byte*, uint, uint, void>)(handle + 0x1A3BB0);
            fmoCryptTransform = (delegate* unmanaged[Cdecl]<byte*, byte*, uint, void>)(handle + 0x1A3C60);

            fmoBlowfishInit    = (delegate* unmanaged[Cdecl]<BlowfishContext*, byte*, int, BlowfishContext*>)(handle + 0x6F4E0);
            fmoBlowfishEncrypt = (delegate* unmanaged[Cdecl]<BlowfishContext*, byte*, int, void>)(handle + 0x6F8B0);
            fmoBlowfishDecrypt = (delegate* unmanaged[Cdecl]<BlowfishContext*, byte*, int, void>)(handle + 0x6FDD0);
        }
    }
}
