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

            List<string> videoUrls = playlistVideos.Select(video => $"https://youtu.be/{video.Id}").ToList();
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

                if (channel.Type is ChannelType.Voice)
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

                        await ctx.Channel.SendMessageAsync($"Now Playing: {track.Title}");
                        Console.WriteLine("PLAYER IS PLAYING");
                        Console.WriteLine($"NOW PLAYING: {track.Title}");
                        Program.skipped = false;
                        await Program.UpdateUserStatus(ctx.Client, "LISTENING", track.Title);
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
                        Program.skipped = true;
                        await conn.PlayAsync(track);

                        await ctx.Channel.SendMessageAsync($"Now Playing: {track.Title}");
                        Console.WriteLine("PLAYER IS PLAYING");
                        Console.WriteLine($"NOW PLAYING: {track.Title}");
                        Program.skipped = false;
                        await Program.UpdateUserStatus(ctx.Client, "LISTENING", track.Title);
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
                await ctx.Channel.SendMessageAsync($"{track.Title} failed to play");
            }
        }
    }
}
