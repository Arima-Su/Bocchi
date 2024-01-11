using System;
using Alice.Commands;
using DSharpPlus.CommandsNext;
using DSharpPlus.SlashCommands;
using DSharpPlus.Lavalink;
using System.Linq;
using Alice_Module.Loaders;
using System.Threading.Tasks;
using Alice;
using System.Collections.Generic;
using Alice.Responses;

namespace Alice_Module.Handlers
{
    public class Optimizations
    {

        //START UP

        public static string StartUpSequence(InteractionContext ctx, bool join = false)
        {
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();

            if (SlashComms._lavastarted == false)                                                                    // LAVALINK CHECK
            {
                return "Please execute /start first so I can boot up the music player..";
            }

            if (ctx.Member.VoiceState == null)                                                            // VOICE CHANNEL CHECK
            {
                return $"{MessageHandler.GetRandomEntry("Nanis")} get into a voice channel first..";
            }

            var cont = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (cont == null)                                                                            // AUTO JOIN
            {
                var channel = ctx.Member.VoiceState.Channel;

                if (channel.Type is DSharpPlus.ChannelType.Voice)
                {
                    try
                    {
                        node.ConnectAsync(channel);
                        SlashComms._ready = true;
                        
                        if(join)
                        {
                            ctx.FollowUpAsync(SlashComms.ResponseBuilder("Thanks.."));
                        }
                    }
                    catch
                    {
                        return $"{MessageHandler.GetRandomEntry("Nanis")} you're not even in a voice channel yet..";
                    }
                }
            }
            else
            {
                if (join)
                {
                    ctx.FollowUpAsync(SlashComms.ResponseBuilder($"{MessageHandler.GetRandomEntry("Nanis")} I'm already here tho.."));
                }
            }

            if (!lava.ConnectedNodes.Any())                                                             // LAVALINK VERIFY
            {
                return "Lavalink not connected.";
            }

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)             // BOT VOICESTATE VERIFY
            {
                return $"{MessageHandler.GetRandomEntry("Nanis")} I'm not even in a voice channel..";
            }

            return null;
        }

        public static string StartUpSequence(CommandContext ctx, bool join = false)
        {
            if (SlashComms._lavastarted == false)                                                                    // LAVALINK CHECK
            {
                return "Please execute /start first so I can boot up the music player..";
            }

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();

            if (ctx.Member.VoiceState == null)                                                            // VOICE CHANNEL CHECK
            {
                return $"{MessageHandler.GetRandomEntry("Nanis")} get into a voice channel first..";
            }

            var cont = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (cont == null)                                                                            // AUTO JOIN
            {
                var channel = ctx.Member.VoiceState.Channel;

                if (channel.Type is DSharpPlus.ChannelType.Voice)
                {
                    try
                    {
                        node.ConnectAsync(channel);
                        SlashComms._ready = true;

                        if (join)
                        {
                            ctx.Channel.SendMessageAsync("Thanks..");
                        }
                    }
                    catch
                    {
                        return $"{MessageHandler.GetRandomEntry("Nanis")} you're not even in a voice channel yet..";
                    }
                }
            }
            else
            {
                if (join)
                {
                    ctx.Channel.SendMessageAsync($"{MessageHandler.GetRandomEntry("Nanis")} I'm already here tho..");
                }
            }

            if (!lava.ConnectedNodes.Any())                                                             // LAVALINK VERIFY
            {
                return "Lavalink not connected.";
            }

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)             // BOT VOICESTATE VERIFY
            {
                return $"{MessageHandler.GetRandomEntry("Nanis")} I'm not even in a voice channel.";
            }

