using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alice.Responses;
using DSharpPlus.Net;
using Alice_Module.Loaders;
using Discord;

namespace Alice.Commands
{
    public class Comms : BaseCommandModule
    {
        [Command("join")]
        public async Task JoinCommand(CommandContext ctx)
        {
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();

            if (SlashComms._lavastarted == false)                                                                    // LAVALINK CHECK
            {
                await ctx.Channel.SendMessageAsync("Please execute /start first so I can boot up the music player..");
                return;
            }

            if (ctx.Member.VoiceState == null)                                                                     // VOICECHANNEL CHECK
            {
                await ctx.Channel.SendMessageAsync("Brother, you're not even in a voice channel yet..");
                return;
            }

            var cont = node.GetGuildConnection(ctx.Member.VoiceState.Guild);                                     // JOIN COMMAND

            if (cont == null)
            {
                var channel = ctx.Member.VoiceState.Channel;

                if (channel.Type is DSharpPlus.ChannelType.Voice)
                {
                    try
                    {
                        SlashComms._invited = true;
                        await node.ConnectAsync(channel);
                        Console.WriteLine("JOINED");
                        SlashComms._ready = true;
                        await ctx.Channel.SendMessageAsync("Thanks..");
                    }
                    catch
                    {
                        await ctx.Channel.SendMessageAsync("Brother, you're not even in a voice channel yet..");
                    }
                }
            }
            else
            {
                await ctx.Channel.SendMessageAsync("I'm already here tho..");
            }

        }

        [Command("playskip"), Aliases("ps")]
        public async Task PlaySkipCommand(CommandContext ctx, [RemainingText] string search)
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
                await ctx.Channel.SendMessageAsync("Nice try prankster but that's not how it works, you gotta be in the same voice channel..");
                return;
            }

            var cont = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (cont == null)
            {
                var channel = ctx.Member.VoiceState.Channel;

                if (channel.Type is DSharpPlus.ChannelType.Voice)
                {
                    try
                    {
                        SlashComms._invited = false;
                        await node.ConnectAsync(channel);
                        Console.WriteLine("JOINED");
                        SlashComms._ready = true;
                    }
                    catch
                    {
                        await ctx.Channel.SendMessageAsync("Brother, you're not even in a voice channel yet..");
                    }
                }
            }

            if (!lava.ConnectedNodes.Any())
            {
                await ctx.Channel.SendMessageAsync("Lavalink not connected.");
                return;
            }

