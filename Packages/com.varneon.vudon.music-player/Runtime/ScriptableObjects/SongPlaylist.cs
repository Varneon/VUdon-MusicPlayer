using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
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

        public static SongPlaylistData FromTSV(string tsv)
        {
            string[] rows = tsv.Split(new char[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

            if(rows.Length == 0) { return new SongPlaylistData(); }

            string[] row1 = rows[0].Split('\t');

            int titleIndex = -1;

            int artistIndex = -1;

            int urlIndex = -1;

            int tagsIndex = -1;

            for(int i = 0; i < row1.Length; i++)
            {
                string value = row1[i].ToLower();

                switch (value)
                {
                    case "title":
                        titleIndex = i;
                        break;
                    case "artist":
                        artistIndex = i;
                        break;
                    case "url":
                        urlIndex = i;
                        break;
                    case "tags":
                        tagsIndex = i;
                        break;
                }
            }

            bool hasHeaderRow = titleIndex >= 0 && artistIndex >= 0 && urlIndex >= 0;

            bool hasTags = (hasHeaderRow && tagsIndex > 0) || row1.Length > 3;

            if (!hasHeaderRow)
            {
                titleIndex = 0;

                artistIndex = 1;

                urlIndex = 2;

                tagsIndex = 3;
            }

            List<Song> songs = new List<Song>();

            for(int i = hasHeaderRow ? 1 : 0; i < rows.Length; i++)
            {
                string[] row = rows[i].Split(new char[] { '\t' }, System.StringSplitOptions.RemoveEmptyEntries);

                songs.Add(new Song() { Title = row[titleIndex], Artist = row[artistIndex], URL = row[urlIndex], Tags = hasTags ? row[tagsIndex] : string.Empty });
            }

            return new SongPlaylistData() { Songs = songs };
        }

        public static bool IsJSONValidSongPlaylistData(string json)
        {
            if (string.IsNullOrEmpty(json)) { return false; }

            try
            {
                JObject root = JObject.Parse(json);

                if (root == null) { return false; }

                HashSet<string> propertyNames = new HashSet<string>(root.Properties().Select(p => p.Name));

                if (typeof(SongPlaylistData).GetFields().Any(f => !propertyNames.Contains(f.Name))) { return false; }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsTSVValidSongPlaylistData(string tsv)
        {
            string[] rows = tsv.Split(new char[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

            if (rows.Length == 0) { return false; }

            int rowLength = rows[0].Split('\t').Length;

            return rows.All(r => r.Split('\t').Length == rowLength);
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
