using UnityEngine;

namespace Varneon.VUdon.MusicPlayer.Enums
{
    /// <summary>
    /// Supported video player types for handling the video playback
    /// </summary>
    public enum MusicPlayerMode
    {
        [Tooltip("Only one audio source is supported, recommended for 2D audio with audio filters, e.g. Reverb, Lowpass")]
        [InspectorName("Unity (VRCUnityVideoPlayer)")]
        Unity,

        [Tooltip("Support for multiple speakers, suitable for making realistic speaker sources")]
        [InspectorName("AVPro (VRCAVProVideoPlayer)")]
        AVPro
    }

    /// <summary>
    /// Different actions for indicating the current state of the player on the owner's end
    /// </summary>
    public enum SyncActionType
    {
        /// <summary>
        /// Song state hasn't changed, likely synchronizing other properties of the music player
        /// </summary>
        None,

        /// <summary>
        /// Owner of the player has selected a new song to be played, triggering load for all users
        /// </summary>
        SongSelected,

        /// <summary>
        /// Owner has successfully started playing a song, remote users will sync based on owner's timestamp
        /// </summary>
        SongStarted,

        /// <summary>
        /// Owner has paused or stopped playback of the current song, on remote there is no difference between the two
        /// </summary>
        SongStopped
    }

    /// <summary>
    /// Enum for tracking the current state of the player in order to act accordingly based on the received sync data from owner
    /// </summary>
    public enum SyncedSongState
    {
        /// <summary>
        /// Remote player hasn't received instructions to load or play anything
        /// </summary>
        None,

        /// <summary>
        /// Loading the synced song
        /// </summary>
        /// <remarks>
        /// Triggered by <see cref="SyncActionType.SongSelected"/> and <see cref="SyncActionType.SongStarted"/>
        /// </remarks>
        Loading,

        /// <summary>
        /// Song is playing normally
        /// </summary>
        Playing,

        /// <summary>
        /// Playback is paused
        /// </summary>
        Paused,

        /// <summary>
        /// Remote player loaded the song faster than the owner, should trigger pause until owner signals to play
        /// </summary>
        WaitingForOwner,

        /// <summary>
        /// Owner of the player has already started playing but remote player hasn't finished loading the song
        /// </summary>
        WaitingForLocal
    }
}
