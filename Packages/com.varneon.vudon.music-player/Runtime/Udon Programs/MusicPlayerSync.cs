using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using Varneon.VUdon.MusicPlayer.Enums;

namespace Varneon.VUdon.MusicPlayer
{
    [DefaultExecutionOrder(1)] // Ensure that this behaviour runs after the main MusicPlayer behaviour
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class MusicPlayerSync : UdonSharpBehaviour
    {
        [SerializeField]
        internal bool allowOwnershipClaimAtStart;

        [SerializeField]
        internal Logger.Abstract.UdonLogger logger;

        private VRCPlayerApi localPlayer;

        private MusicPlayer musicPlayer;

        private bool linked;

        private bool isLocalPlayerOwner;

        [UdonSynced]
        private bool allowOwnershipTransfer;

        private bool lastAllowOwnershipTransfer;

        /// <summary>
        /// Type of synced action that was executed
        /// </summary>
        [UdonSynced]
        private byte syncedActionType;

        /// <summary>
        /// Synced index of the current song
        /// </summary>
        [UdonSynced]
        private int syncedSongIndex;

        /// <summary>
        /// Synced start time of the current song
        /// </summary>
        [UdonSynced]
        private int syncedStartTime;

        /// <summary>
        /// Synced start time offset of the current song
        /// </summary>
        [UdonSynced]
        private float syncedStartTimeOffset;

        [UdonSynced]
        private int syncedActionTimestamp;

        private int lastSyncedActionTimestamp;

        private bool isSyncedSongPlaying;

        private SyncedSongState syncedSongState;

        private void Start()
        {
            localPlayer = Networking.LocalPlayer;
        }

        public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
        {
            Log(string.Format("<color=#c0c0c0>[{0}]</color> {1} requested ownership for <color=#c0c0c0>[{2}]<color> {3}", requestingPlayer.playerId, requestingPlayer.displayName, requestedOwner.playerId, requestedOwner.displayName));

            return allowOwnershipTransfer;
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            Log(string.Format("Ownership has been transferred to <color=#c0c0c0>[{0}]</color> {1}", player.playerId, player.displayName));

            isLocalPlayerOwner = player.isLocal;

            musicPlayer._SetLocalPlayerOwnerStatus(isLocalPlayerOwner);
        }

        internal void _OnSongSelected(int index)
        {
            if (!isLocalPlayerOwner) { return; }

            Log($"_OnSongSelected({index})");

            syncedActionType = (byte)SyncActionType.SongSelected;

            syncedSongIndex = index;

            RequestSerializationWithTimestamp();
        }

        internal void _OnEndSeek(float time, bool playing)
        {
            Log(string.Concat(nameof(_OnEndSeek), "(", time, ")"));

            if (!isLocalPlayerOwner) { return; }

            syncedActionType = playing ? (byte)SyncActionType.SongStarted : (byte)SyncActionType.SongStopped;

            syncedStartTime = Networking.GetServerTimeInMilliseconds();

            syncedStartTimeOffset = time;

            RequestSerializationWithTimestamp();
        }

        internal void _OnSongStarted(int index, float time = 0f)
        {
            Log($"_OnSongStarted({index}, {time})");

            if (isLocalPlayerOwner)
            {
                syncedActionType = (byte)SyncActionType.SongStarted;

                syncedStartTime = Networking.GetServerTimeInMilliseconds();

                syncedStartTimeOffset = time;

                syncedSongIndex = index;

                RequestSerializationWithTimestamp();
            }
            else
            {
                switch (syncedSongState)
                {
                    // Remote has started 
                    case SyncedSongState.Loading:
                        isSyncedSongPlaying = index == syncedSongIndex;

                        if (isSyncedSongPlaying && (SyncActionType)syncedActionType != SyncActionType.SongStarted)
                        {
                            Log("_Pause()");

                            musicPlayer._Pause();

                            syncedSongState = SyncedSongState.WaitingForOwner;
                        }
                        else
                        {
                            musicPlayer._SetPlaybackTime(GetPlaybackOffset());

                            syncedSongState = SyncedSongState.Playing;
                        }
                        break;
                    case SyncedSongState.WaitingForLocal:
                        isSyncedSongPlaying = index == syncedSongIndex;

                        if (isSyncedSongPlaying)
                        {
                            Log("waitingForSyncedSongToLoad");

                            musicPlayer._SetPlaybackTime(GetPlaybackOffset());

                            syncedSongState = SyncedSongState.Playing;
                        }

                        break;
                    case SyncedSongState.Paused:

                        musicPlayer._Play();

                        syncedSongState = SyncedSongState.Playing;

                        break;
                }
            }
        }

        internal void _OnSongStopped(float time)
        {
            if (!isLocalPlayerOwner) { return; }

            Log("_OnSongStopped()");

            syncedActionType = (byte)SyncActionType.SongStopped;

            syncedStartTimeOffset = time;

            RequestSerializationWithTimestamp();
        }

        public override void OnDeserialization()
        {
            Log($"OnDeserialization() {lastSyncedActionTimestamp}, {syncedActionTimestamp}");

            if (lastSyncedActionTimestamp == syncedActionTimestamp) { return; }

            lastSyncedActionTimestamp = syncedActionTimestamp;

            SyncActionType type = (SyncActionType)syncedActionType;

            if (lastAllowOwnershipTransfer != allowOwnershipTransfer)
            {
                lastAllowOwnershipTransfer = allowOwnershipTransfer;

                Log($"_SetAllowOwnershipClaims({allowOwnershipTransfer})");

                musicPlayer._SetAllowOwnershipClaims(allowOwnershipTransfer);
            }

            switch (type)
            {
                // If the synced action was song selected, select the song locally and set isSyncedSongPlaying flag to false
                case SyncActionType.SongSelected:
                    Log("SyncActionType.SongSelected");
                    musicPlayer._SelectSong(syncedSongIndex);

                    syncedSongState = SyncedSongState.Loading;

                    break;
                case SyncActionType.SongStarted:
                    Log("SyncActionType.SongStarted");

                    // Owner scrubbed the timeline
                    if(syncedSongState == SyncedSongState.Playing)
                    {
                        Log("Time changed!");

                        musicPlayer._SetPlaybackTime(GetPlaybackOffset());

                        if(syncedSongState == SyncedSongState.Paused)
                        {
                            musicPlayer._Pause();
                        }
                    }

                    // If the local player is already playing the song, sync the time with the owner
                    else if (syncedSongState == SyncedSongState.WaitingForOwner || syncedSongState == SyncedSongState.Paused)
                    {
                        Log("isSyncedSongPlaying");
                        musicPlayer._SetPlaybackTime(GetPlaybackOffset());

                        musicPlayer._Play();

                        syncedSongState = SyncedSongState.Playing;
                    }
                    else // If local player hasn't started the song yet, set the flag
                    {
                        Log("waitingForSyncedSongToLoad = true");

                        // If local player hasn't received the last instruction to load the song, load it
                        if (syncedSongState == SyncedSongState.None)
                        {
                            Log("!isSyncedSongLoading && !isSyncedSongPlaying");
                            musicPlayer._SelectSong(syncedSongIndex);

                            syncedSongState = SyncedSongState.Loading;
                        }
                        else
                        {
                            syncedSongState = SyncedSongState.WaitingForLocal;
                        }
                    }
                    break;
                case SyncActionType.SongStopped:
                    Log("SyncActionType.SongStopped");

                    musicPlayer._Pause();

                    musicPlayer._SetPlaybackTime(syncedStartTimeOffset);

                    syncedSongState = SyncedSongState.Paused;
                    break;
            }
        }

        internal void _ClaimOwnership()
        {
            Log("_ClaimOwnership()");

            if (!isLocalPlayerOwner && allowOwnershipTransfer)
            {
                Log("Networking.SetOwner(localPlayer, gameObject)");
                Networking.SetOwner(localPlayer, gameObject);
            }
        }

        internal void _SetAllowOwnershipClaims(bool allow)
        {
            if (isLocalPlayerOwner && allowOwnershipTransfer != allow)
            {
                allowOwnershipTransfer = allow;

                lastAllowOwnershipTransfer = allow;

                RequestSerializationWithTimestamp();
            }
        }

        internal void _Link(MusicPlayer player)
        {
            if (linked) { return; }

            isLocalPlayerOwner = Networking.IsOwner(gameObject);

            musicPlayer = player;

            musicPlayer._SetLocalPlayerOwnerStatus(isLocalPlayerOwner);

            if (allowOwnershipClaimAtStart && isLocalPlayerOwner)
            {
                _SetAllowOwnershipClaims(true);

                musicPlayer._SetAllowOwnershipClaims(true);
            }

            linked = true;
        }

        private void Log(string message)
        {
            string logMessage = string.Format("[<color=#ABC>VUdon</color>][<color=#00ffff>MusicPlayerSync</color>]: {0}", message);

            Debug.Log(logMessage);

            if (logger) { logger.Log(logMessage); }
        }

        private void LogError(string message)
        {
            string logMessage = string.Format("[<color=#ABC>VUdon</color>][<color=#00ffff>MusicPlayer</color>]: {0}", message);

            Debug.LogError(logMessage);

            if (logger) { logger.LogError(logMessage); }
        }

        private float GetSecondsSinceSyncedAction()
        {
            int millisecondsSinceAction = Networking.GetServerTimeInMilliseconds() - syncedStartTime;

            return (float)millisecondsSinceAction / 1000f;
        }

        private float GetPlaybackOffset()
        {
            float time = GetSecondsSinceSyncedAction() + syncedStartTimeOffset;

            if (time < 0f) { time = 0f; }

            return time;
        }

        private void RequestSerializationWithTimestamp()
        {
            syncedActionTimestamp = Networking.GetServerTimeInMilliseconds();

            lastSyncedActionTimestamp = syncedActionTimestamp;

            Log($"RequestSerializationWithTimestamp(), {syncedActionTimestamp}");

            RequestSerialization();
        }
    }
}
