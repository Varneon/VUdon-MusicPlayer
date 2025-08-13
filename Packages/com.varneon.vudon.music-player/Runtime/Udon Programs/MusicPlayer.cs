#pragma warning disable IDE0044 // Making serialized fields readonly hides them from the inspector
#pragma warning disable IDE1006 // VRChat public method network execution prevention using underscore
#pragma warning disable 649

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using Varneon.VUdon.Editors;
using Varneon.VUdon.MusicPlayer.Enums;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDK3.Video.Components.Base;
using VRC.SDKBase;

namespace Varneon.VUdon.MusicPlayer
{
    [SelectionBase]
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    //[RequireComponent(typeof(VRCUnityVideoPlayer))] // AVPro doesn't work if VRCUnityVideoPlayer is attached?
    //[RequireComponent(typeof(VRCAVProVideoPlayer))]
    public class MusicPlayer : UdonSharpBehaviour
    {
        #region Public Properties
        public MusicPlayerMode Mode => mode;
        #endregion

        #region Serialized Fields
        [FoldoutHeader("Colors",  "Customize the player colors")]
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Color to use for highlighted content")]
        private float contentHighlightHue = 0.533f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Color to use for highlighted panels")]
        private float panelHighlightHue = 0.75f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("First color to use for background")]
        private float backgroundHue1 = 0.6588235f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Second color to use for background")]
        private float backgroundHue2 = 0.7803922f;

        [FoldoutHeader("Playback Settings", "Set of playback options that can be defined before build and changed later in-game")]
        [SerializeField]
        [Tooltip("Should the player automatically repeat the playlist after it reaches the end")]
        private bool repeat = true;

        [SerializeField]
        [Tooltip("Randomize the playback order of the songs")]
        private bool shuffle = true;

        [SerializeField]
        [Tooltip("Allow automatic playback to switch between playlists when changing songs")]
        private bool shufflePlaylist = true;

        [SerializeField]
        [Tooltip("Don't allow the player to automatically play any playlists that are not marked as 'Copyright Free'")]
        private bool disableCopyrightedAutoplay;

        [FoldoutHeader("Build Configuration", "Set of options for configuring the music player before build")]
        [SerializeField]
        [Tooltip("Which video player will be used internally")]
        private MusicPlayerMode mode = MusicPlayerMode.Unity;

        [SerializeField, Range(10f, 60f)]
        [Tooltip("Time in seconds after which the player will throw an error if the song couldn't be loaded.")]
        private float loadingTimeout = 20f;

        [SerializeField]
        [Tooltip("Should the player be synced between players")]
        internal bool synced;

        [SerializeField]
        [FieldDisable(nameof(synced))]
        [Tooltip("Allow remote players to take ownership of the player at start")]
        internal bool allowOwnershipClaimOnStart = true;

        /// <summary>
        /// Should the music start automatically playing when joining the an instance of the world
        /// </summary>
        [SerializeField]
        [Tooltip("Automatically start playing after player joins the instance")]
        private bool playOnStart = true;

        /// <summary>
        /// Should only the first playlist start playing
        /// </summary>
        [SerializeField]
        [FieldDisable(nameof(playOnStart))]
        [Tooltip("Only allow player to automatically play the first playlist in the library without user input")]
        private bool onlyFirstPlaylistOnStart;

        /// <summary>
        /// Audio source for Unity video player
        /// </summary>
        [SerializeField, HideInInspector]
        internal AudioSource unityPlayerAudioSource;

        /// <summary>
        /// Audio sources for AVPro video player
        /// </summary>
        [SerializeField, HideInInspector]
        internal AudioSource[] audioSources;

        [SerializeField, HideInInspector]
        private bool unlockReferenceEditing;

        [ContextMenu("Unlock Reference Editing")]
        private void UnlockReferenceEditing() { unlockReferenceEditing = true; }

        [FoldoutHeader("References", "Do not edit unless you know what you're doing")]
        [SerializeField, FieldNullWarning(true), FieldDisable(nameof(unlockReferenceEditing))]
        internal RectTransform windowRoot;

        [SerializeField, FieldNullWarning(true), FieldDisable(nameof(unlockReferenceEditing))]
        private RectTransform playlists;

        [SerializeField, FieldNullWarning(true), FieldDisable(nameof(unlockReferenceEditing))]
        private RectTransform songs;

        [SerializeField, FieldNullWarning(true), FieldDisable(nameof(unlockReferenceEditing))]
        private GameObject playlistItem, songItem, errorPrompt, syncedPlaybackOverlay, ownershipLockToggle, ownershipLockedIcon, ownershipUnlockedIcon;

        [SerializeField, FieldNullWarning(true), FieldDisable(nameof(unlockReferenceEditing))]
        private TextMeshProUGUI errorText, timeElapsed, timeLength, textTitle, textArtist, textPlaylist, textLoading, textPlaylistDescription, syncedStateInfo, claimOwnershipButtonText;

        [SerializeField, FieldNullWarning(true), FieldDisable(nameof(unlockReferenceEditing))]
        private RectTransform loadingIcon, volumeIconsRoot, copyrightedPlaylistNotice;

        [SerializeField, FieldNullWarning(true), FieldDisable(nameof(unlockReferenceEditing))]
        private Button buttonPlay, buttonPause, buttonNext, buttonPrev, buttonShuffle, buttonShufflePlaylist, buttonRepeat, buttonRepeatOne, buttonClaimOwnership;

        [SerializeField, FieldNullWarning(true), FieldDisable(nameof(unlockReferenceEditing))]
        private Toggle toggleAllowCopyrightedPlaylists;

        [SerializeField, FieldNullWarning(true), FieldDisable(nameof(unlockReferenceEditing))]
        private Slider timeProgressBar, volumeSlider, remoteVolumeSlider;

