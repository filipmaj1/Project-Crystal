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
using System.IO;

namespace Crystal.Common.PolFileSystem
{
    public static class PolFileSys
    {
        private static readonly Dictionary<string, PolFile> OpenPolFiles = new();
        private static readonly Dictionary<string, int> OpenReferenceCounts = new();
        private static readonly Dictionary<string, object> FileLocks = new();
        private static Dictionary<byte, string> DomainToPath = new Dictionary<byte, string>();
        
        public static void Init(string rootPath) {
            DomainToPath.Add(0x00, $"{rootPath}\\playonline");
            DomainToPath.Add(0x01, $"{rootPath}\\finalfantasyxi");
            DomainToPath.Add(0x02, $"{rootPath}\\tetramaster");
            DomainToPath.Add(0x03, $"{rootPath}\\jangho");
        }

        private static string GetPath(byte domain)
        {
            if (DomainToPath.ContainsKey(domain))
                return DomainToPath[domain];
            else
                return DomainToPath[0x0];
        }

        public static PolFile OpenFile(ulong polId, ushort volume, byte domain, string path, int maxEntries, int entrySize, int dataOffset = 0, int countOffset = -1)
        {
            string polIdStr = SqCrypto.PolIdToPolProData(polId);
            string domainPath = GetPath(domain);

            string filePath = $"{domainPath}\\{polIdStr}\\{path}";
            string key = $"{filePath}_{dataOffset:X}";

            // Check for reference
            if (OpenPolFiles.ContainsKey(key))
            {
                OpenReferenceCounts[key]++;
                return OpenPolFiles[key];
            }
            else
            {
                object fileLock = new();
                PolFile file = new PolFile(fileLock, filePath, maxEntries, entrySize, dataOffset, countOffset);
                OpenPolFiles[key] = file;
                OpenReferenceCounts[key] = 1;
                FileLocks[filePath] = fileLock;
                return file;
            }
        }

        public static PolFile OpenFileDirect(ulong polId, ushort volume, byte domain, string path, out object fileLock)
        {
            PolFile file = OpenFile(polId, volume, domain, path, -1, -1, -1, -1);
            fileLock = FileLocks[file.FilePath];
            return file;
        }

        public static void CloseFile(PolFile file)
        {
            string filePath = file.FilePath;
            string key = $"{filePath}_{file.DataOffset:X}";

            // Check for reference
            if (OpenPolFiles.ContainsKey(key))
            {
                OpenReferenceCounts[key]--;
                if (OpenReferenceCounts[key] == 0)
                {
                    OpenPolFiles.Remove(key);
                    OpenReferenceCounts.Remove(key);
                    FileLocks.Remove(filePath);
                }
            }
        }
    }
}
