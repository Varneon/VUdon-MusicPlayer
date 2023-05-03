using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace Varneon.VUdon.MusicPlayer
{
    [CreateAssetMenu(menuName = "VUdon/Music Player/Song Playlist", fileName = "NewSongPlaylist.asset", order = 100)]
    public class SongPlaylist : ScriptableObject
    {
        public SongPlaylistData Data
        {
            get
            {
                return FromJSON(rawJsonData);
            }
            set
            {
                rawJsonData = JsonConvert.SerializeObject(value, Formatting.Indented);
            }
        }

        public static SongPlaylistData FromJSON(string json)
        {
            return string.IsNullOrWhiteSpace(json) ? new SongPlaylistData() : JsonConvert.DeserializeObject<SongPlaylistData>(json);
        }

        public string RawJsonData => rawJsonData;

        [SerializeField]
        internal string rawJsonData;

        public class SongPlaylistData
        {
            /// <summary>
            /// Name of the music library
            /// </summary>
            public string Name = string.Empty;

            /// <summary>
            /// Description of the music library
            /// </summary>
            public string Description = string.Empty;

            /// <summary>
            /// Is the song allowed to be played on streams and videos
            /// </summary>
            public bool CopyrightSafe = false;

            /// <summary>
            /// Can this playlist be automatically played
            /// </summary>
            public bool CanAutoplay = true;

            /// <summary>
            /// Playlists contained in this library
            /// </summary>
            public List<Song> Songs = new List<Song>();

            public string Args = string.Empty;
        }
    }
}
