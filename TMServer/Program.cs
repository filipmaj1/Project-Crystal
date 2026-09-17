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

using Crystal.Common.MiniGame;
using Crystal.Common.PolFileSystem;
using Crystal.POLAuth;
using NLog;
using System;
using System.Threading;

namespace Crystal.TetraMaster
{
    class Program
    {
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static AuthServer MgServer;
        private static TmBalancerServer BalancerServer;
        private static TmShopMenuServer ShopMenuServer;

        static void Main(string[] args)
        {
            Log.Info("===================");
            Log.Info("Tetra Master Server");
            Log.Info("===================");

            Mg.Init("TM0");
            PolFileSys.Init("D:\\Coding\\project-crystal-server\\Build\\POLProfile\\Debug\\profile_data");

            // Args
            ProcessArgs(args, out bool startMgIrc, out bool startBalancer, out bool startShopMenuServer);

            startMgIrc = startBalancer = startShopMenuServer = true;

            // Start the servers
            if (startMgIrc)
            {
                Log.Info("Starting POL Auth server for MiniGame...");
                MgServer = new("69.254.56.86", MgConstants.MG_IRC_PORT, "pol-1048-51241.pol.com", "", "", "");
                MgServer.StartServer(false);
            }

            if (startBalancer)
            {
                BalancerServer = new TmBalancerServer();
                BalancerServer.StartServer();
            }

            if (startShopMenuServer)
            {
                ShopMenuServer = new TmShopMenuServer(Mg.TmKey(TmConstants.SHOPMENU_POLID), TmConstants.SHOPMENU_DOMAIN, TmConstants.SHOPMENU_VOLUME);
                ShopMenuServer.StartServer();
            }

            // Command line loop
            while (true)
            {
                string command = Console.ReadLine();
                if (command.StartsWith("test"))
                {
                    ShopMenuServer?.Test();
                }
                else if (command.Equals("clear"))
                    Console.Clear();

                Thread.Sleep(500);
            }
        }

        private static void ProcessArgs(string[] args, out bool startMgIrc, out bool startBalancer, out bool startShopMenuServer)
        {
            // Default
            if (args.Length == 0)
                startMgIrc = startBalancer = startShopMenuServer = true;
            else
                startMgIrc = startBalancer = startShopMenuServer = false;

            // Find args
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--mgirc":
                        startMgIrc = true;
                        break;
                    case "--balancer":
                        startBalancer = true;
                        break;
                    case "--shopmenu":
                        startShopMenuServer = true;
                        break;
                }
            }
        }
    }
}
