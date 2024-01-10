using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus;
using System.Threading.Tasks;
using System.Linq;
using RconSharp;
using Alice_Module.Loaders;
using System;
using System.Collections.Generic;
using Alice_Module.Handlers;

namespace Alice.Commands
{
    public class DisComms
    {
        
        // ALICE!PLAY
        public static async Task PlayMusic(DiscordClient client, DiscordMessage message, DiscordGuild guild, string search)
        {
            var lava = client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();

            if (SlashComms._ready == false)
            {
                await message.RespondAsync("She's not ready yet, make sure she's settled in a vc already before you execute ingame commands..");
            }

            if (SlashComms._lavastarted == false)                                                                    // LAVALINK CHECK
            {
                await message.RespondAsync("Please execute /start first so I can boot up the music player..");
                return;
            }

            if (!lava.ConnectedNodes.Any())
            {
                await message.RespondAsync("Lavalink not connected.");
                return;
            }

            if (Validates.IsYoutubeLink(search))
            {
                string validifiedLink = Converts.ConvertToShortenedUrl(search);
                await PlayLoader.GetVideoTitleAsync(validifiedLink);
                search = validifiedLink;
            }

            var loadResult = await node.Rest.GetTracksAsync(search);

            if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                await message.RespondAsync($"Failed to look for {search}");
                await RconSend($"/tellraw @a {{\"text\":\"Bocchi: Failed to look for {search}\",\"color\":\"light_purple\",\"bold\":false}}", guild);
                return;
            }

            var track = loadResult.Tracks.First();
            var conn = node.GetGuildConnection(guild);

            try
            {
                if (conn.CurrentState.CurrentTrack == null)
                {

                    if (SlashComms._invited == true)
                    {
                        if (SlashComms._queueDictionary.ContainsKey(guild.Id))
                        {
                            SlashComms._queueDictionary[guild.Id].Add(new song(track, "Alice"));
                        }
                        else
                        {
                            SlashComms._queueDictionary.Add(guild.Id, new List<song>());
                            await Task.Delay(100);
                            SlashComms._queueDictionary[guild.Id].Add(new song(track, "Alice"));
                        }
                        Program.skipped = true;
                        await conn.PlayAsync(track);
                        Program.skipped = false;

                        await message.RespondAsync($"Now Playing: {track.Title}");
                        await RconSend($"/tellraw @a {{\"text\":\"Bocchi: Now Playing - {track.Title}\",\"color\":\"light_purple\",\"bold\":false}}", guild);
                        return;
                    }
                    else
                    {
                        if (SlashComms._queueDictionary.ContainsKey(guild.Id))
                        {
                            SlashComms._queueDictionary[guild.Id].Add(new song(track, "Alice"));
                        }
                        else
                        {
                            SlashComms._queueDictionary.Add(guild.Id, new List<song>());
                            await Task.Delay(100);
                            SlashComms._queueDictionary[guild.Id].Add(new song(track, "Alice"));
                        }
                        Program.skipped = true;
                        await conn.PlayAsync(track);
                        Program.skipped = false;

                        await message.RespondAsync($"Sigh, guess I'll let myself in..\n \nNow Playing: {track.Title}");
                        await RconSend($"/tellraw @a {{\"text\":\"Bocchi: Now Playing - {track.Title}\",\"color\":\"light_purple\",\"bold\":false}}", guild);
                        return;
                    }
                }
                else
                {
                    if (SlashComms._queueDictionary[guild.Id].Count >= SlashComms.MaxQueueSize)
                    {
                        await message.RespondAsync($"Max queue length was set to {SlashComms.MaxQueueSize}, wait for songs to finish");
                        await RconSend($"/tellraw @a {{\"text\":\"Bocchi: Max queue length was set to {SlashComms.MaxQueueSize}, wait for songs to finish\",\"color\":\"light_purple\",\"bold\":false}}", guild);
                        return;
                    }
                    else
                    {
                        // Add the track to the song queue
                        if (SlashComms._queueDictionary.ContainsKey(guild.Id))
                        {
                            SlashComms._queueDictionary[guild.Id].Add(new song(track, "Alice"));
                        }
                        else
                        {
                            SlashComms._queueDictionary.Add(guild.Id, new List<song>());
                            await Task.Delay(100);
                            SlashComms._queueDictionary[guild.Id].Add(new song(track, "Alice"));
                        }

                        await message.RespondAsync($"Added to Queue: {track.Title}");
                        await RconSend($"/tellraw @a {{\"text\":\"Bocchi: Added to Queue - {track.Title}..\",\"color\":\"light_purple\",\"bold\":false}}", guild);
                        return;
                    }
                }
            }
            catch
            {
                await message.RespondAsync($"{track.Title} failed to play");
                await RconSend($"/tellraw @a {{\"text\":\"Bocchi: {track.Title} failed to play\",\"color\":\"light_purple\",\"bold\":false}}", guild);
                return;
            }
        }


