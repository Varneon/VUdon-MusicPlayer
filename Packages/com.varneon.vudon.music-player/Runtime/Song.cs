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
        /// Tags of the song
        /// </summary>
        public string Tags = string.Empty;

        /// <summary>
        /// Rating of the song on scale 1-3 (0 = Not rated)
        /// </summary>
        public int Rating = 0;

        /// <summary>
        /// Is the song allowed to be played on streams and videos
        /// </summary>
        public bool CopyrightSafe = false;

        /// <summary>
        /// Is the song only a portion of the video's duration
        /// </summary>
        public bool PartialDuration = false;

        /// <summary>
        /// Start time of the partial song
        /// </summary>
        public float StartTime = 0f;

        /// <summary>
        /// End time of the partial song
        /// </summary>
        public float EndTime = 0f;
    }
}
