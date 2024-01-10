using DSharpPlus.Lavalink;

namespace Alice_Module.Handlers
{
    public class song
    {
        LavalinkTrack track;
        string user;

        public song(LavalinkTrack track, string user)
        {
            this.track = track;
            this.user = user;
        }

        public LavalinkTrack getTrack()
        {
            return this.track;
        }

        public string getUser()
        {
            return this.user;
        }
    }
}
