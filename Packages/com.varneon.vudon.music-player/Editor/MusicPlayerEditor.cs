using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Varneon.VUdon.Editors.Editor;

namespace Varneon.VUdon.MusicPlayer.Editor
{
    [CustomEditor(typeof(MusicPlayer))]
    public class MusicPlayerEditor : InspectorBase
    {
        [SerializeField]
        private Texture2D headerIcon;

        private MusicPlayer player;

        private RectTransform canvasRectTransform;

        private MusicPlayerDataStorage playlistStorage;

        private ReorderableList
            playlistReorderableList,
            audioSourcesReorderableList;

        private bool
            isPlaylistDragAndDropValid,
            isSpeakerDragAndDropValid;

        private SerializedProperty
            playlistsProperty,
            speakerProperty,
            speakersProperty;

        private SerializedObject dataStorageSO;

        /// <summary>
        /// Flag for disabling playlist, AudioSource and window editing if the currently inspected MusicPlayer is a prefab asset
        /// </summary>
        private bool isInspectingPrefab;

        protected override int CustomPersistentBoolCount => 3;

        protected override string PersistenceKey => "Varneon/VUdon/MusicPlayer/Editor/Foldouts";

        protected override InspectorHeader Header => new InspectorHeaderBuilder()
            .WithTitle("VUdon - Music Player")
            .WithDescription("Drag & drop music player for worlds")
            .WithURL("GitHub", "https://github.com/Varneon/VUdon-MusicPlayer")
            .WithIcon(headerIcon)
            .Build();

        protected override void OnEnable()
        {
            base.OnEnable();

            player = (MusicPlayer)target;

            canvasRectTransform = player.windowRoot;

            isInspectingPrefab = PrefabUtility.IsPartOfPrefabAsset(player);

            playlistStorage = player.GetComponent<MusicPlayerDataStorage>() ?? player.gameObject.AddComponent<MusicPlayerDataStorage>();

            dataStorageSO = new SerializedObject(playlistStorage);

            speakerProperty = dataStorageSO.FindProperty(nameof(MusicPlayerDataStorage.UnitySpeaker));

            speakersProperty = dataStorageSO.FindProperty(nameof(MusicPlayerDataStorage.AVProSpeakers));

            playlistsProperty = dataStorageSO.FindProperty(nameof(playlistStorage.Playlists));

            playlistReorderableList = new ReorderableList(serializedObject, playlistsProperty, true, true, true, true);

            playlistReorderableList.onAddCallback = (ReorderableList list) =>
            {
                SongPlaylist[] availablePlaylists = AssetDatabase.FindAssets(string.Concat("t:", typeof(SongPlaylist).Name))
                    .Select(a => AssetDatabase.LoadAssetAtPath<SongPlaylist>(AssetDatabase.GUIDToAssetPath(a)))
                    .ToArray();

                GenericMenu menu = new GenericMenu();

                menu.AddItem(new GUIContent("Create New Playlist"), false, () => { CreateNewPlaylist(); });

                menu.AddSeparator(string.Empty);

                List<SongPlaylist> existingPlaylists = new List<SongPlaylist>();

                for(int i = 0; i < playlistsProperty.arraySize; i++)
                {
                    existingPlaylists.Add((SongPlaylist)playlistsProperty.GetArrayElementAtIndex(i).objectReferenceValue);
                }

                foreach(SongPlaylist playlist in availablePlaylists)
                {
                    GUIContent content = new GUIContent(playlist.name);

                    if (existingPlaylists.Contains(playlist))
                    {
                        menu.AddDisabledItem(content);
                    }
                    else
                    {
                        menu.AddItem(content, false, () =>
                        {
                            int index = playlistsProperty.arraySize;

                            playlistsProperty.InsertArrayElementAtIndex(index);

                            playlistsProperty.GetArrayElementAtIndex(index).objectReferenceValue = playlist;

                            ApplyModifiedStorageProperties();
                        });
                    }
                }

                menu.ShowAsContext();
            };

            playlistReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                SerializedProperty element = playlistReorderableList.serializedProperty.GetArrayElementAtIndex(index);

                rect.y += 1;
                rect.height -= 2;

                EditorGUI.PropertyField(rect, element, GUIContent.none);
            };

