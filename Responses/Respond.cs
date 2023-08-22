using DSharpPlus.EventArgs;
using DSharpPlus;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using Alice.Commands;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Alice.Responses
{
    public class MessageHandler
    {
        public static string GetRandomEntry(string category)
        {
            var xmlFilePath = "data.xml";
            
            if (File.Exists(xmlFilePath))
            {
                XDocument xmlDoc = XDocument.Load(xmlFilePath);

                var entries = xmlDoc.Descendants("category")
                                    .FirstOrDefault(e => e.Attribute("name")?.Value == category)
                                    ?.Elements("entry")
                                    .Select(e => e.Value)
                                    .ToList();

                if (entries != null && entries.Count > 0)
                {
                    Random random = new Random();
                    int randomIndex = random.Next(0, entries.Count);
                    return entries[randomIndex];
                }
            }

            return null;
        }

        public static async Task MessageCreatedHandler(DiscordClient client, MessageCreateEventArgs e)
        {
            // ALICE LISTENER
            
            if (e.Message.Author.IsBot && e.Message.Author.Username == "Alice")
            {
                Console.WriteLine("I heard Alice..");

                if (e.Message.Content.Contains("load", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("She said load");
                    var guild = e.Guild;
                    var messageContent = e.Message.Content;

                    var commandPrefix = "alice!load";
                    var list = messageContent.Substring(commandPrefix.Length).Trim();

                    await DisComms.LoadMusic(client, e.Message, guild, list);

                    return;
                }
                if (e.Message.Content.Contains("play", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("She said play..");
                    var guild = e.Guild;
                    var messageContent = e.Message.Content;

                    var commandPrefix = "alice!play";
                    var search = messageContent.Substring(commandPrefix.Length).Trim();

                    await DisComms.PlayMusic(client, e.Message, guild, search);

                    return;
                }
                if (e.Message.Content.Contains("skip", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("She said skip..");
                    var guild = e.Guild;

                    await DisComms.SkipMusic(client, e.Message, guild);

                    return;
                }
                if (e.Message.Content.Contains("np", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("She said np..");
                    var guild = e.Guild;

                    await DisComms.NpMusic(client, e.Message, guild);

                    return;
                }
                if (e.Message.Content.Contains("q", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("She said q");
                    var guild = e.Guild;

                    await DisComms.QueueMusic(e.Message, guild);

                    return;
                }
                if (e.Message.Content.Contains("ps", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("She said ps");
                    var guild = e.Guild;
                    var messageContent = e.Message.Content;

                    var commandPrefix = "alice!ps";
                    var search = messageContent.Substring(commandPrefix.Length).Trim();

                    await DisComms.PlaySkipMusic(client, e.Message, guild, search);

                    return;
                }
            }

            if (e.Message.Author.IsBot && e.Message.Author.Username != "Alice")
            {
                return;
            }

            // DETECTORS

            static bool _IsGreeting(string msg)
            {
                List<string> List = new List<string>
                    {
                              "Hello",
                              "Hi",
                              "Hey",
                              "Sup",
                              "Soup",
                              "Greetings",
                              "Konnichiwa",
                              "Konnichi-what's up",
                              "Ohayo",
                              "Haru"
                    };
                string pattern = $@"\b(?:{string.Join("|", List.Select(Regex.Escape))})\b";

                return Regex.IsMatch(msg, pattern, RegexOptions.IgnoreCase);
            }

            static bool _IsComplement(string msg)
            {
                List<string> List = new List<string>
                    {
                              "Good",
                              "Nice",
                              "Nice job",
                              "Good job",
                              "Naisu",
                              "Thanks",
                              "Great",
                              "Great job",
                              "Rock",
                              "Rocks",
                              "Based"
                    };
                string pattern = $@"\b(?:{string.Join("|", List.Select(Regex.Escape))})\b";

                return Regex.IsMatch(msg, pattern, RegexOptions.IgnoreCase);
            }

            static bool _IsInsult(string msg)
            {
                List<string> List = new List<string>
                    {
                              "Bad",
                              "Not Nice",
                              "Dammit",
                              "Curse",
                              "Curse you",
                              "Dang it",
                              "Damn",
                              "Biased",
                              "Frick",
                              "Frick you",
                              "Darn",
                              "Darn it",
                              "Suck",
                              "Sucks",
                              "Trash",
                              "Garbage",
                              "Acidic"
                    };
                string pattern = $@"\b(?:{string.Join("|", List.Select(Regex.Escape))})\b";

                return Regex.IsMatch(msg, pattern, RegexOptions.IgnoreCase);
            }

            // BOCCHI RESPONSES

            if (e.Message.Content.Contains("Bocchi", StringComparison.OrdinalIgnoreCase) || e.Message.Content.Contains("Bocchi's", StringComparison.OrdinalIgnoreCase))
            {
                string keyword = "Bocchi";
                string messageContent = e.Message.Content;

                int keywordIndex = messageContent.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
                int startIndexAfter = keywordIndex + keyword.Length;

                if (keywordIndex != -1)
                {
                    string Pre = messageContent.Substring(0, keywordIndex).Trim();
                    string Suf = messageContent.Substring(startIndexAfter).Trim();

                    if (_IsGreeting(Pre))
                    {
                        string category = "Greetings";

                        string randomEntry = GetRandomEntry(category);

                        if (randomEntry != null)
                        {
                            await e.Message.Channel.SendMessageAsync(randomEntry);
                            return;
                        }
                        else
                        {
                            string Entry = GetRandomEntry("Nanis");

                            if (Entry != null)
                            {
                                await e.Message.Channel.SendMessageAsync(Entry);
                                return;
                            }
                        }
                    }

                    if (_IsGreeting(Suf))
                    {
                        string category = "Greetings";

                        string randomEntry = GetRandomEntry(category);

                        if (randomEntry != null)
                        {
                            await e.Message.Channel.SendMessageAsync(randomEntry);
                            return;
                        }
                        else
                        {
                            string Entry = GetRandomEntry("Nanis");

                            if (Entry != null)
                            {
                                await e.Message.Channel.SendMessageAsync(Entry);
                                return;
                            }
                        }
                    }

                    if (_IsComplement(Pre))
                    {
                        string category = "Happy_Reacts";

                        string randomEntry = GetRandomEntry(category);

                        if (randomEntry != null)
                        {
                            await e.Message.Channel.SendMessageAsync(randomEntry);
                            return;
                        }
                        else
                        {
                            string Entry = GetRandomEntry("Nanis");

                            if (Entry != null)
                            {
                                await e.Message.Channel.SendMessageAsync(Entry);
                                return;
                            }
                        }
                    }

                    if (_IsComplement(Suf))
                    {
                        string category = "Happy_Reacts";

                        string randomEntry = GetRandomEntry(category);

                        if (randomEntry != null)
                        {
                            await e.Message.Channel.SendMessageAsync(randomEntry);
                            return;
                        }
                        else
                        {
                            string Entry = GetRandomEntry("Nanis");

                            if (Entry != null)
                            {
                                await e.Message.Channel.SendMessageAsync(Entry);
                                return;
                            }
                        }
                    }

                    if (_IsInsult(Pre))
                    {
                        string category = "Sad_Reacts";

                        string randomEntry = GetRandomEntry(category);

                        if (randomEntry != null)
                        {
                            await e.Message.Channel.SendMessageAsync(randomEntry);
                            return;
                        }
                        else
                        {
                            string Entry = GetRandomEntry("Nanis");

                            if (Entry != null)
                            {
                                await e.Message.Channel.SendMessageAsync(Entry);
                                return;
                            }
                        }
                    }

                    if (_IsInsult(Suf))
                    {
                        string category = "Sad_Reacts";

                        string randomEntry = GetRandomEntry(category);

                        if (randomEntry != null)
                        {
                            await e.Message.Channel.SendMessageAsync(randomEntry);
                            return;
                        }
                        else
                        {
                            string Entry = GetRandomEntry("Nanis");

                            if (Entry != null)
                            {
                                await e.Message.Channel.SendMessageAsync(Entry);
                                return;
                            }
                        }
                    }
                }

                if (e.Message.Content.Trim().Equals("bocchi", StringComparison.OrdinalIgnoreCase) || e.Message.Content.Trim().Equals("bocchi?", StringComparison.OrdinalIgnoreCase) || e.Message.Content.Trim().Equals("bocchi.", StringComparison.OrdinalIgnoreCase))
                {
                    string What = GetRandomEntry("Nanis");

                    if (What != null)
                    {
                        await e.Message.Channel.SendMessageAsync(What);
                    }
                }
            }

            //if (e.Message.Content.Contains("Surprise me", StringComparison.OrdinalIgnoreCase))
            //{
            //    ulong desiredChannelId = 747480792124620936;

            //    if (e.Message.Channel.Id == desiredChannelId)
            //    {
            //        // Get the guild from the message author
            //        SocketGuild guild = (message.Author as SocketGuildUser)?.Guild;

            //        SocketGuildUser AbUser = message.Author as SocketGuildUser;

            //        ITextChannel textChannel = e.Message.Channel as ITextChannel;

            //        if (AbUser.VoiceChannel != null)
            //        {
            //            // Find the user named "Bocchi" in the guild
            //            SocketGuildUser bocchiUser = guild?.Users.FirstOrDefault(user => user.Username == "Bocchi");
            //            if (bocchiUser != null && bocchiUser.VoiceChannel != null)
            //            {

            //                string webhookUrl = "https://discord.com/api/webhooks/971744013264777246/JtD_TzMdCDW7ZE5uHLDU_YgHYUCNsli17ZR6HA8epLYT0MI23RYYAzPJPg0NwFaNZChG";

            //                Task.Delay(TimeSpan.FromSeconds(1)).Wait();
            //                e.Message.Channel.SendMessageAsync("Uhh.. I don't know how..");
            //                Task.Delay(TimeSpan.FromSeconds(2)).Wait();
            //                // First message
            //                string content1 = "Wait, I got this..";
            //                string payload1 = $"{{\"content\": \"{content1}\"}}";
            //                using (HttpClient httpClient = new HttpClient())
            //                {
            //                    HttpContent httpContent1 = new StringContent(payload1, Encoding.UTF8, "application/json");
            //                    httpClient.PostAsync(webhookUrl, httpContent1).Wait();
            //                }

            //                Task.Delay(TimeSpan.FromSeconds(2)).Wait();
            //                // Read the contents of the file into an array of strings
            //                string[] lines = File.ReadAllLines("Surprises.txt");

            //                // Choose a random entry from the array
            //                Random random = new Random();
            //                string song = lines[random.Next(lines.Length)];

            //                // Second message
            //                string content2 = $"bocchi!AlicePlay {song}";
            //                string payload2 = $"{{\"content\": \"{content2}\"}}";
            //                using (HttpClient httpClient = new HttpClient())
            //                {
            //                    HttpContent httpContent2 = new StringContent(payload2, Encoding.UTF8, "application/json");
            //                    httpClient.PostAsync(webhookUrl, httpContent2).Wait();
            //                }
            //            }
            //            else
            //            {
            //                Task.Delay(TimeSpan.FromSeconds(1)).Wait();
            //                string webhookUrl = "https://discord.com/api/webhooks/971744013264777246/JtD_TzMdCDW7ZE5uHLDU_YgHYUCNsli17ZR6HA8epLYT0MI23RYYAzPJPg0NwFaNZChG";
            //                string content3 = "C'mon guys, atleast let her in a voice channel first..";
            //                string payload3 = $"{{\"content\": \"{content3}\"}}";
            //                using (HttpClient httpClient = new HttpClient())
            //                {
            //                    HttpContent httpContent3 = new StringContent(payload3, Encoding.UTF8, "application/json");
            //                    httpClient.PostAsync(webhookUrl, httpContent3).Wait();
            //                }

            //                Task.Delay(TimeSpan.FromSeconds(2)).Wait();

            //                string content4 = "Come in, Bocchi..";
            //                string payload4 = $"{{\"content\": \"{content4}\"}}";
            //                using (HttpClient httpClient = new HttpClient())
            //                {
            //                    HttpContent httpContent4 = new StringContent(payload4, Encoding.UTF8, "application/json");
            //                    httpClient.PostAsync(webhookUrl, httpContent4).Wait();
            //                }


            //                LavaLinkAudio.JoinAliceAsync(guild, AbUser as IVoiceState, textChannel);
            //                Task.Delay(TimeSpan.FromSeconds(1)).Wait();
            //                e.Message.Channel.SendMessageAsync("Thanks Alice..");
            //            }
            //        }
            //        else
            //        {
            //            string webhookUrl = "https://discord.com/api/webhooks/971744013264777246/JtD_TzMdCDW7ZE5uHLDU_YgHYUCNsli17ZR6HA8epLYT0MI23RYYAzPJPg0NwFaNZChG";
            //            string content3 = "You guys know that's not how it works, you gotta be the first one to be in the voice channel..";
            //            string payload3 = $"{{\"content\": \"{content3}\"}}";
            //            using (HttpClient httpClient = new HttpClient())
            //            {
            //                HttpContent httpContent3 = new StringContent(payload3, Encoding.UTF8, "application/json");
            //                httpClient.PostAsync(webhookUrl, httpContent3).Wait();
            //            }
            //        }
            //    }
            //    else
            //    {
            //        e.Message.Channel.SendMessageAsync("Uhh.. I don't know how and unfortunately Alice likes to stay at the canteen..");
            //    }
            //}

            if (e.Message.Content.Contains("Alice", StringComparison.OrdinalIgnoreCase))
            {
                ulong desiredChannelId = 747480792124620936;

                if (e.Message.Channel.Id == desiredChannelId)
                {
                    string keyword = "Alice";
                    string messageContent = e.Message.Content;

                    int keywordIndex = messageContent.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);

                    if (keywordIndex != -1)
                    {
                        string extractedContent = messageContent.Substring(0, keywordIndex).Trim();

                        if (_IsGreeting(extractedContent))
                        {
                            string category = "Greetings";

                            string randomEntry = GetRandomEntry(category);
                            string content = randomEntry;
                            string payload = $"{{\"content\": \"{content}\"}}";

                            if (randomEntry != null)
                            {
                                using (HttpClient httpClient = new HttpClient())
                                {
                                    HttpContent httpContent = new StringContent(payload, Encoding.UTF8, "application/json");
                                    httpClient.PostAsync(Program.Alice, httpContent).Wait();
                                }
                            }
                            else
                            {
                                await e.Message.Channel.SendMessageAsync("No entries found for the specified category.");
                            }
                        }
                    }
                }
                else
                {
                    await e.Message.Channel.SendMessageAsync("Uhh.. She isn't here, Alice likes to stay at the canteen..");
                }
            }

            if (e.Message.Content.Contains("Kita", StringComparison.OrdinalIgnoreCase))
            {
                string category = "kita";

                string randomEntry = GetRandomEntry(category);

                if (randomEntry != null)
                {
                    await e.Message.Channel.SendMessageAsync(randomEntry);
                }
                else
                {
                    await e.Message.Channel.SendMessageAsync("No entries found for the specified category.");
                }
            }

            if (e.Message.Content.Contains("Congrats", StringComparison.OrdinalIgnoreCase))
            {
                string category = "Celebrative_Reacts";

                string randomEntry = GetRandomEntry(category);

                if (randomEntry != null)
                {
                    await e.Message.Channel.SendMessageAsync(randomEntry);
                }
                else
                {
                    await e.Message.Channel.SendMessageAsync("No entries found for the specified category.");
                }
            }

            if (e.Message.Content.Contains("Brother", StringComparison.OrdinalIgnoreCase))
            {
                await e.Message.Channel.SendMessageAsync("Sister even..");
            }

            if (e.Message.Content.Contains("Buddy", StringComparison.OrdinalIgnoreCase))
            {
                await e.Message.Channel.SendMessageAsync("Baka~");
            }

            if (e.Message.Content.Contains("So cool", StringComparison.OrdinalIgnoreCase))
            {
                await e.Message.Channel.SendMessageAsync("https://i.imgur.com/kNh7Qlo.png");
            }

            if (e.Message.Content.Contains("Pipebomb", StringComparison.OrdinalIgnoreCase))
            {
                await e.Message.Channel.SendMessageAsync("https://i.imgur.com/2aeyQ8D.png");
            }

            return;
        }
    }
}
