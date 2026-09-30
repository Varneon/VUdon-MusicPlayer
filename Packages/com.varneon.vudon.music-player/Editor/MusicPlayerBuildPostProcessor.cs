using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using UdonSharpEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Varneon.VUdon.MusicPlayer.Enums;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDKBase;

namespace Varneon.VUdon.MusicPlayer.Editor
{
    public static class MusicPlayerBuildPostProcessor
    {
        [UsedImplicitly]
        [PostProcessScene(-1)]
        private static void PostProcessMusicPlayers()
        {
            foreach(MusicPlayer musicPlayer in Object.FindObjectsOfType<MusicPlayer>(true))
            {
                if (musicPlayer.synced)
                {
                    MusicPlayerSync sync = musicPlayer.gameObject.AddUdonSharpComponent<MusicPlayerSync>();

                    sync.allowOwnershipClaimAtStart = musicPlayer.allowOwnershipClaimOnStart;

                    UdonSharpEditorUtility.GetBackingUdonBehaviour(sync).SyncMethod = Networking.SyncType.Manual;

                    if (musicPlayer.logger)
                    {
                        sync.logger = musicPlayer.logger;

                        sync.logLevel = musicPlayer.logLevel;
                    }
                }

                if (musicPlayer.showCustomDataColumn)
                {
                    musicPlayer.customDataColumnHeader.text = musicPlayer.customDataColumnName.ToUpper();
                }
                else
                {
                    // Disable the custom data column on both the header and item sample
                    musicPlayer.customDataColumnHeader.gameObject.SetActive(false);
                    musicPlayer.itemCustomDataText.gameObject.SetActive(false);

                    // Adjust both Title and Artist columns to take up half of the width
                    musicPlayer.titleColumnHeader.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                    musicPlayer.artistColumnHeader.rectTransform.anchorMin = new Vector2(0.5f, 0f);
                    musicPlayer.artistColumnHeader.rectTransform.anchorMax = new Vector2(1f, 1f);

                    musicPlayer.itemTitleText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                    musicPlayer.itemArtistText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
                    musicPlayer.itemArtistText.rectTransform.anchorMax = new Vector2(1f, 1f);
                }

                if (!musicPlayer.TryGetComponent(out MusicPlayerDataStorage playlistStorage)) { continue; }

                SongPlaylist.SongPlaylistData[] playlists = playlistStorage.Playlists.Select(p => p.Data).ToArray();

                List<int> autoplayPlaylistIndices = new List<int>();
                List<int> copyrightFreePlaylistIndices = new List<int>();

                List<int> playlistIndices = new List<int>();
                List<string> playlistNames = new List<string>();
                List<string> playlistArgs = new List<string>();
                List<string> playlistDescriptions = new List<string>();

                int currentSongIndex = 0;

                int maxPlaylistLength = 0;

                foreach (SongPlaylist.SongPlaylistData playlist in playlists)
                {
                    if (playlist.CanAutoplay) { autoplayPlaylistIndices.Add(playlistIndices.Count); }

                    if (playlist.CopyrightSafe) { copyrightFreePlaylistIndices.Add(playlistIndices.Count); }

                    playlistNames.Add(playlist.Name);

                    playlistIndices.Add(currentSongIndex);

                    playlistArgs.Add(playlist.Args);

                    playlistDescriptions.Add(playlist.Description);

                    currentSongIndex += playlist.Songs.Count;

                    maxPlaylistLength = Mathf.Max(maxPlaylistLength, playlist.Songs.Count);
                }

                IEnumerable<int> autoplayCopyrightFreePlaylistIndices = autoplayPlaylistIndices.Intersect(copyrightFreePlaylistIndices);

                IEnumerable<Song> allSongs = playlists.SelectMany(p => p.Songs);

                musicPlayer.Urls = allSongs.Select(s => new VRCUrl(s.URL)).ToArray();

                musicPlayer.Titles = allSongs.Select(s => s.Title).ToArray();
                musicPlayer.Artists = allSongs.Select(s => s.Artist).ToArray();
                musicPlayer.Tags = allSongs.Select(s => s.CustomData).ToArray();

                musicPlayer.PlaylistIndices = playlistIndices.ToArray();
                musicPlayer.PlaylistNames = playlistNames.ToArray();
                musicPlayer.PlaylistArgs = playlistArgs.ToArray();
                musicPlayer.PlaylistDescriptions = playlistDescriptions.ToArray();
                musicPlayer.AutoplayPlaylistIndices = autoplayPlaylistIndices.ToArray();
                musicPlayer.CopyrightFreePlaylistIndices = copyrightFreePlaylistIndices.ToArray();
                musicPlayer.AutoplayCopyrightFreePlaylistIndices = autoplayCopyrightFreePlaylistIndices.ToArray();
                musicPlayer.MaxPlaylistLength = maxPlaylistLength;

                // If only one playlist is included in build, hide the library window to make more space for song list items
                if(playlists.Length == 1)
                {
                    musicPlayer.playlistLibraryWindow.gameObject.SetActive(false);
                    musicPlayer.mainWindow.sizeDelta = new Vector2(0f, musicPlayer.mainWindow.sizeDelta.y);
                    musicPlayer.mainWindow.anchoredPosition = new Vector2(0f, musicPlayer.mainWindow.anchoredPosition.y);
                }

                switch (musicPlayer.Mode)
                {
                    case MusicPlayerMode.Unity:
                        System.Reflection.FieldInfo unityVideoPlayerAudioSourcesField = typeof(VRCUnityVideoPlayer).GetField("targetAudioSources", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                        musicPlayer.audioSources = new AudioSource[] { musicPlayer.unityPlayerAudioSource };

                        unityVideoPlayerAudioSourcesField.SetValue(musicPlayer.GetComponent<VRCUnityVideoPlayer>(), musicPlayer.audioSources);
                        Object.DestroyImmediate(musicPlayer.GetComponent<VRCAVProVideoPlayer>());
                        break;
                    case MusicPlayerMode.AVPro:
                        System.Reflection.FieldInfo avproSpeakerVideoPlayerField = typeof(VRCAVProVideoSpeaker).GetField("videoPlayer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                        VRCAVProVideoPlayer thisAVPro = musicPlayer.GetComponent<VRCAVProVideoPlayer>();

                        foreach (AudioSource source in musicPlayer.audioSources)
                        {
                            if (source == null) { continue; }

                            if (source.TryGetComponent(out VRCAVProVideoSpeaker avproSpeaker))
                            {
                                avproSpeakerVideoPlayerField.SetValue(avproSpeaker, thisAVPro);
                            }
                        }

                        Object.DestroyImmediate(musicPlayer.GetComponent<VRCUnityVideoPlayer>());
                        break;
                    default:
                        Debug.LogError("Unknown MusicPlayerMode!");
                        break;
                }

                foreach (GameObject example in musicPlayer.examplesToDestroyOnBuild)
                {
                    Object.DestroyImmediate(example);
                }

                musicPlayer.InitializeOnBuild();
            }
        }
    }
}
