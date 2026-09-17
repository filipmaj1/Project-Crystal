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
using NLog;
using System;
using System.Threading;
using Crystal.Common.MiniGame;
using System.Text;
using Crystal.Mahjong.Packets;
using Crystal.Common.MiniGame.Packets;

namespace Crystal.Mahjong
{
    class Program
   {
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            Mg.Init("MJS");
            ulong test2 = Mg.MjKey(0x5B01E336D27B5003);
            ulong test1 = Mg.MjKey(0x5B01E44C045A5792);
            SqCrypto.PolIdToPolProData(test2);
            SqCrypto.PolIdToPolProData(test1);

            ulong pid = SqCrypto.PolProDataToPolId(SqCrypto.UnScramblePolId("UQMJKWJ76"), 0, 0);
            Mg.MjKey(pid);

            string test = SqCrypto.PolIdToPolProData(0x00000113405D1B2C);
            string scramble = SqCrypto.ScramblePolId(test);

            string unscramble = SqCrypto.UnScramblePolId("U3MG59EDI");
            ulong unk = SqCrypto.PolProDataToPolId(unscramble, 0, 0);


            Mg.MjKey(MjConstants.POLID_BALANCER);

            Log.Info("=======================");
            Log.Info("Jangho (Mahjong) Server");
            Log.Info("=======================");
            

            MjServer server = new MjServer();
            server.StartServer();

            while (true)
            {
                string command = Console.ReadLine();
                if (command.StartsWith("send lobby "))
                {
                    server.SendLineToAll(string.Format("{0}{1}", "GMJSG", command.Substring(11)));
                }
                else if (command.StartsWith("send "))
                {
                    server.SendLineToAll(command.Substring(5));
                }
                else if (command.StartsWith("test"))
                {
                }
                else if (command.Equals("clear"))
                    Console.Clear();

                Thread.Sleep(200);
            }
        }
    }
}