            // Check whether the bot is in a voice channel
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.Channel.SendMessageAsync("Brother, I'm not even in a voice channel yet..");
                return;
            }

            LavalinkTrack track;
            ulong guild = ctx.Guild.Id;

            if (Validates.IsYoutubeLink(search))
            {
                search = Converts.ConvertToShortenedUrl(search);

                var loadResult = await node.Rest.GetTracksAsync(search, LavalinkSearchType.Plain);

                if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
                {
                    await ctx.Channel.SendMessageAsync($"Failed to look for {search}");
                    return;
                }

                track = loadResult.Tracks.First();
            }
            else if (Validates.IsSpotifyLink(search))
            {
                var loadResult = await node.Rest.GetTracksAsync(search, LavalinkSearchType.Plain);

                if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
                {
                    await ctx.Channel.SendMessageAsync($"Failed to look for {search}");
                    return;
                }

                track = loadResult.Tracks.First();
            }
            else if (Validates.IsSpotifyPlaylistLink(search))
            {
                await ctx.Channel.SendMessageAsync("That's a playlist link.. provide a song link please..");
                return;
            }
            else if (Validates.IsYouTubePlaylistLink(search))
            {
                await ctx.Channel.SendMessageAsync("That's a playlist link.. provide a song link please..");
                return;
            }
            else
            {
                var loadResult = await node.Rest.GetTracksAsync(search, LavalinkSearchType.Youtube);

                if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
                {
                    await ctx.Channel.SendMessageAsync($"Failed to look for {search}");
                    return;
                }

                track = loadResult.Tracks.First();
            }

            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            try
            {
                Program.skipped = true;
                if (SlashComms._queueDictionary.ContainsKey(guild))
                {
                    SlashComms._queueDictionary[guild].Insert(0, track);
                    SlashComms._queueDictionary[guild].RemoveAt(1);
                    await conn.PlayAsync(track);

                    await ctx.Channel.SendMessageAsync($"Found it. Now Playing: {track.Title}");
                    Program.skipped = false;
                    Console.WriteLine("PLAYER IS PLAYING");
                    Console.WriteLine($"NOW PLAYING: {track.Title}");
                    await Program.UpdateUserStatus(ctx.Client, "LISTENING", track.Title);
                }
                else
                {
                    SlashComms._queueDictionary.Add(guild, new List<LavalinkTrack>());
                    await Task.Delay(100);
                    SlashComms._queueDictionary[guild].Insert(0, track);
                    SlashComms._queueDictionary[guild].RemoveAt(1);
                    await conn.PlayAsync(track);

                    await ctx.Channel.SendMessageAsync($"Found it. Now Playing: {track.Title}");
                    Program.skipped = false;
                    Console.WriteLine("PLAYER IS PLAYING");
                    Console.WriteLine($"NOW PLAYING: {track.Title}");
                    await Program.UpdateUserStatus(ctx.Client, "LISTENING", track.Title);
                }
            }
            catch
            {
                await ctx.Channel.SendMessageAsync($"{track.Title} failed to play");
            }
        }

        [Command("play"), Aliases("p")]
        public async Task PlayCommand(CommandContext ctx, [RemainingText] string search)
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
                await ctx.Channel.SendMessageAsync("Nice try prankster but that's not how it works, you gotta be in the same voice channel as the player..");
                return;
            }

            var cont = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (cont == null)
            {
                var channel = ctx.Member.VoiceState.Channel;

                if (channel.Type is DSharpPlus.ChannelType.Voice)
                {
                    try
                    {
                        SlashComms._invited = false;
                        await node.ConnectAsync(channel);
                        Console.WriteLine("JOINED");
                        SlashComms._ready = true;
                    }
                    catch
                    {
                        await ctx.Channel.SendMessageAsync("Brother, you're not even in a voice channel yet..");
                    }
                }
            }

            if (!lava.ConnectedNodes.Any())
            {
                await ctx.Channel.SendMessageAsync("Lavalink not connected.");
                return;
            }

            // Check whether the bot is in a voice channel
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.Channel.SendMessageAsync("The bot is not in a voice channel.");
                return;
                
            }

            LavalinkTrack track;
            ulong guild = ctx.Guild.Id;

            if (Validates.IsYoutubeLink(search))
            {
                search = Converts.ConvertToShortenedUrl(search);

                var loadResult = await node.Rest.GetTracksAsync(search, LavalinkSearchType.Plain);

                if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
                {
                    await ctx.Channel.SendMessageAsync($"Failed to look for {search}");
                    return;
                }

                track = loadResult.Tracks.FirstOrDefault();
            }
            else if (Validates.IsSpotifyLink(search))
            {
                var loadResult = await node.Rest.GetTracksAsync(search, LavalinkSearchType.Plain);

                if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
                {
                    await ctx.Channel.SendMessageAsync($"Failed to look for {search}");
                    return;
                }

                track = loadResult.Tracks.FirstOrDefault();
            }
            else if (Validates.IsSpotifyPlaylistLink(search))
            {
                await ctx.Channel.SendMessageAsync("That's a playlist link.. provide a song link please..");
                return;
            }
            else if (Validates.IsYouTubePlaylistLink(search))
            {
                await ctx.Channel.SendMessageAsync("That's a playlist link.. provide a song link please..");
                return;
            }
            else
            {
                var loadResult = await node.Rest.GetTracksAsync(search, LavalinkSearchType.Youtube);

                if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
                {
                    await ctx.Channel.SendMessageAsync($"Failed to look for {search}");
                    return;
                }

                track = loadResult.Tracks.First();
            }
            
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            try
            {
                if (conn.CurrentState.CurrentTrack == null)
                {

                    if (SlashComms._queueDictionary.ContainsKey(guild))
                    {
                        SlashComms._queueDictionary[guild].Add(track);
                    }
                    else
                    {
                        SlashComms._queueDictionary.Add(guild, new List<LavalinkTrack>());
                        await Task.Delay(100);
                        SlashComms._queueDictionary[guild].Add(track);
                    }
                    Program.skipped = true;
                    await conn.PlayAsync(track);

                    await ctx.Channel.SendMessageAsync($"Now Playing: {track.Title}");
                    Program.skipped = false;

                    Console.WriteLine("PLAYER IS PLAYING");
                    Console.WriteLine($"NOW PLAYING: {track.Title}");
                    await Program.UpdateUserStatus(ctx.Client, "LISTENING", track.Title);
                }
                else
                {
                    if (SlashComms._queueDictionary[guild].Count >= SlashComms.MaxQueueSize)
                    {
                        await ctx.Channel.SendMessageAsync($"Max queue length was set to {SlashComms.MaxQueueSize}, wait for songs to finish");
                    }
                    else
                    {
                        if (SlashComms._queueDictionary.ContainsKey(guild))
                        {
                            SlashComms._queueDictionary[guild].Add(track);
                        }

                        await ctx.Channel.SendMessageAsync($"Added to Queue: {track.Title}");
                    }
                }
            }
            catch
            {
                await ctx.Channel.SendMessageAsync($"{track.Title} failed to play");
            }
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
                var trackInfo = $"{currentTrack.Title} {currentTrack.Length}";
                await ctx.Channel.SendMessageAsync($"Now Playing: {currentTrack.Title} {currentTrack.Length}");
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

        [Command("stop")]
        public async Task StopCommand(CommandContext ctx)
        {
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);
            ulong guild = ctx.Guild.Id;

            if (SlashComms._lavastarted == false)                                                                    // LAVALINK CHECK
            {
                await ctx.Channel.SendMessageAsync("Please execute /start first so I can boot up the music player..");
                return;
            }

            await ctx.Channel.SendMessageAsync("A gun was fired at the player, the queue is in pieces..");
            Console.WriteLine("LAVALINK IS DISCONNECTED");
            SlashComms._queueDictionary[guild].Clear();

            if (Program.lavalinkProcess != null && !Program.lavalinkProcess.HasExited)
            {
                Program.lavalinkProcess.Kill();
                Program.lavalinkProcess.CloseMainWindow();
                Program.lavalinkProcess.Close();
                SlashComms._lavastarted = false;
            }
        }

        [Command("queue"), Aliases("q")]
        public async Task QueueCommand(CommandContext ctx)
        {
            ulong guild = ctx.Guild.Id;
            
            if (SlashComms._lavastarted == false)                                                                    // LAVALINK CHECK
            {
                await ctx.Channel.SendMessageAsync("Please execute /start first so I can boot up the music player..");
                return;
            }

            if (SlashComms._queueDictionary[guild].Count == 0)
            {
                await ctx.Channel.SendMessageAsync("The queue list is blank.");
            }
            else
            {
                var queueContent = string.Join("\n", SlashComms._queueDictionary[guild].Select((track, index) =>
                {
                    var prefix = (index == 0) ? "【Now Playing】 " : string.Empty;
                    return $"{index + 1}. {prefix}{track.Title}";
                }));
                await ctx.Channel.SendMessageAsync($"Look at all these songs:\n{queueContent}");
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
                    SlashComms._invited = false;
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
                    await ctx.Channel.SendMessageAsync($"Removed {tune.Title}");
                    await Program.UpdateUserStatus(ctx.Client, "IDLE", "bocchi");
                    SlashComms._queueDictionary.Remove(guild);
                    await conn.StopAsync();
                    return;
                }

                var nextTrack = SlashComms._queueDictionary[guild][1];
                var nextTrackTitle = nextTrack.Title;
                var track = SlashComms._queueDictionary[guild][0];
                var trackTitle = track.Title;
                Program.skipped = true;
                await conn.PlayAsync(nextTrack);
                SlashComms._queueDictionary[guild].RemoveAt(0);

                await ctx.Channel.SendMessageAsync($"Eliminated {trackTitle}");
                Console.WriteLine($"NOW PLAYING: {nextTrackTitle}");
                await Program.UpdateUserStatus(ctx.Client, "LISTENING", track.Title);
                Program.skipped = false;
                return;
            }

            var song = SlashComms._queueDictionary[guild][trackNum - 1];
            var songTitle = song.Title;
            SlashComms._queueDictionary[guild].RemoveAt(trackNum - 1);
            await ctx.Channel.SendMessageAsync($"Eliminated {songTitle}..");

        }

        [Command("skipto")]
        public async Task SkipToCommand(CommandContext ctx, [RemainingText] string Num)
        {
            if (SlashComms._lavastarted == false)                                                                    // LAVALINK CHECK
            {
                await ctx.Channel.SendMessageAsync("Please execute /start first so I can boot up the music player..");
                return;
            }

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

            if (ctx.Member.VoiceState == null)
            {
                await ctx.Channel.SendMessageAsync("Nice try but that's not how it works, you gotta be in the same voice channel..");
                return;
            }

            // Remove tracks from the beginning of the queue up to the desired track (exclusive)
            var tracksToRemove = SlashComms._queueDictionary[guild].GetRange(0, trackNum - 1);
            SlashComms._queueDictionary[guild].RemoveRange(0, trackNum - 1);

            Program.skipped = true;
            var currentTrack = SlashComms._queueDictionary[guild][0];
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);
            await conn.PlayAsync(currentTrack);

            await ctx.Channel.SendMessageAsync($"Removed {tracksToRemove.Count} tracks from the queue.. Now Playing {currentTrack.Title}");
            Console.WriteLine($"NOW PLAYING: {currentTrack.Title}");
            await Program.UpdateUserStatus(ctx.Client, "LISTENING", currentTrack.Title);
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

            SlashComms._queueDictionary[guild] = new List<LavalinkTrack> { firstSong };
            SlashComms._queueDictionary[guild].AddRange(shuffledSongs);

            await ctx.Channel.SendMessageAsync("I shaked it :D");
        }

        [Command("skip")]
        public async Task SkipCommand(CommandContext ctx)
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

            // Check if there are tracks in the queue
            if (SlashComms._queueDictionary[guild].Count < 2)
            {
                var track = SlashComms._queueDictionary[guild][0];
                await ctx.Channel.SendMessageAsync($"Skipped {track.Title}");
                await Program.UpdateUserStatus(ctx.Client, "IDLE", "bocchi");
                SlashComms._queueDictionary.Remove(guild);
                await conn.StopAsync();
                return;
            }

            // Skip to the next track

            if (ctx.Member.VoiceState == null)
            {
                await ctx.Channel.SendMessageAsync("Nice try but that's not how it works, you gotta be in the same voice channel..");
                return;
            }

            // Check if there is a current track playing
            if (conn.CurrentState.CurrentTrack != null)
            {
                var nextTrack = SlashComms._queueDictionary[guild][1];
                var nextTrackTitle = nextTrack.Title;
                var track = SlashComms._queueDictionary[guild][0];
                var trackTitle = track.Title;
                Program.skipped = true;
                await conn.PlayAsync(nextTrack);
                SlashComms._queueDictionary[guild].RemoveAt(0);

                await ctx.Channel.SendMessageAsync($"Skipped {trackTitle}..");
                Program.skipped = false;
                Console.WriteLine($"NOW PLAYING: {nextTrackTitle}");
                await Program.UpdateUserStatus(ctx.Client, "LISTENING", nextTrackTitle);
            }
            else
            {
                await ctx.Channel.SendMessageAsync("Buddy, there's no song to skip to..");
            }
        }

        [Command("help")]
        public async Task HelpCommand(CommandContext ctx)
        {
            var embedBuilder = new DiscordEmbedBuilder()
                .WithTitle("Command List")
                .AddField("Prefix", "You can either use (/) or (bocchi!) to trigger these commands..")
                .WithDescription("Here are the things you can tell bocchi:")
                .AddField("\u200B", "\u200B")
                .AddField("start", "Starts up the player and enables the music related commands")
                .AddField("\u200B", "\u200B")
                .AddField("join", "Joins the user's current voice channel.")
                .AddField("byebye", "Leaves the user's current voice channel.")
                .AddField("play [song]", "Plays the specified song.")
                .AddField("playskip [song]", "Plays a song immediately")
                .AddField("load [playlist]", "Add a playlist to the queue")
                .AddField("stop", "Stops the music playback and clears the queue.")
                .AddField("skip", "Skips the current song.")
                .AddField("skipto [number]", "Skips the specified song number.")
                .AddField("remove [number]", "Removes an entry from the queue")
                .AddField("np", "Shows currently playing song.")
                .AddField("queue", "Shows song queue.")
                .AddField("resume", "Resumes the current song.")
                .AddField("pause", "Pauses the current song.")
                .AddField("help", "Displays this list.")
                .AddField("\u200B", "\u200B")
                .AddField("Others", "Sometimes specific words can trigger a bocchi response..")
                .AddField("Just be nice to her..", "She's trying her best..")
                .WithColor(new DiscordColor("#ffd8e1"));

            var embed = embedBuilder.Build();
            await ctx.Channel.SendMessageAsync(embed: embed);
        }

        [Command("start")]
        public async Task StartCommand(CommandContext ctx)
        {
            if (SlashComms._lavastarted == true)                                               //LAVALINK CHECK
            {
                await ctx.Channel.SendMessageAsync("The music player is already running bro..");
                return;
            }

            await ctx.Channel.SendMessageAsync("Ooh, its starting up..");
            Console.WriteLine("LAVALINK IS STARTING");

            await Program.StartLava();

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
                node.TrackException += Program.PlaybackErrorHandler;

                await ctx.Channel.SendMessageAsync("Oop, it's running.. there it goes..");
                Console.WriteLine("LAVALINK IS CONNECTED");
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

            if (Validates.IsYouTubePlaylistLink(list))
            {
                var songTitles = new List<string>();
                songTitles = await PlayLoader.YoutubeLoaderAsync(list);

                if (songTitles == null || songTitles.Count == 0)
                {
                    await ctx.Channel.SendMessageAsync("No song titles found in the playlist.");
                    return;
                }

                await ctx.Channel.SendMessageAsync("Loading Playlist..");
                foreach (string title in songTitles)
                {
                    if (PlayLoader._queuefull == true)
                    {
                        PlayLoader._queuefull = false;
                        break;
                    }

                    await PlayLoader.Enqueue(ctx, title);
                }

                await ctx.Channel.SendMessageAsync("Playlist loaded.");
                return;
            }
            else if (Validates.IsSpotifyPlaylistLink(list))
            {
                string playlistId = Converts.ExtractSpotifyPlaylistId(list);

                var songslinks = await SpotifyLoader.GetPlaylistSongLinks(playlistId);
                Console.WriteLine("Ohh its spotify");

                if (songslinks == null || songslinks.Count == 0)
                {
                    await ctx.Channel.SendMessageAsync("No song titles found in the playlist.");
                    return;
                }

                await ctx.Channel.SendMessageAsync("Loading Playlist..");
                foreach (var link in songslinks)
                {
                    
                    if (PlayLoader._queuefull == true)
                    {
                        PlayLoader._queuefull = false;
                        break;
                    }

                    await PlayLoader.Enqueue(ctx, link);
                }

                await ctx.Channel.SendMessageAsync("Playlist loaded.");
                return;
            }
            else
            {
                await ctx.Channel.SendMessageAsync("Invalid playlist link.");
                return;
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
    }
}