        [SerializeField, FieldNullWarning(true), FieldDisable(nameof(unlockReferenceEditing))]
        private Image playlistsScrollbarHandle, songsScrollbarHandle, windowBackground;

#if UNITY_2020_2_OR_NEWER
        [NonReorderable]
#endif
        [SerializeField, FieldNullWarning(true), FieldDisable(nameof(unlockReferenceEditing))]
        private Image[] panelColorExampleImages;

#if UNITY_2020_2_OR_NEWER
        [NonReorderable]
#endif
        [SerializeField, FieldNullWarning(true), FieldDisable(nameof(unlockReferenceEditing))]
        private TextMeshProUGUI[] contentColorExampleTexts;

#if UNITY_2020_2_OR_NEWER
        [NonReorderable]
#endif
        [SerializeField, FieldNullWarning(true), FieldDisable(nameof(unlockReferenceEditing))]
        internal GameObject[] examplesToDestroyOnBuild;

        [FoldoutHeader("Debug")]
        [SerializeField]
        internal Logger.Abstract.UdonLogger logger;
        #endregion

        #region Private Variables
        private BaseVRCVideoPlayer player;

        private const string LogPrefix = "[<color=#ABC>VUdon</color>][<color=#009999>MusicPlayer</color>]";

        [SerializeField, HideInInspector]
        internal VRCUrl[] Urls = new VRCUrl[0];

        [SerializeField, HideInInspector]
        internal string[] Titles = new string[0];

        [SerializeField, HideInInspector]
        internal string[] Artists = new string[0];

        [SerializeField, HideInInspector]
        internal string[] Tags = new string[0];

        [SerializeField, HideInInspector]
        internal int[] PlaylistIndices = new int[0];

        [SerializeField, HideInInspector]
        internal string[] PlaylistNames = new string[0];

        [SerializeField, HideInInspector]
        internal string[] PlaylistArgs = new string[0];

        [SerializeField, HideInInspector]
        internal string[] PlaylistDescriptions = new string[0];

        [SerializeField, HideInInspector]
        internal int[] AutoplayPlaylistIndices = new int[0];

        [SerializeField, HideInInspector]
        internal int[] CopyrightFreePlaylistIndices = new int[0];

        [SerializeField, HideInInspector]
        internal int[] AutoplayCopyrightFreePlaylistIndices = new int[0];

        private int selectedPlaylist;

        private int playlistStartIndex, playlistEndIndex;

        private int currentSongIndex = -1, currentSongPlaylistIndex = -1;

        private int nextSongIndex = -1, nextSongPlaylistIndex = -1;

        private bool loading, seeking;

        private float loadingTime, averageLoadingTime = 5f;

        private Slider loadingSlider;

        private float songDuration;

        private Transform nextSongListItem, currentSongListItem;

        private Image[] volumeIconImages;

        private float originalVolume;

        private bool repeatOne;

        private bool hasUserConfirmedError;

        private bool recoveringFromError;

        private bool isRateLimited;

        private MusicPlayerSync sync;

        private bool isSynced;

        private int syncedStartTimeInServerMS;

        private float syncedTime;

        private bool isLocalPlayerOwner;

        private bool allowOwnershipClaims;

        private Button lastClickedPlaylistButton;

        private Color contentHighlightColor;

        private ColorBlock normalGlassColors, highlightedGlassColors;

        private const float RATE_LIMIT_SECONDS = 5f;
        #endregion

        #region Internal Properties
        internal bool AllowOwnershipClaims
        {
            get => allowOwnershipClaims;
            set
            {
                Log($"AllowOwnershipClaims = {value}");

                if (allowOwnershipClaims != value)
                {
                    _SetAllowOwnershipClaims(value);

                    sync._SetAllowOwnershipClaims(value);
                }
            }
        }
        #endregion

