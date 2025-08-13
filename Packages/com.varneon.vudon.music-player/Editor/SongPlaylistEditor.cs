using System.IO;
using UnityEditor;
using UnityEngine;
using static Varneon.VUdon.MusicPlayer.SongPlaylist;

namespace Varneon.VUdon.MusicPlayer.Editor
{
    [CustomEditor(typeof(SongPlaylist))]
    public class SongPlaylistEditor : UnityEditor.Editor
    {
        private SongPlaylist songPlaylist;

        private SongPlaylistData songPlaylistData;

        private bool isDirty;

        private Vector2 scrollPos;

        private bool showReorderingTools;

        private static readonly GUIContent
            NameFieldContent = new GUIContent("Name", "Name of the playlist"),
            DescriptionFieldContent = new GUIContent("Description", "Description of the playlist"),
            CopyrightSafeFieldContent = new GUIContent("Copyright Safe", "Is this playlist copyright safe"),
            CanAutoplayFieldContent = new GUIContent("Can Autoplay", "Can this playlist be automatically played at start of the instance"),
            ShowReorderingToolsToggleContent = new GUIContent("Show Reordering Tools", "Show options for reordering songs in the playlist");

        private static Texture
            AddIcon,
            SaveIcon,
            DuplicateIcon,
            ImportIcon;

        private static GUIContent
            AddButtonContent,
            SaveButtonContent,
            CopyButtonContent,
            ExportButtonContent,
            ImportButtonContent;

        private void OnEnable()
        {
            AddIcon = EditorGUIUtility.IconContent("d_Toolbar Plus").image;
            SaveIcon = EditorGUIUtility.IconContent("d_SaveAs").image;
            DuplicateIcon = EditorGUIUtility.IconContent("d_TreeEditor.Duplicate").image;
            ImportIcon = EditorGUIUtility.IconContent("d_Import").image;

            AddButtonContent = new GUIContent(" Add Song", AddIcon);
            SaveButtonContent = new GUIContent(" Save", SaveIcon);
            CopyButtonContent = new GUIContent(" Copy", DuplicateIcon, "Copy the raw JSON data");
            ExportButtonContent = new GUIContent(" Export", SaveIcon, "Export the SongPlaylist");
            ImportButtonContent = new GUIContent(" Import", ImportIcon, "Import SongPlaylist from external data");

            songPlaylist = (SongPlaylist)target;

            LoadPlaylistData();
        }

        private void OnDestroy()
        {
            if(songPlaylist == null || !isDirty) { return; }

            SavePlaylist();
        }

        private void SavePlaylist()
        {
            songPlaylist.Data = songPlaylistData;

            EditorUtility.SetDirty(songPlaylist);

            isDirty = false;
        }

        public override void OnInspectorGUI()
        {
            using (EditorGUI.ChangeCheckScope scope = new EditorGUI.ChangeCheckScope())
            {
                string name = EditorGUILayout.TextField(NameFieldContent, songPlaylistData.Name);

                string description = EditorGUILayout.TextField(DescriptionFieldContent, songPlaylistData.Description);

                bool copyrightSafe = EditorGUILayout.Toggle(CopyrightSafeFieldContent, songPlaylistData.CopyrightSafe);

                bool canAutoplay = EditorGUILayout.Toggle(CanAutoplayFieldContent, songPlaylistData.CanAutoplay);

                if (scope.changed)
                {
                    songPlaylistData.Name = name;

                    songPlaylistData.Description = description;

                    songPlaylistData.CopyrightSafe = copyrightSafe;

                    songPlaylistData.CanAutoplay = canAutoplay;

                    isDirty = true;
                }
            }

            using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                showReorderingTools = GUILayout.Toggle(showReorderingTools, ShowReorderingToolsToggleContent);
            }

            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new GUILayout.HorizontalScope())
                {
                    if (showReorderingTools) { GUILayout.Space(84); }

                    GUILayout.Label("Title");

                    GUILayout.Label("Artist");

                    GUILayout.Label("URL");

                    GUILayout.Space(28);
                }

                int songCount = songPlaylistData.Songs.Count;

