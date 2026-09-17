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
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace Crystal.POLAuth
{
    public class PolAuthConfig
    {
        public readonly string ServerIp;
        public readonly string ServerName;

        public readonly string PolProNotiferId;
        public readonly string PolProNotiferPassword;
        public readonly string PolProNotiferIp;

        public readonly string DbHost;
        public readonly string DbPort;
        public readonly string DbName;
        public readonly string DbUsername;
        public readonly string DbPassword;
        public readonly byte[] DbPasswordStorageKey;

        public PolAuthConfig(string path) 
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Program.Log.Info($"Loading config: {path}");
            XmlDocument doc = new();

            doc.Load(path);

            // Load the server configs
            XmlNode cfgNode = doc.DocumentElement.SelectSingleNode("/polauthcfg");
            ServerIp = cfgNode.Attributes["serverIp"]?.InnerText;
            ServerName = cfgNode.Attributes["serverName"]?.InnerText;

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
                    string passwordStorageKey = cfgChildNode.Attributes["passwordStorageKey"]?.InnerText;
                    
                    if (passwordStorageKey != null)
                        DbPasswordStorageKey = MD5.HashData(Encoding.ASCII.GetBytes(passwordStorageKey));
                }
                if (cfgChildNode.Name.Equals("polpro"))
                {
                    PolProNotiferId = cfgChildNode.Attributes["notifierPolId"]?.InnerText;
                    PolProNotiferPassword = cfgChildNode.Attributes["notifierPassword"]?.InnerText;
                    PolProNotiferIp = cfgChildNode.Attributes["notifierIp"]?.InnerText;
                }
            }
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }

}