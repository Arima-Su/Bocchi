using System;
using System.Collections.Generic;
using System.Linq;
using YoutubeExplode;
using System.Threading.Tasks;
using YoutubeExplode.Common;
using DSharpPlus.CommandsNext;
using Alice.Commands;
using Alice;
using DSharpPlus.Lavalink;
using DSharpPlus;
using System.IO;
using YoutubeExplode.Videos.Streams;
using Alice_Module.Handlers;

namespace Alice_Module.Loaders
{
    public class PlayLoader
    {
        public static bool _queuefull = false;

        // YOUTUBE LOADER
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

        public static async Task<string> GetVideoTitleAsync(string videoUrl)
        {
            var youtubeClient = new YoutubeClient();
            var videoId = ParseVideoId(videoUrl);
            var video = await youtubeClient.Videos.GetAsync(videoId);
            string title = video.Title;
            return title;
        }

        public static async Task Save(CommandContext ctx, string videoUrl)
        {
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
                                    //await save.SendAsync(ctx.Channel.Id, outputFilePath, title);
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
            catch (Exception ex)
            {
                await ctx.Channel.SendMessageAsync($"I hit a wall, the logs say: {ex.Message}");
            }
        }

        public static string ParseVideoId(string playlistLink)
        {
            const string playlistParam = "v=";
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

        // SONG QUEUE
        public static async Task Enqueue(CommandContext ctx, string search)
        {
            var res = Optimizations.StartUpSequence(ctx, false);
            if (res != null)
            {
                await ctx.Channel.SendMessageAsync(res);
                return;
            }
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();

            var tracks = await Optimizations.QueueUpSequence(ctx, search);
            await Optimizations.PlayUpSequence(ctx, node.GetGuildConnection(ctx.Member.VoiceState.Guild), tracks, false, true);
        }

        //FREEPLAY
        public static async Task FreeEnqueue(LavalinkGuildConnection ctx, string search)
        {
            var node = ctx.Node;

            if (SlashComms._lavastarted == false)                                                                    // LAVALINK CHECK
            {
                await ctx.Channel.SendMessageAsync("Please execute /start first so I can boot up the music player..");
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
                var loadResult = await node.Rest.GetTracksAsync(search);

                if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
                {
                    await ctx.Channel.SendMessageAsync($"Failed to look for {search}");
                    return;
                }

                track = loadResult.Tracks.First();
            }

            var conn = ctx.Node.GetGuildConnection(ctx.Guild);

            try
            {
                if (conn.CurrentState.CurrentTrack == null)
                {

                    if (SlashComms._invited == true)
                    {
                        if (SlashComms._queueDictionary.ContainsKey(guild))
                        {
                            SlashComms._queueDictionary[guild].Add(new song(track, Program.User));
                        }
                        else
                        {
                            SlashComms._queueDictionary.Add(guild, new List<song>());
                            await Task.Delay(100);
                            SlashComms._queueDictionary[guild].Add(new song(track, Program.User));
                        }
                        Program.skipped = true;
                        await conn.PlayAsync(track);

                        await ctx.Channel.SendMessageAsync($"Now Playing: {track.Title} {track.Author}");
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
                        await Program.UpdateUserStatus(Program.discord, "LISTENING", $"{track.Title} {track.Author}");
                    }
                    else
                    {
                        if (SlashComms._queueDictionary.ContainsKey(guild))
                        {
                            SlashComms._queueDictionary[guild].Add(new song(track, Program.User));
                        }
                        else
                        {
                            SlashComms._queueDictionary.Add(guild, new List<song>());
                            await Task.Delay(100);
                            SlashComms._queueDictionary[guild].Add(new song(track, Program.User));
                        }
                        Program.skipped = true;
                        await conn.PlayAsync(track);

                        await ctx.Channel.SendMessageAsync($"Now Playing: {track.Title} {track.Author}");
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
                        await Program.UpdateUserStatus(Program.discord, "LISTENING", $"{track.Title} {track.Author}");
                    }
                }
                else
                {
                    if (SlashComms._queueDictionary[guild].Count >= SlashComms.MaxQueueSize)
                    {
                        _queuefull = true;
                        await ctx.Channel.SendMessageAsync($"Max queue length was set to {SlashComms.MaxQueueSize}, wait for songs to finish");
                    }
                    else
                    {
                        if (SlashComms._queueDictionary.ContainsKey(guild))
                        {
                            SlashComms._queueDictionary[guild].Add(new song(track, Program.User));
                        }
                        else
                        {
                            SlashComms._queueDictionary.Add(guild, new List<song>());
                            await Task.Delay(100);
                            SlashComms._queueDictionary[guild].Add(new song(track, Program.User));
                        }
                    }
                }
            }
            catch
            {
                await ctx.Channel.SendMessageAsync($"{track.Title} {track.Author} failed to play");
            }
        }
    }
}
