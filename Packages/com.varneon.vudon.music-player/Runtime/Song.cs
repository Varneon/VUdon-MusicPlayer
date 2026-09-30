namespace Varneon.VUdon.MusicPlayer
{
    public class Song
    {
        /// <summary>
        /// Title of the song
        /// </summary>
        public string Title = string.Empty;

        /// <summary>
        /// Artist of the song
        /// </summary>
        public string Artist = string.Empty;

        /// <summary>
        /// Default URL of the song
        /// </summary>
        public string URL = string.Empty;

        /// <summary>
        /// Custom data associated with the song, e.g. a release date: 'Sep 29, 2026'
        /// </summary>
        /// <remarks>
        /// The world author chooses whether to utilize this or not, and what data to use the feature for
        /// </remarks>
        public string CustomData = string.Empty;

        /// <summary>
        /// Is the song only a portion of the video's duration
        /// </summary>
        //public bool PartialDuration = false;

        /// <summary>
        /// Start time of the partial song
        /// </summary>
        //public float StartTime = 0f;

        /// <summary>
        /// End time of the partial song
        /// </summary>
        //public float EndTime = 0f;
    }
}