                using (EditorGUILayout.ScrollViewScope scrollScope = new EditorGUILayout.ScrollViewScope(scrollPos, GUILayout.MaxHeight(Screen.height - 320f)))
                {
                    scrollPos = scrollScope.scrollPosition;

                    for (int i = 0; i < songCount; i++)
                    {
                        Song song = songPlaylistData.Songs[i];

                        using (new GUILayout.HorizontalScope())
                        {
                            if (showReorderingTools)
                            {
                                using (new EditorGUI.DisabledGroupScope(i == 0))
                                {
                                    if (GUILayout.Button("▲", GUILayout.Width(25)))
                                    {
                                        songPlaylistData.Songs.RemoveAt(i);
                                        songPlaylistData.Songs.Insert(i - 1, song);

                                        isDirty = true;

                                        break;
                                    }
                                }

                                using (var scope = new EditorGUI.ChangeCheckScope())
                                {
                                    int index = EditorGUILayout.DelayedIntField(i, GUILayout.Width(25));

                                    if (scope.changed)
                                    {
                                        index = Mathf.Clamp(index, 0, songCount - 1);

                                        songPlaylistData.Songs.RemoveAt(i);
                                        songPlaylistData.Songs.Insert(index, song);

                                        isDirty = true;

                                        break;
                                    }
                                }

                                using (new EditorGUI.DisabledGroupScope(i == songCount - 1))
                                {
                                    if (GUILayout.Button("▼", GUILayout.Width(25)))
                                    {
                                        songPlaylistData.Songs.RemoveAt(i);
                                        songPlaylistData.Songs.Insert(i + 1, song);

                                        isDirty = true;

                                        break;
                                    }
                                }
                            }

                            using (var scope = new EditorGUI.ChangeCheckScope())
                            {
                                song.Title = EditorGUILayout.TextField(song.Title);

                                song.Artist = EditorGUILayout.TextField(song.Artist);

                                song.URL = EditorGUILayout.TextField(song.URL);

                                if (scope.changed)
                                {
                                    songPlaylistData.Songs[i] = song;

                                    isDirty = true;
                                }
                            }

                            if (GUILayout.Button("X", GUILayout.Width(25)))
                            {
                                songPlaylistData.Songs.RemoveAt(i);

                                isDirty = true;

                                break;
                            }
                        }
                    }
                }
            }

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button(AddButtonContent, GUILayout.Height(32f)))
                {
                    songPlaylistData.Songs.Add(new Song());

                    isDirty = true;
                }

                using (new EditorGUI.DisabledScope(!isDirty))
                {
                    if (GUILayout.Button(SaveButtonContent, GUILayout.Height(32f)))
                    {
                        SavePlaylist();
                    }
                }

                using (new EditorGUI.DisabledScope(isDirty))
                {
                    if (GUILayout.Button(CopyButtonContent, GUILayout.Height(32f)))
                    {
                        GenericMenu menu = new GenericMenu();

                        menu.AddItem(new GUIContent("Copy raw JSON", "Copies the raw JSON to your clipboard"), false, () => EditorGUIUtility.systemCopyBuffer = songPlaylist.RawJsonData);

                        menu.AddItem(new GUIContent("Copy raw JSON with code block formatting", "Copies the raw JSON to your clipboard with code block formatting (useful for displaying data in e.g. Discord, Markdown, etc.)"), false, () => EditorGUIUtility.systemCopyBuffer = string.Format("```json\n{0}\n```", songPlaylist.RawJsonData));

                        menu.ShowAsContext();
                    }
                    else if (GUILayout.Button(ExportButtonContent, GUILayout.Height(32f)))
                    {
                        GenericMenu menu = new GenericMenu();

                        menu.AddItem(new GUIContent("Export SongPlaylist as Unitypackage", "Exports the SongPlaylist ScriptableObject as a Unitypackage"), false, () => ExportAsUnitypackage());

                        menu.AddItem(new GUIContent("Export raw JSON to file", "Saves the raw JSON data from the playlist to a file"), false, () => SaveRawJSONToFile());

                        menu.ShowAsContext();
                    }
                }
                if (GUILayout.Button(ImportButtonContent, GUILayout.Height(32f)))
                {
                    GenericMenu menu = new GenericMenu();

                    bool isJSONCopyBufferValid = SongPlaylist.IsJSONValidSongPlaylistData(EditorGUIUtility.systemCopyBuffer);

                    bool isTSVCopyBufferValid = SongPlaylist.IsTSVValidSongPlaylistData(EditorGUIUtility.systemCopyBuffer);

                    menu.AddItem(new GUIContent("Import from JSON/JSON File/Replace"), false, () => ImportPlaylistFromJSON());

                    menu.AddItem(new GUIContent("Import from JSON/JSON File/Append"), false, () => ImportPlaylistFromJSON(true));

                    if (isJSONCopyBufferValid)
                    {
                        menu.AddItem(new GUIContent("Import from JSON/JSON on clipboard/Replace"), false, () => ImportPlaylistFromJSON(EditorGUIUtility.systemCopyBuffer));

                        menu.AddItem(new GUIContent("Import from JSON/JSON on clipboard/Append"), false, () => ImportPlaylistFromJSON(EditorGUIUtility.systemCopyBuffer, true));
                    }
                    else
                    {
                        menu.AddDisabledItem(new GUIContent("Import from JSON/JSON on clipboard"));
                    }

                    menu.AddItem(new GUIContent("Import from TSV/TSV File/Replace"), false, () => ImportPlaylistFromTSV());

                    menu.AddItem(new GUIContent("Import from TSV/TSV File/Append"), false, () => ImportPlaylistFromTSV(true));

                    if (isTSVCopyBufferValid)
                    {
                        menu.AddItem(new GUIContent("Import from TSV/TSV on clipboard/Replace"), false, () => ImportPlaylistFromTSV(EditorGUIUtility.systemCopyBuffer));

                        menu.AddItem(new GUIContent("Import from TSV/TSV on clipboard/Append"), false, () => ImportPlaylistFromTSV(EditorGUIUtility.systemCopyBuffer, true));
                    }
                    else
                    {
                        menu.AddDisabledItem(new GUIContent("Import from TSV in clipboard"));
                    }

                    menu.ShowAsContext();
                }
            }
        }

        private void LoadPlaylistData()
        {
            songPlaylistData = songPlaylist.Data;
        }

        private void ImportPlaylistFromJSON(bool append = false)
        {
            string path = EditorUtility.OpenFilePanel("Import Playlist from JSON", "", "json");

            if (string.IsNullOrEmpty(path) || !File.Exists(path)) { return; }

            string json;

            using (StreamReader reader = new StreamReader(path))
            {
                json = reader.ReadToEnd();
            }

            ImportPlaylistFromJSON(json, append);
        }

        private void ImportPlaylistFromJSON(string json, bool append = false)
        {
            SongPlaylistData data = SongPlaylist.FromJSON(json);

            if (append)
            {
                songPlaylistData.Songs.AddRange(data.Songs);

                songPlaylist.Data = songPlaylistData;
            }
            else
            {
                songPlaylist.Data = data;
            }

            isDirty = true;

            LoadPlaylistData();
        }

        private void ImportPlaylistFromTSV(bool append = false)
        {
            string path = EditorUtility.OpenFilePanel("Import Playlist from TSV", "", "tsv");

            if (string.IsNullOrEmpty(path) || !File.Exists(path)) { return; }

            string tsv;

            using (StreamReader reader = new StreamReader(path))
            {
                tsv = reader.ReadToEnd();
            }

            ImportPlaylistFromTSV(tsv, append);
        }

        private void ImportPlaylistFromTSV(string tsv, bool append = false)
        {
            SongPlaylistData data = SongPlaylist.FromTSV(tsv);

            if (append)
            {
                songPlaylistData.Songs.AddRange(data.Songs);
            }
            else
            {
                songPlaylistData.Songs = data.Songs;
            }

            songPlaylist.Data = songPlaylistData;

            isDirty = true;

            LoadPlaylistData();
        }

        private void ExportAsUnitypackage()
        {
            string path = EditorUtility.SaveFilePanel("Save SongPlaylist as Unitypackage", Path.GetDirectoryName(AssetDatabase.GetAssetPath(songPlaylist)), string.Format("SongPlaylist_{0}", songPlaylist.name), "unitypackage");

            if (string.IsNullOrEmpty(path)) { return; }

            AssetDatabase.ExportPackage(AssetDatabase.GetAssetPath(songPlaylist), path, ExportPackageOptions.Interactive);

            AssetDatabase.Refresh();
        }

        private void SaveRawJSONToFile()
        {
            string path = EditorUtility.SaveFilePanel("Save SongPlaylist data as JSON", Path.GetDirectoryName(AssetDatabase.GetAssetPath(songPlaylist)), string.Format("SongPlaylist_{0}", songPlaylist.name), "json");

            if (string.IsNullOrEmpty(path)) { return; }

            using (StreamWriter writer = new StreamWriter(path))
            {
                writer.Write(songPlaylist.RawJsonData);
            }

            AssetDatabase.Refresh();
        }
    }
}
