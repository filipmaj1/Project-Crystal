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

namespace Crystal.POLProfile.Packets
{
    class ObjectBuilders
    {
        public static byte[] CreateGetFilesObject(FileInfo[] files)
        {
            byte[] result = new byte[0x8 + (0x108 * files.Length)];

            using (MemoryStream memStream = new(result))
            using (BinaryWriter binWriter = new(memStream))
            {
                if (files.Length != 0)
                {
                    binWriter.Write((UInt16)files.Length);                    

                    for (int i = 0; i < files.Length; i++)
                    {
                        binWriter.BaseStream.Seek(0x8 + i * 0x108, SeekOrigin.Begin);
                        binWriter.Write((UInt32)Utils.UnixTimeStampUTC(files[i].CreationTimeUtc));
                        binWriter.Write((UInt32)0x23);
                        byte[] str = Encoding.ASCII.GetBytes(files[i].Name);
                        for (int j = 0; j < str.Length; j++)
                        {
                            binWriter.Write(str[j]);
                            if (str[j] == 0)                            
                                break;                           
                        }
                    }
                }
            }

            return result;
        }
    }    
}
