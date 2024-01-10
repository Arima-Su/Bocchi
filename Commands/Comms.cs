using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Lavalink;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alice.Responses;
using DSharpPlus.Net;
using Alice_Module.Loaders;
using YoutubeExplode;
using System.IO;
using YoutubeExplode.Videos.Streams;
using Alice_Module.Handlers;
using NAudio.Wave;
using System.Text;
using AngleSharp.Text;
using System.Windows.Forms;
using Discord;
using SpotifyAPI.Web;
using DSharpPlus.EventArgs;
using System.Xml.Linq;

namespace Alice.Commands
{
    public class Comms : BaseCommandModule
    {
        [Command("join")]
        public async Task JoinCommand(CommandContext ctx)
        {
            var res = Optimizations.StartUpSequence(ctx, true);
            if (res != null)
            {
                await ctx.Channel.SendMessageAsync(res);
                return;
            }
        }

        [Command("playskip"), Aliases("ps")]
        public async Task PlaySkipCommand(CommandContext ctx, [RemainingText] string search)
        {
            var res = Optimizations.StartUpSequence(ctx);
            if (res != null)
            {
                await ctx.Channel.SendMessageAsync(res);
                return;
            }
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();

            var tracks = await Optimizations.QueueUpSequence(ctx, search);
            await Optimizations.PlayUpSequence(ctx, node.GetGuildConnection(ctx.Member.VoiceState.Guild), tracks, true);
        }

        [Command("play"), Aliases("p")]
        public async Task PlayCommand(CommandContext ctx, [RemainingText] string search)
        {
            var res = Optimizations.StartUpSequence(ctx);
            if (res != null)
            {
                await ctx.Channel.SendMessageAsync(res);
                return;
            }
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();

            var tracks = await Optimizations.QueueUpSequence(ctx, search);
            await Optimizations.PlayUpSequence(ctx, node.GetGuildConnection(ctx.Member.VoiceState.Guild), tracks);
        }

        [Command("np"), Aliases("nowplaying")]
        public async Task NowPlayingCommand(CommandContext ctx)
        {
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();

            if (SlashComms._lavastarted == false)                                                                    // LAVALINK CHECK
            {
                await ctx.Channel.SendMessageAsync("Please execute /start first so I can boot up the music player..");
                return;
            }

            if (ctx.Member.VoiceState == null)
            {
                await ctx.Channel.SendMessageAsync("Unfortunately that's not how it works, you gotta be in the same voice channel..");
                return;
            }

            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.Channel.SendMessageAsync("Brother, I'm not even in a voice channel yet..");
                return;
            }

            if (conn.CurrentState.CurrentTrack != null)
            {
                var currentTrack = conn.CurrentState.CurrentTrack;
                var trackInfo = $"{currentTrack.Title} {currentTrack.Author} [{currentTrack.Length}]";
                await ctx.Channel.SendMessageAsync($"***Now Playing: {trackInfo}***\n*Requested by: {SlashComms._queueDictionary[ctx.Guild.Id][0].getUser()}*");
                //await ctx.Channel.SendMessageAsync($"*Requested by: {SlashComms._queueDictionary[ctx.Guild.Id][0].getUser()}*");
            }
            else
            {
                await ctx.Channel.SendMessageAsync("Nothing but silence..");
            }
        }

        [Command("pause")]
        public async Task PauseCommand(CommandContext ctx)
        {
            if (SlashComms._lavastarted == false)                                                                    // LAVALINK CHECK
            {
                await ctx.Channel.SendMessageAsync("Please execute /start first so I can boot up the music player..");
                return;
            }

            if (SlashComms._playerIsPaused == true)
            {
                await ctx.Channel.SendMessageAsync("I already told the song not to move..");
                return;
            }

            SlashComms._playerIsPaused = true;

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();

            if (ctx.Member.VoiceState == null)
            {
                await ctx.Channel.SendMessageAsync("Nice try prankster but that's not how it works, you gotta be in the same voice channel as the player..");
                return;
            }

            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.Channel.SendMessageAsync("Brother, I'm not even in a voice channel yet..");
                return;
            }