        //ALICE!SKIP
        public static async Task SkipMusic(DiscordClient client, DiscordMessage message, DiscordGuild guild)
        {
            if (SlashComms._lavastarted == false)                                                                    // LAVALINK CHECK
            {
                await message.RespondAsync("Please execute /start first so I can boot up the music player..");
                return;
            }

            if (SlashComms._queueDictionary[guild.Id].Count < 1)
            {
                await message.RespondAsync("Buddy, there's no song to skip to..");
                await RconSend($"/tellraw @a {{\"text\":\"Bocchi: Buddy, there's no song to skip to..\",\"color\":\"light_purple\",\"bold\":false}}", guild);
                return;
            }

            var lava = client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();

            var conn = node.GetGuildConnection(guild);

            if (conn.CurrentState.CurrentTrack != null)
            {
                var nextTrack = SlashComms._queueDictionary[guild.Id][1];
                var nextTrackTitle = nextTrack.getTrack().Title;
                var track = SlashComms._queueDictionary[guild.Id][0];
                var trackTitle = track.getTrack().Title;
                Program.skipped = true;
                await conn.PlayAsync(nextTrack.getTrack());
                SlashComms._queueDictionary[guild.Id].RemoveAt(0);
                Program.skipped = false;

                await message.RespondAsync($"Skipped {trackTitle}, now playing {nextTrackTitle}..");
                await RconSend($"/tellraw @a {{\"text\":\"Bocchi: Skipped {trackTitle}, now playing {nextTrackTitle}..\",\"color\":\"light_purple\",\"bold\":false}}", guild);
            }
            else
            {
                await message.RespondAsync("Buddy, there's no song to skip to..");
                await RconSend($"/tellraw @a {{\"text\":\"Bocchi: Buddy, there's no song to skip to..\",\"color\":\"light_purple\",\"bold\":false}}", guild);
            }
        }


        // ALICE!NP
        public static async Task NpMusic(DiscordClient client, DiscordMessage message, DiscordGuild guild)
        {
            var lava = client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();

            if (SlashComms._lavastarted == false)                                                                    // LAVALINK CHECK
            {
                await message.RespondAsync("Please execute /start first so I can boot up the music player..");
                return;
            }

            var conn = node.GetGuildConnection(guild);

            if (conn == null)
            {
                await message.RespondAsync("Brother, I'm not even in a voice channel yet..");
                return;
            }

            if (conn.CurrentState.CurrentTrack != null)
            {
                var currentTrack = conn.CurrentState.CurrentTrack;
                var trackInfo = $"{currentTrack.Title} {currentTrack.Length}";
                await message.RespondAsync($"{currentTrack.Title} {currentTrack.Length}");
                await RconSend($"/tellraw @a {{\"text\":\"Bocchi: {currentTrack.Title} {currentTrack.Length}\",\"color\":\"light_purple\",\"bold\":false}}", guild);
            }
            else
            {
                await message.RespondAsync("Nothing but silence..");
                await RconSend("/tellraw @a {\"text\":\"Bocchi: Nothing but silence..\",\"color\":\"light_purple\",\"bold\":false}", guild);
            }
        }

