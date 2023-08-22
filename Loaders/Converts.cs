using System;

namespace Alice_Module.Loaders
{
    public class Converts
    {
        public static string ConvertToShortenedUrl(string fullUrl)
        {
            if (Validates.IsShortHandYoutubeLink(fullUrl))
            {
                return fullUrl;
            }

            // Check if the URL is a valid YouTube URL
            if (!IsValidYouTubeUrl(fullUrl))
            {
                // Handle invalid URLs
                throw new ArgumentException("Invalid YouTube URL");
            }

            // Extract the video ID from the URL
            string videoId = ExtractYouTubeVideoId(fullUrl);

            // Construct the shortened URL
            string shortenedUrl = $"https://youtu.be/{videoId}";

            return shortenedUrl;
        }

        static bool IsValidYouTubeUrl(string url)
        {
            // Check if the URL contains "youtube.com" and "v="
            return url.Contains("youtube.com") && url.Contains("v=");
        }

        static string ExtractYouTubeVideoId(string url)
        {
            // Extract the video ID from the URL
            int startIndex = url.IndexOf("v=") + 2;
            int endIndex = url.IndexOf('&', startIndex);
            if (endIndex == -1)
            {
                // If there's no '&' character after the video ID, take the substring from startIndex to the end
                endIndex = url.Length;
            }

            string videoId = url.Substring(startIndex, endIndex - startIndex);
            return videoId;
        }

        public static string ExtractSpotifyPlaylistId(string playlistLink)
        {
            // Define the start and end strings that identify the playlist ID in the link
            const string startString = "playlist/";
            const string endString = "?si=";

            // Find the index of the start and end strings
            int startIndex = playlistLink.IndexOf(startString, StringComparison.OrdinalIgnoreCase);
            int endIndex = playlistLink.IndexOf(endString, StringComparison.OrdinalIgnoreCase);

            if (startIndex != -1 && endIndex != -1)
            {
                // Extract the playlist ID from the link
                startIndex += startString.Length;
                return playlistLink.Substring(startIndex, endIndex - startIndex);
            }

            throw new ArgumentException("Invalid Spotify playlist link");
        }
    }
}
