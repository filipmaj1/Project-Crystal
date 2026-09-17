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
using System.Xml;

namespace Crystal.POLProfile
{
    public class PolProConfig
    {
        public readonly string ServerIp;
        public readonly string ProfileDir;

        public readonly string PolProNotiferId;
        public readonly string PolProNotiferPassword;
        public readonly string PolProNotiferAuthIp;

        public readonly string DbHost;
        public readonly string DbPort;
        public readonly string DbName;
        public readonly string DbUsername;
        public readonly string DbPassword;

        public PolProConfig(string path)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Program.Log.Info($"Loading config: {path}");
            XmlDocument doc = new();

            doc.Load(path);

            // Load the server configs
            XmlNode cfgNode = doc.DocumentElement.SelectSingleNode("/polprocfg");
            ServerIp = cfgNode.Attributes["serverIp"]?.InnerText;
            ProfileDir = cfgNode.Attributes["profileDir"]?.InnerText;

            // Go through subsettings
            foreach (XmlNode cfgChildNode in doc.DocumentElement.ChildNodes)
            {
                if (cfgChildNode.Name.Equals("database"))
                {
                    DbHost = cfgChildNode.Attributes["host"]?.InnerText;
                    DbPort = cfgChildNode.Attributes["port"]?.InnerText;
                    DbName = cfgChildNode.Attributes["database"]?.InnerText;
                    DbUsername = cfgChildNode.Attributes["username"]?.InnerText;
                    DbPassword = cfgChildNode.Attributes["password"]?.InnerText;
                }
                if (cfgChildNode.Name.Equals("notifier"))
                {
                    PolProNotiferAuthIp = cfgChildNode.Attributes["authIp"]?.InnerText;
                    PolProNotiferId = cfgChildNode.Attributes["polId"]?.InnerText;
                    PolProNotiferPassword = cfgChildNode.Attributes["password"]?.InnerText;
                }
            }
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }

}