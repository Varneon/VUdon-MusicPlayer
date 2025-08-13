#if !COMPILER_UDONSHARP

using System;
using System.Collections.Immutable;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Video.Components.AVPro;

namespace Varneon.VUdon.MusicPlayer
{
    [AddComponentMenu("VUdon/Music Player/AVPro Music Player Speaker")]
    [RequireComponent(typeof(VRCAVProVideoSpeaker))]
    public class AVProMusicPlayerSpeaker : Abstract.MusicPlayerSpeaker
    {
        public enum AudioPreset
        {
            [InspectorName("Apply Preset...")]
            ApplyPreset,

            [InspectorName("3D Room, Spherical Volume (AVPro)")]
            AVProRoom,

            [InspectorName("3D Speaker, Real Source (AVPro)")]
            AVProSpeaker
        }
    }

#if UNITY_EDITOR
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AVProMusicPlayerSpeaker))]
    public class AVProMusicPlayerSpeakerEditor : Editor
    {
        private static readonly Type[] InvalidComponentTypes = new Type[]
        {
            typeof(AudioChorusFilter),
            typeof(AudioDistortionFilter),
            typeof(AudioEchoFilter),
            typeof(AudioHighPassFilter),
            typeof(AudioLowPassFilter),
            typeof(AudioReverbFilter)
        };

        private ImmutableArray<Type> foundInvalidComponents;

        private void OnEnable()
        {
            AVProMusicPlayerSpeaker speaker = (AVProMusicPlayerSpeaker)target;

            foundInvalidComponents = InvalidComponentTypes.Where(t => speaker.TryGetComponent(t, out Component _)).ToImmutableArray();
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("This is a speaker for music player set to 'AVPro' mode", MessageType.Info);

            foreach(Type invalidComponentType in foundInvalidComponents)
            {
                EditorGUILayout.HelpBox(string.Concat(invalidComponentType.Name, " cannot be used with VRCAVProVideoSpeaker. Audio filters won't have an effect on the audio due to the final order of components in-game."), MessageType.Error);
            }
        }
    }
#endif
}

#endif