            playlistReorderableList.drawHeaderCallback = (Rect rect) =>
            {
                if (Event.current.type == EventType.DragExited)
                {
                    isPlaylistDragAndDropValid = false;
                }

                if (rect.Contains(Event.current.mousePosition))
                {
                    switch (Event.current.type)
                    {
                        case EventType.DragUpdated:
                            isPlaylistDragAndDropValid = DragAndDrop.objectReferences.All(o => o.GetType().Equals(typeof(SongPlaylist)));
                            DragAndDrop.visualMode = isPlaylistDragAndDropValid ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                            Event.current.Use();
                            break;
                        case EventType.DragPerform:
                            isPlaylistDragAndDropValid = false;
                            dataStorageSO.Update();
                            IEnumerable<SongPlaylist> playlists = playlistStorage.Playlists.Union(DragAndDrop.objectReferences.Select(o => (SongPlaylist)o)).ToList();
                            playlistsProperty.arraySize = playlists.Count();
                            for(int i = 0; i < playlists.Count(); i++)
                            {
                                playlistsProperty.GetArrayElementAtIndex(i).objectReferenceValue = playlists.ElementAt(i);
                            }
                            dataStorageSO.ApplyModifiedProperties();
                            Event.current.Use();
                            break;
                    }
                }

                if (isPlaylistDragAndDropValid)
                {
                    int objectCount = DragAndDrop.objectReferences.Length;

                    GUI.Label(rect, string.Concat("Add ", objectCount, " playlist", objectCount > 1 ? "s" : string.Empty, " to music player"));
                }
                else
                {
                    GUI.Label(rect, new GUIContent(string.Concat(playlistReorderableList.serializedProperty.arraySize, " Playlists (Drag & Drop here)"), "Drag and drop playlists here to add them"));
                }
            };

            audioSourcesReorderableList = new ReorderableList(dataStorageSO, speakersProperty, false, true, false, true);

            audioSourcesReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                rect.x += 20f;
                rect.width -= 20f;
                rect.y += 1;
                rect.height -= 2;

                using (EditorGUI.ChangeCheckScope changeScope = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUI.PropertyField(rect, speakersProperty.GetArrayElementAtIndex(index), GUIContent.none);

                    if(changeScope.changed)
                    {
                        ApplyModifiedStorageProperties();
                    }
                }
            };

            audioSourcesReorderableList.drawHeaderCallback = (Rect rect) =>
            {
                if (Event.current.type == EventType.DragExited)
                {
                    isSpeakerDragAndDropValid = false;
                }

                if (rect.Contains(Event.current.mousePosition))
                {
                    switch (Event.current.type)
                    {
                        case EventType.DragUpdated:
                            isSpeakerDragAndDropValid = DragAndDrop.objectReferences.All(o => o.GetType().Equals(typeof(GameObject)) && ((GameObject)o).TryGetComponent(out AVProMusicPlayerSpeaker _));
                            DragAndDrop.visualMode = isSpeakerDragAndDropValid ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                            Event.current.Use();
                            break;
                        case EventType.DragPerform:
                            isSpeakerDragAndDropValid = false;
                            Undo.RecordObject(player, "Add Speakers");
                            IEnumerable<AVProMusicPlayerSpeaker> speakers = DragAndDrop.objectReferences.Select(o => ((GameObject)o).GetComponent<AVProMusicPlayerSpeaker>()).Where(s => !playlistStorage.AVProSpeakers.Contains(s));
                            
                            foreach(AVProMusicPlayerSpeaker speaker in speakers)
                            {
                                int index = speakersProperty.arraySize;

                                speakersProperty.InsertArrayElementAtIndex(index);

                                speakersProperty.GetArrayElementAtIndex(index).objectReferenceValue = speaker;
                            }

                            ApplyModifiedStorageProperties();
                            Event.current.Use();
                            break;
                    }
                }

                if (isSpeakerDragAndDropValid)
                {
                    int objectCount = DragAndDrop.objectReferences.Length;

                    GUI.Label(rect, string.Concat("Add ", objectCount, " speakers", objectCount > 1 ? "s" : string.Empty, " to music player"));
                }
                else
                {
                    GUI.Label(rect, new GUIContent(string.Concat(speakersProperty.arraySize, " AVPro Player Speakers (Drag & Drop here)"), "Drag and drop speakers here to add them"));
                }
            };

