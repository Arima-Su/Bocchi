using DSharpPlus;
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
using Alice_Module.Loaders;
using System.Xml.Linq;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace Alice
{
    class Program
    {
        public static string Alice;
        public static string prefix;

        public static bool skipped = false; //TO BE UPDATED, CURRENTLY NOT CONCURRENT COMPAT

        public static List<ulong> loop = new List<ulong>();
        public static List<ulong> freeplaylist = new List<ulong>();
        public static List<ulong> reconlist = new List<ulong>();
        public static Dictionary<ulong, string> IPs = new Dictionary<ulong, string>();
        //public static Dictionary<ulong, int> Volumes = new Dictionary<ulong, int>();
        
        public static Process lavalinkProcess;
        public static CommandsNextConfiguration commandsConfig;
        public static DiscordClient discord;
        private static Timer disconnectionTimer;

        public static XDocument doc = XDocument.Load("data.xml");
        public static XElement tokenElement;
        public static XElement username;

        public static string User, Token, Prefix;

        public static bool forcestop = false; //TO BE UPDATED, CURRENTLY NOT CONCURRENT COMPAT
        public static bool unbroken = false; //TO BE UPDATED, CURRENTLY NOT CONCURRENT COMPAT

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.Unicode;

            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

            var logFactory = new LoggerFactory().AddSerilog();

            DateTime targetDate = new DateTime(DateTime.Now.Year, 9, 9);

            DateTime currentDate = DateTime.Now;

            if (currentDate.Date == targetDate.Date)
            {
                // CIRNO DAY
                Token = "cirno_token";
                Prefix = "cirno_prefix";
                User = "cirno_user";
            }
            else
            {
                Token = "token";
                Prefix = "prefix";
                User = "username";
            }

            tokenElement = doc.Descendants("category")
                .FirstOrDefault(category => category.Attribute("name")?.Value == Token)?
                .Element("entry");
            XElement prefixElement = doc.Descendants("category")
                .FirstOrDefault(category => category.Attribute("name")?.Value == Prefix)?
                .Element("entry");
            XElement weebhook = doc.Descendants("category")
                .FirstOrDefault(category => category.Attribute("name")?.Value == "hook")?
                .Element("entry");
            username = doc.Descendants("category")
                .FirstOrDefault(category => category.Attribute("name")?.Value == User)?
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

        public static async Task PlaybackFinishedHandler(LavalinkGuildConnection sender, TrackFinishEventArgs e)
        {
            ulong guild = sender.Guild.Id;

            if (skipped == true)
            {
                if(!freeplaylist.Contains(guild))
                {
                    return;
                }
            }
            else
            {
                if (freeplaylist.Contains(guild) && !reconlist.Contains(guild))
                {
                    Console.WriteLine("FREE BIRD YEAHH..");
                    await sender.Channel.SendMessageAsync("Hmmm.. let me see here..");
                    var youtube = new YoutubeClient();

                    //GET ORIGINAL VIDEO
                    var videos = await youtube.Search.GetPlaylistsAsync(SlashComms._queueDictionary[guild][0].getTrack().Title + " " + SlashComms._queueDictionary[guild][0].getTrack().Author);

                    //GET PLAYLIST
                    if (videos.Count > 0)
                    {
                        Random random = new Random();
                        int randomIndex = random.Next(0, videos.Count);

                        var songTitles = await PlayLoader.YoutubeLoaderAsync(videos[randomIndex].Url);

                        if (songTitles == null || songTitles.Count == 0)
                        {
                            await sender.Channel.SendMessageAsync("No song titles found in the playlist.");
                            return;
                        }

                        await sender.Channel.SendMessageAsync("Loading Playlist..");
                        var progressMessage = await sender.Channel.SendMessageAsync("_ songs queue'd..");
                        int songCount = 0;

                        foreach (string title in songTitles)
                        {
                            Program.unbroken = true;

                            if (PlayLoader._queuefull == true)
                            {
                                PlayLoader._queuefull = false;
                                break;
                            }

                            if (Program.forcestop == true)
                            {
                                Program.unbroken = false;
                                break;
                            }

                            if (songCount > 9)
                            {
                                break;
                            }

                            ++songCount;

                            await PlayLoader.FreeEnqueue(sender, title);
                            await progressMessage.ModifyAsync($"{songCount} songs queue'd..");
                        }

                        await progressMessage.ModifyAsync("Playlist loaded.");
                        Console.WriteLine("Theoretically, it should be playing..");
                        SlashComms._queueDictionary[guild].RemoveAt(0);
                        reconlist.Add(guild);
                    }
                    else
                    {
                        await sender.Channel.SendMessageAsync("I can't think of a next song to recommend..");
                        Console.WriteLine("Could not get playlist");
                        SlashComms._queueDictionary[guild].RemoveAt(0);
                    }

                    return;
                }

                if (SlashComms._queueDictionary[guild] != null)
                {
                    if (SlashComms._queueDictionary[guild].Count > 0)
                    {
                        if (loop.Contains(guild))
                        {
                            var loopTrack = SlashComms._queueDictionary[guild][0];
                            skipped = true;
                            await sender.PlayAsync(loopTrack.getTrack());
                            skipped = false;
                        }
                        else
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

                                if (SlashComms._queueDictionary[guild].Count == 2 && freeplaylist.Contains(guild))
                                {
                                    reconlist.Remove(guild);
                                }

                                SlashComms._queueDictionary[guild].RemoveAt(0);

                                try
                                {
                                    skipped = true;
                                    await sender.PlayAsync(nextTrack.getTrack());

                                    if (SlashComms._queueDictionary.Count > 1)
                                    {
                                        await Program.UpdateUserStatus(discord, "CONCURRENT", "backflip");
                                        Console.WriteLine($"CONCURRENT: {SlashComms._queueDictionary.Count}");
                                    }
                                    else
                                    {
                                        await Program.UpdateUserStatus(discord, "LISTENING", $"{nextTrack.getTrack().Title} {nextTrack.getTrack().Author}");
                                        Console.WriteLine($"NOW PLAYING: {nextTrack.getTrack().Title} {nextTrack.getTrack().Author}");
                                    }
                                    skipped = false;
                                }
                                catch
                                {
                                    await RetryAsync(sender, nextTrack.getTrack());
                                }
                            }
                        }
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
        }

        public static async Task RetryAsync(LavalinkGuildConnection conn, LavalinkTrack track)
        {
            Console.WriteLine("Retried..");
            try
            {
                skipped = true;
                await conn.PlayAsync(track);

                if (SlashComms._queueDictionary.Count > 1)
                {
                    await Program.UpdateUserStatus(discord, "CONCURRENT", "backflip");
                    Console.WriteLine($"CONCURRENT: {SlashComms._queueDictionary.Count}");
                }
                else
                {
                    await Program.UpdateUserStatus(discord, "LISTENING", $"{track.Title} {track.Author}");
                    Console.WriteLine($"NOW PLAYING: {track.Title} {track.Author}");
                }
                skipped = false;
            }
            catch
            {
                Console.WriteLine("Well that failed, I'll try that again..");
                await RetryAsync(conn, track);
            }
        }

        public static async Task DisconnectionHandler(DiscordClient client, VoiceStateUpdateEventArgs e)
        {
            if (e.User == client.CurrentUser)
            {
                ulong guild = e.Guild.Id;
                var guildconn = e.Guild;

                if (SlashComms._queueDictionary.ContainsKey(guild))
                {
                    SlashComms._queueDictionary.Remove(guild);
                }

                if (e.After?.Channel == null)
                {
                    if (SlashComms._queueDictionary.Count > 1)
                    {
                        Console.WriteLine($"CONCURRENT: {SlashComms._queueDictionary.Count}");
                    }
                    else if (SlashComms._queueDictionary.Count == 1)
                    {
                        var remainingList = SlashComms._queueDictionary.Values.FirstOrDefault();

                        if (remainingList != null)
                        {
                            if (remainingList[0] != null)
                            {
                                var currentTrack = remainingList[0];

                                Console.WriteLine("PLAYER IS PLAYING");
                                Console.WriteLine($"NOW PLAYING: {currentTrack.getTrack().Title} {currentTrack.getTrack().Author}");
                                await Program.UpdateUserStatus(client, "LISTENING", $"{currentTrack.getTrack().Title} {currentTrack.getTrack().Author}");
                            }
                            
                        }
                    }
                    else
                    {
                        Console.WriteLine("LEFT");
                        await Program.UpdateUserStatus(client, "IDLE", "bocchi");
                    }

                    var lava = client.GetLavalink();
                    var node = lava.ConnectedNodes.Values.First();
                    var conn = node.GetGuildConnection(guildconn);

                    if (loop.Contains(guild))
                    {
                        loop.Remove(guild);
                    }
                    
                    if (conn != null)
                    {
                        Program.skipped = true;
                        await conn.StopAsync();
                        Program.skipped = false;
                    }

                    await Task.Delay(10);

                    if(SlashComms._queueDictionary.Count > 0)
                    {
                        Console.WriteLine("Disconnected, Keeping alive.");
                        return;
                    }

                    disconnectionTimer = new Timer(TimerCallback, null, TimeSpan.FromSeconds(15), Timeout.InfiniteTimeSpan);
                    Console.WriteLine("Timer Started");
                }
                else
                {
                    RemoveDisconnectionTimer();

                    if (SlashComms._queueDictionary.ContainsKey(guild) && SlashComms._queueDictionary[guild].Count > 0)
                    {
                        if (SlashComms._queueDictionary[guild][0] != null)
                        {
                            var currentTrack = SlashComms._queueDictionary[guild][0];

                            if (SlashComms._queueDictionary.Count > 1)
                            {
                                Console.WriteLine($"CONCURRENT: {SlashComms._queueDictionary.Count}");
                            }
                            else if (SlashComms._queueDictionary.Count == 1)
                            {
                                Console.WriteLine("PLAYER IS PLAYING");
                                Console.WriteLine($"NOW PLAYING: {currentTrack.getTrack().Title} {currentTrack.getTrack().Author}");
                                await Program.UpdateUserStatus(client, "LISTENING", $"{currentTrack.getTrack().Title} {currentTrack.getTrack().Author}");
                            }
                            else
                            {
                                Console.WriteLine("JOINED");
                                await Program.UpdateUserStatus(client, "IDLE", "bocchi");
                            }
                        }
                        else
                        {
                            if (SlashComms._queueDictionary.Count > 1)
                            {
                                Console.WriteLine($"CONCURRENT: {SlashComms._queueDictionary.Count}");
                            }
                            else
                            {
                                Console.WriteLine("JOINED");
                                await Program.UpdateUserStatus(client, "IDLE", "bocchi");
                            }
                        }
                    }
                    else
                    {
                        if (SlashComms._queueDictionary.Count > 1)
                        {
                            Console.WriteLine($"CONCURRENT: {SlashComms._queueDictionary.Count}");
                        }
                        else
                        {
                            Console.WriteLine("JOINED");
                            await Program.UpdateUserStatus(client, "IDLE", "bocchi");
                        }
                    }
                }
            }

            return;
        }

        private static void TimerCallback(object state)
        {

            if (SlashComms._queueDictionary.Count == 0)
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
            Console.WriteLine("Timer Ended");
        } //POTENTIAL SOURCE OF RANDOM "STATE" ERROR

        private static void RemoveDisconnectionTimer()
        {
            // Dispose of the timer to remove it
            disconnectionTimer?.Dispose();
            disconnectionTimer = null;
        }

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
