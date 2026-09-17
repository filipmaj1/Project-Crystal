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
using Crystal.FrontMissionOnline.FmoUtils;
using NLog;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using static Crystal.Common.FmoBlowfish;

namespace Crystal.FrontMissionOnline
{
    class Program
    {
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        unsafe static void Main(string[] args)
        {
            BlowfishContext blowfishCtx = new();


            ReadOnlySpan<byte> keyBuff = "101173flobby"u8;
            fixed (byte* keyPtr = &keyBuff[0])
                FrontMissionOnlineDll.fmoBlowfishInit(&blowfishCtx, keyPtr, 0xC);

            Span<byte> pktBuff = [
                0xc6, 0x8a, 0x7d, 0x89, 0xc8, 0xdb, 0x50, 0x7a, 0x60, 0x83, 0x0d, 0x40, 0xf2, 0x09, 0x08, 0xe8, 
                0x2e, 0x1d, 0x76, 0xfd, 0x68, 0x7c, 0x21, 0xbe, 0x75, 0x10, 0x32, 0xf9, 0x9b, 0x9a, 0x3f, 0xdf, 
                0x5d, 0x00, 0x69, 0x94, 0x09, 0xa3, 0xc0, 0x4f, 0x0b, 0x07, 0xf3, 0x3f, 0xe9, 0xe2, 0xc8, 0xd3, 
                0xba, 0x47, 0x65, 0xff, 0x45, 0xd4, 0x8f, 0xa8, 0x0b, 0x07, 0xf3, 0x3f, 0xe9, 0xe2, 0xc8, 0xd3, 
                0x7e, 0xee, 0x4e, 0xe9, 0x33, 0x64, 0xe3, 0xaf, 0xde, 0x2b, 0x65, 0x85, 0xa4, 0x14, 0xbe, 0x1e, 
                0x44, 0xbd, 0x16, 0xd6, 0x7e, 0x9f, 0x65, 0x08, 0x9d, 0xf5, 0x8e, 0x77
            ];

            fixed (byte* pkt = &pktBuff[0])
                FrontMissionOnlineDll.fmoBlowfishDecrypt(&blowfishCtx, pkt, 0x5C);

            Program.Log.Info("\n"+Utils.ByteArrayToHex(pktBuff.ToArray()));

            Log.Info("======================================");
            Log.Info("Front Mission Online Server");
            Log.Info("======================================");

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            ushort serverPort = 61300;
            if (args.Length <= 2)
                serverPort = ushort.Parse(args[0]);

            Server server = new Server("0.0.0.0", serverPort);
            server.StartServer();
            MapServer? mapServer = null;

            if (args.Length == 2 && args[1].Equals("M"))
            {
                mapServer = new("0.0.0.0", 0x1338);
                mapServer.StartServer();
            }


            while (true)
            {
                string? command = Console.ReadLine();
                string[] cArgs = command!.Split(" ");
                if (command.StartsWith("send") && cArgs.Length >= 3)
                {
                    if (ushort.TryParse(cArgs[2], NumberStyles.HexNumber, CultureInfo.CurrentCulture, out ushort cmdId) 
                        && uint.TryParse(cArgs[3], NumberStyles.HexNumber, CultureInfo.CurrentCulture, out uint sessionId))
                        server.DebugSendToAll(cmdId, sessionId, $"./{cArgs[1]}");
                }
                else if (command.StartsWith("test"))
                {
                    mapServer.SendNpc();
                }
                else if (command.Equals("clear"))
                    Console.Clear();

                Thread.Sleep(200);
            }
        }
    }
}
