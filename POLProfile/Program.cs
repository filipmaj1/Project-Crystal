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

using Crystal.POLProfile.PolDb;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Crystal.POLProfile
{
    class Program
    {
        private const int PROFILE_PORT = 51220;
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        async static Task Main(string[] args)
        {
            // Setup Base DIR
            string baseDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
            Directory.SetCurrentDirectory(baseDir);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Load NLog Config
            using var nlogStream = typeof(Program).Assembly.GetManifestResourceStream("Crystal.POLProfile.NLog.config");
            using var nlogReader = System.Xml.XmlReader.Create(nlogStream);
            LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(nlogReader, null);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Log.Info("===============================");
            Log.Info("Project Crystal: Profile Server");
            Log.Info("===============================");
            Console.ForegroundColor = ConsoleColor.Gray;

            // Config Load
            PolProConfig config = LoadConfig(args);
            if (config == null)
                return;

            // Setup Database
            Database.DB_HOST = config.DbHost;
            Database.DB_PORT = config.DbPort;
            Database.DB_NAME = config.DbName;
            Database.DB_USERNAME = config.DbUsername;
            Database.DB_PASSWORD = config.DbPassword;

            PolBesDb.DB_HOST = config.DbHost;
            PolBesDb.DB_PORT = config.DbPort;
            PolBesDb.DB_NAME = config.DbName;
            PolBesDb.DB_USERNAME = config.DbUsername;
            PolBesDb.DB_PASSWORD = config.DbPassword;

            // Startup!
            PolProServer server;
            try
            {
                server = new(
                    ip: config.ServerIp,
                    port: PROFILE_PORT,
                    profilePath: config.ProfileDir,
                    authIp: config.PolProNotiferAuthIp,
                    authPort: 51240,
                    polId: config.PolProNotiferId,
                    polPassword: config.PolProNotiferPassword
                    );
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return;
            }

            // Setup Service
            var builder = Host.CreateApplicationBuilder(args);
            builder.Logging.ClearProviders();
            builder.Services.AddSystemd();
            builder.Services.AddWindowsService();
            builder.Services.AddSingleton(server);
            builder.Services.AddHostedService<PolProServerWorker>();
            var host = builder.Build();
            await host.RunAsync();
        }

        static PolProConfig LoadConfig(string[] args)
        {
            // Get path if exists
            string cfgPath = "./polpro.cfg";
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].ToLower().Equals("--config") && args.Length > i + 1)
                {
                    cfgPath = args[i + 1];
                    break;
                }
            }

            // Load and perform checks for mandatory fields
            PolProConfig config;
            try
            {
                config = new(cfgPath);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Log.Error($"Could not load the config: {e.Message}");
                Console.ForegroundColor = ConsoleColor.Gray;
                return null;
            };

            if (config.PolProNotiferId == null ||
                config.PolProNotiferPassword == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Log.Warn("Config: Notifier credentials not set.");
                Console.ForegroundColor = ConsoleColor.Gray;
                return null;
            }

            if (config.DbHost == null ||
                config.DbPort == null ||
                config.DbName == null ||
                config.DbUsername == null ||
                config.DbPassword == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Log.Error("Config: Database was not set.");
                Console.ForegroundColor = ConsoleColor.Gray;
                return null;
            }

            return config;
        }
    }

    public class PolProServerWorker(PolProServer server) : BackgroundService
    {
        private readonly PolProServer Server = server;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();
            try
            {
                Server.StartServer();
                Server.StartNotifier();
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
                Server.StopServer();
            }
            catch (Exception ex)
            {
                Program.Log.Debug($"Error stopping server: {ex.Message}");
            }
            await base.StopAsync(cancellationToken);
        }
    }
}