using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Alice_Module.Loaders
{
    internal class SpotifyLoader
    {
        static XDocument xmlDoc = XDocument.Load("data.xml");

        static XElement ID = xmlDoc.Descendants("category")
                .FirstOrDefault(category => category.Attribute("name")?.Value == "spotID")?
                .Element("entry");

        static XElement Secret = xmlDoc.Descendants("category")
                .FirstOrDefault(category => category.Attribute("name")?.Value == "secret")?
                .Element("entry");

        static string _clientId = ID.Value;
        static string _clientSecret = Secret.Value;

        public SpotifyLoader(string clientId, string clientSecret)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
        }

        private static async Task<string> GetAccessToken()
        {
            var client = new RestClient("https://accounts.spotify.com");
            var request = new RestRequest("/api/token", Method.Post);
            request.AddHeader("Authorization", "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}")));
            request.AddParameter("grant_type", "client_credentials");

            var response = await client.ExecuteAsync(request);
            var content = response.Content;

            var accessToken = JObject.Parse(content)["access_token"].ToString();
            return accessToken;
        }

        public static async Task<List<string>> GetPlaylistSongLinks(string playlistId)
        {
            var accessToken = await GetAccessToken();

            var client = new RestClient("https://api.spotify.com");
            var request = new RestRequest($"/v1/playlists/{playlistId}/tracks", Method.Get);
            request.AddHeader("Authorization", "Bearer " + accessToken);

            var response = await client.ExecuteAsync(request);
            var content = response.Content;

            // Parse the response content to get the song links
            var songLinks = new List<string>();
            var items = JObject.Parse(content)["items"];
            foreach (var item in items)
            {
                var link = item["track"]?["external_urls"]?["spotify"]?.ToString();
                if (!string.IsNullOrEmpty(link))
                {
                    songLinks.Add(link);
                }
            }

            return songLinks;
        }
    }
}
