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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crystal.FrontMissionOnline.FmoUtils
{
    public class SysMsgFileParser
    {
        public static bool Parse(string pathIn, string pathOut)
        {
            uint[] sectionData;

            using (BinaryWriter bWriter = new BinaryWriter(File.OpenWrite(pathOut)))
            using (BinaryReader bReader = new BinaryReader(File.OpenRead(pathIn)))
            {
                // Read in header
                if (bReader.ReadUInt32() != 0x545854)
                    return false;
                uint fileSize = bReader.ReadUInt32();
                uint language = bReader.ReadUInt32();
                uint headerSize = bReader.ReadUInt32();

                bReader.BaseStream.Seek(headerSize, SeekOrigin.Begin);
                uint numSections = bReader.ReadUInt32();
                sectionData = new uint[numSections];
                bReader.BaseStream.Seek(headerSize + 0x24, SeekOrigin.Begin);

                // Get Section Info
                for (int i = 0; i < numSections; i++)
                {
                    sectionData[i] = bReader.ReadUInt32();
                }

                // Go Through all the sections
                for (int i = 0; i < numSections; i++)
                {
                    uint numStrings = sectionData[i] >> 0x15;
                    uint sectionOffset = sectionData[i] & 0x1FFFFF;
                    uint[] stringOffsets = new uint[numStrings];

                    // Read in the string offsets
                    bReader.BaseStream.Seek(sectionOffset, SeekOrigin.Begin);
                    for (int j = 0; j < numStrings; j++)
                        stringOffsets[j] = bReader.ReadUInt32();

                    // Write out strings
                    for (int j = 0; j < numStrings; j++)
                    {
                        bWriter.Write(Encoding.ASCII.GetBytes($"0x{i:X} - 0x{j:X}: "));
                        bReader.BaseStream.Seek(stringOffsets[j], SeekOrigin.Begin);
                        byte b;
                        while ((b = bReader.ReadByte()) != 0)
                        {
                            bWriter.Write(b);
                        };
                        bWriter.Write(Encoding.ASCII.GetBytes("\r\n"));
                    }
                    bWriter.Write(Encoding.ASCII.GetBytes("---------------------------------------------------------------------------------\r\n"));
                }
            }

            return true;
        }
    }
}
