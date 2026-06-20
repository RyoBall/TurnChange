#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameAudioCatalog))]
public class GameAudioCatalogEditor : Editor
{
    private string m_SearchText = string.Empty;
    private int m_CategoryFilter = -1;
    private bool m_ShowUnboundOnly;

    public override void OnInspectorGUI()
    {
        var catalog = (GameAudioCatalog)target;
        SerializedProperty entriesProperty = serializedObject.FindProperty("entries");

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("音频配置表", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "在此统一管理所有 BGM 与音效。事件类条目勾选「绑定事件」，BGM 勾选「绑定 BGM」。也可通过 Tools/Audio/音频配置表 打开专用窗口。",
            MessageType.Info);

        DrawToolbar(catalog, entriesProperty);
        EditorGUILayout.Space(4f);

        serializedObject.Update();
        DrawFilteredEntries(entriesProperty);
        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
        {
            catalog.RebuildCaches();
            EditorUtility.SetDirty(catalog);
        }
    }

    private void DrawToolbar(GameAudioCatalog catalog, SerializedProperty entriesProperty)
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("打开配置窗口", GUILayout.Width(110f)))
        {
            GameAudioCatalogWindow.ShowWindow(catalog);
        }

        if (GUILayout.Button("从 SE & BGM 扫描", GUILayout.Width(130f)))
        {
            GameAudioCatalogInitializer.ScanAudioFolder(catalog);
            serializedObject.Update();
        }

        if (GUILayout.Button("添加条目", GUILayout.Width(80f)))
        {
            entriesProperty.arraySize++;
            SerializedProperty newEntry = entriesProperty.GetArrayElementAtIndex(entriesProperty.arraySize - 1);
            ResetEntry(newEntry, $"entry_{entriesProperty.arraySize}");
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        m_SearchText = EditorGUILayout.TextField("搜索", m_SearchText);
        m_CategoryFilter = EditorGUILayout.Popup(
            m_CategoryFilter + 1,
            BuildCategoryOptions(),
            GUILayout.Width(100f)) - 1;
        m_ShowUnboundOnly = EditorGUILayout.ToggleLeft("仅未绑定", m_ShowUnboundOnly, GUILayout.Width(90f));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawFilteredEntries(SerializedProperty entriesProperty)
    {
        for (int i = 0; i < entriesProperty.arraySize; i++)
        {
            SerializedProperty entryProperty = entriesProperty.GetArrayElementAtIndex(i);
            if (!PassesFilter(entryProperty))
            {
                continue;
            }

            EditorGUILayout.BeginVertical("box");
            DrawEntryHeader(entryProperty, i, entriesProperty);
            DrawEntryBody(entryProperty);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2f);
        }
    }

    private void DrawEntryHeader(SerializedProperty entryProperty, int index, SerializedProperty entriesProperty)
    {
        SerializedProperty idProperty = entryProperty.FindPropertyRelative("entryId");
        SerializedProperty displayNameProperty = entryProperty.FindPropertyRelative("displayName");
        SerializedProperty clipProperty = entryProperty.FindPropertyRelative("clip");

        string title = string.IsNullOrEmpty(displayNameProperty.stringValue)
            ? idProperty.stringValue
            : displayNameProperty.stringValue;

        EditorGUILayout.BeginHorizontal();
        entryProperty.isExpanded = EditorGUILayout.Foldout(entryProperty.isExpanded, title, true);
        GUILayout.FlexibleSpace();

        if (clipProperty.objectReferenceValue != null && GUILayout.Button("试听", GUILayout.Width(44f)))
        {
            SerializedProperty volumeProperty = entryProperty.FindPropertyRelative("volume");
            SerializedProperty pitchProperty = entryProperty.FindPropertyRelative("pitch");
            GameAudioCatalogEditorUtility.PlayPreview(
                (AudioClip)clipProperty.objectReferenceValue,
                volumeProperty.floatValue,
                pitchProperty.floatValue);
        }

        if (GUILayout.Button("删除", GUILayout.Width(44f)))
        {
            entriesProperty.DeleteArrayElementAtIndex(index);
            return;
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawEntryBody(SerializedProperty entryProperty)
    {
        if (!entryProperty.isExpanded)
        {
            return;
        }

        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("entryId"));
        EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("displayName"));
        EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("category"));
        EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("clip"));

        EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("bindToEvent"));
        if (entryProperty.FindPropertyRelative("bindToEvent").boolValue)
        {
            EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("eventType"));
        }

        EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("bindToBgm"));
        if (entryProperty.FindPropertyRelative("bindToBgm").boolValue)
        {
            EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("bgmType"));
            EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("loop"));
        }

        EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("track"));
        EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("volume"));
        EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("pitch"));
        EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("useTargetPosition"));
        EditorGUI.indentLevel--;
    }

    private bool PassesFilter(SerializedProperty entryProperty)
    {
        if (m_ShowUnboundOnly)
        {
            bool bindToEvent = entryProperty.FindPropertyRelative("bindToEvent").boolValue;
            bool bindToBgm = entryProperty.FindPropertyRelative("bindToBgm").boolValue;
            if (bindToEvent || bindToBgm)
            {
                return false;
            }
        }

        if (m_CategoryFilter >= 0)
        {
            var category = (GameAudioCategory)entryProperty.FindPropertyRelative("category").enumValueIndex;
            if ((int)category != m_CategoryFilter)
            {
                return false;
            }
        }

        if (string.IsNullOrWhiteSpace(m_SearchText))
        {
            return true;
        }

        string search = m_SearchText.Trim();
        string entryId = entryProperty.FindPropertyRelative("entryId").stringValue;
        string displayName = entryProperty.FindPropertyRelative("displayName").stringValue;
        Object clip = entryProperty.FindPropertyRelative("clip").objectReferenceValue;

        return (!string.IsNullOrEmpty(entryId) && entryId.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0)
            || (!string.IsNullOrEmpty(displayName) && displayName.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0)
            || (clip != null && clip.name.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static string[] BuildCategoryOptions()
    {
        var names = new List<string> { "全部分类" };
        names.AddRange(System.Enum.GetNames(typeof(GameAudioCategory)));
        return names.ToArray();
    }

    private static void ResetEntry(SerializedProperty entryProperty, string entryId)
    {
        entryProperty.FindPropertyRelative("entryId").stringValue = entryId;
        entryProperty.FindPropertyRelative("displayName").stringValue = entryId;
        entryProperty.FindPropertyRelative("category").enumValueIndex = (int)GameAudioCategory.Other;
        entryProperty.FindPropertyRelative("clip").objectReferenceValue = null;
        entryProperty.FindPropertyRelative("bindToEvent").boolValue = false;
        entryProperty.FindPropertyRelative("bindToBgm").boolValue = false;
        entryProperty.FindPropertyRelative("volume").floatValue = 1f;
        entryProperty.FindPropertyRelative("pitch").floatValue = 1f;
        entryProperty.FindPropertyRelative("loop").boolValue = false;
        entryProperty.FindPropertyRelative("track").enumValueIndex = (int)MMSoundManager.MMSoundManagerTracks.Sfx;
        entryProperty.isExpanded = true;
    }
}

public static class GameAudioCatalogEditorUtility
{
    public static void PlayPreview(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null)
        {
            return;
        }

        float resolvedVolume = Mathf.Clamp(volume, 0f, 2f);
        float resolvedPitch = Mathf.Approximately(pitch, 0f) ? 1f : pitch;

        System.Type audioUtilType = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        if (audioUtilType == null)
        {
            return;
        }

        System.Reflection.MethodInfo playMethod = audioUtilType.GetMethod(
            "PlayPreviewClip",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
            null,
            new[] { typeof(AudioClip), typeof(int), typeof(bool), typeof(float), typeof(float) },
            null);

        if (playMethod != null)
        {
            playMethod.Invoke(null, new object[] { clip, 0, false, resolvedVolume, resolvedPitch });
            return;
        }

        playMethod = audioUtilType.GetMethod(
            "PlayPreviewClip",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
            null,
            new[] { typeof(AudioClip), typeof(int), typeof(bool) },
            null);

        playMethod?.Invoke(null, new object[] { clip, 0, false });
    }
}
#endif