            audioSourcesReorderableList.onRemoveCallback = (ReorderableList list) =>
            {
                speakersProperty.DeleteArrayElementAtIndex(list.index);

                ApplyModifiedStorageProperties();
            };
        }

        private void CreateNewPlaylist()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create playlist", "New_Playlist", "asset", "Please enter a file name to save the playlist to");

            if (string.IsNullOrWhiteSpace(path)) { return; }

            string directory = Path.GetDirectoryName(path);

            if (!Directory.Exists(directory)) { Directory.CreateDirectory(directory); }

            SongPlaylist newPlaylist = CreateInstance<SongPlaylist>();

            AssetDatabase.CreateAsset(newPlaylist, path);

            playlistsProperty.arraySize++;

            playlistsProperty.GetArrayElementAtIndex(playlistsProperty.arraySize - 1).objectReferenceValue = newPlaylist;

            ApplyModifiedStorageProperties();

            Selection.activeObject = newPlaylist;
        }

        private void DrawLabel(Vector3 point, string text)
        {
            Handles.BeginGUI();

            GUIContent content = new GUIContent(text);

            Rect rect = HandleUtility.WorldPointToSizedRect(point, content, EditorStyles.textArea);

            rect.position += rect.size * new Vector2(-0.5f, 1f) + new Vector2(4f, 0f);

            GUI.Label(rect, content, EditorStyles.textArea);

            Handles.EndGUI();
        }

        private void OnSceneGUI()
        {
            if(player.Mode == Enums.MusicPlayerMode.Unity)
            {
                if (player.unityPlayerAudioSource == null) { return; }

                Transform sourceTransform = player.unityPlayerAudioSource.transform;

                using (EditorGUI.ChangeCheckScope changeScope = new EditorGUI.ChangeCheckScope())
                {
                    Vector3 pos = Handles.PositionHandle(sourceTransform.position, Quaternion.identity);

                    DrawLabel(pos, pos.ToString("F1"));

                    if (changeScope.changed)
                    {
                        Undo.RecordObject(sourceTransform, "Move Speaker");

                        sourceTransform.position = pos;
                    }
                }
            }
            else
            {
                foreach (AudioSource source in player.audioSources)
                {
                    if (source == null) { continue; }

                    Transform sourceTransform = source.transform;

                    using (EditorGUI.ChangeCheckScope changeScope = new EditorGUI.ChangeCheckScope())
                    {
                        Vector3 pos = Handles.PositionHandle(sourceTransform.position, Quaternion.identity);

                        DrawLabel(pos, pos.ToString("F1"));

                        if (changeScope.changed)
                        {
                            Undo.RecordObject(sourceTransform, "Move Speaker");

                            sourceTransform.position = pos;
                        }
                    }
                }
            }
        }

        protected override void OnPreDrawFields()
        {
            if (isInspectingPrefab) { return; }

            DrawPlaylistStorageEditor();

            DrawAudioSourceEditor();

            DrawWindowEditor();
        }

        protected override void OnPostDrawFields()
        {
            DrawWindowValidation();
        }

        private void DrawPlaylistStorageEditor()
        {
            bool expanded = customPersistentBools[0];

            bool hasPlaylistAssigned = playlistStorage.Playlists.Count > 0;

            using (EditorGUI.ChangeCheckScope changeScope = new EditorGUI.ChangeCheckScope())
            {
                if (!hasPlaylistAssigned) { GUI.contentColor = Color.red; }

                expanded = EditorGUILayout.BeginFoldoutHeaderGroup(expanded, hasPlaylistAssigned ? "Music Library" : "Music Library (No playlists found)");

                if (!hasPlaylistAssigned) { GUI.contentColor = Color.white; }

                if (changeScope.changed)
                {
                    customPersistentBools[0] = expanded;
                }
            }

            if (expanded)
            {
                dataStorageSO.Update();

                playlistReorderableList.DoLayoutList();

                dataStorageSO.ApplyModifiedProperties();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawAudioSourceEditor()
        {
            bool expanded = customPersistentBools[1];

            bool hasSpeakerAssigned = player.Mode == Enums.MusicPlayerMode.Unity ? (speakerProperty.objectReferenceValue != null) : (speakersProperty.arraySize > 0 && speakersProperty.GetArrayElementAtIndex(0).objectReferenceValue != null);

            using (EditorGUI.ChangeCheckScope changeScope = new EditorGUI.ChangeCheckScope())
            {
                if (!hasSpeakerAssigned) { GUI.contentColor = Color.red; }

                expanded = EditorGUILayout.BeginFoldoutHeaderGroup(expanded, hasSpeakerAssigned ? "Speakers" : "Speakers (No speaker found)");

                if (!hasSpeakerAssigned) { GUI.contentColor = Color.white; }

                if (changeScope.changed)
                {
                    customPersistentBools[1] = expanded;
                }
            }

            if (expanded)
            {
                GUI.color = Color.black;

                Rect rect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                if (EditorDarkMode) { GUI.Box(rect, string.Empty); }

                GUI.color = Color.white;

                if (player.Mode == Enums.MusicPlayerMode.Unity)
                {
                    using (EditorGUI.ChangeCheckScope changeScope = new EditorGUI.ChangeCheckScope())
                    {
                        GUILayout.Label("Unity Player Speaker");

                        EditorGUILayout.PropertyField(speakerProperty, GUIContent.none);

                        if (speakerProperty.objectReferenceValue == null)
                        {
                            EditorGUILayout.HelpBox("Music player doesn't have an active audio source assigned to it! Please create one by clicking the button below or assign one from the scene.", MessageType.Error);

                            if (GUILayout.Button("Create New Speaker"))
                            {
                                speakerProperty.objectReferenceValue = MusicPlayerEditorUtility.CreateMusicPlayerSpeaker(player.Mode, player.transform.TransformPoint(Vector3.back));
                            }
                        }

                        if (changeScope.changed)
                        {
                            ApplyModifiedStorageProperties();
                        }
                    }

                    if (speakerProperty.objectReferenceValue)
                    {
                        using (EditorGUI.ChangeCheckScope changeScope = new EditorGUI.ChangeCheckScope())
                        {
                            UnityMusicPlayerSpeaker.AudioPreset preset = (UnityMusicPlayerSpeaker.AudioPreset)EditorGUILayout.EnumPopup(UnityMusicPlayerSpeaker.AudioPreset.ApplyPreset);

                            if (changeScope.changed)
                            {
                                MusicPlayerEditorUtility.ApplyPresetToSource((UnityMusicPlayerSpeaker)speakerProperty.objectReferenceValue, preset);
                            }
                        }
                    }
                }
                else
                {
                    using (EditorGUI.ChangeCheckScope changeScope = new EditorGUI.ChangeCheckScope())
                    {
                        audioSourcesReorderableList.DoLayoutList();

                        if (speakersProperty.arraySize < 1 || speakersProperty.GetArrayElementAtIndex(0).objectReferenceValue == null)
                        {
                            EditorGUILayout.HelpBox("At least one speaker is required for the music player to function! Please create one by clicking the button below or assign one from the scene.", MessageType.Error);
                        }

                        if (GUILayout.Button("Create New Speaker"))
                        {
                            AVProMusicPlayerSpeaker newSpeaker = (AVProMusicPlayerSpeaker)MusicPlayerEditorUtility.CreateMusicPlayerSpeaker(player.Mode, player.transform.TransformPoint(Vector3.back));

                            for(int i = speakersProperty.arraySize - 1; i >= 0; i--)
                            {
                                if(speakersProperty.GetArrayElementAtIndex(i).objectReferenceValue == null)
                                {
                                    speakersProperty.DeleteArrayElementAtIndex(i);
                                }
                            }

                            int index = speakersProperty.arraySize;

                            speakersProperty.InsertArrayElementAtIndex(index);

                            speakersProperty.GetArrayElementAtIndex(index).objectReferenceValue = newSpeaker;
                        }

                        if (changeScope.changed)
                        {
                            ApplyModifiedStorageProperties();
                        }
                    }

                    if (speakersProperty.arraySize > 0 && speakersProperty.GetArrayElementAtIndex(0).objectReferenceValue)
                    {
                        using (EditorGUI.ChangeCheckScope changeScope = new EditorGUI.ChangeCheckScope())
                        {
                            AVProMusicPlayerSpeaker.AudioPreset preset = (AVProMusicPlayerSpeaker.AudioPreset)EditorGUILayout.EnumPopup(AVProMusicPlayerSpeaker.AudioPreset.ApplyPreset);

                            if (changeScope.changed)
                            {
                                for (int i = 0; i < speakersProperty.arraySize; i++)
                                {
                                    AVProMusicPlayerSpeaker speaker = (AVProMusicPlayerSpeaker)speakersProperty.GetArrayElementAtIndex(i).objectReferenceValue;

                                    if (speaker == null) { continue; }

                                    MusicPlayerEditorUtility.ApplyPresetToSource(speaker, preset);
                                }
                            }
                        }
                    }
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void ApplyModifiedStorageProperties()
        {
            dataStorageSO.ApplyModifiedProperties();

            Undo.RegisterCompleteObjectUndo(player, "Configure Music Player");

            serializedObject.FindProperty(nameof(player.unityPlayerAudioSource)).objectReferenceValue = playlistStorage.UnitySpeaker?.AudioSource;

            SerializedProperty sourcesProperty = serializedObject.FindProperty(nameof(player.audioSources));

            IEnumerable<AudioSource> sources = playlistStorage.AVProSpeakers.Where(s => s != null).Select(s => s.AudioSource);

            int count = sources.Count();

            sourcesProperty.arraySize = count;

            for (int i = 0; i < count; i++)
            {
                sourcesProperty.GetArrayElementAtIndex(i).objectReferenceValue = sources.ElementAt(i);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawWindowEditor()
        {
            bool expanded = customPersistentBools[2];

            using (EditorGUI.ChangeCheckScope changeScope = new EditorGUI.ChangeCheckScope())
            {
                if (!canvasRectTransform) { GUI.contentColor = Color.red; }

                expanded = EditorGUILayout.BeginFoldoutHeaderGroup(expanded, "Window Settings");

                if (!canvasRectTransform) { GUI.contentColor = Color.white; }

                if (changeScope.changed)
                {
                    customPersistentBools[2] = expanded;
                }
            }

            if (expanded)
            {
                if (canvasRectTransform)
                {
                    GUI.color = Color.black;

                    Rect rect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    if (EditorDarkMode) { GUI.Box(rect, string.Empty); }

                    GUI.color = Color.white;

                    EditorGUI.indentLevel++;

                    using (var scope = new EditorGUI.ChangeCheckScope())
                    {
                        float width = EditorGUILayout.Slider("Width", canvasRectTransform.sizeDelta.x, 1200f, 1800f);

                        if (scope.changed)
                        {
                            Undo.RecordObject(canvasRectTransform, "Adjust music player resolution");

                            canvasRectTransform.sizeDelta = new Vector2(width, canvasRectTransform.sizeDelta.y);
                        }
                    }

                    using (var scope = new EditorGUI.ChangeCheckScope())
                    {
                        float height = EditorGUILayout.Slider("Height", canvasRectTransform.sizeDelta.y, 800f, 1600f);

                        if (scope.changed)
                        {
                            Undo.RecordObject(canvasRectTransform, "Adjust music player resolution");

                            canvasRectTransform.sizeDelta = new Vector2(canvasRectTransform.sizeDelta.x, height);
                        }
                    }

                    using (var scope = new EditorGUI.ChangeCheckScope())
                    {
                        float scale = Mathf.Clamp(EditorGUILayout.FloatField("Canvas Scale", canvasRectTransform.localScale.x), 0.0001f, 10f);

                        if (scope.changed)
                        {
                            Undo.RecordObject(canvasRectTransform, "Adjust music player scale");

                            canvasRectTransform.localScale = Vector3.one * scale;
                        }
                    }

                    EditorGUI.indentLevel--;

                    EditorGUILayout.EndVertical();
                }
                else
                {
                    EditorGUILayout.HelpBox(string.Concat("Window properties aren't available! The reference, '", nameof(MusicPlayer.windowRoot), "' hasn't been assigned!"), MessageType.Error);
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawWindowValidation()
        {
            if (player.transform.localScale != Vector3.one)
            {
                EditorGUILayout.HelpBox("Do not change the music player transform's scale!\n\nIf you want to scale the window, use the 'Canvas Scale' option in 'Window Settings' panel.", MessageType.Error);

                if (GUILayout.Button("Apply Music Player Scale To Canvas"))
                {
                    Undo.RecordObjects(new Object[] { player.transform, canvasRectTransform }, "Fix Music Player Scale");

                    Vector3 localScale = player.transform.localScale;

                    float transformScale = (localScale.x + localScale.y + localScale.z) / 3f;

                    float finalCanvasScale = canvasRectTransform.localScale.x * transformScale;

                    player.transform.localScale = Vector3.one;

                    canvasRectTransform.localScale = Vector3.one * finalCanvasScale;
                }
            }
        }
    }
}
