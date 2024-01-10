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
using Alice_Module.Handlers;

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
            var res = Optimizations.StartUpSequence(ctx, false);
            if (res != null)
            {
                await ctx.FollowUpAsync(SlashComms.ResponseBuilder(res));
                return;
            }
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();

            var tracks = await Optimizations.QueueUpSequence(ctx, search);
            await Optimizations.PlayUpSequence(ctx, node.GetGuildConnection(ctx.Member.VoiceState.Guild), tracks, false, true);
        }
    }
}