        // ALICE!QUEUE
        public static async Task QueueMusic(DiscordMessage message, DiscordGuild guild)
        {
            if (SlashComms._queueDictionary[guild.Id].Count == 0)
            {
                await RconSend("/tellraw @a {\"text\":\"Bocchi: The queue list is blank..\",\"color\":\"light_purple\",\"bold\":false}", guild);
            }
            else
            {
                await RconSend("/tellraw @a {\"text\":\"Bocchi: Look at all these songs:\",\"color\":\"light_purple\",\"bold\":false}", guild);
                var queueContent = string.Join("\n", SlashComms._queueDictionary[guild.Id].Select((track, index) =>
                {
                    var prefix = (index == 0) ? "【Now Playing】 " : string.Empty;
                    return $"{index + 1}. {prefix}{track.getTrack().Title}";
                }));

                var lines = queueContent.Split('\n');

                foreach (var line in lines)
                {
                    var trimmedLine = line.Length <= 50 ? line : line.Substring(0, 50);
                    await RconSend($"/tellraw @a {{\"text\":\"{trimmedLine}\",\"color\":\"dark_purple\",\"bold\":false}}", guild);
                }
            }

            await message.Channel.SendMessageAsync("Constructed queue.");
        }

        // ALICE!PS
        public static async Task PlaySkipMusic(DiscordClient client, DiscordMessage message, DiscordGuild guild, string search)
        {
            var lava = client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();

            if (SlashComms._ready == false)
            {
                await message.RespondAsync("She's not ready yet, make sure she's settled in a vc already before you execute ingame commands..");
            }

            if (SlashComms._lavastarted == false)                                                                    // LAVALINK CHECK
            {
                await message.RespondAsync("Please execute /start first so I can boot up the music player..");
                return;
            }

            if (!lava.ConnectedNodes.Any())
            {
                await message.RespondAsync("Lavalink not connected.");
                return;
            }

            if (Validates.IsYoutubeLink(search))
            {
                string validifiedLink = Converts.ConvertToShortenedUrl(search);
                await PlayLoader.GetVideoTitleAsync(validifiedLink);
                search = validifiedLink;
            }

            var loadResult = await node.Rest.GetTracksAsync(search);

            if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                await message.RespondAsync($"Failed to look for {search}");
                await RconSend($"/tellraw @a {{\"text\":\"Bocchi: Failed to look for {search}\",\"color\":\"light_purple\",\"bold\":false}}", guild);
                return;
            }

            var track = loadResult.Tracks.First();
            var conn = node.GetGuildConnection(guild);