            if (conn.CurrentState.CurrentTrack != null)
            {
                var currentTrack = conn.CurrentState.CurrentTrack;

                await ctx.Channel.SendMessageAsync($"A gun has been pointed at {currentTrack.Title}");
                await conn.PauseAsync();
            }
        }

        [Command("resume")]
        public async Task ResumeCommand(CommandContext ctx)
        {
            if (SlashComms._lavastarted == false)                                                                    // LAVALINK CHECK
            {
                await ctx.Channel.SendMessageAsync("Please execute /start first so I can boot up the music player..");
                return;
            }

            if (SlashComms._playerIsPaused != true)
            {
                await ctx.Channel.SendMessageAsync("The song is already on the move..");
                return;
            }

            SlashComms._playerIsPaused = false;

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();

            if (ctx.Member.VoiceState == null)
            {
                await ctx.Channel.SendMessageAsync("Nice try prankster but that's not how it works, you gotta be in the same voice channel as the player..");
                return;
            }

            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.Channel.SendMessageAsync("Brother, I'm not even in a voice channel yet..");
                return;
            }

            if (conn.CurrentState.CurrentTrack != null)
            {
                var currentTrack = conn.CurrentState.CurrentTrack;

                await ctx.Channel.SendMessageAsync($"The gun was fired near {currentTrack.Title}");
                await conn.ResumeAsync();
            }
        }
        /*
        [Command("stop")]
        public async Task StopCommand(CommandContext ctx)
        {
            var lava = ctx.Client.GetLavalink();
            if (lava != null)
            {
                var node = lava.ConnectedNodes.Values.First();
                if (node != null)
                {
                    var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);
                    if (conn != null)
                    {
                        await conn.StopAsync();
                    }
                }
            }
            
            ulong guild = ctx.Guild.Id;

            if (SlashComms._lavastarted == false)                                                                    // LAVALINK CHECK
            {
                await ctx.Channel.SendMessageAsync("Please execute /start first so I can boot up the music player..");
                return;
            }

            if (SlashComms._queueDictionary.ContainsKey(guild))
            {
                SlashComms._queueDictionary.Remove(guild);
            }

            

            if (Program.lavalinkProcess != null && !Program.lavalinkProcess.HasExited)
            {
                Program.lavalinkProcess.Kill();
                Program.lavalinkProcess.CloseMainWindow();
                Program.lavalinkProcess.Close();
            }

            SlashComms._lavastarted = false;
            await ctx.Channel.SendMessageAsync("A gun was fired at the player, the queue is in pieces..");
            Console.WriteLine("LAVALINK IS DISCONNECTED");
        }
        */
        [Command("queue"), Aliases("q")]
        public async Task QueueCommand(CommandContext ctx, [RemainingText] string page)
        {
            ulong guild = ctx.Guild.Id;
            
            if (SlashComms._lavastarted == false)                                                                    // LAVALINK CHECK
            {
                await ctx.Channel.SendMessageAsync("Please execute /start first so I can boot up the music player..");
                return;
            }

            if (SlashComms._queueDictionary.Count == 0 || SlashComms._queueDictionary[guild] is null || SlashComms._queueDictionary[guild].Count == 0)
            {
                await ctx.Channel.SendMessageAsync("The queue is empty.");
                return;
            }

            if (SlashComms._queueDictionary[guild].Count > 0 && SlashComms._queueDictionary[guild] != null)
            {
                int swipe;
                
                if (SlashComms._queueDictionary[guild].Count > 20)
                {
                    if(page is null)
                    {
                        swipe = 1;
                    }
                    else
                    {
                        swipe = page.ToInteger(0);
                    }

                    var message = new StringBuilder();
                    TimeSpan queueLength = TimeSpan.Zero;
                    int trackNumber = 1;

                    if (swipe <= 0)
                    {
                        swipe = 1;
                    }

                    if (swipe * 20 > SlashComms._queueDictionary[guild].Count+19)
                    {
                        await ctx.Channel.SendMessageAsync("The queue list isn't *that* long, I'll just give you the last page..");
                        if (SlashComms._queueDictionary[guild].Count%20 != 0)
                        {
                            swipe = (SlashComms._queueDictionary[guild].Count / 20) + 1;
                        }
                        else
                        {
                            swipe = (SlashComms._queueDictionary[guild].Count / 20);
                        }

                    }

                    await ctx.Channel.SendMessageAsync("Look at all these songs: [Page: " + swipe + "]");

                    foreach (var track in SlashComms._queueDictionary[guild])
                    {
                        if (track != null)
                        {
                            if (trackNumber >= (swipe - 1) * 20 && trackNumber <= swipe * 20)
                            {
                                // Append the track information to the message
                                if (trackNumber == 1)
                                {
                                    message.AppendLine($"***Now Playing: {track.getTrack().Title} {track.getTrack().Author}***");
                                }
                                else
                                {
                                    message.AppendLine($"{trackNumber}. {track.getTrack().Title} {track.getTrack().Author}");
                                }
                            }

                            queueLength += track.getTrack().Length;
                            trackNumber++;
                        }
                    }

                    if (trackNumber > 1)
                    {
                        message.AppendLine($"***{trackNumber - 1} total songs for {queueLength} long..***");
                        await ctx.Channel.SendMessageAsync(message.ToString());
                    }
                    else
                    {
                        await ctx.Channel.SendMessageAsync("The queue is empty.");
                    }
                }
                else
                {
                    var message = new StringBuilder();
                    TimeSpan queueLength = TimeSpan.Zero;
                    int trackNumber = 1;

                    await ctx.Channel.SendMessageAsync("Look at all these songs: ");

                    foreach (var track in SlashComms._queueDictionary[guild])
                    {
                        if (track != null)
                        {
                            // Append the track information to the message
                            if (trackNumber == 1)
                            {
                                message.AppendLine($"***Now Playing: {track.getTrack().Title} {track.getTrack().Author}***");
                            }
                            else
                            {
                                message.AppendLine($"{trackNumber}. {track.getTrack().Title} {track.getTrack().Author}");
                            }

                            queueLength += track.getTrack().Length;
                            trackNumber++;
                        }
                    }

                    if (trackNumber > 1)
                    {
                        message.AppendLine($"***{trackNumber - 1} total songs for {TimeSpan.FromSeconds(Math.Round(queueLength.TotalSeconds))} long..***");
                        await ctx.Channel.SendMessageAsync(message.ToString());
                    }
                    else
                    {
                        await ctx.Channel.SendMessageAsync("The queue is empty.");
                    }
                }
            }
            else
            {
                await ctx.Channel.SendMessageAsync("The queue list is blank.");
            }
        }

        [Command("byebye"), Aliases("leave")]
        public async Task LeaveCommand(CommandContext ctx)
        {
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();

            if (ctx.Member.VoiceState == null)
            {
                await ctx.Channel.SendMessageAsync("Unfortunately that's not how it works, you gotta be in the same voice channel..");
                return;
            }

            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);
            ulong guild = ctx.Guild.Id;

            if (conn == null)
            {
                await ctx.Channel.SendMessageAsync("What do you mean? I'm already out..");
            }
            else
            {
                string category = "Byes";

                string randomEntry = MessageHandler.GetRandomEntry(category);

                if (randomEntry != null)
                {
                    await ctx.Channel.SendMessageAsync(randomEntry);
                    Program.skipped = true;
                    await conn.StopAsync();
                    Program.skipped = false;
                    SlashComms._queueDictionary.Remove(guild);
                    await conn.DisconnectAsync();
                }
                else
                {
                    Console.WriteLine("No entries found for the specified category.");
                }
            }
        }

        [Command("remove")]
        public async Task RemoveCommand(CommandContext ctx, [RemainingText] string Num)
        {
            var trackNum = 0;
            ulong guild = ctx.Guild.Id;

            if (Num is string)
            {
                try
                {
                    trackNum = Convert.ToInt32(Num);
                }
                catch
                {
                    await ctx.Channel.SendMessageAsync("That is not a number..");
                    return;
                }
            }

            if (SlashComms._lavastarted == false)                                                                    // LAVALINK CHECK
            {
                await ctx.Channel.SendMessageAsync("Please execute /start first so I can boot up the music player..");
                return;
            }

            if (trackNum < 0 || trackNum >= SlashComms._queueDictionary[guild].Count + 1)
            {
                await ctx.Channel.SendMessageAsync("Invalid track number.");
                return;
            }

            if (trackNum == 1)
            {
                var lava = ctx.Client.GetLavalink();
                var node = lava.ConnectedNodes.Values.First();
                var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

                if (ctx.Member.VoiceState == null)
                {
                    await ctx.Channel.SendMessageAsync("Nice try but that's not how it works, you gotta be in the same voice channel..");
                    return;
                }

                if (SlashComms._queueDictionary[guild].Count < 2)
                {
                    var tune = SlashComms._queueDictionary[guild][0];
                    await ctx.Channel.SendMessageAsync($"Removed {tune.getTrack().Title} {tune.getTrack().Author}");
                    await Program.UpdateUserStatus(ctx.Client, "IDLE", "bocchi");
                    SlashComms._queueDictionary.Remove(guild);
                    await conn.StopAsync();
                    return;
                }

                var nextTrack = SlashComms._queueDictionary[guild][1];
                var nextTrackTitle = nextTrack.getTrack().Title;
                var track = SlashComms._queueDictionary[guild][0];
                var trackTitle = track.getTrack().Title;
                Program.skipped = true;
                await conn.PlayAsync(nextTrack.getTrack());
                SlashComms._queueDictionary[guild].RemoveAt(0);

                await ctx.Channel.SendMessageAsync($"Eliminated {trackTitle} {track.getTrack().Author}");
                if (SlashComms._queueDictionary.Count > 1)
                {
                    Console.WriteLine($"CONCURRENT: {SlashComms._queueDictionary.Count}");
                }
                else
                {
                    Console.WriteLine($"NOW PLAYING: {track.getTrack().Title} {track.getTrack().Author}");
                }
                await Program.UpdateUserStatus(ctx.Client, "LISTENING", $"{track.getTrack().Title} {track.getTrack().Author}");
                Program.skipped = false;
                return;
            }

            var song = SlashComms._queueDictionary[guild][trackNum - 1];
            var songTitle = song.getTrack().Title;
            SlashComms._queueDictionary[guild].RemoveAt(trackNum - 1);
            await ctx.Channel.SendMessageAsync($"Eliminated {songTitle}..");

        }

        [Command("skipto")]
        public async Task SkipToCommand(CommandContext ctx, [RemainingText] string Num)
        {
            var res = Optimizations.StartUpSequence(ctx);
            if (res != null)
            {
                await ctx.Channel.SendMessageAsync(res);
                return;
            }
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();

            ulong guild = ctx.Guild.Id;

            if (!int.TryParse(Num, out int trackNum))
            {
                await ctx.Channel.SendMessageAsync("That is not a valid number.");
                return;
            }

            if (trackNum <= 0 || trackNum > SlashComms._queueDictionary[guild].Count + 1)
            {
                await ctx.Channel.SendMessageAsync("Invalid track number.");
                return;
            }

            // Remove tracks from the beginning of the queue up to the desired track (exclusive)
            var tracksToRemove = SlashComms._queueDictionary[guild].GetRange(0, trackNum - 1);
            SlashComms._queueDictionary[guild].RemoveRange(0, trackNum - 1);

            Program.skipped = true;
            var currentTrack = SlashComms._queueDictionary[guild][0];

            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);
            await conn.PlayAsync(currentTrack.getTrack());

            await ctx.Channel.SendMessageAsync($"Removed {tracksToRemove.Count} tracks from the queue..");
            await ctx.Channel.SendMessageAsync($"Now Playing: {currentTrack.getTrack().Title} {currentTrack.getTrack().Author}");
            if (SlashComms._queueDictionary.Count > 1)
            {
                Console.WriteLine($"CONCURRENT: {SlashComms._queueDictionary.Count}");
            }
            else
            {
                Console.WriteLine($"NOW PLAYING: {currentTrack.getTrack().Title} {currentTrack.getTrack().Author}");
            }
            await Program.UpdateUserStatus(ctx.Client, "LISTENING", $"{currentTrack.getTrack().Title} {currentTrack.getTrack().Author}");
            Program.skipped = false;
        }

        [Command("shuffle")]
        public async Task ShuffleCommand(CommandContext ctx)
        {
            if (SlashComms._lavastarted == false)                                                                    // LAVALINK CHECK
            {
                await ctx.Channel.SendMessageAsync("Please execute /start first so I can boot up the music player..");
                return;
            }

            ulong guild = ctx.Guild.Id;

            if (SlashComms._queueDictionary[guild].Count <= 1)
            {
                await ctx.Channel.SendMessageAsync("*proceeds to shake empty box..*");
                return;
            }

            var firstSong = SlashComms._queueDictionary[guild][0];
            var remainingSongs = SlashComms._queueDictionary[guild].Skip(1).ToList();

            var random = new Random();
            var shuffledSongs = remainingSongs.OrderBy(x => random.Next()).ToList();

            SlashComms._queueDictionary[guild] = new List<song> { firstSong };
            SlashComms._queueDictionary[guild].AddRange(shuffledSongs);

            await ctx.Channel.SendMessageAsync("I shaked it :D");
        }

        [Command("skip")]
        public async Task SkipCommand(CommandContext ctx)
        {
            var res = Optimizations.StartUpSequence(ctx);
            if (res != null)
            {
                await ctx.Channel.SendMessageAsync(res);
                return;
            }
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();

            ulong guild = ctx.Guild.Id;

            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (SlashComms._queueDictionary[guild] == null)
            {
                await ctx.Channel.SendMessageAsync("Buddy, there's no song to skip to..");
                return;
            }

            // Check if there are tracks in the queue
            if (SlashComms._queueDictionary[guild].Count < 2)
            {
                var track = SlashComms._queueDictionary[guild][0];
                await ctx.Channel.SendMessageAsync($"Skipped {track.getTrack().Title} {track.getTrack().Author}");
                await Program.UpdateUserStatus(ctx.Client, "IDLE", "bocchi");
                SlashComms._queueDictionary.Remove(guild);
                await conn.StopAsync();
                return;
            }

            try
            {
                var nextTrack = SlashComms._queueDictionary[guild][1];
                var nextTrackTitle = nextTrack.getTrack().Title;
                var track = SlashComms._queueDictionary[guild][0];
                var trackTitle = track.getTrack().Title;
                Program.skipped = true;
                await conn.PlayAsync(nextTrack.getTrack());
                SlashComms._queueDictionary[guild].RemoveAt(0);

                await ctx.Channel.SendMessageAsync($"Skipped {trackTitle} {track.getTrack().Author}..");
                Program.skipped = false;
                if (SlashComms._queueDictionary.Count > 1)
                {
                    Console.WriteLine($"CONCURRENT: {SlashComms._queueDictionary.Count}");
                }
                else
                {
                    Console.WriteLine($"NOW PLAYING: {nextTrackTitle} {nextTrack.getTrack().Author}");
                }
                await Program.UpdateUserStatus(ctx.Client, "LISTENING", $"{nextTrackTitle} {nextTrack.getTrack().Author}");
            }
            catch
            {
                await ctx.Channel.SendMessageAsync("I tried, but there really is no song to skip to..");
            }
        }

        [Command("help")]
        public async Task HelpCommand(CommandContext ctx)
        {
            var embedBuilder = SlashComms.HelpBuilder();

            var embed = embedBuilder.Build();
            await ctx.Channel.SendMessageAsync(embed: embed);
        }

        [Command("start")]
        public async Task StartCommand(CommandContext ctx)
        {
            if(SlashComms._lavastarted)
            {
                await ctx.Channel.SendMessageAsync("It's already running..");
                return;
            }

            try
            {
                await ctx.Channel.SendMessageAsync("Ooh, its starting up..");

                var endpoint = new ConnectionEndpoint
                {
                    Hostname = "127.0.0.1",
                    Port = 2333
                };

                var lavalinkConfig = new LavalinkConfiguration
                {
                    Password = "If there was an",
                    RestEndpoint = endpoint,
                    SocketEndpoint = endpoint,
                    SocketAutoReconnect = false
                };

                var discord = ctx.Client;
                var lavalink = discord.GetLavalink();
                await lavalink.ConnectAsync(lavalinkConfig);

                SlashComms._lavastarted = true;

                var lava = discord.GetLavalink();
                var node = lava.ConnectedNodes.Values.First();

                //Lavalink Event Handlers
                node.PlaybackFinished += Program.PlaybackFinishedHandler;
                node.TrackException += Program.PlaybackErrorHandler;

                await ctx.Channel.SendMessageAsync("Oop, it's running.. there it goes..");
                Console.WriteLine("LAVALINK IS CONNECTED");
            }
            catch
            {
                Console.WriteLine("LAVALINK IS STARTING");
                await Program.StartLava();
                await StartCommand(ctx);

                if (SlashComms._failed == true)
                {
                await ctx.Channel.SendMessageAsync("I encountered a problem, @Sean-san send help please..");
                return;
                }
            }
        }

        [Command("debugstart")]
        public async Task DebugStartCommand(CommandContext ctx)
        {
            if (SlashComms._lavastarted == true)                                               //LAVALINK CHECK
            {
                await ctx.Channel.SendMessageAsync("The music player is already running bro..");
                return;
            }

            await ctx.Channel.SendMessageAsync("Ooh, its starting up..");

            //await Program.StartLava();

            if (SlashComms._failed == true)
            {
                await ctx.Channel.SendMessageAsync("I encountered a problem, @Sean-san send help please..");
                return;
            }
            else
            {
                var endpoint = new ConnectionEndpoint
                {
                    Hostname = "127.0.0.1",
                    Port = 2333
                };

                var lavalinkConfig = new LavalinkConfiguration
                {
                    Password = "If there was an",
                    RestEndpoint = endpoint,
                    SocketEndpoint = endpoint,
                    SocketAutoReconnect = false
                };

                var discord = ctx.Client;
                var lavalink = discord.GetLavalink();
                await lavalink.ConnectAsync(lavalinkConfig);

                SlashComms._lavastarted = true;

                var lava = discord.GetLavalink();
                var node = lava.ConnectedNodes.Values.First();

                //Lavalink Event Handlers
                node.PlaybackFinished += Program.PlaybackFinishedHandler;

                await ctx.Channel.SendMessageAsync("Oop, it's running.. there it goes..");
            }
        }

        [Command("load")]
        public async Task LoadCommand(CommandContext ctx, [RemainingText] string list)
        {
            var res = Optimizations.StartUpSequence(ctx);
            if (res != null)
            {
                await ctx.Channel.SendMessageAsync(res);
                return;
            }

            var songLinks = new List<string>();

            if (Validates.IsYouTubePlaylistLink(list))
            {
                songLinks = await PlayLoader.YoutubeLoaderAsync(list);
            }
            else if(Validates.IsSpotifyPlaylistLink(list))
            {
                songLinks = await SpotifyLoader.GetPlaylistSongLinks(Converts.ExtractSpotifyPlaylistId(list));
            }

            if (songLinks == null || songLinks.Count == 0)
            {
                await ctx.Channel.SendMessageAsync("No song titles found in the playlist.");
                return;
            }

            await ctx.Channel.SendMessageAsync("Loading Playlist..");
            var progressMessage = await ctx.Channel.SendMessageAsync("_ songs queue'd..");
            int songCount = 0;

            foreach (string link in songLinks)
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

                ++songCount;

                await PlayLoader.Enqueue(ctx, link);
                await progressMessage.ModifyAsync($"{songCount} songs queue'd..");
            }

            await progressMessage.ModifyAsync("Playlist loaded.");
        }

        [Command("repeat"), Aliases("loop")]
        public async Task LoopCommand(CommandContext ctx)
        {
            ulong guild = ctx.Guild.Id;
            
            if(Program.loop.Contains(guild))
            {
                Program.loop.Remove(guild);
                await ctx.Channel.SendMessageAsync("Finally, we can move on..");
            }
            else
            {
                Program.loop.Add(guild);
                await ctx.Channel.SendMessageAsync("This song's boutta get stuck in your head..");
            }
        }

        [Command("stop")]
        public async Task StopCommand(CommandContext ctx)
        {
            if (SlashComms._lavastarted == false)                                                                    // LAVALINK CHECK
            {
                await ctx.Channel.SendMessageAsync("Please execute /start first so I can boot up the music player..");
                return;
            }

            ulong guild = ctx.Guild.Id;
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            try
            {
                Program.skipped = true;
                await conn.StopAsync();
                SlashComms._queueDictionary.Remove(guild);
                await ctx.Channel.SendMessageAsync("Bam, dead.");
                Program.skipped = false;
            }
            catch
            {
                await ctx.Channel.SendMessageAsync("Stop it? It's already dead..");
            }
        }

        [Command("debugplayers")]
        public async Task PlayersCommand(CommandContext ctx)
        {
            var num = SlashComms._queueDictionary.Count;
            await ctx.Channel.SendMessageAsync($"There are currently {num} active ongoing queues..");
        }

        [Command("debugkillplayer")]
        public async Task KillPlayerCommand(CommandContext ctx)
        {
            var guild = ctx.Guild.Id;
            SlashComms._queueDictionary.Remove(guild);
            await ctx.Channel.SendMessageAsync($"Queue {guild} reduced to atoms..");
        }

        [Command("forcestop"), Aliases("fs")]
        public async Task ForceStop(CommandContext ctx)
        {
            Program.forcestop = true;

            try
            {
                var msg = await ctx.Channel.SendMessageAsync("Stopping..");

                while (Program.unbroken == true)
                {
                    await Task.Delay(10);
                }

                await msg.ModifyAsync("Stopped.");
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Program.forcestop = false;
        }

        [Command("rest")]
        public async Task RestCommand(CommandContext ctx)
        {
            List<song> holdqueue;
            //STORE
            try
            {
                holdqueue = SlashComms._queueDictionary[ctx.Guild.Id];
            }
            catch
            {
                await ctx.Channel.SendMessageAsync($"{MessageHandler.GetRandomEntry("Nanis")} I don't need that right now..");
                return;
            }

            await ctx.Channel.SendMessageAsync("Alright, let me just fix myself up..");
            Program.skipped = true;
            //LEAVE
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();

            if (ctx.Member.VoiceState == null)
            {
                await ctx.Channel.SendMessageAsync("Unfortunately that's not how it works, you gotta be in the same voice channel..");
                return;
            }

            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);
            ulong guild = ctx.Guild.Id;

            if (conn == null)
            {
                await ctx.Channel.SendMessageAsync($"{MessageHandler.GetRandomEntry("Nanis")} I'm already out..");
            }
            else
            {
                Program.skipped = true;
                await conn.StopAsync();
                Program.skipped = false;
                SlashComms._invited = false;
                SlashComms._queueDictionary.Remove(guild);
                await conn.DisconnectAsync();
            }
            await Task.Delay(1000);
            //JOIN
            try
            {
                var res = Optimizations.StartUpSequence(ctx);
                if (res != null)
                {
                    await ctx.Channel.SendMessageAsync(res);
                    return;
                }
                lava = ctx.Client.GetLavalink();
                node = lava.ConnectedNodes.Values.First();

                await Task.Delay(1000);
                SlashComms._queueDictionary.Add(guild, holdqueue);
                var song = SlashComms._queueDictionary[guild][0];

                var connt = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

                await connt.PlayAsync(song.getTrack());
                await ctx.Channel.SendMessageAsync("That was nice, time to get back to work..");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            Program.skipped = false;
        }

        [Command("save")]
        public async Task SaveCommand(CommandContext ctx, [RemainingText] string videoUrl)
        {
            if (videoUrl == null)
            {
                var re = MessageHandler.GetRandomEntry("Nanis");
                await ctx.Channel.SendMessageAsync($"{re}.. provide a youtube link please..");
                return;
            }
            else if (videoUrl.Contains("results?search_query"))
            {
                await ctx.Channel.SendMessageAsync($"{MessageHandler.GetRandomEntry("Nanis")}.. thats the wrong URL..");
                return;
            }
            else if (Validates.IsYouTubePlaylistLink(videoUrl))
            {
                var songTitles = await PlayLoader.YoutubeLoaderAsync(videoUrl);

                if (songTitles == null || songTitles.Count == 0)
                {
                    await ctx.Channel.SendMessageAsync("No song titles found in the playlist.");
                    return;
                }

                if (!ctx.User.Username.Equals("_arimasu", StringComparison.OrdinalIgnoreCase))
                {
                    await ctx.Channel.SendMessageAsync("Only _Arimasu can save playlists, if this was a mistake please provide a non-playlist link.");
                    return;
                }

                await ctx.Channel.SendMessageAsync("Oh it's you.. Do you really want to save this playlist?");

                DSharpPlus.AsyncEvents.AsyncEventHandler<DSharpPlus.DiscordClient, DSharpPlus.EventArgs.MessageCreateEventArgs> messageHandler = null;

                messageHandler = async (s, e) =>
                {
                    // Check if the message is in the same channel as the command
                    if (e.Channel.Id == ctx.Channel.Id)
                    {
                        if (e.Author.Id == ctx.User.Id)
                        {
                            string userResponse = e.Message.Content;
                            List<string> ye = new List<string>();

                            var xmlFilePath = "data.xml";

                            if (File.Exists(xmlFilePath))
                            {
                                XDocument xmlDoc = XDocument.Load(xmlFilePath);

                                var entries = xmlDoc.Descendants("category")
                                                    .FirstOrDefault(e => e.Attribute("name")?.Value == "Affirmatives")
                                                    ?.Elements("entry")
                                                    .Select(e => e.Value)
                                                    .ToList();

                                if (entries != null && entries.Count > 0)
                                {
                                    ye = entries;
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

                            // Perform actions based on the user response
                            if (ye.Contains(userResponse, StringComparer.OrdinalIgnoreCase))
                            {
                                await ctx.Channel.SendMessageAsync("Saving Playlist..");
                                var progressMessage = await ctx.Channel.SendMessageAsync("_ songs saved..");
                                int songCount = 0;

                                foreach (string deets in songTitles)
                                {
                                    Program.unbroken = true;

                                    if (Program.forcestop == true)
                                    {
                                        Program.unbroken = false;
                                        break;
                                    }

                                    ++songCount;

                                    await PlayLoader.Save(ctx, deets);
                                    await progressMessage.ModifyAsync($"{songCount} songs saved..");
                                    Program.discord.MessageCreated -= messageHandler;
                                }

                                await ctx.Channel.SendMessageAsync("Playlist saved.");
                            }
                            else
                            {
                                await ctx.Channel.SendMessageAsync("Maybe next time then..");
                                Program.discord.MessageCreated -= messageHandler;
                            }
                        }
                    }
                };

                Program.discord.MessageCreated += messageHandler;
                return;
            }
            else if (Validates.IsYoutubeLink(videoUrl))
            {
                await ctx.Channel.SendMessageAsync("Alr, wait a moment..");
            }
            else if (Validates.IsSpotifyLink(videoUrl))
            {
                await ctx.Channel.SendMessageAsync("That's a spotify link.. provide a youtube link please..");
                return;
            }
            else if (Validates.IsSpotifyPlaylistLink(videoUrl))
            {
                await ctx.Channel.SendMessageAsync("That's a spotify playlist link.. provide a youtube link please..");
                return;
            }
            else
            {
                var re = MessageHandler.GetRandomEntry("Nanis");
                await ctx.Channel.SendMessageAsync($"{re}.. provide a youtube link please..");
                return;
            }

            var youtube = new YoutubeClient();

            var streamInfoSet = await youtube.Videos.Streams.GetManifestAsync(videoUrl);
            var streamInfo = streamInfoSet.GetAudioOnlyStreams().GetWithHighestBitrate();
            string outputFilePath = null;
            string title;
            try
            {
                if (streamInfo != null)
                {
                    using (var audioStream = await youtube.Videos.Streams.GetAsync(streamInfo))
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await audioStream.CopyToAsync(memoryStream);
                            var audioBytes = memoryStream.ToArray();

                            title = await PlayLoader.GetVideoTitleAsync(videoUrl);
                            if (title.Contains("<") || title.Contains(">") || title.Contains(@"\") || title.Contains("/") || title.Contains(":") || title.Contains("?") || title.Contains("*") || title.Contains(@"|") || title.Contains("\""))
                            {
                                // Define a list of characters to remove
                                char[] charsToRemove = { '<', '>', '\\', '/', ':', '?', '*', '|', '"' };

                                // Remove each character from the string
                                foreach (char c in charsToRemove)
                                {
                                    title = title.Replace(c.ToString(), "");
                                }
                            }

                            outputFilePath = Path.Combine("songs", $"{title}" + ".mp3");
                            Console.WriteLine(outputFilePath);

                            File.WriteAllBytes(outputFilePath, audioBytes);

                            if (outputFilePath != null)
                            {
                                try
                                {
                                    await save.SendAsync(ctx.Channel.Id, outputFilePath, title);
                                    Console.WriteLine(ctx.Channel.Id);
                                }
                                catch (Exception ex)
                                {
                                    await ctx.Channel.SendMessageAsync($"I hit a wall, the logs say: {ex.Message}");
                                }
                            }
                        }
                    }
                }  
            }
            catch(Exception ex)
            {
                await ctx.Channel.SendMessageAsync($"I hit a wall, the logs say: {ex.Message}");
            }
        }

        [Command("freeplay")]
        public async Task FreeplayCommand(CommandContext ctx)
        {
            ulong guild = ctx.Guild.Id;

            if (Program.freeplaylist.Contains(guild))
            {
                Program.freeplaylist.Remove(guild);
                Program.reconlist.Remove(guild);
                await ctx.Channel.SendMessageAsync("I hope I did well, you take the lead now..");
            }
            else
            {
                Program.freeplaylist.Add(guild);
                await ctx.Channel.SendMessageAsync("I'll try my best recommending new songs after this one..");
            }
        }

        [Command("volume")]
        public async Task VolumeCommand(CommandContext ctx, [RemainingText] string volume)
        {
            var res = Optimizations.StartUpSequence(ctx);
            if (res != null)
            {
                await ctx.Channel.SendMessageAsync(res);
                return;
            }
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            

            if (!int.TryParse(volume, out int level))
            {
                await ctx.Channel.SendMessageAsync("That is not a valid number.");
                return;
            }

            if (level >= 0 && level <= 100)
            {
                var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);
                await conn.SetVolumeAsync(level);

                await ctx.Channel.SendMessageAsync("I turned the volume knob to " + level);

                //if(!Program.Volumes.ContainsKey(ctx.Guild.Id))  //Default setter
                //{
                //    Program.Volumes.Add(ctx.Guild.Id, level);

                //    using (StreamWriter writer = File.AppendText("volume.txt"))
                //    {
                //        writer.WriteLine($"{ctx.Guild.Id} {level}");
                //    }
                //}
            }
            else
            {
                await ctx.Channel.SendMessageAsync("Invalid track number.");
                return;
            }
        }

    }
}
