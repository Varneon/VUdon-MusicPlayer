using System.Collections.Generic;
using System.Linq;
using UdonSharpEditor;
using UnityEditor.Callbacks;
using VRC.SDKBase;

namespace Varneon.VUdon.MusicPlayer.Editor
{
    public static class MusicPlayerBuildPostProcessor
    {
        [PostProcessScene(-1)]
        public static void PostProcessMusicPlayers()
        {
            MusicPlayer[] musicPlayers = UnityEngine.Object.FindObjectsOfType<MusicPlayer>();

            foreach(MusicPlayer musicPlayer in musicPlayers)
            {
                if (musicPlayer.synced)
                {
                    MusicPlayerSync sync = musicPlayer.gameObject.AddUdonSharpComponent<MusicPlayerSync>();

                    sync.allowOwnershipClaimAtStart = musicPlayer.allowOwnershipClaimOnStart;

                    UdonSharpEditorUtility.GetBackingUdonBehaviour(sync).SyncMethod = Networking.SyncType.Manual;

                    if (musicPlayer.logger)
                    {
                        sync.logger = musicPlayer.logger;
                    }
                }

                if(!musicPlayer.TryGetComponent(out MusicPlayerDataStorage playlistStorage)) { continue; }

                SongPlaylist.SongPlaylistData[] playlists = playlistStorage.Playlists.Select(p => p.Data).ToArray();

                List<int> autoplayPlaylistIndices = new List<int>();
                List<int> copyrightFreePlaylistIndices = new List<int>();

                List<int> playlistIndices = new List<int>();
                List<string> playlistNames = new List<string>();
                List<string> playlistArgs = new List<string>();
                List<string> playlistDescriptions = new List<string>();

                int currentSongIndex = 0;

                foreach (SongPlaylist.SongPlaylistData playlist in playlists)
                {
                    if (playlist.CanAutoplay) { autoplayPlaylistIndices.Add(playlistIndices.Count); }

                    if (playlist.CopyrightSafe) { copyrightFreePlaylistIndices.Add(playlistIndices.Count); }

                    playlistNames.Add(playlist.Name);

                    playlistIndices.Add(currentSongIndex);

                    playlistArgs.Add(playlist.Args);

                    playlistDescriptions.Add(playlist.Description);

                    currentSongIndex += playlist.Songs.Count;
                }

                IEnumerable<int> autoplayCopyrightFreePlaylistIndices = autoplayPlaylistIndices.Intersect(copyrightFreePlaylistIndices);

                IEnumerable<Song> allSongs = playlists.SelectMany(p => p.Songs);

                musicPlayer.Urls = allSongs.Select(s => new VRCUrl(s.URL)).ToArray();

                musicPlayer.Titles = allSongs.Select(s => s.Title).ToArray();
                musicPlayer.Artists = allSongs.Select(s => s.Artist).ToArray();
                musicPlayer.Tags = allSongs.Select(s => s.Tags).ToArray();

                musicPlayer.PlaylistIndices = playlistIndices.ToArray();
                musicPlayer.PlaylistNames = playlistNames.ToArray();
                musicPlayer.PlaylistArgs = playlistArgs.ToArray();
                musicPlayer.PlaylistDescriptions = playlistDescriptions.ToArray();
                musicPlayer.AutoplayPlaylistIndices = autoplayPlaylistIndices.ToArray();
                musicPlayer.CopyrightFreePlaylistIndices = copyrightFreePlaylistIndices.ToArray();
                musicPlayer.AutoplayCopyrightFreePlaylistIndices = autoplayCopyrightFreePlaylistIndices.ToArray();
            }
        }
    }
}
