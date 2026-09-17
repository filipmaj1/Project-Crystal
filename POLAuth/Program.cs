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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Crystal.POLAuth
{
    class Program
    {
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        static async Task Main(string[] args)
        {
            // Setup Base DIR
            string baseDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
            Directory.SetCurrentDirectory(baseDir);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Load NLog Config
            using var nlogStream = typeof(Program).Assembly.GetManifestResourceStream("Crystal.POLAuth.NLog.config");
            using var nlogReader = System.Xml.XmlReader.Create(nlogStream);
            LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(nlogReader, null);

            // Sending command to polauth in service mode
            if (args.Length >= 2 && args[0].Equals("--control"))
            {
                string[] cmdArgs = args[1..];
                SendCommandToBackgroundService(cmdArgs);
                return;
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Log.Info("======================================");
            Log.Info("Project Crystal: Authentication Server");
            Log.Info("======================================");
            Console.ForegroundColor = ConsoleColor.Gray;

            // Config path arg + load config
            string cfgPath = "./polauth.cfg";
            if (args.Length >= 2 && args[0].Equals("--cfg"))
                cfgPath = args[1];
            PolAuthConfig config;
            try
            {
                config = new(cfgPath);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Log.Error($"Could not load the config: {e.Message}");
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            };

            if (config.PolProNotiferId == null ||
                config.PolProNotiferPassword == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Log.Error("Polconfig: PolPro Notifier credentials not set.");
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            }

            if (config.DbHost == null ||
                config.DbPort == null ||
                config.DbName == null ||
                config.DbUsername == null ||
                config.DbPassword == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Log.Error("Polconfig: Database was not set.");
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            }

            // Setup Database
            Database.DB_HOST = config.DbHost;
            Database.DB_PORT = config.DbPort;
            Database.DB_NAME = config.DbName;
            Database.DB_USERNAME = config.DbUsername;
            Database.DB_PASSWORD = config.DbPassword;
            Database.DB_PASSWORD_KEY = config.DbPasswordStorageKey;

            if (Database.DB_PASSWORD_KEY == null)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.DarkRed;
                Log.Warn("!!!NO PASSWORD STORAGE KEY SET!!!");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.Gray;
            }

            // Setup Server
            AuthServer server = new(
                ip: config.ServerIp ?? "0.0.0.0",
                name: config.ServerName ?? "pol-0000-00000.pol.com",
                polProId: config.PolProNotiferId,
                polProPassword: config.PolProNotiferPassword,
                polProIpAddress: config.PolProNotiferIp
                );
            PolAuthAdminControl.SetProfileServer(config.PolProNotiferIp, 51220);

            // Setup Service
            var builder = Host.CreateApplicationBuilder(args);
            builder.Logging.ClearProviders();
            builder.Services.AddSystemd();
            builder.Services.AddWindowsService();
            builder.Services.AddSingleton(server);
            builder.Services.AddHostedService<AuthServerWorker>();
            var host = builder.Build();
            await host.RunAsync();
        }

        private static void SendCommandToBackgroundService(string[] cmdArgs)
        {
            try
            {
                // "." means local machine. This maps perfectly on both Windows and Linux via .NET abstraction.
                using var pipeClient = new NamedPipeClientStream(".", AuthServerWorker.COMM_PIPE, PipeDirection.InOut);
                pipeClient.Connect(2000); // 2-second connection timeout window

                using var writer = new StreamWriter(pipeClient, leaveOpen: true) { AutoFlush = true };
                using var reader = new StreamReader(pipeClient, leaveOpen: true);
                writer.WriteLine(string.Join(" ", cmdArgs));
                string response = reader.ReadToEnd();

                if (!string.IsNullOrEmpty(response))
                    Console.WriteLine(response);
                else
                    Console.WriteLine("No messsage back from auth server.");
            }
            catch (TimeoutException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Log.Error("Could not send command. Is the service running?");
            }
            catch (IOException)
            {
            }
            finally
            {
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
    }

    public class AuthServerWorker : BackgroundService
    {
        public const string COMM_PIPE = "polauth.comm";
        private readonly AuthServer Server;
        private readonly IHostApplicationLifetime Lifetime;

        public AuthServerWorker(AuthServer server, IHostApplicationLifetime lifetime)
        {
            Server = server;
            Lifetime = lifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();
            // Start server
            try
            {
                Server.StartServer(isMainPolAuth: true);
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Program.Log.Info($"Authentication Server has started @ {Server.ServerIp}:{Server.ServerPort}");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            catch (ApplicationException e)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Program.Log.Error($"Failed to start server: {e.Message}");
                Console.ForegroundColor = ConsoleColor.Gray;
                throw;
            }

            // Fork runtime listening path dynamically depending on user attachment
            try
            {
                // Figuring out this is a service barely works on linux.
                bool isServiceMode = ((OperatingSystem.IsLinux() && Environment.GetEnvironmentVariable("INVOCATION_ID") != null)) || !Environment.UserInteractive;

                if (isServiceMode)
                {
                    Program.Log.Info("Starting in service mode...");
                    await RunPipeCommandListenerAsync(stoppingToken);
                }
                else
                {
                    Program.Log.Info("Starting in terminal mode...");
                    _ = Task.Run(() => RunPipeCommandListenerAsync(stoppingToken), stoppingToken);
                    await RunConsoleInputLoopAsync(stoppingToken);
                }
            }
            finally
            {
                try
                {
                    Server.StopServer();
                }
                catch (Exception ex)
                {
                }
            }
        }

        private async Task RunConsoleInputLoopAsync(CancellationToken stoppingToken)
        {
            StringBuilder inputBuilder = new();
            while (!stoppingToken.IsCancellationRequested)
            {
                if (!Console.KeyAvailable)
                {
                    await Task.Delay(50, stoppingToken);
                    continue;
                }

                ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);

                // Execute command when enter pressed
                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine(); // Push terminal cursor to a clean newline

                    string command = inputBuilder.ToString().Trim();
                    inputBuilder.Clear(); // Flush the builder cache for the next input sequence

                    if (string.IsNullOrEmpty(command))
                        continue;

                    if (command.Equals("shutdown"))
                    {
                        Lifetime.StopApplication();
                        break;
                    }

                    string[] args = command.Split(' ');
                    string message = ProcessControlCommand(args[0], args.Length > 1 ? args[1..] : []);

                    if (!string.IsNullOrEmpty(message))
                        Program.Log.Info(message);

                    continue;
                }

                // Backspace
                if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    if (inputBuilder.Length > 0)
                    {
                        inputBuilder.Remove(inputBuilder.Length - 1, 1);
                        Console.Write("\b \b");
                    }
                    continue;
                }

                // Keypress
                if (keyInfo.KeyChar == '\0')
                    continue;
                inputBuilder.Append(keyInfo.KeyChar);
                Console.Write(keyInfo.KeyChar);
            }
        }

        private async Task RunPipeCommandListenerAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var pipeServer = new NamedPipeServerStream(
                        COMM_PIPE,
                        PipeDirection.InOut,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    await pipeServer.WaitForConnectionAsync(stoppingToken);

                    // Fix access rights issues when process runs under system service privileges
                    if (OperatingSystem.IsLinux())
                    {
                        string expectedPath = $"/tmp/CoreFXPipe_{COMM_PIPE}";
                        if (File.Exists(expectedPath))
                        {
                            File.SetUnixFileMode(expectedPath, UnixFileMode.UserRead | UnixFileMode.UserWrite |
                                                               UnixFileMode.GroupRead | UnixFileMode.GroupWrite |
                                                               UnixFileMode.OtherRead | UnixFileMode.OtherWrite);
                        }
                    }

                    using var reader = new StreamReader(pipeServer);
                    using var writer = new StreamWriter(pipeServer) { AutoFlush = true };
                    string command = await reader.ReadLineAsync(stoppingToken);
                    if (!string.IsNullOrEmpty(command))
                    {
                        string[] split = command.Split(' ');
                        string response = ProcessControlCommand(split[0], split.Length > 1 ? split[1..] : []);
                        await writer.WriteLineAsync(response);
                    }

                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Program.Log.Error($"Channel Error encountered: {ex.Message}");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }

        private string ProcessControlCommand(string command, string[] args)
        {
            /*
                polauth create-account <email> <password> [--force <polid>]: Prints out a POLID
                polauth delete-account <polid>
                polauth set-password <polid> <pwd>
                polauth set-admindata <polid> <val>: Used to ban users or w.e
                polauth create-contentid <contentsClass>
                polauth delete-contentid <contentId>
                polauth enable-contentid <contentId>
                polauth disable-contentid <contentId>
                polauth list online
                polauth list users
                polauth get user <polId>
                polauth get content-id <contentId>
             */

            if (command.Equals("debug-send"))
            {
                Server.SendLineToAll(string.Join(' ', args));
            }
            else if (command.Equals("debug-notice"))
            {
                Server.SendLineToAll(":PMY4QWYZH!~x@ NOTICE UE1RDM9N7 :" + string.Join(' ', args));
            }
            else if (command.Equals("create-account") && args.Length >= 2)
            {
                // Forced ID argument.
                string forcedId = null;
                if (args.Length >= 4 && (args[2].Contains("--f") || args[2].Contains("--force")))
                {
                    forcedId = args[3];
                    bool failedIdCheck = false;

                    if (forcedId.Length != 8)
                        return "Invalid forced polid";

                    // Check first 4 characters for Alpha
                    for (int i = 0; i < 4; i++)
                    {
                        if (!char.IsAsciiLetter(forcedId[i]))
                        {
                            failedIdCheck = true;
                            break;
                        }
                    }

                    // Check last 4 characters for Numeric
                    for (int i = 4; i < 8; i++)
                    {
                        if (!char.IsAsciiDigit(forcedId[i]))
                        {
                            failedIdCheck = true;
                            break;
                        }
                    }

                    if (failedIdCheck)
                    {
                        return "Invalid forced polid";
                    }

                }

                // Check email and pwd
                if (!System.Net.Mail.MailAddress.TryCreate(args[0], out _))
                    return "Invalid email address.";
                if (Encoding.ASCII.GetByteCount(args[1]) > 15)
                    return "Password must be 15 characters or less.";

                AdminControlResult result = Server.AdminControl.AddAccount(args[0], args[1], out string newPolId, forcedId);
                switch (result)
                {
                    case AdminControlResult.Success:
                        return $"New account created! Generated PolId is: {newPolId}";
                    default:
                        return "Could not create the account.";
                }
            }
            else if (command.Equals("delete-account") && args.Length >= 1)
            {
                AdminControlResult result = Server.AdminControl.DeleteAccount(args[0]);
                switch (result)
                {
                    case AdminControlResult.Success:
                        return $"Successfully deleted account {args[0]}.";
                    case AdminControlResult.CantFindPolId:
                        return "PolId does not exit.";
                    default:
                        return "Unknown error attempting to delete the account.";
                }
            }
            else if (command.Equals("set-password") && args.Length >= 2)
            {
                AdminControlResult result = PolAuthAdminControl.SetAccountPassword(args[0], args[1]);
                switch (result)
                {
                    case AdminControlResult.Success:
                        return $"Password has been changed.";
                    case AdminControlResult.CantFindPolId:
                        return "PolId does not exit.";
                    default:
                        return "Unknown error attempting to change the password.";
                }
            }
            else if (command.Equals("set-admindata") && args.Length >= 2)
            {
                if (!ushort.TryParse(args[1], out ushort adminDataValue))
                    return "Invalid AdminData value.";

                AdminControlResult result = PolAuthAdminControl.SetAdminData(args[0], adminDataValue);
                switch (result)
                {
                    case AdminControlResult.Success:
                        return $"AdminData for the account has been set to {args[1]}.";
                    case AdminControlResult.CantFindPolId:
                        return "PolId does not exit.";
                    case AdminControlResult.InvalidValue:
                        return "Invalid AdminData value.";
                    default:
                        return "Unknown error attempting to set the AdminData.";
                }
            }
            else if (command.Equals("create-contentid") && args.Length >= 2)
            {
                if (!ushort.TryParse(args[1], out ushort gameId) || !PolAuthAdminControl.IsGameIdValid(gameId))
                    return "Invalid game.";

                AdminControlResult result = PolAuthAdminControl.CreateContentId(args[0], gameId, out ulong cId);
                switch (result)
                {
                    case AdminControlResult.Success:
                        return $"New content id for '{PolAuthAdminControl.GetGameIdName(gameId)}' added to {args[0]}. ID: {cId}.";
                    case AdminControlResult.CantFindPolId:
                        return "PolId does not exit.";
                    case AdminControlResult.MaxContentIds:
                        return "This account has the max amount of content ids.";
                    default:
                        return "Unknown error attempting to create the content id.";
                }
            }
            else if (command.Equals("delete-contentid") && args.Length >= 1)
            {
                AdminControlResult result = PolAuthAdminControl.DeleteContentId(args[0]);
                switch (result)
                {
                    case AdminControlResult.Success:
                        return $"Successfully deleted content id from {args[0]}.";
                    case AdminControlResult.CantFindPolId:
                        return "PolId does not exit.";
                    case AdminControlResult.CantFindContentId:
                        return "Content with that id does not exist.";
                    default:
                        return "Unknown error attempting to delete the account.";
                }
            }
            else if (command.Equals("enable-contentid") && args.Length >= 1)
            {
                AdminControlResult result = PolAuthAdminControl.SetContentIdPaid(args[0], true);
                switch (result)
                {
                    case AdminControlResult.Success:
                        return $"ContentId {args[0]} set to paid.";
                    case AdminControlResult.CantFindPolId:
                        return "PolId does not exit.";
                    case AdminControlResult.CantFindContentId:
                        return $"ContentId {args[1]} does not exist.";
                    default:
                        return "Unknown error attempting to set paid value.";
                }
            }
            else if (command.Equals("disable-contentid") && args.Length >= 1)
            {
                AdminControlResult result = PolAuthAdminControl.SetContentIdPaid(args[0], false);
                switch (result)
                {
                    case AdminControlResult.Success:
                        return $"ContentId {args[0]} set to unpaid.";
                    case AdminControlResult.CantFindPolId:
                        return "PolId does not exit.";
                    case AdminControlResult.CantFindContentId:
                        return $"ContentId {args[1]} does not exist.";
                    default:
                        return "Unknown error attempting to set paid value.";
                }
            }
            else if (command.Equals("list") && args.Length >= 1)
            {
                if (args[0].Equals("online"))
                {
                    List<string> clients = Server.GetOnlineUserNames();
                    if (clients.Count == 0)
                        return $"There are {clients.Count} currently online.";
                    return $"There are {clients.Count} currently online.\n{string.Join('\n', clients.Select(c => $"\t*{c}").ToList())}";
                }
                else if (args[0].Equals("users"))
                {
                    List<string> userLines = PolAuthAdminControl.GetUserList();
                    return $"There are {userLines.Count - 1} accounts.\n{string.Join('\n', userLines)}";
                }
                else
                    return "Missing valid list type, either 'online' or 'users'.";
            }
            else if (command.Equals("user") && args.Length >= 1)
            {
                AdminControlResult result = PolAuthAdminControl.GetUserInfo(args[0], out string userOutStr);
                switch (result)
                {
                    case AdminControlResult.Success:
                        return $"\n{userOutStr}";
                    case AdminControlResult.CantFindPolId:
                        return "PolId does not exit.";
                    default:
                        return "Unknown error looking up user.";
                }
            }
            else if (command.Equals("clear") && Environment.UserInteractive)
            {
                Console.Clear();
                return "";
            }

            return "Unknown command.";
        }
    }
}
