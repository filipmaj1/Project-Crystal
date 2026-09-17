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

using NLog.Fluent;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;

namespace Crystal.POLPatch
{
    public class PolPatchConfig
    {
        public readonly Dictionary<string, Dictionary<string, Tuple<PatchVersion, List<PatchVersion>>>> PatchInfo = new();
        public readonly string RootDir;
        public readonly string ServerIp;

        public PolPatchConfig(string path) 
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Program.Log.Info($"Loading config: {path}");
            XmlDocument doc = new();
            doc.Load(path);

            // Load the server configs
            XmlNode cfgNode = doc.DocumentElement.SelectSingleNode("/polpatchcfg");
            RootDir = cfgNode.Attributes["rootDir"]?.InnerText;
            ServerIp = cfgNode.Attributes["serverIp"]?.InnerText;

            // Go through all applications
            foreach (XmlNode appNode in doc.DocumentElement.ChildNodes)
            {
                if (appNode.Name.Equals("application"))
                {
                    // Check the content id
                    if (appNode.Attributes["contentId"] == null)
                    {
                        Program.Log.Warn($"PolCfg: Application defined without appId!");
                        continue;
                    }
                    string applicationId = appNode.Attributes["contentId"].InnerText;
                    if (!int.TryParse(applicationId, out int _))
                    {
                        Program.Log.Warn($"PolCfg: {applicationId} is not a valid contentId.");
                        continue;
                    }

                    if (PatchInfo.ContainsKey(applicationId))
                    {
                        Program.Log.Warn($"PolCfg: Duplicate app with contentId {applicationId} found.");
                        continue;
                    }
                    PatchInfo[applicationId] = [];

                    // Load patches for each platform
                    foreach (XmlNode platformNode in appNode.ChildNodes)
                    {
                        if (platformNode.Name.Equals("platform"))
                        {
                            if (platformNode.Attributes["hardwareId"] != null)
                            {
                                string hardwareId = platformNode.Attributes["hardwareId"].InnerText;

                                if (PatchInfo[applicationId].ContainsKey(hardwareId))
                                {
                                    Program.Log.Warn($"PolCfg: Duplicate platform with contentId {applicationId} and hardwareId {hardwareId} found.");
                                    continue;
                                }

                                PatchVersion latestPatch = null;
                                List<PatchVersion> bypassPatches = [];

                                //Grab the latest/bypass patches
                                foreach (XmlNode patchNode in platformNode.ChildNodes)
                                {
                                    if (patchNode.Name.Equals("latestPatch"))
                                    {
                                        if (patchNode.InnerText.Length != 0 && latestPatch == null)
                                        {
                                            latestPatch = new(patchNode.InnerText.Trim(), ParseDate(patchNode.Attributes["date"]?.InnerText));
                                        }
                                    }
                                    else if (patchNode.Name.Equals("bypassPatch"))
                                    {
                                        string[] patches = patchNode.InnerText.Split(',');
                                        foreach (string patchName in patches)
                                            bypassPatches.Add(new(patchName.Trim(), 0));
                                    }
                                }

                                // Was there a latest patch?
                                if (latestPatch == null)
                                {
                                    continue;
                                }

                                // Add it
                                PatchInfo[applicationId][hardwareId] = new Tuple<PatchVersion, List<PatchVersion>>(latestPatch, bypassPatches);
                            }
                            else
                            {
                                Program.Log.Warn("PolCfg: Platform defined without hardwareId!");
                                continue;
                            }
                        }
                    }
                }
            }
            Console.ForegroundColor = ConsoleColor.Gray;
        }
        private static int ParseDate(string date)
        {
            if (date == null)
                return 0;

            // Hex
            if (date.StartsWith("0x"))
            {
                return Convert.ToInt32(date, 16);
            }
            // Dec
            else
            {
                if (int.TryParse(date, out int num))
                    return num;
                return 0;
            }
        }
    }

}

/*
 * 
        private Dictionary<string, Dictionary<string, PatchInfo>> patchInfo = new()
        {
            [PolConstants.HardwareID.WindowsJP] = new Dictionary<string, PatchInfo>()
            {
                [PolConstants.ApplicationID.PlayOnline] = new PatchInfo("20110829_E", 0x4e4e6597),
                [PolConstants.ApplicationID.POLFriendList] = new PatchInfo("20071010_1", 0x470f46ec),
                [PolConstants.ApplicationID.FinalFantasyXI] = new PatchInfo("30220705_0", 0),
                [PolConstants.ApplicationID.TetraMaster] = new PatchInfo("20020410_0", 0),
                [PolConstants.ApplicationID.FrontMissionOnline] = new PatchInfo("0000000000", 0)
            },

            [PolConstants.HardwareID.WindowsUS] = new Dictionary<string, PatchInfo>()
            {
                [PolConstants.ApplicationID.PlayOnline] = new PatchInfo("20110829_E", 0x4e4e6597),
                [PolConstants.ApplicationID.FinalFantasyXI] = new PatchInfo("30220705_0", 0),
                [PolConstants.ApplicationID.TetraMaster] = new PatchInfo("20030909_0", 0),
                [PolConstants.ApplicationID.FrontMissionOnline] = new PatchInfo("0000000000", 0)
            },


            //20150901_X: POL, 2015 DUMP
            //20020329_3: FFXI, from Entry DVD
            //20081212_1: FFXI dump
            //20020314_0: Tetra Master, from the POL DVD
            //20040908_0: Tetra Master, last version? From Dump.
            //20020314_0: Jangho, from the POL DVD.
            //20040727_2: Jangho, from a dump.
            //20060124_3: Dirge, deffo last verion dump
            //20051209_6: Dirge, from fresh install
            [PolConstants.HardwareID.PS2JP] = new Dictionary<string, PatchInfo>()
            {
                [PolConstants.ApplicationID.PlayOnline] = new PatchInfo("20150901_X", 0x56331762),
                [PolConstants.ApplicationID.FinalFantasyXI] = new PatchInfo("20020329_3", 0),
                [PolConstants.ApplicationID.TetraMaster] = new PatchInfo("20020314_0", 0),
                [PolConstants.ApplicationID.Janhourou] = new PatchInfo("20020314_0", 0),
                [PolConstants.ApplicationID.DirgeOfCerberus] = new PatchInfo("20051209_6", 0)
            },

            //20070911_0: Fresh Vana'diel 2008 Disc
            //20081212_1: KrHacken's Aght Urgan Dump for FFXI
            //20081127_3: Bacardi's dump
            //20031021_0: KrHacken's Aght Urgan Dump for Tetra Master
            [PolConstants.HardwareID.PS2US] = new Dictionary<string, PatchInfo>()
            {
                [PolConstants.ApplicationID.PlayOnline] = new PatchInfo("20081127_3", 0x56331762),
                [PolConstants.ApplicationID.FinalFantasyXI] = new PatchInfo("20160203_0", 0),
                [PolConstants.ApplicationID.TetraMaster] = new PatchInfo("20031021_0", 0),
                [PolConstants.ApplicationID.FrontMissionOnline] = new PatchInfo("ps2jp_050324_1531", 0)
            },

            [PolConstants.HardwareID.XbxJP] = new Dictionary<string, PatchInfo>()
            {
                [PolConstants.ApplicationID.PlayOnline] = new PatchInfo("20150901_X", 0x56331AC6),
            },

            [PolConstants.HardwareID.XbxUS] = new Dictionary<string, PatchInfo>()
            {
                [PolConstants.ApplicationID.PlayOnline] = new PatchInfo("20150901_X", 0x56331BB2),
            }
        };*/