            return null;
        }

        // QUEUE UP

        public static async Task<LavalinkTrack> QueueUpSequence(InteractionContext ctx, string search)
        {
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();

            if (Validates.IsYoutubeLink(search))
            {
                search = Converts.ConvertToShortenedUrl(search);

                var loadResult = await node.Rest.GetTracksAsync(search, LavalinkSearchType.Plain);

                if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
                {
                    await ctx.FollowUpAsync(SlashComms.ResponseBuilder($"Failed to look for {search}"));
                    return null;
                }

                return loadResult.Tracks.First();
            }
            else if (Validates.IsSpotifyLink(search))
            {
                var loadResult = await node.Rest.GetTracksAsync(search, LavalinkSearchType.Plain);

                if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
                {
                    await ctx.FollowUpAsync(SlashComms.ResponseBuilder($"Failed to look for {search}"));
                    return null;
                }

                return loadResult.Tracks.First();
            }
            else if (Validates.IsSpotifyPlaylistLink(search))
            {
                await ctx.FollowUpAsync(SlashComms.ResponseBuilder("That's a playlist link.. provide a song link please.."));
                return null;
            }
            else if (Validates.IsYouTubePlaylistLink(search))
            {
                await ctx.FollowUpAsync(SlashComms.ResponseBuilder("That's a playlist link.. provide a song link please.."));
                return null;
            }
            else
            {
                LavalinkLoadResult loadResult = await node.Rest.GetTracksAsync(search, LavalinkSearchType.Youtube);

                if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
                {
                    await ctx.FollowUpAsync(SlashComms.ResponseBuilder($"Failed to look for {search}"));
                    return null;
                }

                return loadResult.Tracks.First();
            }
        }

        public static async Task<LavalinkTrack> QueueUpSequence(CommandContext ctx, string search)
        {
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();

            if (Validates.IsYoutubeLink(search))
            {
                search = Converts.ConvertToShortenedUrl(search);

                var loadResult = await node.Rest.GetTracksAsync(search, LavalinkSearchType.Plain);

                if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
                {
                    await ctx.Channel.SendMessageAsync($"Failed to look for {search}");
                    return null;
                }

                return loadResult.Tracks.First();
            }
            else if (Validates.IsSpotifyLink(search))
            {
                var loadResult = await node.Rest.GetTracksAsync(search, LavalinkSearchType.Plain);

                if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
                {
                    await ctx.Channel.SendMessageAsync($"Failed to look for {search}");
                    return null;
                }

                return loadResult.Tracks.First();
            }
            else if (Validates.IsSpotifyPlaylistLink(search))
            {
                await ctx.Channel.SendMessageAsync("That's a playlist link.. provide a song link please..");
                return null;
            }
            else if (Validates.IsYouTubePlaylistLink(search))
            {
                await ctx.Channel.SendMessageAsync("That's a playlist link.. provide a song link please..");
                return null;
            }
            else
            {
                LavalinkLoadResult loadResult = await node.Rest.GetTracksAsync(search, LavalinkSearchType.Youtube);

                if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
                {
                    await ctx.Channel.SendMessageAsync($"Failed to look for {search}");
                    return null;
                }

                return loadResult.Tracks.First();
            }
        }

        // PLAY UP

        public static async Task PlayUpSequence(CommandContext ctx, LavalinkGuildConnection conn, LavalinkTrack track, bool skip = false, bool list = false)
        {
            var guild = ctx.Guild.Id;

            //if (Program.Volumes.ContainsKey(guild))
            //{
            //    await conn.SetVolumeAsync(Program.Volumes[guild]);
            //}
            //else
            //{
            //    await conn.SetVolumeAsync(50);
            //}

            //await conn.SetVolumeAsync(50);

            try
            {
                if (conn.CurrentState.CurrentTrack == null || skip)
                {

                    if (SlashComms._queueDictionary.ContainsKey(guild))
                    {
                        SlashComms._queueDictionary[guild].Add(new song(track, ctx.User.Username));
                    }
                    else
                    {
                        SlashComms._queueDictionary.Add(guild, new List<song>());
                        await Task.Delay(100);
                        SlashComms._queueDictionary[guild].Add(new song(track, ctx.User.Username));
                    }

                    Program.skipped = true;
                    await conn.PlayAsync(track);

                    await ctx.Channel.SendMessageAsync($"Now Playing: {track.Title} {track.Author}");
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
                    if (SlashComms._queueDictionary[guild].Count >= SlashComms.MaxQueueSize)
                    {
                        await ctx.Channel.SendMessageAsync($"Max queue length was set to {SlashComms.MaxQueueSize}, wait for songs to finish");
                    }
                    else
                    {
                        if (SlashComms._queueDictionary.ContainsKey(guild))
                        {
                            SlashComms._queueDictionary[guild].Add(new song(track, ctx.User.Username));
                        }

                        if(!list)
                        {
                            await ctx.Channel.SendMessageAsync($"Added to Queue: {track.Title} {track.Author}");
                        } 
                    }
                }
            }
            catch
            {
                await ctx.Channel.SendMessageAsync($"{track.Title} {track.Author} failed to play");
                SlashComms._queueDictionary[guild].RemoveAt(0);
                await ctx.Channel.SendMessageAsync("I'm gonna try that again..");
                await PlayUpSequence(ctx, conn, track, skip);
            }
        }

        public static async Task PlayUpSequence(InteractionContext ctx, LavalinkGuildConnection conn, LavalinkTrack track, bool skip = false, bool list = false)
        {
            var guild = ctx.Guild.Id;

            try
            {
                if (conn.CurrentState.CurrentTrack == null || skip)
                {

                    if (SlashComms._queueDictionary.ContainsKey(guild))
                    {
                        SlashComms._queueDictionary[guild].Add(new song(track, ctx.User.Username));
                    }
                    else
                    {
                        SlashComms._queueDictionary.Add(guild, new List<song>());
                        await Task.Delay(100);
                        SlashComms._queueDictionary[guild].Add(new song(track, ctx.User.Username));
                    }

                    Program.skipped = true;
                    await conn.PlayAsync(track);

                    await ctx.FollowUpAsync(SlashComms.ResponseBuilder($"Now Playing: {track.Title} {track.Author}"));
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
                    if (SlashComms._queueDictionary[guild].Count >= SlashComms.MaxQueueSize)
                    {
                        await ctx.FollowUpAsync(SlashComms.ResponseBuilder($"Max queue length was set to {SlashComms.MaxQueueSize}, wait for songs to finish"));
                    }
                    else
                    {
                        if (SlashComms._queueDictionary.ContainsKey(guild))
                        {
                            SlashComms._queueDictionary[guild].Add(new song(track, ctx.User.Username));
                        }

                        if (!list)
                        {
                            await ctx.FollowUpAsync(SlashComms.ResponseBuilder($"Added to Queue: {track.Title} {track.Author}"));
                        }
                    }
                }
            }
            catch
            {
                await ctx.FollowUpAsync(SlashComms.ResponseBuilder($"{track.Title} {track.Author} failed to play"));
                SlashComms._queueDictionary[guild].RemoveAt(0);
                await ctx.FollowUpAsync(SlashComms.ResponseBuilder("I'm gonna try that again.."));
                await PlayUpSequence(ctx, conn, track, skip);
            }
        }
    }
}
