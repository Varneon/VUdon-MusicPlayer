using UnityEngine;

namespace Varneon.VUdon.MusicPlayer.Abstract
{
    [RequireComponent(typeof(AudioSource))]
    [DisallowMultipleComponent]
    public abstract class MusicPlayerSpeaker : MonoBehaviour
    {
        public AudioSource AudioSource => GetComponent<AudioSource>();
    }
}
