using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Varneon.VUdon.MusicPlayer.Abstract;
using Varneon.VUdon.MusicPlayer.Enums;

namespace Varneon.VUdon.MusicPlayer.Editor
{
    public static class MusicPlayerEditorUtility
    {
        public static MusicPlayerSpeaker CreateMusicPlayerSpeaker(MusicPlayerMode mode, Vector3 position)
        {
            MusicPlayerSpeaker newSpeaker;

            if(mode == MusicPlayerMode.Unity)
            {
                newSpeaker = new GameObject("Music Player Speaker (Unity)", typeof(UnityMusicPlayerSpeaker), typeof(AudioReverbFilter), typeof(AudioLowPassFilter)).GetComponent<UnityMusicPlayerSpeaker>();

                ApplyPresetToSource(newSpeaker, UnityMusicPlayerSpeaker.AudioPreset.UnityBackground);
            }
            else
            {
                newSpeaker = new GameObject("Music Player Speaker (AVPro)", typeof(AVProMusicPlayerSpeaker)).GetComponent<AVProMusicPlayerSpeaker>();

                ApplyPresetToSource(newSpeaker, AVProMusicPlayerSpeaker.AudioPreset.AVProSpeaker);
            }

            newSpeaker.transform.position = position;

            AudioSource source = newSpeaker.GetComponent<AudioSource>();

            // Ensure that the music has highest priority
            source.priority = 0;

            // New audio sources have playOnAwake enabled by default
            source.playOnAwake = false;

            // New audio sources have doppler enabled by default
            source.dopplerLevel = 0f;

            Undo.RegisterCreatedObjectUndo(newSpeaker.gameObject, "Create Music Player Speaker");

            EditorGUIUtility.PingObject(newSpeaker.gameObject);

            return newSpeaker;
        }

        public static void ApplyPresetToSource(MusicPlayerSpeaker speaker, UnityMusicPlayerSpeaker.AudioPreset preset)
        {
            if(preset == UnityMusicPlayerSpeaker.AudioPreset.ApplyPreset) { return; }

            AudioSource source = speaker.AudioSource;

            List<Object> undoObjects = new List<Object>() { source };

            if (source.TryGetComponent(out AudioReverbFilter reverbFilter))
            {
                undoObjects.Add(reverbFilter);
            }
            if (source.TryGetComponent(out AudioLowPassFilter lowPassFilter))
            {
                undoObjects.Add(lowPassFilter);
            }

            Undo.RecordObjects(undoObjects.ToArray(), "Apply Music Source Preset");

            switch (preset)
            {
                case UnityMusicPlayerSpeaker.AudioPreset.UnityBackground:
                    source.minDistance = 0f;
                    source.maxDistance = 10000f;
                    source.spatialBlend = 0f;
                    source.rolloffMode = AudioRolloffMode.Custom;
                    source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, AnimationCurve.Linear(0f, 1f, 1f, 1f));
                    source.SetCustomCurve(AudioSourceCurveType.SpatialBlend, new AnimationCurve(new Keyframe(0f, 0f)));
                    if (reverbFilter)
                    {
                        reverbFilter.reverbPreset = AudioReverbPreset.Quarry;
                    }
                    if (lowPassFilter)
                    {
                        lowPassFilter.customCutoffCurve = new AnimationCurve(new Keyframe(0f, 1f));
                    }
                    break;
                case UnityMusicPlayerSpeaker.AudioPreset.UnityRoom:
                    source.minDistance = 25f;
                    source.maxDistance = 50f;
                    source.spatialBlend = 1f;
                    source.rolloffMode = AudioRolloffMode.Custom;
                    source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, new AnimationCurve(new Keyframe(source.minDistance / source.maxDistance, 1f, 0f, -2.5f), new Keyframe(1f, 0f, 0f, 0f)));
                    source.SetCustomCurve(AudioSourceCurveType.SpatialBlend, new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f), new Keyframe(0.5f, 1f, 0f, 0f)));
                    if (reverbFilter)
                    {
                        reverbFilter.reverbPreset = AudioReverbPreset.Quarry;
                    }
                    if (lowPassFilter)
                    {
                        lowPassFilter.customCutoffCurve = GetLowPassCutoffCurve(source.maxDistance * 0.25f, source.maxDistance);
                    }
                    break;
            }
        }

        public static void ApplyPresetToSource(MusicPlayerSpeaker speaker, AVProMusicPlayerSpeaker.AudioPreset preset)
        {
            if (preset == AVProMusicPlayerSpeaker.AudioPreset.ApplyPreset) { return; }

            AudioSource source = speaker.AudioSource;

            Undo.RecordObject(source, "Apply Music Source Preset");

            switch (preset)
            {
                case AVProMusicPlayerSpeaker.AudioPreset.AVProRoom:
                    source.minDistance = 25f;
                    source.maxDistance = 50f;
                    source.spatialBlend = 1f;
                    source.rolloffMode = AudioRolloffMode.Custom;
                    source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, new AnimationCurve(new Keyframe(source.minDistance / source.maxDistance, 1f, 0f, -2.5f), new Keyframe(1f, 0f, 0f, 0f)));
                    source.SetCustomCurve(AudioSourceCurveType.SpatialBlend, new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f), new Keyframe(0.5f, 1f, 0f, 0f)));
                    break;
                case AVProMusicPlayerSpeaker.AudioPreset.AVProSpeaker:
                    source.minDistance = 0.5f;
                    source.maxDistance = 30f;
                    source.spatialBlend = 1f;
                    source.rolloffMode = AudioRolloffMode.Custom;
                    source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, new AnimationCurve(new Keyframe(source.minDistance / source.maxDistance, 1f, 0f, -2.5f), new Keyframe(1f, 0f, 0f, 0f)));
                    source.SetCustomCurve(AudioSourceCurveType.SpatialBlend, new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f), new Keyframe(0.25f, 1f, 0f, 0f)));
                    break;
            }
        }

        public static AnimationCurve GetLowPassCutoffCurve(float minDistance, float maxDistance, float farCutoffFrequency = 500f)
        {
            if(maxDistance < 0f) { maxDistance = 1f; }

            minDistance = Mathf.Clamp(minDistance, 0f, maxDistance);

            farCutoffFrequency = Mathf.Clamp(farCutoffFrequency, 0f, 22000f);

            return new AnimationCurve(
                new Keyframe(0f, 1f, 0f, 0f),
                new Keyframe(minDistance / maxDistance, 1f, 0f, 0f),
                new Keyframe(1f, farCutoffFrequency / 22000f, 0f, 0f)
            );
        }
    }
}
