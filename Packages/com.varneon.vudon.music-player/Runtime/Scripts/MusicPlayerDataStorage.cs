#if !COMPILER_UDONSHARP

using System.Collections.Generic;
using UnityEngine;

namespace Varneon.VUdon.MusicPlayer
{
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public class MusicPlayerDataStorage : MonoBehaviour
    {
        public List<SongPlaylist> Playlists = new List<SongPlaylist>();

        public UnityMusicPlayerSpeaker UnitySpeaker;

        public List<AVProMusicPlayerSpeaker> AVProSpeakers = new List<AVProMusicPlayerSpeaker>();
    }
}

#endif
