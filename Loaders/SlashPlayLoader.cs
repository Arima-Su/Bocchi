using System;
using System.Collections.Generic;
using System.Linq;
using YoutubeExplode;
using System.Threading.Tasks;
using YoutubeExplode.Common;
using DSharpPlus.SlashCommands;
using Alice.Commands;
using Alice;
using DSharpPlus.Lavalink;
using DSharpPlus;
using DSharpPlus.Entities;

namespace Alice_Module.Loaders
{
    public class SlashPlayLoader
    {
        public static bool _queuefull = false;

        public static async Task<List<string>> YoutubeLoaderAsync(string playlistLink)
        {
            var youtubeClient = new YoutubeClient();

            string playlistId = ExtractPlaylistId(playlistLink);

            var playlist = await youtubeClient.Playlists.GetAsync(playlistId);
            var playlistVideos = await youtubeClient.Playlists.GetVideosAsync(playlistId);

            List<string> videoUrls = playlistVideos.Select(video => $"https://www.youtube.com/watch?v={video.Id}").ToList();
            return videoUrls;
        }

        static string ExtractPlaylistId(string playlistLink)
        {
            const string playlistParam = "list=";
            int index = playlistLink.IndexOf(playlistParam);

            if (index != -1)
            {
                index += playlistParam.Length;

                // Check if there are any additional parameters after the playlist ID
                int nextAmpersand = playlistLink.IndexOf('&', index);
                if (nextAmpersand != -1)
                {
                    // If there are additional parameters, extract the playlist ID up to the next ampersand
                    return playlistLink.Substring(index, nextAmpersand - index);
                }
                else
                {
                    // If there are no additional parameters, extract the playlist ID until the end of the URL
                    return playlistLink.Substring(index);
                }
            }

            throw new ArgumentException("Invalid YouTube playlist link");
        }

        public static async Task Enqueue(InteractionContext ctx, string search)
        {
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();

            if (SlashComms._lavastarted == false)                                                                    // LAVALINK CHECK
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

                if (channel.Type is ChannelType.Voice)
                {
                    try
                    {
                        SlashComms._invited = false;
                        await node.ConnectAsync(channel);
                        Console.WriteLine("JOINED");
                        SlashComms._ready = true;
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

                    if (SlashComms._invited == true)
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
                        Console.WriteLine("PLAYER IS PLAYING");
                        if (SlashComms._queueDictionary.Count > 1)
                        {
                            Console.WriteLine($"CONCURRENT: {SlashComms._queueDictionary.Count}");
                        }
                        else
                        {
                            Console.WriteLine($"NOW PLAYING: {track.Title} {track.Author}");
                        }
                        Program.skipped = false;
                        await Program.UpdateUserStatus(ctx.Client, "LISTENING", $"{track.Title} {track.Author}");
                    }
                    else
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
                        SlashComms._invited = true;
                        Program.skipped = true;
                        await conn.PlayAsync(track);
                        var ephemeralMessage1 = new DiscordFollowupMessageBuilder()
                            .WithContent($"Now Playing: {track.Title} {track.Author}")
                            .AsEphemeral(true);

                        await ctx.FollowUpAsync(ephemeralMessage1);
                        Console.WriteLine("PLAYER IS PLAYING");
                        if (SlashComms._queueDictionary.Count > 1)
                        {
                            Console.WriteLine($"CONCURRENT: {SlashComms._queueDictionary.Count}");
                        }
                        else
                        {
                            Console.WriteLine($"NOW PLAYING: {track.Title} {track.Author}");
                        }
                        Program.skipped = false;
                        await Program.UpdateUserStatus(ctx.Client, "LISTENING", $"{track.Title} {track.Author}");
                    }
                }
                else
                {
                    if (SlashComms._queueDictionary[guild].Count >= SlashComms.MaxQueueSize)                                            // QUEUE SIZE CHECK
                    {
                        _queuefull = true;
                        var ephemeralMessage2 = new DiscordFollowupMessageBuilder()
                            .WithContent($"Max queue length was set to {SlashComms.MaxQueueSize}, wait for songs to finish")
                            .AsEphemeral(true);

                        await ctx.FollowUpAsync(ephemeralMessage2);
                    }
                    else
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
                    }
                }
            }
            catch
            {
                var ephemeralMessage4 = new DiscordFollowupMessageBuilder()
                    .WithContent($"{track.Title} {track.Author} failed to play")
                    .AsEphemeral(true);

                await ctx.FollowUpAsync(ephemeralMessage4);
            }
        }
    }
}
