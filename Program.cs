﻿using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Threading.Tasks;
using DSharpPlus.SlashCommands;
using DSharpPlus.Lavalink;
using System.Linq;
using DSharpPlus.Lavalink.EventArgs;
using DSharpPlus.CommandsNext;
using Alice.Commands;
using Alice.Responses;
using System.Xml.Linq;
using System.Diagnostics;
using System.Threading;

namespace Alice
{
    class Program
    {
        public static string Alice;
        public static bool skipped = false;
        public static string prefix;
        public static Process lavalinkProcess;
        public static CommandsNextConfiguration commandsConfig;
        private static DiscordClient discord;
        static XDocument doc = XDocument.Load("data.xml");
        private static Timer disconnectionTimer;

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.Unicode;

            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

            var logFactory = new LoggerFactory().AddSerilog();

            
            XElement tokenElement = doc.Descendants("category")
                .FirstOrDefault(category => category.Attribute("name")?.Value == "token")?
                .Element("entry");
            XElement prefixElement = doc.Descendants("category")
                .FirstOrDefault(category => category.Attribute("name")?.Value == "prefix")?
                .Element("entry");
            XElement weebhook = doc.Descendants("category")
                .FirstOrDefault(category => category.Attribute("name")?.Value == "hook")?
                .Element("entry");

