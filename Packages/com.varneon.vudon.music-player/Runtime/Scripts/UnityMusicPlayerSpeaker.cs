#if !COMPILER_UDONSHARP
using UnityEditor;
using UnityEngine;

namespace Varneon.VUdon.MusicPlayer
{
    [AddComponentMenu("VUdon/Music Player/Unity Music Player Speaker")]
    public class UnityMusicPlayerSpeaker : Abstract.MusicPlayerSpeaker
    {
        public enum AudioPreset
        {
            [InspectorName("Apply Preset...")]
            ApplyPreset,

            [InspectorName("2D Background, Directionless (Unity)")]
            UnityBackground,

            [InspectorName("3D Room, Spherical Volume (Unity)")]
            UnityRoom,
        }
    }

#if UNITY_EDITOR
    [CanEditMultipleObjects]
    [CustomEditor(typeof(UnityMusicPlayerSpeaker))]
    public class UnityMusicPlayerSpeakerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("This is a speaker for music player set to 'Unity' mode", MessageType.Info);
        }
    }
#endif
}

#endif
