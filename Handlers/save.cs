using Alice;
using Discord;
using Discord.WebSocket;
using SpotifyAPI.Web;
using System;
using System.Text;
using System.Threading.Tasks;
using UnidecodeSharpCore;

namespace Alice_Module.Handlers
{
    public class save
    {
        public static async Task SendAsync(ulong channelId, string path, string title)
        {
            DiscordSocketClient client = new DiscordSocketClient();

            string token = Program.tokenElement.Value;

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            try
            {
                var channel = await client.GetChannelAsync(channelId) as ITextChannel;
                await channel.SendMessageAsync("Sending file..");
                await channel.SendFileAsync(path, $"Here you go: {title}");
                
                client.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static async Task SendSilentAsync(ulong channelId, string path)
        {
            DiscordSocketClient client = new DiscordSocketClient();

            string token = Program.tokenElement.Value;

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            try
            {
                var channel = await client.GetChannelAsync(channelId) as ITextChannel;
                await channel.SendFileAsync(path);

                client.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static string ConvertToAsciiCompatible(string input)
        {
            // Create a StringBuilder to store the converted characters
            StringBuilder result = new StringBuilder();

            foreach (char c in input)
            {
                // Check if the character is non-ASCII
                if (c > 127)
                {
                    string converted = c.Unidecode();
                    if (!string.IsNullOrEmpty(converted))
                    {
                        result.Append(converted);
                    }
                    else
                    {
                        result.Append('_'); // Replace with an underscore if the conversion fails
                    }
                }
                else
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }
    }
}