            try
            {
                Program.skipped = true;
                if (SlashComms._queueDictionary.ContainsKey(guild.Id))
                {
                    SlashComms._queueDictionary[guild.Id].Insert(0, new song(track, "Alice"));
                    SlashComms._queueDictionary[guild.Id].RemoveAt(1);
                    await conn.PlayAsync(track);

                    await message.RespondAsync($"Found it. Now Playing: {track.Title}");
                    await RconSend($"/tellraw @a {{\"text\":\"Bocchi: Found it. Now Playing - {track.Title}\",\"color\":\"light_purple\",\"bold\":false}}", guild);
                    Program.skipped = false;
                }
                else
                {
                    SlashComms._queueDictionary.Add(guild.Id, new List<song>());
                    await Task.Delay(100);
                    SlashComms._queueDictionary[guild.Id].Insert(0, new song(track, "Alice"));
                    SlashComms._queueDictionary[guild.Id].RemoveAt(1);
                    await conn.PlayAsync(track);

                    await message.RespondAsync($"Found it. Now Playing: {track.Title}");
                    await RconSend($"/tellraw @a {{\"text\":\"Bocchi: Found it. Now Playing - {track.Title}\",\"color\":\"light_purple\",\"bold\":false}}", guild);
                    Program.skipped = false;
                }
            }
            catch
            {
                await message.RespondAsync($"{track.Title} failed to play");
                await RconSend($"/tellraw @a {{\"text\":\"Bocchi: {track.Title} failed to play..\",\"color\":\"light_purple\",\"bold\":false}}", guild);
            }
        }

        // ALICE!LOAD
        public static async Task LoadMusic(DiscordClient client, DiscordMessage message, DiscordGuild guild, string list)
        {
            List<string> songTitles;

            if (Validates.IsYouTubePlaylistLink(list))
            {
                songTitles = await PlayLoader.YoutubeLoaderAsync(list);
            }

            else if (Validates.IsSpotifyPlaylistLink(list))
            {
                //songTitles = await PlayLoader.SpotifyLoaderAsync(list);
                await message.RespondAsync("Sean-san is not a Spotify Developer yet so I don't have support for Spotify links at the moment..");
                await RconSend($"/tellraw @a {{\"text\":\"Alice: Sean-san is not a Spotify Developer yet so I don't have support for Spotify links at the moment..\",\"color\":\"dark_purple\",\"bold\":false}}", guild);

                return;
            }
            else
            {
                await message.RespondAsync("Invalid playlist link.");
                await RconSend($"/tellraw @a {{\"text\":\"Alice: Invalid playlist link.\",\"color\":\"dark_purple\",\"bold\":false}}", guild);
                return;
            }

            if (songTitles == null || songTitles.Count == 0)
            {
                await message.RespondAsync("No song titles found in the playlist.");
                return;
            }

            foreach (string title in songTitles)
            {
                if (PlayLoader._queuefull == true)
                {
                    await message.RespondAsync("Playlist loaded.");
                    await RconSend($"/tellraw @a {{\"text\":\"Alice: Playlist Loaded..\",\"color\":\"dark_purple\",\"bold\":false}}", guild);
                    PlayLoader._queuefull = false;
                    break;
                }

                await Enqueue(client, message, guild, title);
            }

            if (PlayLoader._queuefull != true)
            {
                await message.RespondAsync("Playlist loaded.");
                await RconSend($"/tellraw @a {{\"text\":\"Alice: Playlist Loaded..\",\"color\":\"dark_purple\",\"bold\":false}}", guild);
            }
        }

        // ENQUEUE HELPER
        public static async Task Enqueue(DiscordClient client, DiscordMessage message, DiscordGuild guild, string search)
        {
            var lava = client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();

            if (SlashComms._ready == false)
            {
                await message.RespondAsync("She's not ready yet, make sure she's settled in a vc already before you execute ingame commands..");
                await RconSend($"/tellraw @a {{\"text\":\"Alice: She's not ready yet, make sure she's settled in a vc already before you execute ingame commands..\",\"color\":\"dark_purple\",\"bold\":false}}", guild);
            }

            if (SlashComms._lavastarted == false)                                                                    // LAVALINK CHECK
            {
                await message.RespondAsync("Please execute /start first so I can boot up the music player..");
                await RconSend($"/tellraw @a {{\"text\":\"Alice: The player is not started..\",\"color\":\"dark_purple\",\"bold\":false}}", guild);

                return;
            }

            if (!lava.ConnectedNodes.Any())
            {
                await message.RespondAsync("Lavalink not connected.");
                await RconSend($"/tellraw @a {{\"text\":\"Alice: Sean-san send help pls..\",\"color\":\"dark_purple\",\"bold\":false}}", guild);

                return;
            }

            var loadResult = await node.Rest.GetTracksAsync(search);

            if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                await message.RespondAsync($"Failed to look for {search}");
                return;
            }

            var track = loadResult.Tracks.First();
            var conn = node.GetGuildConnection(guild);

            try
            {
                if (conn.CurrentState.CurrentTrack == null)
                {

                    if (SlashComms._invited == true)
                    {
                        if (SlashComms._queueDictionary.ContainsKey(guild.Id))
                        {
                            SlashComms._queueDictionary[guild.Id].Add(new song(track, "Alice"));
                            Program.skipped = true;
                            await conn.PlayAsync(track);

                            await message.RespondAsync($"Loading Playlist.. \n \nNow Playing: {track.Title}");
                            await RconSend($"/tellraw @a {{\"text\":\"Alice: Loading Playlist..\",\"color\":\"dark_purple\",\"bold\":false}}", guild);
                            await RconSend($"/tellraw @a {{\"text\":\"Alice: Now Playing: {track.Title}\",\"color\":\"dark_purple\",\"bold\":false}}", guild);

                            Program.skipped = false;
                        }
                        else
                        {
                            SlashComms._queueDictionary.Add(guild.Id, new List<song>());
                            await Task.Delay(100);
                            SlashComms._queueDictionary[guild.Id].Add(new song(track, "Alice"));
                            Program.skipped = true;
                            await conn.PlayAsync(track);

                            await message.RespondAsync($"Loading Playlist.. \n \nNow Playing: {track.Title}");
                            await RconSend($"/tellraw @a {{\"text\":\"Alice: Loading Playlist..\",\"color\":\"dark_purple\",\"bold\":false}}", guild);
                            await RconSend($"/tellraw @a {{\"text\":\"Alice: Now Playing: {track.Title}\",\"color\":\"dark_purple\",\"bold\":false}}", guild);

                            Program.skipped = false;
                        }
                    }
                    else
                    {
                        if (SlashComms._queueDictionary.ContainsKey(guild.Id))
                        {
                            SlashComms._queueDictionary[guild.Id].Add(new song(track, "Alice"));
                            Program.skipped = true;
                            await conn.PlayAsync(track);

                            await message.RespondAsync($"Loading Playlist.. \n \nNow Playing: {track.Title}");
                            await RconSend($"/tellraw @a {{\"text\":\"Alice: Loading Playlist..\",\"color\":\"dark_purple\",\"bold\":false}}", guild);
                            await RconSend($"/tellraw @a {{\"text\":\"Alice: Now Playing: {track.Title}\",\"color\":\"dark_purple\",\"bold\":false}}", guild);
                            Program.skipped = false;
                        }
                        else
                        {
                            SlashComms._queueDictionary.Add(guild.Id, new List<song>());
                            await Task.Delay(100);
                            SlashComms._queueDictionary[guild.Id].Add(new song(track, "Alice"));
                            Program.skipped = true;
                            await conn.PlayAsync(track);

                            await message.RespondAsync($"Loading Playlist.. \n \nNow Playing: {track.Title}");
                            await RconSend($"/tellraw @a {{\"text\":\"Alice: Loading Playlist..\",\"color\":\"dark_purple\",\"bold\":false}}", guild);
                            await RconSend($"/tellraw @a {{\"text\":\"Alice: Now Playing: {track.Title}\",\"color\":\"dark_purple\",\"bold\":false}}", guild);
                            Program.skipped = false;
                        }
                    }
                }
                else
                {
                    if (SlashComms._queueDictionary[guild.Id].Count >= SlashComms.MaxQueueSize)
                    {
                        PlayLoader._queuefull = true;
                        await message.RespondAsync($"Max queue length was set to {SlashComms.MaxQueueSize}, wait for songs to finish");
                    }
                    else
                    {
                        SlashComms._queueDictionary[guild.Id].Add(new song(track, "Alice"));
                    }
                }
            }
            catch
            {
                await message.RespondAsync($"{track.Title} failed to play");
                await RconSend($"/tellraw @a {{\"text\":\"Alice: Failed to play {track.Title}..\",\"color\":\"dark_purple\",\"bold\":false}}", guild);
            }
        }

        // SERVER MESSAGING SERVICE
        public static async Task RconSend(string message, DiscordGuild guild)
        {
            try
            {
                //string serverIp = Program.doc.Descendants("category")
                //.FirstOrDefault(category => category.Attribute("name")?.Value == "sIP")?
                //.Element("entry").Value;
                string serverIp = Program.IPs[guild.Id];
                
                int serverPort = int.Parse(Program.doc.Descendants("category")
                .FirstOrDefault(category => category.Attribute("name")?.Value == "sPort")?
                .Element("entry").Value);

                var rclient = RconClient.Create($"{serverIp}", serverPort);

                await rclient.ConnectAsync();

                var authenticated = await rclient.AuthenticateAsync("727");      //FOR CLEANUP
                if (authenticated)
                {
                    await rclient.ExecuteCommandAsync($"{message}");
                }
            }
            catch
            {
                Console.WriteLine("In-game messaging service failed");
                return;
            }
        }
    }
}