        #region Unity Methods
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "UNT0026:GetComponent always allocates", Justification = "Method is not exposed to Udon")]
        private void Start()
        {
            lastClickedPlaylistButton = playlists.GetComponentInChildren<Button>();

            contentHighlightColor = Color.HSVToRGB(contentHighlightHue, 1f, 1f);

            Color panelHighlightColor = Color.HSVToRGB(panelHighlightHue, 0.5f, 1f);

            Button songButton = songItem.GetComponent<Button>();
            songButton.GetComponentInChildren<Slider>(true).fillRect.GetComponent<Image>().color = contentHighlightColor * new Color(1f, 1f, 1f, 0.25f);

            normalGlassColors = songButton.colors;
            normalGlassColors.highlightedColor = panelHighlightColor * new Color(0.25f, 0.25f, 0.25f, 0.5f);
            normalGlassColors.pressedColor = panelHighlightColor * new Color(0.5f, 0.5f, 0.5f, 0.5f);
            normalGlassColors.selectedColor = panelHighlightColor * new Color(0.5f, 0.5f, 0.5f, 0.25f);

            songButton.colors = normalGlassColors;
            lastClickedPlaylistButton.colors = normalGlassColors;

            panelHighlightColor *= new Color(0.5f, 0.5f, 0.5f, 0.25f);

            highlightedGlassColors = normalGlassColors;
            highlightedGlassColors.normalColor = panelHighlightColor;
            highlightedGlassColors.highlightedColor = panelHighlightColor;
            highlightedGlassColors.pressedColor = panelHighlightColor;
            highlightedGlassColors.selectedColor = panelHighlightColor;

            ColorBlock sliderColorBlock = volumeSlider.colors;
            sliderColorBlock.highlightedColor = contentHighlightColor;
            sliderColorBlock.pressedColor = contentHighlightColor;

            timeProgressBar.colors = sliderColorBlock;
            volumeSlider.colors = sliderColorBlock;
            remoteVolumeSlider.colors = sliderColorBlock;

            ownershipUnlockedIcon.GetComponent<Image>().color = contentHighlightColor;

            Button songPlayingButton = songItem.transform.GetChild(5).GetComponent<Button>();

            ColorBlock songPlayingButtonColorBlock = songPlayingButton.colors;
            songPlayingButtonColorBlock.disabledColor = contentHighlightColor;
            songPlayingButton.colors = songPlayingButtonColorBlock;

            sync = GetComponent<MusicPlayerSync>();

            if (sync != null)
            {
                sync._Link(this);

                isSynced = true;

                ownershipLockToggle.SetActive(true);

                Log("Successfully linked to MusicPlayerSync!");
            }

            player = mode == MusicPlayerMode.Unity ? (BaseVRCVideoPlayer)GetComponent<VRCUnityVideoPlayer>() : (BaseVRCVideoPlayer)GetComponent<VRCAVProVideoPlayer>();

            volumeIconImages = volumeIconsRoot.GetComponentsInChildren<Image>(true);

            toggleAllowCopyrightedPlaylists.isOn = !disableCopyrightedAutoplay;

            textTitle.text = string.Empty;

            textArtist.text = string.Empty;

            // Initialize the highlight color on the playlist playing icon
            playlistItem.transform.GetChild(1).GetComponent<Image>().color = contentHighlightColor;

            InitializePlaylists();

            // Wait for playlists to be initialized before setting the color of the first playlist button
            HighlightPlaylistListItem(lastClickedPlaylistButton, true);

            UpdateSongList();

            if (!shuffle) { shufflePlaylist = false; }
            else if (shufflePlaylist) { shuffle = false; SetShuffleButtonMode(true); }

            // Play if not synced or local player is the owner
            if (playOnStart && (!isSynced || Networking.IsOwner(gameObject)))
            {
                if (!onlyFirstPlaylistOnStart)
                {
                    LoadAndPlayAnyRandomSong();
                }
                else
                {
                    if (!disableCopyrightedAutoplay || (CopyrightFreePlaylistIndices.Length > 0 && CopyrightFreePlaylistIndices[0] == 0))
                    {
                        selectedPlaylist = nextSongPlaylistIndex = 0;
                        if (shuffle || shufflePlaylist) { LoadAndPlayRandomSongOnList(nextSongPlaylistIndex); }
                        else { LoadAndPlaySong(0); }
                    }
                    else
                    {
                        ShowPromptNoCopyrightFreeSongs();
                    }
                }

                UpdateSongList();
            }

            SetButtonHighlight(buttonShuffle, shuffle);

            SetButtonHighlight(buttonRepeat, repeat);

            SetButtonHighlight(buttonRepeatOne, true);

            SetButtonHighlight(buttonShufflePlaylist, true);

            _UpdateVolume();
        }

        private void Update()
        {
            UpdateTimeInfo();

            LoadSong();
        }
        #endregion

        #region Public Control Methods
        public void _Play()
        {
            player.Play();
        }

        public void _Stop()
        {
            player.Stop();
        }

        public void _Pause()
        {
            if (isSynced && isLocalPlayerOwner)
            {
                sync._OnSongStopped(player.GetTime());
            }

            player.Pause();

            UpdatePlayPauseButton(false);

            UpdatePlayingPlaylistIcon(false);
        }

        public void _Next()
        {
            LoadAndPlayNextSong();
        }

        public void _Prev()
        {
            LoadAndPlayPreviousSong();
        }

        public void _SelectPlaylist()
        {
            if (lastClickedPlaylistButton)
            {
                HighlightPlaylistListItem(lastClickedPlaylistButton, false);
            }

            selectedPlaylist = GetPressedButtonIndex(playlists, out lastClickedPlaylistButton);

            HighlightPlaylistListItem(lastClickedPlaylistButton, true);

            ResetLoadingProgressBar();

            HighlightSongListItem(false);

            loadingSlider = null;

            nextSongListItem = null;

            currentSongListItem = null;

            UpdateSongList();
        }

        public void _SelectSong()
        {
            int songListIndex = GetPressedButtonIndex(songs, out Button button);

            if (loading || songListIndex < 0 || isRateLimited) { return; }

            int songIndex = PlaylistIndices[selectedPlaylist] + songListIndex;

            if (songIndex == currentSongIndex || songIndex == nextSongIndex) { return; }

            _SelectSong(songIndex);
        }

        public void _SelectSong(int songIndex)
        {
            nextSongIndex = songIndex;

            if (playOnStart) { playOnStart = false; }

            LoadAndPlaySong(nextSongIndex);
        }

        public void _ToggleShuffle()
        {
            shuffle ^= true;

            SetButtonHighlight(buttonShuffle, shuffle);

            if (shuffle) { return; }

            shufflePlaylist = true;

            SetShuffleButtonMode(true);
        }

        public void _ToggleShufflePlaylist()
        {
            shufflePlaylist = false;

            SetShuffleButtonMode(false);
        }

        public void _ToggleRepeat()
        {
            repeat ^= true;

            SetButtonHighlight(buttonRepeat, repeat);

            if (repeat) { return; }

            repeatOne = true;

            buttonRepeatOne.gameObject.SetActive(true);
            buttonRepeat.gameObject.SetActive(false);
        }

        public void _ToggleRepeatOne()
        {
            repeatOne = false;

            SetButtonHighlight(buttonRepeat, false);

            buttonRepeat.gameObject.SetActive(true);
            buttonRepeatOne.gameObject.SetActive(false);
        }

        public void _UpdateVolume()
        {
            remoteVolumeSlider.SetValueWithoutNotify(volumeSlider.value);

            float volume = Mathf.Clamp01(-Mathf.Log10(1f - volumeSlider.value * 0.9f));

            foreach (AudioSource source in audioSources)
            {
                source.volume = volume;
            }

            UpdateVolumeIcon();
        }

        public void _BeginVolumeChange()
        {
            SetSliderHighlight(volumeSlider, true);
        }

        public void _EndVolumeChange()
        {
            SetSliderHighlight(volumeSlider, false);
        }

        public void _ToggleMute()
        {
            bool muted = volumeSlider.value == volumeSlider.minValue;

            if (!muted) { originalVolume = volumeSlider.value; }

            volumeSlider.value = muted ? originalVolume : volumeSlider.minValue;
        }

        public void _BeginSeek()
        {
            SetSliderHighlight(timeProgressBar, seeking = true);
        }

        public void _EndSeek()
        {
            float seconds = timeProgressBar.value * songDuration;

            if (isSynced && isLocalPlayerOwner)
            {
                sync._OnEndSeek(seconds, player.IsPlaying);
                //sync._OnSongStarted(currentSongIndex, seconds);
            }

            player.SetTime(seconds);

            SetSliderHighlight(timeProgressBar, seeking = false);
        }

        public void _ToggleAllowCopyrightedPlaylists()
        {
            disableCopyrightedAutoplay = !toggleAllowCopyrightedPlaylists.isOn;

            Log($"DisableCopyrightedAutoplay: <color=#4887BF>{disableCopyrightedAutoplay}</color>");
        }

        public void _TryToRecoverFromError()
        {
            recoveringFromError = false;

            if (isRateLimited || hasUserConfirmedError) { return; }

            _Next();
        }

        public void _DisableRateLimiting()
        {
            Log($"<color=#DCDCAA>{nameof(_DisableRateLimiting)}</color>()");

            isRateLimited = false;

            SetNavigationButtonsInteractable(true);
        }

        public void _ConfirmError()
        {
            hasUserConfirmedError = true;

            errorPrompt.SetActive(false);
        }

        public void _ApplySyncedStartTime(int startTimeInServerMS, float offset = 0f)
        {
            syncedStartTimeInServerMS = startTimeInServerMS;

            syncedTime = offset;
        }

        public void _SetPlaybackTime(float time)
        {
            player.SetTime(time);
        }

        public void _ToggleAllowOwnershipClaims()
        {
            Log("_ToggleAllowOwnershipClaims()");

            if (isSynced && isLocalPlayerOwner)
            {
                AllowOwnershipClaims ^= true;
            }
        }

        public void _ClaimOwnership()
        {
            Log("_ClaimOwnership()");

            sync._ClaimOwnership();
        }

        internal void _SetAllowOwnershipClaims(bool allowClaims)
        {
            Log($"_SetAllowOwnershipClaims({allowClaims})");

            allowOwnershipClaims = allowClaims;

            GenerateSyncedPlaybackStateInfo();

            claimOwnershipButtonText.text = allowClaims ? "CLAIM OWNERSHIP" : "PLAYER LOCKED";

            buttonClaimOwnership.interactable = allowClaims;

            ownershipLockedIcon.SetActive(!allowClaims);

            ownershipUnlockedIcon.SetActive(allowClaims);

            buttonClaimOwnership.interactable = allowClaims;
        }

        internal void _SetLocalPlayerOwnerStatus(bool isOwner)
        {
            isLocalPlayerOwner = isOwner;

            syncedPlaybackOverlay.SetActive(!isOwner);

            GenerateSyncedPlaybackStateInfo();
        }
        #endregion

        #region Private Methods

        #region Return Methods
        /// <summary>
        /// Abstract method for getting the index of the pressed button on a list
        /// </summary>
        /// <param name="root"></param>
        /// <returns>Index of the button pressed</returns>
        private int GetPressedButtonIndex(RectTransform root, out Button button)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                button = root.GetChild(i).GetComponent<Button>();

                if (button.interactable) { continue; }

                button.interactable = true;

                return i;
            }

            button = null;

            return -1;
        }

        /// <summary>
        /// Gets the index of the last song on the playlist
        /// </summary>
        /// <param name="playlist"></param>
        /// <returns>Index of the last song on the playlist</returns>
        private int GetLastPlaylistSongIndex(int playlist)
        {
            return ((PlaylistIndices.Length > playlist + 1) ? PlaylistIndices[playlist + 1] : Urls.Length) - 1;
        }

        /// <summary>
        /// Get the formatted time string based on seconds
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns>Formatted string from seconds in following format: M:SS</returns>
        private string GetFormattedTimeFromSeconds(float seconds)
        {
            return TimeSpan.FromSeconds(seconds).ToString($@"{(seconds >= 3600f ? @"h\:m" : string.Empty)}m\:ss");
        }

        /// <summary>
        /// Get the playlist index of any song based on its library index
        /// </summary>
        /// <param name="songIndex"></param>
        /// <returns>Index of the playlist</returns>
        private int GetPlaylistIndexOfSong(int songIndex)
        {
            if (songIndex < 0) { return -1; }

            if (songIndex >= PlaylistIndices[PlaylistIndices.Length - 1]) { return PlaylistIndices.Length - 1; }

            for (int i = 0; i < PlaylistIndices.Length; i++)
            {
                if (PlaylistIndices[i] <= songIndex) { continue; }

                return i - 1;
            }

            return 0;
        }
        #endregion

        private void GenerateSyncedPlaybackStateInfo()
        {
            syncedStateInfo.text = string.Format("Owner: {0}\n\nAllow ownership claim: {1}", Networking.GetOwner(gameObject).displayName, AllowOwnershipClaims);
        }

        /// <summary>
        /// Initialize the list of playlists when the program starts
        /// </summary>
        private void InitializePlaylists()
        {
            if (PlaylistIndices.Length == 0)
            {
                PlaylistIndices = new int[] { 0 };

                PlaylistNames = new string[] { "All Songs" };
            }

            for (int i = 0; i < PlaylistIndices.Length; i++)
            {
                if (i > 0) { AddNewListItem(playlists, playlistItem); }

                playlists.GetChild(i).GetChild(0).GetComponent<TextMeshProUGUI>().text = PlaylistNames[i];
            }
        }

        /// <summary>
        /// Updates the song list based on selected playlist
        /// </summary>
        private void UpdateSongList()
        {
            textPlaylist.text = PlaylistNames[selectedPlaylist];

            string description = PlaylistDescriptions[selectedPlaylist];

            textPlaylistDescription.text = string.IsNullOrEmpty(description) ? "No description" : description;

            bool hasCopyrightedSongs = true;

            for (int i = 0; i < CopyrightFreePlaylistIndices.Length; i++)
            {
                if (CopyrightFreePlaylistIndices[i] == selectedPlaylist) { hasCopyrightedSongs = false; break; }
            }

            copyrightedPlaylistNotice.gameObject.SetActive(hasCopyrightedSongs);

            playlistStartIndex = (PlaylistIndices.Length == 0) ? 0 : PlaylistIndices[selectedPlaylist];

            playlistEndIndex = GetLastPlaylistSongIndex(selectedPlaylist) + 1;

            int songCount = playlistEndIndex - playlistStartIndex;

            int itemCount = songs.childCount;

            for (int i = 0; i < Mathf.Max(songCount, itemCount); i++)
            {
                int songIndex = playlistStartIndex + i;

                if (i >= itemCount)
                {
                    AddNewListItem(songs, songItem);
                }
                else if (i >= songCount)
                {
                    Destroy(songs.GetChild(i).gameObject);
                    continue;
                }

                Transform panel = songs.GetChild(i);

                string[] content = new string[] { (i + 1).ToString(), Titles[songIndex], Artists[songIndex], Tags[songIndex] };

                TextMeshProUGUI[] texts = panel.GetComponentsInChildren<TextMeshProUGUI>(true);

                for (int j = 0; j < 4; j++)
                {
                    texts[j].text = content[j];
                }
            }

            GetActiveSongListItems();
        }

        /// <summary>
        /// Add abstract list item to any UI list with automatic layout
        /// </summary>
        /// <param name="list"></param>
        /// <param name="item"></param>
        private void AddNewListItem(RectTransform list, GameObject item)
        {
            Instantiate(item, list, false);
        }

        /// <summary>
        /// Update the time progress bar and texts based on info from the player
        /// </summary>
        private void UpdateTimeInfo()
        {
            if (!player.IsPlaying && !seeking) { return; }

            float timeElapsed = seeking ? timeProgressBar.value * songDuration : player.GetTime();

            this.timeElapsed.text = GetFormattedTimeFromSeconds(timeElapsed);

            timeProgressBar.value = Mathf.Clamp01(timeElapsed / songDuration);
        }

        /// <summary>
        /// Update the progress bar on the song list item while a song is loading
        /// </summary>
        private void UpdateLoadingProgressBar()
        {
            if (loadingSlider == null) { return; }

            loadingSlider.value = Mathf.Clamp01(loadingTime / averageLoadingTime);
        }

        /// <summary>
        /// Finish the loading process after successful load
        /// </summary>
        private void FinishLoading()
        {
            loading = false;

            loadingTime = 0f;

            loadingIcon.gameObject.SetActive(false);

            textLoading.gameObject.SetActive(false);

            ResetLoadingProgressBar();
        }

        /// <summary>
        /// Resets loading progress bar status
        /// </summary>
        private void ResetLoadingProgressBar()
        {
            if (loadingSlider == null) { return; }

            loadingSlider.value = 0f;

            loadingSlider.gameObject.SetActive(false);
        }

        /// <summary>
        /// Resets all info about the song playing
        /// </summary>
        private void ResetSongInfo()
        {
            textTitle.text = Titles[nextSongIndex];

            textArtist.text = Artists[nextSongIndex];

            timeLength.text = GetFormattedTimeFromSeconds(0f);

            timeElapsed.text = GetFormattedTimeFromSeconds(0f);

            timeProgressBar.value = 0f;
        }

        /// <summary>
        /// Load a new song and automatically play it after done loading
        /// </summary>
        /// <param name="index"></param>
        private void LoadAndPlaySong(int index)
        {
            if (isSynced && isLocalPlayerOwner)
            {
                sync._OnSongSelected(index);

                //SetPlayPauseButtonsInteractable(false);
            }

            if (loading) { return; }

            nextSongIndex = index;

            nextSongPlaylistIndex = GetPlaylistIndexOfSong(nextSongIndex);

            GetActiveSongListItems();

            loading = true;

            loadingTime = 0f;

            loadingIcon.gameObject.SetActive(true);

            textLoading.gameObject.SetActive(true);

            isRateLimited = true;

            SetNavigationButtonsInteractable(false);

            SendCustomEventDelayedSeconds(nameof(_DisableRateLimiting), isSynced ? RATE_LIMIT_SECONDS * 2f : RATE_LIMIT_SECONDS);

            Log($"<color=#DCDCAA>{nameof(LoadAndPlaySong)}</color>(<color=#4887BF>int</color> <color=#9CDCFE>index</color>: <color=#B5CEA8>{index}</color>) | <color=#808080><color=#c0c0c0>{Titles[nextSongIndex]}</color> - <color=#c0c0c0>{Artists[nextSongIndex]}</color> (<color=#c0c0c0>{Urls[nextSongIndex]}</color>)</color>");

            player.PlayURL(Urls[index]);
        }

        /// <summary>
        /// Load next song on the playlist and play it automatically
        /// </summary>
        private void LoadAndPlayNextSong()
        {
            if (shufflePlaylist) { LoadAndPlayAnyRandomSong(); return; }
            else if (shuffle) { LoadAndPlayRandomSongOnList(currentSongPlaylistIndex); return; }

            if (currentSongIndex >= GetLastPlaylistSongIndex(currentSongPlaylistIndex))
            {
                if (repeat)
                {
                    LoadAndPlaySong(PlaylistIndices[currentSongPlaylistIndex]);

                    return;
                }

                UpdatePlayingPlaylistIcon(false);

                Log("<color=#808080>Current song is last on the list, can't play next song</color>");

                return;
            }

            Log("<color=#808080>Loading next song...</color>");

            LoadAndPlaySong(currentSongIndex + 1);
        }

        /// <summary>
        /// Load previous song on the playlist and play it automatically
        /// </summary>
        private void LoadAndPlayPreviousSong()
        {
            if (shufflePlaylist) { LoadAndPlayAnyRandomSong(); return; }
            else if (shuffle) { LoadAndPlayRandomSongOnList(currentSongPlaylistIndex); return; }

            if (currentSongIndex <= PlaylistIndices[currentSongPlaylistIndex]) { Log("<color=#808080>Current song is first on the list, can't play previous song</color>"); return; }

            Log("<color=#808080>Loading previous song...</color>");

            LoadAndPlaySong(currentSongIndex - 1);
        }

        /// <summary>
        /// Load any random song from any one of the playlists and play it automatically
        /// </summary>
        private void LoadAndPlayAnyRandomSong()
        {
            if (playOnStart)
            {
                int[] playlistIndices = disableCopyrightedAutoplay ? AutoplayCopyrightFreePlaylistIndices : AutoplayPlaylistIndices;

                if (playlistIndices.Length == 0) { playOnStart = false; LoadAndPlayAnyRandomSong(); return; }

                nextSongPlaylistIndex = playlistIndices[UnityEngine.Random.Range(0, playlistIndices.Length)];
            }
            else if (disableCopyrightedAutoplay)
            {
                if (CopyrightFreePlaylistIndices.Length == 0)
                {
                    ShowPromptNoCopyrightFreeSongs();

                    return;
                }

                nextSongPlaylistIndex = CopyrightFreePlaylistIndices[UnityEngine.Random.Range(0, CopyrightFreePlaylistIndices.Length)];
            }
            else
            {
                nextSongPlaylistIndex = UnityEngine.Random.Range(0, PlaylistIndices.Length - 1);
            }

            LoadAndPlayRandomSongOnList(nextSongPlaylistIndex);
        }

        /// <summary>
        /// Load any random song on the list and play it automatically
        /// </summary>
        private void LoadAndPlayRandomSongOnList(int playlistIndex)
        {
            if (playlistIndex < 0) { LogError("Can't load song: Invalid playlist index!"); return; }

            Log($"<color=#DCDCAA>{nameof(LoadAndPlayRandomSongOnList)}</color>(<color=#4887BF>int</color> <color=#9CDCFE>playlistIndex</color>: <color=#B5CEA8>{playlistIndex}</color>)");

            LoadAndPlaySong(UnityEngine.Random.Range(PlaylistIndices[playlistIndex], GetLastPlaylistSongIndex(playlistIndex) + 1));
        }

        /// <summary>
        /// Automatically play the next or random song based on current options
        /// </summary>
        private void AutoPlayNextOrRandom()
        {
            if (shufflePlaylist) { LoadAndPlayAnyRandomSong(); return; }
            else if (shuffle) { LoadAndPlayRandomSongOnList(currentSongPlaylistIndex); return; }

            LoadAndPlayNextSong();
        }

        /// <summary>
        /// Get the active selected and loading song list item after changing playlists
        /// </summary>
        private void GetActiveSongListItems()
        {
            if (selectedPlaylist == currentSongPlaylistIndex)
            {
                int songListIndex = currentSongIndex - PlaylistIndices[currentSongPlaylistIndex];

                currentSongListItem = songs.GetChild(songListIndex);

                if (player.IsPlaying && currentSongIndex == nextSongIndex)
                {
                    nextSongListItem = currentSongListItem;

                    HighlightSongListItem(true);
                }
            }

            if (selectedPlaylist == nextSongPlaylistIndex)
            {
                int songListIndex = nextSongIndex - PlaylistIndices[nextSongPlaylistIndex];

                nextSongListItem = songs.GetChild(songListIndex);

                loadingSlider = nextSongListItem.GetComponentInChildren<Slider>(true);

                loadingSlider.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Shows the speaker icon next to the playlist to indicate from which playlist the song is playing from
        /// </summary>
        /// <param name="enabled"></param>
        private void UpdatePlayingPlaylistIcon(bool enabled)
        {
            if (currentSongPlaylistIndex >= 0 && currentSongPlaylistIndex != nextSongPlaylistIndex) { playlists.GetChild(currentSongPlaylistIndex).GetChild(1).gameObject.SetActive(false); }
            playlists.GetChild(nextSongPlaylistIndex).GetChild(1).gameObject.SetActive(enabled);
        }

        private void HighlightPlaylistListItem(Button button, bool highlighted)
        {
            button.colors = highlighted ? highlightedGlassColors : normalGlassColors;

            button.GetComponentInChildren<TextMeshProUGUI>().color = highlighted ? contentHighlightColor : Color.white;
        }

        /// <summary>
        /// Enable or disable the highlight on the active song list item
        /// </summary>
        /// <param name="highlighted"></param>
        private void HighlightSongListItem(bool highlighted)
        {
            if (highlighted && selectedPlaylist != nextSongPlaylistIndex) { return; }

            Transform listItem = highlighted ? nextSongListItem : currentSongListItem;

            if (listItem == null) { return; }

            #region Play Button
            Button button = listItem.GetComponent<Button>();

            button.colors = highlighted ? highlightedGlassColors : normalGlassColors;

            Button playButton = listItem.GetChild(5).GetComponent<Button>();

            playButton.interactable = !highlighted;
            playButton.image.raycastTarget = !highlighted;
            #endregion

            #region Texts
            Color color = highlighted ? contentHighlightColor : Color.white;

            TextMeshProUGUI[] texts = listItem.GetComponentsInChildren<TextMeshProUGUI>(true);

            texts[0].color = highlighted ? new Color() : Color.white;

            for(int i = 1; i < 4; i++)
            {
                texts[i].color = color;
            }
            #endregion
        }

        /// <summary>
        /// Show prompt and error indicating that no copyright free songs could be found in the library
        /// </summary>
        private void ShowPromptNoCopyrightFreeSongs()
        {
            LogWarning("Couldn't find any copyright free playlists in the library, try allowing playback of copyrighted content");

            ShowError("Couldn't find copyright free playlists in the library");
        }

        /// <summary>
        /// Shows the error message on the screen with the provided message
        /// </summary>
        /// <param name="error"></param>
        private void ShowError(string error)
        {
            hasUserConfirmedError = false;

            errorPrompt.SetActive(true);

            errorText.text = error;
        }

        /// <summary>
        /// Song loading process
        /// </summary>
        private void LoadSong()
        {
            if (!loading) { return; }

            loadingTime += Time.deltaTime;

            textLoading.color = Color.white * (0.75f + Mathf.Sin(loadingTime * 5f) * 0.25f);

            UpdateLoadingProgressBar();

            if (loadingTime < loadingTimeout) { return; }

            ShowError($"LOADING_TIMEOUT");

            FinishLoading();
        }

        /// <summary>
        /// Change the highlighted status on button
        /// </summary>
        /// <param name="button"></param>
        /// <param name="highlight"></param>
        private void SetButtonHighlight(Button button, bool highlight)
        {
            button.image.color = highlight ? contentHighlightColor : new Color(1f, 1f, 1f);
        }

        /// <summary>
        /// Switches the Play and Pause buttons based on whether or not the player is currently playing
        /// </summary>
        /// <param name="isPlaying"></param>
        private void UpdatePlayPauseButton(bool isPlaying)
        {
            buttonPlay.gameObject.SetActive(!isPlaying);

            buttonPause.gameObject.SetActive(isPlaying);
        }

        /// <summary>
        /// Sets shuffle button to either standard shuffle or playlist shuffle
        /// </summary>
        /// <param name="mode"></param>
        private void SetShuffleButtonMode(bool playlistShuffle)
        {
            buttonShufflePlaylist.gameObject.SetActive(playlistShuffle);
            buttonShuffle.gameObject.SetActive(!playlistShuffle);
        }

        /// <summary>
        /// Set loaded song as currently playing
        /// </summary>
        private void SetNextSongAsCurrent()
        {
            if (currentSongIndex == nextSongIndex) { return; }

            currentSongPlaylistIndex = nextSongPlaylistIndex;

            currentSongIndex = nextSongIndex;

            HighlightSongListItem(false);

            currentSongListItem = nextSongListItem;

            HighlightSongListItem(true);
        }

        /// <summary>
        /// Set seeking mode active
        /// </summary>
        /// <param name="active"></param>
        private void SetSeekingActive(bool active)
        {
            SetSliderHighlight(timeProgressBar, active);

            seeking = active;
        }

        /// <summary>
        /// Highlights the slider
        /// </summary>
        /// <param name="slider"></param>
        /// <param name="highlight"></param>
        private void SetSliderHighlight(Slider slider, bool highlight)
        {
            //slider.fillRect.GetComponent<Image>().color = highlight ? highlightColor : Color.white;
        }

        /// <summary>
        /// Sets the interactable state on song navigation buttons
        /// </summary>
        /// <param name="interactable"></param>
        private void SetNavigationButtonsInteractable(bool interactable)
        {
            buttonNext.interactable = interactable;
            buttonPrev.interactable = interactable;
        }

        /// <summary>
        /// Sets the interactable state on play and pause buttons
        /// </summary>
        /// <param name="interactable"></param>
        private void SetPlayPauseButtonsInteractable(bool interactable)
        {
            buttonPlay.interactable = interactable;
            buttonPause.interactable = interactable;
        }

        /// <summary>
        /// Updates the volume icon next to the volume slider based on the slider's value
        /// </summary>
        private void UpdateVolumeIcon()
        {
            for (int i = 0; i < volumeIconImages.Length; i++)
            {
                volumeIconImages[i].enabled = Mathf.CeilToInt((volumeSlider.value - volumeSlider.minValue) * (volumeIconImages.Length - 1f)) == i;
            }
        }

        /// <summary>
        /// Proxy for printing messages in logs
        /// </summary>
        /// <param name="text"></param>
        private void Log(string text)
        {
            if (logger) { logger.Log($"{LogPrefix} {text}"); }

            Debug.Log($"{LogPrefix} {text}");
        }

        /// <summary>
        /// Proxy for printing warnings in logs
        /// </summary>
        /// <param name="text"></param>
        private void LogWarning(string text)
        {
            if (logger) { logger.LogWarning($"{LogPrefix} {text}"); }

            Debug.LogWarning($"{LogPrefix} {text}");
        }

        /// <summary>
        /// Proxy for printing errors in logs
        /// </summary>
        /// <param name="text"></param>
        private void LogError(string text)
        {
            if (logger) { logger.LogError($"{LogPrefix} {text}"); }

            Debug.LogError($"{LogPrefix} {text}");
        }
        #endregion

        #region VRC Video Methods
        public override void OnVideoEnd()
        {
            if (isSynced && isLocalPlayerOwner)
            {
                sync._OnSongStopped(player.GetTime());
            }

            Log($"<color=#DCDCAA>{nameof(OnVideoEnd)}</color>()");

            timeProgressBar.fillRect.gameObject.SetActive(false);

            UpdatePlayPauseButton(false);

            ResetSongInfo();

            if (loading) { return; }

            if (repeatOne && (isSynced == isLocalPlayerOwner)) { player.SetTime(0f); player.Play(); return; }

            HighlightSongListItem(false);

            if (isSynced && !isLocalPlayerOwner) { return; }

            AutoPlayNextOrRandom();
        }

        public override void OnVideoStart()
        {
            if (mode == MusicPlayerMode.AVPro && loading)
            {
                OnVideoReady();
            }

            Log($"<color=#DCDCAA>{nameof(OnVideoStart)}</color>()");

            timeProgressBar.fillRect.gameObject.SetActive(true);

            UpdatePlayPauseButton(true);

            textTitle.text = Titles[nextSongIndex];

            textArtist.text = Artists[nextSongIndex];

            songDuration = player.GetDuration();

            timeLength.text = GetFormattedTimeFromSeconds(songDuration);

            UpdatePlayingPlaylistIcon(true);

            SetNextSongAsCurrent();

            if (isSynced)
            {
                sync._OnSongStarted(currentSongIndex, player.GetTime());
            }
        }

        public override void OnVideoReady()
        {
            Log($"<color=#DCDCAA>{nameof(OnVideoReady)}</color>()");

            averageLoadingTime = (averageLoadingTime + loadingTime) / 2f;

            SetPlayPauseButtonsInteractable(true);

            FinishLoading();
        }

        public override void OnVideoError(VideoError videoError)
        {
            string error = videoError.ToString();

            ShowError(error);

            LogError($"<color=#CC0000>{nameof(OnVideoError)}</color>: {error}");

            FinishLoading();

            if (recoveringFromError) { return; }

            bool isPlaying = player.IsPlaying;

            Log($"<color=#DCDCAA>{nameof(OnVideoError)}</color> | <color=#4887BF>bool</color> <color=#9CDCFE>isPlaying</color>: <color=#4887BF>{isPlaying}</color>");

            if (isPlaying) { return; }

            if (error != "RateLimited") { SendCustomEventDelayedSeconds(nameof(_TryToRecoverFromError), RATE_LIMIT_SECONDS); recoveringFromError = true; return; }

            UpdatePlayingPlaylistIcon(false);

            SetNextSongAsCurrent();

            if (!isPlaying) { HighlightSongListItem(false); }

            ResetSongInfo();
        }
        #endregion

        #region Validation
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        private void OnValidate()
        {
            Color contentColor = Color.HSVToRGB(contentHighlightHue, 1f, 1f);

            foreach(TextMeshProUGUI text in contentColorExampleTexts)
            {
                text.color = contentColor;
            }

            Color panelColor = Color.HSVToRGB(panelHighlightHue, 0.5f, 0.5f);
            panelColor.a = 0.25f;

            foreach(Image image in panelColorExampleImages)
            {
                image.color = panelColor;
            }

            Color scrollbarHandleColor = Color.HSVToRGB(panelHighlightHue, 0.5f, 0.5f);

            if (playlistsScrollbarHandle) { playlistsScrollbarHandle.color = scrollbarHandleColor; }
            if (songsScrollbarHandle) { songsScrollbarHandle.color = scrollbarHandleColor; }
            if (windowBackground) { windowBackground.color = new Color(backgroundHue1, backgroundHue2, 0.75f, 0.3058824f); }
        }
#endif
        #endregion

        #region Editor Visualization
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        private void OnDrawGizmosSelected()
        {
            Color originalColor = Gizmos.color;

            Gizmos.color = Color.cyan;

            Vector3 playerPos = transform.position;

            if (Mode == MusicPlayerMode.Unity)
            {
                if (unityPlayerAudioSource)
                {
                    VisualizeAudioSource(unityPlayerAudioSource);
                }
            }
            else
            {
                foreach(AudioSource source in audioSources)
                {
                    if(source == null) { continue; }

                    VisualizeAudioSource(source);
                }
            }

            Gizmos.color = originalColor;

            void VisualizeAudioSource(AudioSource source)
            {
                Vector3 speakerPos = source.transform.position;

                Gizmos.DrawLine(playerPos, speakerPos);

                Gizmos.DrawWireSphere(speakerPos, 0.5f);
            }
        }
#endif
        #endregion

        #region Initialization

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        [UsedImplicitly]
        [UnityEditor.Callbacks.PostProcessScene(-1)]
        private static void InitializeOnBuild()
        {
            GameObject[] sceneRoots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

            IEnumerable<MusicPlayer> musicPlayers = sceneRoots.SelectMany(r => r.GetComponentsInChildren<MusicPlayer>());

            foreach (MusicPlayer player in musicPlayers)
            {
                switch (player.mode)
                {
                    case MusicPlayerMode.Unity:
                        System.Reflection.FieldInfo unityVideoPlayerAudioSourcesField = typeof(VRCUnityVideoPlayer).GetField("targetAudioSources", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                        player.audioSources = new AudioSource[] { player.unityPlayerAudioSource };

                        unityVideoPlayerAudioSourcesField.SetValue(player.GetComponent<VRCUnityVideoPlayer>(), player.audioSources);
                        DestroyImmediate(player.GetComponent<VRCAVProVideoPlayer>());
                        break;
                    case MusicPlayerMode.AVPro:
                        System.Reflection.FieldInfo avproSpeakerVideoPlayerField = typeof(VRCAVProVideoSpeaker).GetField("videoPlayer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                        VRCAVProVideoPlayer thisAVPro = player.GetComponent<VRCAVProVideoPlayer>();

                        foreach(AudioSource source in player.audioSources)
                        {
                            if(source == null) { continue; }

                            if(source.TryGetComponent(out VRCAVProVideoSpeaker avproSpeaker))
                            {
                                avproSpeakerVideoPlayerField.SetValue(avproSpeaker, thisAVPro);
                            }
                        }

                        DestroyImmediate(player.GetComponent<VRCUnityVideoPlayer>());
                        break;
                    default:
                        Debug.LogError("Unknown MusicPlayerMode!");
                        break;
                }

                foreach (GameObject example in player.examplesToDestroyOnBuild)
                {
                    DestroyImmediate(example);
                }
            }
        }
#endif

#endregion
    }
}