            if (tokenElement != null && prefixElement != null)
            {
                string token = tokenElement.Value;
                prefix = prefixElement.Value;
                Alice = weebhook.Value;

                discord = new DiscordClient(new DiscordConfiguration()
                {
                    Token = token,
                    TokenType = DSharpPlus.TokenType.Bot,
                    Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents,
                    LoggerFactory = logFactory,
                    MinimumLogLevel = LogLevel.Debug
                }); ;

                var lavalink = discord.UseLavalink();

                commandsConfig = new CommandsNextConfiguration()
                {
                    StringPrefixes = new string[] { prefix },
                    EnableMentionPrefix = false,
                    EnableDms = false,
                    EnableDefaultHelp = false,
                };

                //DSharp Prefix Commands
                var comms = discord.UseCommandsNext(commandsConfig);
                comms.RegisterCommands<Comms>();

                //DSharp Slash Commands
                var slash = discord.UseSlashCommands();
                slash.RegisterCommands<SlashComms>();

                //Event Handlers
                discord.MessageCreated += MessageHandler.MessageCreatedHandler;
                discord.Ready += ClientReadyHandler;
                discord.VoiceStateUpdated += DisconnectionHandler;
                AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

                //Connection Methods
                await discord.ConnectAsync();

                //Twenty Four Seven
                await Task.Delay(-1);
            }
            else
            {
                Console.WriteLine("Token or Prefix not specified in data.xml");
            }
        }

        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            if (lavalinkProcess != null && !lavalinkProcess.HasExited)
            {
                lavalinkProcess.Kill();
                lavalinkProcess.CloseMainWindow();
                lavalinkProcess.Close();
                SlashComms._lavastarted = false;
            }
        }

        //public static async Task PlaybackFinishedHandler(LavalinkGuildConnection sender, TrackFinishEventArgs e)
        //{
        //    if (skipped == true)
        //    {
        //        return;
        //    }
        //    else
        //    {
        //        if (SlashComms.songQueue.Count > 0)
        //        {
        //            if (SlashComms.songQueue.Count == 1)
        //            {
        //                SlashComms.songQueue.RemoveAt(0);
        //                Console.WriteLine("SONG ENDED");
        //                return;
        //            }
        //            else
        //            {
        //                var nextTrack = SlashComms.songQueue[1];
        //                SlashComms.songQueue.RemoveAt(0);

        //                skipped = true;
        //                await sender.PlayAsync(nextTrack);
        //                Console.WriteLine($"NOW PLAYING: {nextTrack.Title}");
        //                skipped = false;
        //                return;
        //            }
        //        }
        //        else
        //        {
        //            return;
        //        }
        //    }
        //}
        public static async Task PlaybackFinishedHandler(LavalinkGuildConnection sender, TrackFinishEventArgs e)
        {
            if (skipped == true)
            {
                return;
            }
            else
            {
                ulong guild = sender.Guild.Id;

                if (SlashComms._queueDictionary[guild].Count > 0)
                {
                    if (SlashComms._queueDictionary[guild].Count == 1)
                    {
                        SlashComms._queueDictionary[guild].RemoveAt(0);
                        SlashComms._queueDictionary.Remove(guild);
                        Console.WriteLine("SONG ENDED");
                        Console.WriteLine("JOINED");
                        string status = MessageHandler.GetRandomEntry("state");
                        await Program.UpdateUserStatus(discord, "IDLE", status);
                        return;
                    }
                    else
                    {
                        var nextTrack = SlashComms._queueDictionary[guild][1];
                        SlashComms._queueDictionary[guild].RemoveAt(0);

                        skipped = true;
                        await sender.PlayAsync(nextTrack);
                        Console.WriteLine($"NOW PLAYING: {nextTrack.Title}");
                        if (SlashComms._queueDictionary.Count > 1)
                        {
                            await Program.UpdateUserStatus(discord, "CONCURRENT", "backflip");
                        }
                        else
                        {
                            await Program.UpdateUserStatus(discord, "LISTENING", nextTrack.Title);
                        }
                        skipped = false;
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
        }

        public static async Task DisconnectionHandler(DiscordClient client, VoiceStateUpdateEventArgs e)
        {
            if (e.User == client.CurrentUser)
            {
                ulong guild = e.Guild.Id;
                var guildconn = e.Guild;


                if (e.After?.Channel == null)
                {
                    Console.WriteLine("LEFT");
                    SlashComms._queueDictionary.Remove(guild);
                    var lava = client.GetLavalink();
                    var node = lava.ConnectedNodes.Values.First();
                    var conn = node.GetGuildConnection(guildconn);
                    await conn.StopAsync();

                    disconnectionTimer = new Timer(TimerCallback, guild, TimeSpan.FromSeconds(15), Timeout.InfiniteTimeSpan);

                    await Program.UpdateUserStatus(client, "IDLE", "bocchi");
                }
                else
                {
                    RemoveDisconnectionTimer();

                    if (SlashComms._queueDictionary[guild][0] != null)
                    {
                        var currentTrack = SlashComms._queueDictionary[guild][0];
                        
                        Console.WriteLine("PLAYER IS PLAYING");
                        Console.WriteLine($"NOW PLAYING: {currentTrack.Title}");
                        await Program.UpdateUserStatus(client, "LISTENING", currentTrack.Title);
                    }
                    else
                    {
                        Console.WriteLine("JOINED");
                    }
                }
            }

            return;
        }

        private static void TimerCallback(object state)
        {
            ulong guild = (ulong)state;

            if (SlashComms._queueDictionary.Count == 1)
            {
                if (lavalinkProcess != null && !lavalinkProcess.HasExited)
                {
                    lavalinkProcess.Kill();
                    lavalinkProcess.CloseMainWindow();
                    lavalinkProcess.Close();
                    SlashComms._lavastarted = false;
                }
                Console.WriteLine("LAVALINK IS DISCONNECTED");
            }

            RemoveDisconnectionTimer();
        }

        private static void RemoveDisconnectionTimer()
        {
            // Dispose of the timer to remove it
            disconnectionTimer?.Dispose();
            disconnectionTimer = null;
        }

        //public static async Task PlaybackFinishedHandler(LavalinkGuildConnection sender, TrackFinishEventArgs e)
        //{
        //    Console.WriteLine("SONG ENDED");
        //    try
        //    {
        //        if (skipped == true)
        //        {
        //            ulong channelId = 1118545693053829170;

        //            DiscordChannel channel = await discord.GetChannelAsync(channelId);

        //            if (SlashComms.songQueue.Count == 0)
        //            {
        //                await channel.SendMessageAsync("The queue list is blank.");
        //            }
        //            else
        //            {
        //                var queueContent = string.Join("\n", SlashComms.songQueue.Select((track, index) =>
        //                {
        //                    var prefix = (index == 0) ? "【Now Playing】 " : string.Empty;
        //                    Console.WriteLine($"NOW PLAYING: {track.Title}");
        //                    return $"{index + 1}. {prefix}{track.Title}";
        //                }));
        //                await channel.SendMessageAsync($"Status = Skipped\n{queueContent}");

        //            }

        //            return;
        //        }

        //        if (SlashComms.songQueue.Count > 0)
        //        {
        //            if (SlashComms.songQueue.Count == 1)
        //            {
        //                SlashComms.songQueue.RemoveAt(0);
        //            }

        //            var nextTrack = SlashComms.songQueue[1];
        //            SlashComms.songQueue.RemoveAt(0);

        //            skipped = true;
        //            Console.WriteLine($"NOW PLAYING: {nextTrack.Title}");
        //            await sender.PlayAsync(nextTrack);
        //            skipped = false;

        //            ulong channelId = 1118545693053829170;

        //            DiscordChannel channel = await discord.GetChannelAsync(channelId);

        //            if (SlashComms.songQueue.Count == 0)
        //            {
        //                await channel.SendMessageAsync("The queue list is blank.");
        //            }
        //            else
        //            {
        //                var queueContent = string.Join("\n", SlashComms.songQueue.Select((track, index) =>
        //                {
        //                    var prefix = (index == 0) ? "【Now Playing】 " : string.Empty;
        //                    Console.WriteLine($"NOW PLAYING: {track.Title}");
        //                    return $"{index + 1}. {prefix}{track.Title}";
        //                }));
        //                await channel.SendMessageAsync($"Status = Playback Finished\n{queueContent}");
        //            }
        //        }

        //        if (SlashComms.songQueue.Count == 0)
        //        {
        //            return;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.ToString());
        //        Console.WriteLine("SONG ENDED");
        //    }
        //}

        public static Task PlaybackErrorHandler(LavalinkGuildConnection sender, TrackExceptionEventArgs e)
        {
            Console.WriteLine("I fumbled a song..");
            return Task.CompletedTask;
        }

        private static async Task ClientReadyHandler(DiscordClient client, ReadyEventArgs e)
        {
            string status = MessageHandler.GetRandomEntry("state");

            await client.UpdateStatusAsync(new DiscordActivity(status, DSharpPlus.Entities.ActivityType.Watching), DSharpPlus.Entities.UserStatus.Idle);

            return;
        }

        public static async Task UpdateUserStatus(DiscordClient client, string state, string title)
        {
            if (state == "LISTENING")
            {
                if (SlashComms._queueDictionary.Count > 1)
                {
                    await Program.UpdateUserStatus(discord, "CONCURRENT", "backflip");
                }
                else
                {
                    await client.UpdateStatusAsync(new DiscordActivity(title, DSharpPlus.Entities.ActivityType.ListeningTo), DSharpPlus.Entities.UserStatus.Idle);
                }
            }
            else if (state == "IDLE")
            {
                string status = MessageHandler.GetRandomEntry("state");

                await client.UpdateStatusAsync(new DiscordActivity(status, DSharpPlus.Entities.ActivityType.Watching), DSharpPlus.Entities.UserStatus.Idle);
            }
            else if (state == "CONCURRENT")
            {
                int num = SlashComms._queueDictionary.Count;

                await client.UpdateStatusAsync(new DiscordActivity($"in {num} servers..", DSharpPlus.Entities.ActivityType.Playing), DSharpPlus.Entities.UserStatus.Idle);
            }

            return;
        }

        public static async Task StartLava()
        {
            lavalinkProcess = new Process();
            lavalinkProcess.StartInfo.FileName = @"C:\Program Files\Java\jdk-11\bin\java.exe";
            lavalinkProcess.StartInfo.Arguments = "-jar Lavalink.jar";
            lavalinkProcess.StartInfo.RedirectStandardOutput = true;
            lavalinkProcess.StartInfo.UseShellExecute = false;
            lavalinkProcess.StartInfo.CreateNoWindow = true;

            var taskCompletionSource = new TaskCompletionSource<bool>();

            void OutputDataReceivedHandler(object sender, DataReceivedEventArgs args)
            {
                if (args.Data.Contains("Lavalink is ready to accept connections."))
                {
                    taskCompletionSource.SetResult(true);

                    // Unsubscribe from the event
                    lavalinkProcess.OutputDataReceived -= OutputDataReceivedHandler;
                }
                if (args.Data.Contains("Web server failed to start"))
                {
                    taskCompletionSource.SetResult(true);
                    SlashComms._failed = true;

                    // Unsubscribe from the event
                    lavalinkProcess.OutputDataReceived -= OutputDataReceivedHandler;
                }
            }

            // Subscribe to the event
            lavalinkProcess.OutputDataReceived += OutputDataReceivedHandler;

            lavalinkProcess.Start();
            lavalinkProcess.BeginOutputReadLine();

            await taskCompletionSource.Task;
        }
    }
}