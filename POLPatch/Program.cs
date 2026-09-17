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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Crystal.POLPatch
{
    class Program
    {
        public const int PORT_START = 53000;
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static PatchServer[] ServerList;

        static async Task Main(string[] args)
        {
            // Setup Base DIR
            string baseDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
            Directory.SetCurrentDirectory(baseDir);

            // Load NLog Config
            using var nlogStream = typeof(Program).Assembly.GetManifestResourceStream("Crystal.POLPatch.NLog.config");
            using var nlogReader = System.Xml.XmlReader.Create(nlogStream);
            LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(nlogReader, null);

            //Get Args
            List<string> contentIDFilter = [];
            string cfgFile = "./polpatch.cfg";
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-cid" || args[i] == "-contentid")
                {
                    if (args.Length >= i + 1)
                    {
                        try
                        {
                            string[] idStrs = args[i + 1].Split(',');
                            foreach (string idStr in idStrs)
                            {
                                if (int.TryParse(idStr, out int contentId))
                                    contentIDFilter.Add(idStr);
                                else
                                {
                                    Console.WriteLine("Invalid contentId arg provided.");
                                    return;
                                }
                            }
                            i++;
                        }
                        catch (FormatException)
                        {
                            Console.WriteLine("Invalid contentId arg provided.");
                        }
                    }
                    else
                        Console.WriteLine("Invalid contentId arg provided.");
                }
                else if (args[i] == "-cfg" || args[i] == "-config")
                {
                    if (args.Length >= i + 1)
                    {
                        try
                        {
                            cfgFile = args[i + 1];
                            i++;
                        }
                        catch (FormatException)
                        {
                            Console.WriteLine("Invalid config arg provided.");
                        }
                    }
                    else
                        Console.WriteLine("Invalid config arg provided.");
                }
            }

            // Arguments good, start the server

            Console.ForegroundColor = ConsoleColor.Cyan;
            Log.Info("=============================");
            Log.Info("Project Crystal: Patch Server");
            Log.Info("=============================");

            PolPatchConfig config;
            try
            {
                config = new(cfgFile);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Log.Error($"Could not load the config: {e.Message}");
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Log.Info($"Patch file folder is: \"{config.RootDir}\"");
            Log.Info($"Server IP is: \"{config.ServerIp}\"");
            Console.ForegroundColor = ConsoleColor.Gray;

            // Figure out what we are serving
            int numServers = config.PatchInfo.Keys.Count;
            ServerList = new PatchServer[contentIDFilter.Count == 0 ? numServers : Math.Min(contentIDFilter.Count, numServers)];
            int j = 0;
            foreach (string applicationID in config.PatchInfo.Keys)
            {
                if (contentIDFilter.Count > 0 && !contentIDFilter.Contains(applicationID))
                    continue;

                int indx = 0;
                int numPatchContainers = config.PatchInfo[applicationID].Count;
                PatchPlatformContainer[] patches = new PatchPlatformContainer[numPatchContainers];

                // Load in patches
                foreach (var hardware in config.PatchInfo[applicationID])
                {
                    PatchVersion latest = hardware.Value.Item1;
                    List<PatchVersion> bypassed = hardware.Value.Item2;
                    patches[indx++] = new(hardware.Key, latest, bypassed.ToArray());
                }

                // Instantiate the server
                ServerList[j++] = new(
                basePort: PORT_START,
                rootDir: config.RootDir,
                serverIp: config.ServerIp,
                applicationId: applicationID,
                patches: patches
                );
            }

            // Setup Service
            var builder = Host.CreateApplicationBuilder(args);
            builder.Logging.ClearProviders();
            builder.Services.AddSystemd();
            builder.Services.AddWindowsService();
            builder.Services.AddSingleton(ServerList);
            builder.Services.AddHostedService<PatchServerWorker>();
            var host = builder.Build();
            await host.RunAsync();
        }
    }

    public class PatchServerWorker(PatchServer[] servers) : BackgroundService
    {
        private readonly PatchServer[] ServerList = servers;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();
            try
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Program.Log.Info("Starting servers...");
                foreach (PatchServer server in ServerList)
                {
                    try
                    {
                        bool didStart = server.Start();
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Program.Log.Info($"Patch Server for contentId {server.ApplicationId} has started @ {server.ServerIP}:{server.ServerPort}");
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                    catch (ApplicationException e)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Program.Log.Error($"Patch Server for contentId {server.ApplicationId} failed: {e.Message}");
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                }
            }
            catch (ApplicationException e)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Program.Log.Error($"Failed to start server: {e.Message}");
                Console.ForegroundColor = ConsoleColor.Gray;
                throw;
            }
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                Program.Log.Info("Stopping servers...");
                foreach (PatchServer server in ServerList)
                {
                    try
                    {
                        server.Stop();
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Program.Log.Info($"Patch Server for contentId {server.ApplicationId} has stopped.");
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                    catch (ApplicationException e)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Program.Log.Error($"Patch Server for contentId {server.ApplicationId} had error stopping server.");
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Log.Debug($"Error stopping server: {ex.Message}");
            }
            await base.StopAsync(cancellationToken);
        }
    }
}
