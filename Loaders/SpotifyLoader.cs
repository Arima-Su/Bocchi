using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Alice_Module.Loaders
{
    internal class SpotifyLoader
    {
        static string _clientId = "b02e017b87a5498c9357ffe4ff672d01";
        static string _clientSecret = "e0b39aa43e5e46209741403020d08484";

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

        public static async Task<List<(string Title, string Artist)>> GetPlaylistSongsInfo(string playlistId)
        {
            var accessToken = await GetAccessToken();

            var client = new RestClient("https://api.spotify.com");
            var request = new RestRequest($"/v1/playlists/{playlistId}/tracks", Method.Get);
            request.AddHeader("Authorization", "Bearer " + accessToken);

            var response = await client.ExecuteAsync(request);
            var content = response.Content;

            var songsInfo = new List<(string Title, string Artist)>();
            var items = JObject.Parse(content)["items"];
            foreach (var item in items)
            {
                var track = item["track"];
                if (track != null)
                {
                    var title = track["name"]?.ToString();
                    var artists = track["artists"] as JArray;
                    var artist = artists?.FirstOrDefault()?["name"]?.ToString();

                    if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(artist))
                    {
                        songsInfo.Add((title, artist));
                    }
                }
            }

            return songsInfo;
        }

        public static async Task<(string Title, string Artist)> GetSongInfoFromLink(string songLink)
        {
            var accessToken = await GetAccessToken();

            var client = new RestClient("https://api.spotify.com");
            var request = new RestRequest(new Uri(songLink).AbsolutePath, Method.Get);
            request.AddHeader("Authorization", "Bearer " + accessToken);

            var response = await client.ExecuteAsync(request);
            var content = response.Content;

            var songInfo = JObject.Parse(content);
            var title = songInfo["name"].ToString();
            var artist = songInfo["artists"][0]["name"].ToString();

            return (title, artist);
        }
    }
}
