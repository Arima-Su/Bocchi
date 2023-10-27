using Alice.Responses;
using Alice_Module.Loaders;
using AngleSharp.Text;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alice.Commands
{
    public class SlashComms : ApplicationCommandModule
    {
        public static Dictionary<ulong, List<LavalinkTrack>> _queueDictionary = new Dictionary<ulong, List<LavalinkTrack>>();
        public static int MaxQueueSize = 500; // Maximum number of songs allowed in the queue
        public static bool _lavastarted = false;
        public static bool _playerIsPaused = false;
        public static bool _invited = true;
        public static bool _failed = false;
        public static bool _ready = false;

        [SlashCommand("join", "Invite Bocchi to your voice channel, just don't try anything weird..")]
        public async Task JoinCommand(InteractionContext ctx)
        {
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();

            if (_lavastarted == false)                                                                    // LAVALINK CHECK
            {
                var ephemeralMessage = new DiscordInteractionResponseBuilder()
                    .WithContent("Please execute /start first so I can boot up the music player..")
                    .AsEphemeral(true);

                await ctx.CreateResponseAsync(ephemeralMessage);
                return;
            }

            if (ctx.Member.VoiceState == null)                                                           // VOICE CHANNEL CHECK
            {
                var ephemeralMessage = new DiscordInteractionResponseBuilder()
                    .WithContent("Brother, you're not even in a voice channel yet..")
                    .AsEphemeral(true);

                await ctx.CreateResponseAsync(ephemeralMessage);
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
                        _invited = true;
                        _ready = true;
                        await node.ConnectAsync(channel);
                        //Console.WriteLine("JOINED");
                        var ephemeralMessage = new DiscordInteractionResponseBuilder()
                        .WithContent("Thanks..")
                        .AsEphemeral(true); // Set ephemeral to true to make the message visible only to the user

                        await ctx.CreateResponseAsync(ephemeralMessage);
                    }
                    catch
                    {
                        var ephemeralMessage = new DiscordInteractionResponseBuilder()
                        .WithContent("Brother, you're not even in a voice channel yet..")
                        .AsEphemeral(true); // Set ephemeral to true to make the message visible only to the user

                        await ctx.CreateResponseAsync(ephemeralMessage);
                    }
                }
            }
            else
            {
                var ephemeralMessage = new DiscordInteractionResponseBuilder()
                        .WithContent("I'm already here tho..")
                        .AsEphemeral(true); // Set ephemeral to true to make the message visible only to the user

                await ctx.CreateResponseAsync(ephemeralMessage);
            }
        }

        [SlashCommand("playskip", "Screw queues, play your song immediately..")]
        public async Task PlaySkipCommand(InteractionContext ctx, [Option("song", "Just put the title of the song you want..")] string search)
        {
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            await ctx.DeferAsync(ephemeral: true);

            if (_lavastarted == false)                                                                    // LAVALINK CHECK
            {
                var ephemeralMessage = new DiscordFollowupMessageBuilder()
                    .WithContent("Please execute /start first so I can boot up the music player..")
                    .AsEphemeral(true);

                await ctx.FollowUpAsync(ephemeralMessage);
                return;
            }

            if (ctx.Member.VoiceState == null)                                                           // VOICE CHANNEL CHECK
            {
                var ephemeralMessage = new DiscordFollowupMessageBuilder()
                    .WithContent("Nice try but that's not how it works, you gotta be in the same voice channel..")
                    .AsEphemeral(true);

                await ctx.FollowUpAsync(ephemeralMessage);
                return;
            }

            var cont = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (cont == null)                                                                            // AUTO JOIN
            {
                var channel = ctx.Member.VoiceState.Channel;

                if (channel.Type is DSharpPlus.ChannelType.Voice)
                {
                    try
                    {
                        _invited = false;
                        await node.ConnectAsync(channel);
                        //Console.WriteLine("JOINED");
                        _ready = true;
                    }
                    catch
                    {
                        var ephemeralMessage = new DiscordFollowupMessageBuilder()
                        .WithContent($"Brother, you're not even in a voice channel yet..")
                        .AsEphemeral(true);

                        await ctx.FollowUpAsync(ephemeralMessage);
                    }
                }
            }

            if (!lava.ConnectedNodes.Any())                                                             // LAVALINK VERIFY
            {
                var ephemeralMessage = new DiscordFollowupMessageBuilder()
                    .WithContent("Lavalink not connected.")
                    .AsEphemeral(true);

                await ctx.FollowUpAsync(ephemeralMessage);
                return;
            }

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)                 // BOT VOICESTATE VERIFY
            {
                var ephemeralMessage = new DiscordFollowupMessageBuilder()
                    .WithContent("Brother, I'm not even in a voice channel yet..")
                    .AsEphemeral(true);

                await ctx.FollowUpAsync(ephemeralMessage);
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
                    var ephemeralMessage3 = new DiscordFollowupMessageBuilder()
                    .WithContent($"Failed to look for {search}")
                    .AsEphemeral(true);

                    await ctx.FollowUpAsync(ephemeralMessage3);
                    return;
                }

                track = loadResult.Tracks.First();
            }
            else if (Validates.IsSpotifyLink(search))
            {
                var loadResult = await node.Rest.GetTracksAsync(search, LavalinkSearchType.Plain);

                if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
                {
                    var ephemeralMessage3 = new DiscordFollowupMessageBuilder()
                    .WithContent($"Failed to look for {search}")
                    .AsEphemeral(true);

                    await ctx.FollowUpAsync(ephemeralMessage3);
                    return;
                }

                track = loadResult.Tracks.First();
            }
            else if (Validates.IsSpotifyPlaylistLink(search))
            {
                var ephemeralMessage3 = new DiscordFollowupMessageBuilder()
                    .WithContent("That's a playlist link.. provide a song link please..")
                    .AsEphemeral(true);

                await ctx.FollowUpAsync(ephemeralMessage3);
                return;
            }
            else if (Validates.IsYouTubePlaylistLink(search))
            {
                var ephemeralMessage3 = new DiscordFollowupMessageBuilder()
                    .WithContent("That's a playlist link.. provide a song link please..")
                    .AsEphemeral(true);

                await ctx.FollowUpAsync(ephemeralMessage3);
                return;
            }
            else
            {
                var loadResult = await node.Rest.GetTracksAsync(search, LavalinkSearchType.Youtube);

                if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
                {
                    var ephemeralMessage3 = new DiscordFollowupMessageBuilder()
                    .WithContent($"Failed to look for {search}")
                    .AsEphemeral(true);

                    await ctx.FollowUpAsync(ephemeralMessage3);
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
                    _queueDictionary[guild].Insert(0, track);
                    _queueDictionary[guild].RemoveAt(1);
                    await conn.PlayAsync(track);
                    var ephemeralMessage = new DiscordFollowupMessageBuilder()
                        .WithContent($"Found it. Now Playing: {track.Title} {track.Author}")
                        .AsEphemeral(true);

                    await ctx.FollowUpAsync(ephemeralMessage);
                    Program.skipped = false;
                    Console.WriteLine("PLAYER IS PLAYING");
                    if (SlashComms._queueDictionary.Count > 1)
                    {
                        Console.WriteLine($"CONCURRENT: {SlashComms._queueDictionary.Count}");
                    }
                    else
                    {
                        Console.WriteLine($"NOW PLAYING: {track.Title} {track.Author}");
                    }
                    await Program.UpdateUserStatus(ctx.Client, "LISTENING", $"{track.Title} {track.Author}");
                }
                else
                {
                    SlashComms._queueDictionary.Add(guild, new List<LavalinkTrack>());
                    await Task.Delay(100);
                    _queueDictionary[guild].Insert(0, track);
                    _queueDictionary[guild].RemoveAt(1);
                    await conn.PlayAsync(track);
                    var ephemeralMessage = new DiscordFollowupMessageBuilder()
                        .WithContent($"Found it. Now Playing: {track.Title} {track.Author}")
                        .AsEphemeral(true);

                    await ctx.FollowUpAsync(ephemeralMessage);
                    Program.skipped = false;
                    Console.WriteLine("PLAYER IS PLAYING");
                    if (SlashComms._queueDictionary.Count > 1)
                    {
                        Console.WriteLine($"CONCURRENT: {SlashComms._queueDictionary.Count}");
                    }
                    else
                    {
                        Console.WriteLine($"NOW PLAYING: {track.Title} {track.Author}");
                    }
                    await Program.UpdateUserStatus(ctx.Client, "LISTENING", $"{track.Title} {track.Author}");
                }
            }
            catch
            {
                var ephemeralMessage4 = new DiscordFollowupMessageBuilder()
                    .WithContent($"{track.Title} {track.Author} failed to play")
                    .AsEphemeral(true);

                await ctx.FollowUpAsync(ephemeralMessage4);

                var ephemeralMessage41 = new DiscordFollowupMessageBuilder()
                    .WithContent("I'm gonna try that again..")
                    .AsEphemeral(true);

                await ctx.FollowUpAsync(ephemeralMessage41);

                await PlaySkipCommand(ctx, search);
            }
        }

        [SlashCommand("play", "Abider of social norms, put your song in the waiting queue..")]
        public async Task PlayCommand(InteractionContext ctx, [Option("song", "Just put the title of the song you want..")] string search)
        {
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            await ctx.DeferAsync(ephemeral: true);

            if (_lavastarted == false)                                                                    // LAVALINK CHECK
            {
                var ephemeralMessage = new DiscordFollowupMessageBuilder()
                    .WithContent("Please execute /start first so I can boot up the music player..")
                    .AsEphemeral(true);

                await ctx.FollowUpAsync(ephemeralMessage);
                return;
            }

            if (ctx.Member.VoiceState == null)                                                            // VOICE CHANNEL CHECK
            {
                var ephemeralMessage = new DiscordFollowupMessageBuilder()
                    .WithContent("Nice try prankster but that's not how it works, you gotta be in the same voice channel as the player..")
                    .AsEphemeral(true);

                await ctx.FollowUpAsync(ephemeralMessage);
                return;
            }

            var cont = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (cont == null)                                                                            // AUTO JOIN
            {
                var channel = ctx.Member.VoiceState.Channel;

                if (channel.Type is DSharpPlus.ChannelType.Voice)
                {
                    try
                    {
                        _invited = false;
                        await node.ConnectAsync(channel);
                        //Console.WriteLine("JOINED");
                        _ready = true;
                        Console.WriteLine("Auto Connected");
                    }
                    catch
                    {
                        var ephemeralMessage = new DiscordFollowupMessageBuilder()
                        .WithContent($"Brother, you're not even in a voice channel yet..")
                        .AsEphemeral(true);

                        await ctx.FollowUpAsync(ephemeralMessage);
                    }
                }
            }

            if (!lava.ConnectedNodes.Any())                                                             // LAVALINK VERIFY
            {
                var ephemeralMessage2 = new DiscordFollowupMessageBuilder()
                    .WithContent("Lavalink not connected.")
                    .AsEphemeral(true);

                await ctx.FollowUpAsync(ephemeralMessage2);
                return;
            }

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)             // BOT VOICESTATE VERIFY
            {
                var ephemeralMessage3 = new DiscordFollowupMessageBuilder()
                    .WithContent("The bot is not in a voice channel.")
                    .AsEphemeral(true);

                await ctx.FollowUpAsync(ephemeralMessage3);
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
                    var ephemeralMessage3 = new DiscordFollowupMessageBuilder()
                    .WithContent($"Failed to look for {search}")
                    .AsEphemeral(true);

                    await ctx.FollowUpAsync(ephemeralMessage3);
                    return;
                }

                track = loadResult.Tracks.First();
            }
            else if (Validates.IsSpotifyLink(search))
            {
                var loadResult = await node.Rest.GetTracksAsync(search, LavalinkSearchType.Plain);

                if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
                {
                    var ephemeralMessage3 = new DiscordFollowupMessageBuilder()
                    .WithContent($"Failed to look for {search}")
                    .AsEphemeral(true);

                    await ctx.FollowUpAsync(ephemeralMessage3);
                    return;
                }

                track = loadResult.Tracks.First();
            }
            else if (Validates.IsSpotifyPlaylistLink(search))
            {
                var ephemeralMessage3 = new DiscordFollowupMessageBuilder()
                    .WithContent("That's a playlist link.. provide a song link please..")
                    .AsEphemeral(true);

                await ctx.FollowUpAsync(ephemeralMessage3);
                return;
            }
            else if (Validates.IsYouTubePlaylistLink(search))
            {
                var ephemeralMessage3 = new DiscordFollowupMessageBuilder()
                    .WithContent("That's a playlist link.. provide a song link please..")
                    .AsEphemeral(true);

                await ctx.FollowUpAsync(ephemeralMessage3);
                return;
            }
            else
            {
                var loadResult = await node.Rest.GetTracksAsync(search, LavalinkSearchType.Youtube);

                if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
                {
                    var ephemeralMessage3 = new DiscordFollowupMessageBuilder()
                    .WithContent($"Failed to look for {search}")
                    .AsEphemeral(true);

                    await ctx.FollowUpAsync(ephemeralMessage3);
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
                    var ephemeralMessage1 = new DiscordFollowupMessageBuilder()
                        .WithContent($"Now Playing: {track.Title} {track.Author}")
                        .AsEphemeral(true);

                    await ctx.FollowUpAsync(ephemeralMessage1);
                    Program.skipped = false;

                    Console.WriteLine("PLAYER IS PLAYING");
                    if (SlashComms._queueDictionary.Count > 1)
                    {
                        Console.WriteLine($"CONCURRENT: {SlashComms._queueDictionary.Count}");
                    }
                    else
                    {
                        Console.WriteLine($"NOW PLAYING: {track.Title} {track.Author}");
                    }
                    await Program.UpdateUserStatus(ctx.Client, "LISTENING", $"{track.Title} {track.Author}");
                }
                else
                {
                    if (_queueDictionary[guild].Count >= MaxQueueSize)                                            // QUEUE SIZE CHECK
                    {
                        var ephemeralMessage2 = new DiscordFollowupMessageBuilder()
                            .WithContent($"Max queue length was set to {MaxQueueSize}, wait for songs to finish")
                            .AsEphemeral(true);

                        await ctx.FollowUpAsync(ephemeralMessage2);
                    }
                    else
                    {
                        _queueDictionary[guild].Add(track);
                        var ephemeralMessage3 = new DiscordFollowupMessageBuilder()
                            .WithContent($"Added to Queue: {track.Title} {track.Author}")
                            .AsEphemeral(true);

                        await ctx.FollowUpAsync(ephemeralMessage3);
                    }
                }
            }
            catch
            {
                var ephemeralMessage4 = new DiscordFollowupMessageBuilder()
                    .WithContent($"{track.Title} {track.Author} failed to play")
                    .AsEphemeral(true);

                await ctx.FollowUpAsync(ephemeralMessage4);

                var ephemeralMessage41 = new DiscordFollowupMessageBuilder()
                    .WithContent("I'm gonna try that again..")
                    .AsEphemeral(true);

                await ctx.FollowUpAsync(ephemeralMessage41);

                await PlayCommand(ctx, search);
            }
        }

        [SlashCommand("np", "What the heel is this song?")]
        public async Task NowPlayingCommand(InteractionContext ctx)
        {
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();

            if (_lavastarted == false)                                                                    // LAVALINK CHECK
            {
                var ephemeralMessage = new DiscordInteractionResponseBuilder()
                    .WithContent("Please execute /start first so I can boot up the music player..")
                    .AsEphemeral(true);

                await ctx.CreateResponseAsync(ephemeralMessage);
                return;
            }

            if (ctx.Member.VoiceState == null)                                                           // VOICE CHANNEL CHECK
            {
                var ephemeralMessage = new DiscordInteractionResponseBuilder()
                    .WithContent("Unfortunately that's not how it works, you gotta be in the same voice channel..")
                    .AsEphemeral(true);

                await ctx.CreateResponseAsync(ephemeralMessage);
                return;
            }

            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);                            // BOT VOICE STATE VERIFY

            if (conn == null)
            {
                var ephemeralMessage = new DiscordInteractionResponseBuilder()
                    .WithContent($"Brother, I'm not even in a voice channel yet..")
                    .AsEphemeral(true);

                await ctx.CreateResponseAsync(ephemeralMessage);
                return;
            }

            if (conn.CurrentState.CurrentTrack != null)                                               // NOW PLAYING COMMAND
            {
                var currentTrack = conn.CurrentState.CurrentTrack;
                var trackInfo = $"***Now Playing: {currentTrack.Title} {currentTrack.Author} [{currentTrack.Length}]***";

                var ephemeralMessage = new DiscordInteractionResponseBuilder()
                    .WithContent(trackInfo)
                    .AsEphemeral(true);

                await ctx.CreateResponseAsync(ephemeralMessage);
            }
            else
            {
                var currentTrack = conn.CurrentState.CurrentTrack;
                var trackInfo = "Nothing but silence..";

                var ephemeralMessage = new DiscordInteractionResponseBuilder()
                    .WithContent(trackInfo)
                    .AsEphemeral(true); // Set ephemeral to true to make the message visible only to the user

                await ctx.CreateResponseAsync(ephemeralMessage);
            }
        }

        [SlashCommand("pause", "Nobody move, nobody get hurt")]
        public async Task PauseCommand(InteractionContext ctx)
        {
            if (_lavastarted == false)                                                                    // LAVALINK CHECK
            {
                var ephemeralMessage = new DiscordInteractionResponseBuilder()
                    .WithContent("Please execute /start first so I can boot up the music player..")
                    .AsEphemeral(true);

                await ctx.CreateResponseAsync(ephemeralMessage);
                return;
            }

            if (_playerIsPaused == true)                                                                // PLAYER STATE CHECK
            {
                var trackInfo = "I already told the song not to move";

                var ephemeralMessage = new DiscordInteractionResponseBuilder()
                    .WithContent(trackInfo)
                    .AsEphemeral(true);

                await ctx.CreateResponseAsync(ephemeralMessage);
                return;
            }

            _playerIsPaused = true;

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();

            if (ctx.Member.VoiceState == null)                                                      // VOICE CHANNEL CHECK
            {
                var ephemeralMessage = new DiscordInteractionResponseBuilder()
                    .WithContent("Nice try but that's not how it works, you gotta be in the same voice channel as the player..")
                    .AsEphemeral(true); // Set ephemeral to true to make the message visible only to the user

                await ctx.CreateResponseAsync(ephemeralMessage);
                return;
            }

            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)                                                                     // BOT VOICESTATE CHECK
            {
                var ephemeralMessage = new DiscordInteractionResponseBuilder()
                    .WithContent($"Brother, I'm not even in a voice channel yet..")
                    .AsEphemeral(true);

                await ctx.CreateResponseAsync(ephemeralMessage);
                return;
            }

            if (conn.CurrentState.CurrentTrack != null)                                         // PAUSE COMMAND
            {
                var currentTrack = conn.CurrentState.CurrentTrack;
                var trackInfo = $"A gun has been pointed at {currentTrack.Title}";

                var ephemeralMessage = new DiscordInteractionResponseBuilder()
                    .WithContent(trackInfo)
                    .AsEphemeral(true); // Set ephemeral to true to make the message visible only to the user

                await ctx.CreateResponseAsync(ephemeralMessage);
                await conn.PauseAsync();
            }
        }

        [SlashCommand("resume", "Move it, NOW!")]
        public async Task ResumeCommand(InteractionContext ctx)
        {
            if (_lavastarted == false)                                                                    // LAVALINK CHECK
            {
                var ephemeralMessage = new DiscordInteractionResponseBuilder()
                    .WithContent("Please execute /start first so I can boot up the music player..")
                    .AsEphemeral(true);

                await ctx.CreateResponseAsync(ephemeralMessage);
                return;
            }

            if (_playerIsPaused != true)                                                                // PLAYERSTATE CHECK
            {
                var trackInfo = "The song is already moving..";

                var ephemeralMessage = new DiscordInteractionResponseBuilder()
                    .WithContent(trackInfo)
                    .AsEphemeral(true);

                await ctx.CreateResponseAsync(ephemeralMessage);
                return;
            }

            _playerIsPaused = false;

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();

            if (ctx.Member.VoiceState == null)                                                        // VOICE CHANNEL CHECK
            {
                var ephemeralMessage = new DiscordInteractionResponseBuilder()
                    .WithContent("Nice try but that's not how it works, you gotta be in the same voice channel as the player..")
                    .AsEphemeral(true);

                await ctx.CreateResponseAsync(ephemeralMessage);
                return;
            }

            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);                       // BOT VOICESTATE VERIFY

            if (conn == null)
            {
                var ephemeralMessage = new DiscordInteractionResponseBuilder()
                    .WithContent($"Brother, I'm not even in a voice channel yet..")
                    .AsEphemeral(true);

                await ctx.CreateResponseAsync(ephemeralMessage);
                return;
            }

            if (conn.CurrentState.CurrentTrack != null)                                             // RESUME COMMAND
            {
                var currentTrack = conn.CurrentState.CurrentTrack;
                var trackInfo = $"The gun was fired near {currentTrack.Title}";

                var ephemeralMessage = new DiscordInteractionResponseBuilder()
                    .WithContent(trackInfo)
                    .AsEphemeral(true);

                await ctx.CreateResponseAsync(ephemeralMessage);
                await conn.ResumeAsync();
            }
        }
        /*
        [SlashCommand("stop", "Halt.")]
        public async Task StopCommand(InteractionContext ctx)
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

            if (_lavastarted == false)                                                                    // LAVALINK CHECK
            {
                var ephemeralMessage1 = new DiscordInteractionResponseBuilder()
                    .WithContent("Please execute /start first so I can boot up the music player..")
                    .AsEphemeral(true);

                await ctx.CreateResponseAsync(ephemeralMessage1);
                return;
            }

            if (ctx.Member.VoiceState == null)                                                        // VOICE CHANNEL CHECK
            {
                var ephemeralMessage3 = new DiscordInteractionResponseBuilder()
                    .WithContent("Nice try but that's not how it works, you gotta be in the same voice channel as the player..")
                    .AsEphemeral(true);

                await ctx.CreateResponseAsync(ephemeralMessage3);
                return;
            }

            var trackInfo = "A gun was fired at the player, the queue is in pieces..";
            Console.WriteLine("LAVALINK IS DISCONNECTED");
            // STOP COMMAND

            var ephemeralMessage = new DiscordInteractionResponseBuilder()
                .WithContent(trackInfo)
                .AsEphemeral(true);

            await ctx.CreateResponseAsync(ephemeralMessage);
            _queueDictionary[guild].Clear();

            if (Program.lavalinkProcess != null && !Program.lavalinkProcess.HasExited)
            {
                Program.lavalinkProcess.Kill();
                Program.lavalinkProcess.CloseMainWindow();
                Program.lavalinkProcess.Close();
                SlashComms._lavastarted = false;
            }
        }
        */
        [SlashCommand("queue", "Show the list of songs requested by abiders of social norms.. Silently..")]
        public async Task QueueCommand(InteractionContext ctx, [Option("page", "Input an integer, jokes on you I handled exceptions.. Can't break my code today..")] string page)
        {
            await ctx.DeferAsync(ephemeral: true);

            if (_lavastarted == false)                                                                    // LAVALINK CHECK
            {
                await ctx.FollowUpAsync(ResponseBuilder("Please execute /start first so I can boot up the music player.."));
                return;
            }

            ulong guild = ctx.Guild.Id;

            if (SlashComms._queueDictionary[guild].Count > 0 && SlashComms._queueDictionary[guild] != null)
            {
                int swipe;

                if (SlashComms._queueDictionary[guild].Count > 20)
                {
                    if (page is null)
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

                    if (swipe * 20 > SlashComms._queueDictionary[guild].Count + 19)
                    {
                        await ctx.FollowUpAsync(ResponseBuilder("The queue list isn't *that* long, I'll just give you the last page.."));
                        if (SlashComms._queueDictionary[guild].Count % 20 != 0)
                        {
                            swipe = (SlashComms._queueDictionary[guild].Count / 20) + 1;
                        }
                        else
                        {
                            swipe = (SlashComms._queueDictionary[guild].Count / 20);
                        }

                    }

                    await ctx.FollowUpAsync(ResponseBuilder("Look at all these songs: [Page: " + swipe + "]"));

                    foreach (var track in SlashComms._queueDictionary[guild])
                    {
                        if (track != null)
                        {
                            if (trackNumber >= (swipe - 1) * 20 && trackNumber <= swipe * 20)
                            {
                                // Append the track information to the message
                                if (trackNumber == 1)
                                {
                                    message.AppendLine($"***Now Playing: {track.Title} {track.Author}***");
                                }
                                else
                                {
                                    message.AppendLine($"{trackNumber}. {track.Title} {track.Author}");
                                }
                            }

                            queueLength += track.Length;
                            trackNumber++;
                        }
                    }

                    if (trackNumber > 1)
                    {
                        message.AppendLine($"***{trackNumber - 1} total songs for {queueLength} long..***");
                        await ctx.FollowUpAsync(ResponseBuilder(message.ToString()));
                    }
                    else
                    {
                        await ctx.FollowUpAsync(ResponseBuilder("The queue is empty."));
                    }
                }
                else
                {
                    var message = new StringBuilder();
                    TimeSpan queueLength = TimeSpan.Zero;
                    int trackNumber = 1;

                    await ctx.FollowUpAsync(ResponseBuilder("Look at all these songs: "));

                    foreach (var track in SlashComms._queueDictionary[guild])
                    {
                        if (track != null)
                        {
                            // Append the track information to the message
                            if (trackNumber == 1)
                            {
                                message.AppendLine($"***Now Playing: {track.Title} {track.Author}***");
                            }
                            else
                            {
                                message.AppendLine($"{trackNumber}. {track.Title} {track.Author}");
                            }

                            queueLength += track.Length;
                            trackNumber++;
                        }
                    }

                    if (trackNumber > 1)
                    {
                        message.AppendLine($"***{trackNumber - 1} total songs for {queueLength} long..***");
                        await ctx.FollowUpAsync(ResponseBuilder(message.ToString()));
                    }
                    else
                    {
                        await ctx.FollowUpAsync(ResponseBuilder("The queue is empty."));
                    }
                }
            }
            else
            {
                await ctx.FollowUpAsync(ResponseBuilder("The queue list is blank."));
            }
        }

        [SlashCommand("byebye", "I said, leave..")]
        public async Task LeaveCommand(InteractionContext ctx)
        {
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            ulong guild = ctx.Guild.Id;

            if (ctx.Member.VoiceState == null)                                                      // VOICE CHANNEL CHECK
            {
                var ephemeralMessage = new DiscordInteractionResponseBuilder()
                    .WithContent("Unfortunately that's not how it works, you gotta be in the same voice channel..")
                    .AsEphemeral(true);

                await ctx.CreateResponseAsync(ephemeralMessage);
                return;
            }

            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)                                                                     // BOT VOICESTATE VERIFY
            {
                var ephemeralMessage = new DiscordInteractionResponseBuilder()
                    .WithContent("What do you mean? I'm already out..")
                    .AsEphemeral(true);

                await ctx.CreateResponseAsync(ephemeralMessage);
            }
            else
            {
                string category = "Byes";                                                       // LEAVE COMMAND

                string randomEntry = MessageHandler.GetRandomEntry(category);

                if (randomEntry != null)
                {
                    var ephemeralMessage = new DiscordInteractionResponseBuilder()
                    .WithContent(randomEntry)
                    .AsEphemeral(true);

                    await ctx.CreateResponseAsync(ephemeralMessage);
                    Program.skipped = true;
                    await conn.StopAsync();
                    _queueDictionary.Remove(guild);
                    Program.skipped = false;
                    _invited = false;
                    await conn.DisconnectAsync();
                }
                else
                {
                    Console.WriteLine("No entries found for the specified category.");
                }
            }
        }

        [SlashCommand("remove", "Added the wrong song?")]
        public async Task RemoveCommand(InteractionContext ctx, [Option("track", "Just put the track number of the song you want to remove..")] string Num)
        {
            var trackNum = 0;
            ulong guild = ctx.Guild.Id;

            if (_lavastarted == false)                                                                    // LAVALINK CHECK
            {
                var ephemeralMessage1 = new DiscordInteractionResponseBuilder()
                    .WithContent("Please execute /start first so I can boot up the music player..")
                    .AsEphemeral(true);

                await ctx.CreateResponseAsync(ephemeralMessage1);
                return;
            }

            if (Num is string)                                                      // NUMBER LOGIC
            {
                try
                {
                    trackNum = Convert.ToInt32(Num);
                }
                catch
                {
                    var ephemeralMessage1 = new DiscordInteractionResponseBuilder()
                    .WithContent($"That is not a number..")
                    .AsEphemeral(true);

                    await ctx.CreateResponseAsync(ephemeralMessage1);
                    return;
                }
            }

            if (trackNum < 0 || trackNum >= _queueDictionary[guild].Count + 1)
            {
                var ephemeralMessage1 = new DiscordInteractionResponseBuilder()
                    .WithContent("Invalid track number.")
                    .AsEphemeral(true);

                await ctx.CreateResponseAsync(ephemeralMessage1);
                return;
            }

            if (trackNum == 1)
            {
                var lava = ctx.Client.GetLavalink();
                var node = lava.ConnectedNodes.Values.First();

                if (ctx.Member.VoiceState == null)                                          // VOICE CHANNEL CHECK
                {
                    var ephemeralMessage1 = new DiscordInteractionResponseBuilder()
                        .WithContent("Nice try but that's not how it works, you gotta be in the same voice channel..")
                        .AsEphemeral(true);

                    await ctx.CreateResponseAsync(ephemeralMessage1);
                    return;
                }

                var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

                if (SlashComms._queueDictionary[guild].Count < 2)
                {
                    var tune = SlashComms._queueDictionary[guild][0];
                    var ephemeralMessage1 = new DiscordInteractionResponseBuilder()                          // BOT VOICESTATE VERIFY
                        .WithContent($"Removed {tune.Title}")
                        .AsEphemeral(true);

                    await ctx.CreateResponseAsync(ephemeralMessage1);
                    SlashComms._queueDictionary.Remove(guild);
                    await conn.StopAsync();
                    return;
                }

                if (conn == null)
                {
                    var ephemeralMessage1 = new DiscordInteractionResponseBuilder()                          // BOT VOICESTATE VERIFY
                        .WithContent($"Brother, I'm not even in a voice channel yet..")
                        .AsEphemeral(true);

                    await ctx.CreateResponseAsync(ephemeralMessage1);
                    return;
                }

                var nextTrack = _queueDictionary[guild][1];                                                           // REMOVE COMMAND
                var nextTrackTitle = nextTrack.Title;
                var track = _queueDictionary[guild][0];
                var trackTitle = track.Title;
                Program.skipped = true;
                await conn.PlayAsync(nextTrack);
                _queueDictionary[guild].RemoveAt(0);

                var ephemeralMessage2 = new DiscordInteractionResponseBuilder()
                        .WithContent($"Eliminated {trackTitle}")
                        .AsEphemeral(true);

                await ctx.CreateResponseAsync(ephemeralMessage2);
                Program.skipped = false;
                if (SlashComms._queueDictionary.Count > 1)
                {
                    Console.WriteLine($"CONCURRENT: {SlashComms._queueDictionary.Count}");
                }
                else
                {
                    Console.WriteLine($"***Now Playing: {nextTrackTitle} {nextTrack.Author}***");
                }
                await Program.UpdateUserStatus(ctx.Client, "LISTENING", nextTrackTitle);
                return;
            }

            var song = _queueDictionary[guild][trackNum - 1];
            var songTitle = song.Title;
            _queueDictionary[guild].RemoveAt(trackNum - 1);
            var ephemeralMessage = new DiscordInteractionResponseBuilder()
                .WithContent($"Eliminated {songTitle}..")
                .AsEphemeral(true);

            await ctx.CreateResponseAsync(ephemeralMessage);
        }

        [SlashCommand("skipto", "Line cutter..")]
        public async Task SkipToCommand(InteractionContext ctx, [Option("track", "Just put the track number of the song you want to skip to..")] string Num)
        {
            await ctx.DeferAsync(ephemeral: true);
            if (_lavastarted == false)                                                                    // LAVALINK CHECK
            {
                await ctx.FollowUpAsync(ResponseBuilder("Please execute /start first so I can boot up the music player.."));
                return;
            }

            ulong guild = ctx.Guild.Id;

            if (!int.TryParse(Num, out int trackNum))                                                     // NUMBER LOGIC
            {
                await ctx.FollowUpAsync(ResponseBuilder("That is not a valid number."));
                return;
            }

            if (trackNum <= 0 || trackNum > _queueDictionary[guild].Count + 1)
            {
                await ctx.FollowUpAsync(ResponseBuilder("That is not a valid number."));
                return;
            }

            if (ctx.Member.VoiceState == null)                                                  // VOICECHANNEL CHECK
            {
                await ctx.FollowUpAsync(ResponseBuilder("Nice try but that's not how it works, you gotta be in the same voice channel.."));
                return;
            }

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.FollowUpAsync(ResponseBuilder("Brother, I'm not even in a voice channel yet.."));
                return;
            }

            var tracksToRemove = _queueDictionary[guild].GetRange(0, trackNum - 1);                         // SKIPTO COMMAND
            _queueDictionary[guild].RemoveRange(0, trackNum - 1);

            Program.skipped = true;
            var currentTrack = _queueDictionary[guild][0];
            await conn.PlayAsync(currentTrack);
            await ctx.FollowUpAsync(ResponseBuilder($"Removed {tracksToRemove.Count} tracks from the queue.."));
            await ctx.FollowUpAsync(ResponseBuilder($"***Now Playing: {currentTrack.Title} {currentTrack.Author}***"));

            if (SlashComms._queueDictionary.Count > 1)
            {
                Console.WriteLine($"CONCURRENT: {SlashComms._queueDictionary.Count}");
            }
            else
            {
                Console.WriteLine($"NOW PLAYING: {currentTrack.Title} {currentTrack.Author}");
            }
            await Program.UpdateUserStatus(ctx.Client, "LISTENING", $"{currentTrack.Title} {currentTrack.Author}");
            Program.skipped = false;
        }

        [SlashCommand("shuffle", "*shuffles away*")]
        public async Task ShuffleCommand(InteractionContext ctx)
        {
            if (_lavastarted == false)                                                                    // LAVALINK CHECK
            {
                await HiddenSend("Please execute /start first so I can boot up the music player..",ctx);
                return;
            }

            ulong guild = ctx.Guild.Id;

            if (_queueDictionary[guild].Count <= 1)                                                                   // SHUFFLE COMMAND
            {
                await HiddenSend("There are not enough songs in the queue to shuffle.", ctx);
                return;
            }

            var firstSong = _queueDictionary[guild][0];
            var remainingSongs = _queueDictionary[guild].Skip(1).ToList();

            var random = new Random();
            var shuffledSongs = remainingSongs.OrderBy(x => random.Next()).ToList();

            _queueDictionary[guild] = new List<LavalinkTrack> { firstSong };
            _queueDictionary[guild].AddRange(shuffledSongs);

            await HiddenSend("Disorganized the list of songs requested by abiders of social norms..", ctx);
        }

        [SlashCommand("skip", "Hate the song?")]
        public async Task SkipCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync(ephemeral: true);
            
            ulong guild = ctx.Guild.Id;
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (_lavastarted == false)                                                                    // LAVALINK CHECK
            {
                await ctx.FollowUpAsync(ResponseBuilder("Please execute /start first so I can boot up the music player.."));
                return;
            }

            if (ctx.Member.VoiceState == null)                                              // VOICECHANNEL CHECK
            {
                await ctx.FollowUpAsync(ResponseBuilder("Nice try but that's not how it works, you gotta be in the same voice channel.."));
                return;
            }

            if (conn == null)
            {
                await ctx.FollowUpAsync(ResponseBuilder("Brother, I'm not even in a voice channel yet.."));
                return;
            }

            if (_queueDictionary[guild].Count < 2)                               // NUMBER LOGIC
            {
                var track = SlashComms._queueDictionary[guild][0];
                await ctx.FollowUpAsync(ResponseBuilder($"Skipped {track.Title} {track.Author}"));
                await Program.UpdateUserStatus(ctx.Client, "IDLE", "bocchi");
                SlashComms._queueDictionary.Remove(guild);
                await conn.StopAsync();
                return;
            }

            if (SlashComms._queueDictionary[guild] == null)
            {
                await ctx.FollowUpAsync(ResponseBuilder("Bro, there's no song to skip to.."));
                return;
            }

            if (SlashComms._queueDictionary[guild].Count < 2)
            {
                var track = SlashComms._queueDictionary[guild][0];

                await ctx.FollowUpAsync(ResponseBuilder($"Skipped {track.Title} {track.Author}"));

                SlashComms._queueDictionary.Remove(guild);
                await conn.StopAsync();
                return;
            }

            if (conn.CurrentState.CurrentTrack != null)                                            //SKIP COMMAND
            {
                var nextTrack = _queueDictionary[guild][1];
                var track = _queueDictionary[guild][0];
                Program.skipped = true;
                await conn.PlayAsync(nextTrack);
                _queueDictionary[guild].RemoveAt(0);
                await ctx.FollowUpAsync(ResponseBuilder($"Skipped {track.Title} {track.Author}.."));
                if (SlashComms._queueDictionary.Count > 1)
                {
                    Console.WriteLine($"CONCURRENT: {SlashComms._queueDictionary.Count}");
                }
                else
                {
                    Console.WriteLine($"NOW PLAYING: {nextTrack.Title} {nextTrack.Author}");
                }
                await Program.UpdateUserStatus(ctx.Client, "LISTENING", $"{nextTrack.Title} {nextTrack.Author}");
                Program.skipped = false;
            }
            else
            {
                try
                {
                    var nextTrack = _queueDictionary[guild][1];
                    var track = _queueDictionary[guild][0];
                    Program.skipped = true;
                    await conn.PlayAsync(nextTrack);
                    _queueDictionary[guild].RemoveAt(0);
                    await ctx.FollowUpAsync(ResponseBuilder($"Skipped {track.Title} {track.Author}.."));
                    if (SlashComms._queueDictionary.Count > 1)
                    {
                        Console.WriteLine($"CONCURRENT: {SlashComms._queueDictionary.Count}");
                    }
                    else
                    {
                        Console.WriteLine($"NOW PLAYING: {nextTrack.Title} {nextTrack.Author}");
                    }
                    await Program.UpdateUserStatus(ctx.Client, "LISTENING", $"{nextTrack.Title} {nextTrack.Author}");
                    Program.skipped = false;
                }
                catch
                {
                    await ctx.FollowUpAsync(ResponseBuilder("I tried, but there really is no song to skip to.."));
                }     
            }
        }

        [SlashCommand("help", "Displays the list of commands")]
        public async Task HelpCommand(InteractionContext ctx)
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
                .AddField("loop", "Loops the current song.")
                .AddField("help", "Displays this list.")
                .AddField("\u200B", "\u200B")
                .AddField("Others", "Sometimes specific words can trigger a bocchi response..")
                .AddField("Just be nice to her..", "She's trying her best..")
                .WithColor(new DiscordColor("#ffd8e1"));

            var embed = embedBuilder.Build();
            await ctx.CreateResponseAsync(embed: embed);
        }

        [SlashCommand("start", "Boots up the music player..")]
        public async Task StartCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync(ephemeral: true);

            if (_lavastarted == true)                                               //LAVALINK CHECK
            {
                await ctx.FollowUpAsync(ResponseBuilder("The music player is already running bro.."));
                return;
            }

            var f = await ctx.FollowUpAsync(ResponseBuilder("Ooh it's starting up.."));
            Console.WriteLine("LAVALINK IS STARTING");
            await Program.StartLava();

            if (_failed == true)
            {
                await ctx.EditFollowupAsync(f.Id, ResponseEditBuilder("I encountered a problem, @Sean-san send help please.."));
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

                _lavastarted = true;

                var lava = discord.GetLavalink();
                var node = lava.ConnectedNodes.Values.First();

                //Lavalink Event Handlers
                node.PlaybackFinished += Program.PlaybackFinishedHandler;
                node.TrackException += Program.PlaybackErrorHandler;

                await ctx.EditFollowupAsync(f.Id, ResponseEditBuilder("Oop, it's running.. there it goes.."));
                Console.WriteLine("LAVALINK IS CONNECTED");
            }
        }

        [SlashCommand("load", "Carpet bomb the queue with your songs..")]
        public async Task LoadCommand(InteractionContext ctx, [Option("playlist", "Paste in your playlist link..")] string list)
        {
            await ctx.DeferAsync(ephemeral: true);
            List<string> songTitles;

            if (_lavastarted == false)                                                                    // LAVALINK CHECK
            {
                await ctx.FollowUpAsync(ResponseBuilder("Please execute /start first so I can boot up the music player.."));
                return;
            }

            if (ctx.Member.VoiceState == null)
            {
                var re = MessageHandler.GetRandomEntry("Nanis");
                await ctx.FollowUpAsync(ResponseBuilder($"{re} get into a voice channel first.."));
                return;
            }

            if (Validates.IsYouTubePlaylistLink(list))
            {
                songTitles = await SlashPlayLoader.YoutubeLoaderAsync(list);

                if (songTitles == null || songTitles.Count == 0)
                {
                    await ctx.FollowUpAsync(ResponseBuilder("No songs found in playlist."));
                    return;
                }

                await ctx.FollowUpAsync(ResponseBuilder("Loading Playlist.."));
                var f = await ctx.FollowUpAsync(ResponseBuilder("_ songs queue'd.."));
                int songCount = 0;
                
                foreach (string title in songTitles)
                {
                    Program.unbroken = true;

                    if (SlashPlayLoader._queuefull == true)
                    {
                        SlashPlayLoader._queuefull = false;
                        break;
                    }

                    if (Program.forcestop == true)
                    {
                        Program.unbroken = false;
                        break;
                    }

                    ++songCount;

                    await ctx.EditFollowupAsync(f.Id, ResponseEditBuilder($"{songCount} songs queue'd.."));
                    await SlashPlayLoader.Enqueue(ctx, title);
                }

                await ctx.EditFollowupAsync(f.Id, ResponseEditBuilder("Playlist Loaded."));
                return;
            }
            else if (Validates.IsSpotifyPlaylistLink(list))
            {
                string playlistId = Converts.ExtractSpotifyPlaylistId(list);

                var songslinks = await SpotifyLoader.GetPlaylistSongLinks(playlistId);
                Console.WriteLine("Ohh its spotify");

                if (songslinks == null || songslinks.Count == 0)
                {
                    await ctx.FollowUpAsync(ResponseBuilder("No songs found in the playlist."));
                    return;
                }

                await ctx.FollowUpAsync(ResponseBuilder("Loading Playlist.."));
                var f = await ctx.FollowUpAsync(ResponseBuilder("_ songs queue'd.."));
                int songCount = 0;

                foreach (var link in songslinks)
                {
                    Program.unbroken = true;

                    if (SlashPlayLoader._queuefull == true)
                    {
                        SlashPlayLoader._queuefull = false;
                        break;
                    }

                    if (Program.forcestop == true)
                    {
                        Program.unbroken = false;
                        break;
                    }

                    songCount++;

                    await ctx.EditFollowupAsync(f.Id, ResponseEditBuilder($"{songCount} songs queue'd.."));
                    await SlashPlayLoader.Enqueue(ctx, link);
                }

                var ephemeralMessage14 = new DiscordFollowupMessageBuilder()
                           .WithContent("Playlist loaded.")
                           .AsEphemeral(true);

                await ctx.FollowUpAsync(ResponseBuilder("Playlist Loaded."));
                return;
            }
            else
            {
                await ctx.FollowUpAsync(ResponseBuilder("Invalid playlist link."));
                return;
            }
        }

        [SlashCommand("loop", "Shawty's like a melody in my head..")]
        public async Task LoopCommand(InteractionContext ctx)
        {
            ulong guild = ctx.Guild.Id;
            
            if (Program.loop.Contains(guild))
            {
                Program.loop.Remove(guild);
                await HiddenSend("Finally we can move on..", ctx);
            }
            else
            {
                Program.loop.Add(guild);
                await HiddenSend("This song's boutta get stuck in your head..", ctx);
            }
        }

        [SlashCommand("stop", "Halt.")]
        public async Task StopCommand(InteractionContext ctx)
        {
            if (SlashComms._lavastarted == false)                                                                    // LAVALINK CHECK
            {
                await HiddenSend("Please execute /start first so I can boot up the music player..", ctx);
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
                await HiddenSend("Bam, dead.", ctx);
                Program.skipped = false;
            }
            catch
            {
                await HiddenSend("Stop it? It's already dead..", ctx);
            }
        }

        [SlashCommand("forcestop", "Stop the loop!")]
        public async Task ForceStopCommand(InteractionContext ctx)
        {
            Program.forcestop = true;
            await ctx.DeferAsync(true);

            try
            {
                var followup = ResponseBuilder("Stopping..");
                var f = await ctx.FollowUpAsync(followup);

                var edit = ResponseEditBuilder("Stopped.");

                while(Program.unbroken == true)
                {
                    await Task.Delay(10);
                }

                await ctx.EditFollowupAsync(f.Id, edit);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Program.forcestop = false;

        }

        // SENDING METHODS

        private static async Task HiddenSend(string msg, InteractionContext ctx)   //CLASSIC HIDDEN RESPONSE
        {
            var messageContent = new DiscordInteractionResponseBuilder()
                    .WithContent(msg)
                    .AsEphemeral(true);

            await ctx.CreateResponseAsync(messageContent);
        }

        private static DiscordFollowupMessageBuilder ResponseBuilder(string msg)  //FOLLOW-UP MESSAGE
        {
            return new DiscordFollowupMessageBuilder().WithContent(msg).AsEphemeral(true);
        }

        private static DiscordWebhookBuilder ResponseEditBuilder(string msg)  //EDIT FOLLOW-UP MESSAGE
        {
            return new DiscordWebhookBuilder().WithContent(msg);
        }
    }
}
