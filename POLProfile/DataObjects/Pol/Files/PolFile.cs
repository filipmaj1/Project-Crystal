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
using System.Text;

namespace Crystal.POLProfile.DataObjects.Pol.Files
{
    unsafe struct PolFile
    {
        public uint LastUpdated;
        public uint Unknown;
        private fixed byte PathBuff[0x100];

        public const int SIZE = 0x108;

        public string FilePath
        {
            get
            {
                fixed (byte* ptr = &PathBuff[0])
                {
                    string str = Encoding.ASCII.GetString(new ReadOnlySpan<byte>(ptr, 0x100));
                    return str[..str.IndexOf('\0')];
                }
            }

            set
            {
                ReadOnlySpan<byte> name = Encoding.ASCII.GetBytes(value);
                int len = name.Length <= 0x100 ? name.Length : 0x100;
                fixed (byte* pName = &PathBuff[0])
                {
                    name.CopyTo(new Span<byte>(pName, len));
                }
            }
        }
    }
}
