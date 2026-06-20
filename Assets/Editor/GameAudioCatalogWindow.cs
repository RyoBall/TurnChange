#if UNITY_EDITOR
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEditor;
using UnityEngine;

public class GameAudioCatalogWindow : EditorWindow
{
    private const string DefaultCatalogPath = "Assets/Resources/GameAudioCatalog.asset";

    private GameAudioCatalog m_Catalog;
    private SerializedObject m_SerializedCatalog;
    private Vector2 m_ScrollPosition;
    private string m_SearchText = string.Empty;
    private int m_CategoryFilter = -1;
    private bool m_ShowUnboundOnly;

    [MenuItem("Tools/Audio/音频配置表")]
    public static void ShowWindow()
    {
        var window = GetWindow<GameAudioCatalogWindow>("音频配置表");
        window.minSize = new Vector2(560f, 420f);
        window.TryLoadDefaultCatalog();
        window.Show();
    }

    public static void ShowWindow(GameAudioCatalog catalog)
    {
        var window = GetWindow<GameAudioCatalogWindow>("音频配置表");
        window.minSize = new Vector2(560f, 420f);
        window.SetCatalog(catalog);
        window.Show();
    }

    private void OnEnable()
    {
        TryLoadDefaultCatalog();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("游戏音频统一管理", EditorStyles.boldLabel);

        DrawCatalogField();
        if (m_Catalog == null)
        {
            EditorGUILayout.HelpBox("请先创建或指定 GameAudioCatalog 资源。", MessageType.Warning);
            if (GUILayout.Button("创建默认配置表"))
            {
                GameAudioCatalogInitializer.CreateOrUpdateDefaultCatalog();
                TryLoadDefaultCatalog();
            }

            return;
        }

        EnsureSerializedObject();
        m_SerializedCatalog.Update();

        DrawActions();
        DrawFilters();

        m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);
        DrawEntries();
        EditorGUILayout.EndScrollView();

        if (m_SerializedCatalog.ApplyModifiedProperties())
        {
            m_Catalog.RebuildCaches();
            EditorUtility.SetDirty(m_Catalog);
        }
    }

    private void DrawCatalogField()
    {
        EditorGUI.BeginChangeCheck();
        m_Catalog = (GameAudioCatalog)EditorGUILayout.ObjectField("配置表", m_Catalog, typeof(GameAudioCatalog), false);
        if (EditorGUI.EndChangeCheck())
        {
            SetCatalog(m_Catalog);
        }
    }

    private void DrawActions()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("创建/重置默认配置", GUILayout.Width(140f)))
        {
            GameAudioCatalogInitializer.CreateOrUpdateDefaultCatalog();
            TryLoadDefaultCatalog();
        }

        if (GUILayout.Button("扫描 SE & BGM 文件夹", GUILayout.Width(150f)))
        {
            GameAudioCatalogInitializer.ScanAudioFolder(m_Catalog);
            EnsureSerializedObject();
            m_SerializedCatalog.Update();
        }

        if (GUILayout.Button("选中资源", GUILayout.Width(80f)))
        {
            Selection.activeObject = m_Catalog;
            EditorGUIUtility.PingObject(m_Catalog);
        }

        if (GUILayout.Button("添加条目", GUILayout.Width(80f)))
        {
            SerializedProperty entriesProperty = m_SerializedCatalog.FindProperty("entries");
            entriesProperty.arraySize++;
            SerializedProperty newEntry = entriesProperty.GetArrayElementAtIndex(entriesProperty.arraySize - 1);
            ResetEntry(newEntry, $"entry_{entriesProperty.arraySize}");
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawFilters()
    {
        EditorGUILayout.BeginHorizontal();
        m_SearchText = EditorGUILayout.TextField("搜索", m_SearchText);
        m_CategoryFilter = EditorGUILayout.Popup(
            m_CategoryFilter + 1,
            BuildCategoryOptions(),
            GUILayout.Width(120f)) - 1;
        m_ShowUnboundOnly = EditorGUILayout.ToggleLeft("仅未绑定", m_ShowUnboundOnly, GUILayout.Width(90f));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(4f);
    }

    private void DrawEntries()
    {
        SerializedProperty entriesProperty = m_SerializedCatalog.FindProperty("entries");
        int visibleCount = 0;

        for (int i = 0; i < entriesProperty.arraySize; i++)
        {
            SerializedProperty entryProperty = entriesProperty.GetArrayElementAtIndex(i);
            if (!PassesFilter(entryProperty))
            {
                continue;
            }

            visibleCount++;
            DrawEntryCard(entryProperty, i, entriesProperty);
        }

        if (visibleCount == 0)
        {
            EditorGUILayout.HelpBox("没有匹配的音频条目。", MessageType.Info);
        }
    }

    private void DrawEntryCard(SerializedProperty entryProperty, int index, SerializedProperty entriesProperty)
    {
        SerializedProperty displayNameProperty = entryProperty.FindPropertyRelative("displayName");
        SerializedProperty idProperty = entryProperty.FindPropertyRelative("entryId");
        SerializedProperty clipProperty = entryProperty.FindPropertyRelative("clip");
        SerializedProperty volumeProperty = entryProperty.FindPropertyRelative("volume");
        SerializedProperty pitchProperty = entryProperty.FindPropertyRelative("pitch");

        string title = string.IsNullOrEmpty(displayNameProperty.stringValue)
            ? idProperty.stringValue
            : displayNameProperty.stringValue;

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        entryProperty.isExpanded = EditorGUILayout.Foldout(entryProperty.isExpanded, title, true);
        GUILayout.Label(clipProperty.objectReferenceValue != null ? clipProperty.objectReferenceValue.name : "无 Clip", EditorStyles.miniLabel, GUILayout.Width(160f));

        if (clipProperty.objectReferenceValue != null && GUILayout.Button("试听", GUILayout.Width(44f)))
        {
            GameAudioCatalogEditorUtility.PlayPreview(
                (AudioClip)clipProperty.objectReferenceValue,
                volumeProperty.floatValue,
                pitchProperty.floatValue);
        }

        if (GUILayout.Button("X", GUILayout.Width(22f)))
        {
            entriesProperty.DeleteArrayElementAtIndex(index);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.PropertyField(volumeProperty, new GUIContent("音量"));
        EditorGUILayout.PropertyField(pitchProperty, new GUIContent("音高"));

        if (entryProperty.isExpanded)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(idProperty);
            EditorGUILayout.PropertyField(displayNameProperty);
            EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("category"));
            EditorGUILayout.PropertyField(clipProperty);

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
            EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("useTargetPosition"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2f);
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
            int category = entryProperty.FindPropertyRelative("category").enumValueIndex;
            if (category != m_CategoryFilter)
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

    private void TryLoadDefaultCatalog()
    {
        GameAudioCatalog catalog = AssetDatabase.LoadAssetAtPath<GameAudioCatalog>(DefaultCatalogPath);
        if (catalog == null)
        {
            catalog = GameAudioCatalog.Instance;
        }

        SetCatalog(catalog);
    }

    private void SetCatalog(GameAudioCatalog catalog)
    {
        m_Catalog = catalog;
        m_SerializedCatalog = catalog != null ? new SerializedObject(catalog) : null;
    }

    private void EnsureSerializedObject()
    {
        if (m_Catalog != null && (m_SerializedCatalog == null || m_SerializedCatalog.targetObject != m_Catalog))
        {
            m_SerializedCatalog = new SerializedObject(m_Catalog);
        }
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
#endif
