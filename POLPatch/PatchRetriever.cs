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
using System.IO;
using System.Linq;
using Crystal.POLPatch.Packets.Send;
using System.Collections.Generic;

namespace Crystal.POLPatch
{
    internal class PatchRetriever(string rootDir, string serverIp, string appId, PatchPlatformContainer[] platformPatches)
    {
        private readonly string RootDir = rootDir;
        private readonly string ServerIP = serverIp;

        private readonly string ApplicationID = appId;

        private readonly PatchPlatformContainer[] PlatformPatches = platformPatches;

        private readonly Dictionary<string, DateTime> PS2POLDoUpdateHack = new();

        public DownloadResponse GetDownloadPacket(string hardwareId, string applicationId, string path, uint chunkCounter, uint chunkSize)
        {
            // Check if this is the app we are serving
            if (!applicationId.Equals(ApplicationID) || !PlatformPatches.Where((entry) => entry.PlatformId == hardwareId).Any())
                return null;

            // Load the patch if it exists
            byte[] fileData;
            try
            {
                fileData = File.ReadAllBytes(Path.Combine(RootDir, hardwareId, ApplicationID, path));
            }
            catch (Exception)
            {
                return null;
            }

            uint offset = chunkCounter * 0x010000;

            // Ran off EOF
            if (offset + chunkSize > fileData.Length || offset + chunkSize <= 0)
                return null;

            // Return chunk
            byte[] chunk = new byte[chunkSize];
            Array.Copy(fileData, offset, chunk, 0, chunkSize);

            return new DownloadResponse(path, chunk, chunk.Length);
        }

        public AskNewestResponse GetNewestPacket(string hardwareId, string applicationId, byte[] versionDataIn, string ip)
        {
            // Check if this is the app we are serving
            if (!applicationId.Equals(ApplicationID) || !PlatformPatches.Where((entry) => entry.PlatformId == hardwareId).Any())
                return null;

            PatchPlatformContainer patches = PlatformPatches.Where((entry) => entry.PlatformId == hardwareId).First();

            // Check version and write the appropriate response
            string serverVersion = patches.LatestVersion.Name;
            string clientVersion = Encoding.ASCII.GetString(versionDataIn).Trim(['\0']);
            byte[] versionDataOut = BuildVersionData(clientVersion.Length == 0, ServerIP);

            // Bypass if it exists
            if (patches.BypassVersions.Where((val) => val.Name.Equals(clientVersion)).Any())
                serverVersion = clientVersion;

            AskNewestResponse response = new(patches.LatestVersion.Date, versionDataOut, serverVersion);
            return response;
        }

        public StatusResponse GetStatusResponsePacket(string hardwareId, string applicationId, string ip)
        {
            // Check if this is the app we are serving
            if (!applicationId.Equals(ApplicationID) || !PlatformPatches.Where((entry) => entry.PlatformId == hardwareId).Any())
                return null;

            try
            {
                PatchPlatformContainer patches = PlatformPatches.Where((entry) => entry.PlatformId == hardwareId).First();
                byte[] statusFile;
                statusFile = File.ReadAllBytes(Path.Combine(RootDir, hardwareId, ApplicationID, "status"));
                return new StatusResponse(statusFile);
            }
            catch (Exception) { return null; }
        }

        private static byte[] BuildVersionData(bool isEmpty, string patchServerIP)
        {
            byte[] versionData = new byte[0x40];
            using (MemoryStream mem = new(versionData))
            using (BinaryWriter binWriter = new(mem))
            {
                binWriter.Write(Encoding.ASCII.GetBytes(isEmpty ? "empty\0" : "registered\0"));
                binWriter.Write(Encoding.ASCII.GetBytes(patchServerIP + "\0" + "0"));
            }
            return versionData;
        }

    }
}

