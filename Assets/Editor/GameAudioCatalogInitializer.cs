#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using MoreMountains.Tools;
using UnityEditor;
using UnityEngine;

public static class GameAudioCatalogInitializer
{
    private const string CatalogAssetPath = "Assets/Resources/GameAudioCatalog.asset";
    private const string AudioRootFolder = "Assets/SE & BGM";
    private const string SharedPrefabPath = "Assets/Resources/Prefabs/通用物体/通用物体.prefab";

    [InitializeOnLoadMethod]
    private static void EnsureCatalogOnLoad()
    {
        EditorApplication.delayCall += EnsureCatalogExists;
    }

    private static void EnsureCatalogExists()
    {
        if (AssetDatabase.LoadAssetAtPath<GameAudioCatalog>(CatalogAssetPath) != null)
        {
            return;
        }

        CreateOrUpdateDefaultCatalog();
    }

    [MenuItem("Tools/Audio/创建默认音频配置表")]
    public static void CreateOrUpdateDefaultCatalogMenu()
    {
        GameAudioCatalog catalog = CreateOrUpdateDefaultCatalog();
        Selection.activeObject = catalog;
        EditorGUIUtility.PingObject(catalog);
        EditorUtility.DisplayDialog("音频配置表", $"已创建/更新：\n{CatalogAssetPath}", "确定");
    }

    public static GameAudioCatalog CreateOrUpdateDefaultCatalog()
    {
        EnsureFolderExists("Assets/Resources");

        GameAudioCatalog catalog = AssetDatabase.LoadAssetAtPath<GameAudioCatalog>(CatalogAssetPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<GameAudioCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogAssetPath);
        }

        SerializedObject serializedCatalog = new SerializedObject(catalog);
        SerializedProperty entriesProperty = serializedCatalog.FindProperty("entries");
        entriesProperty.ClearArray();

        AddDefaultEntries(entriesProperty);
        serializedCatalog.ApplyModifiedPropertiesWithoutUndo();

        catalog.RebuildCaches();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();

        WireSharedPrefab(catalog);
        return catalog;
    }

    public static void ScanAudioFolder(GameAudioCatalog catalog)
    {
        if (catalog == null)
        {
            return;
        }

        SerializedObject serializedCatalog = new SerializedObject(catalog);
        SerializedProperty entriesProperty = serializedCatalog.FindProperty("entries");
        var existingClipIds = new HashSet<string>();

        for (int i = 0; i < entriesProperty.arraySize; i++)
        {
            SerializedProperty clipProperty = entriesProperty.GetArrayElementAtIndex(i).FindPropertyRelative("clip");
            if (clipProperty.objectReferenceValue != null)
            {
                existingClipIds.Add(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(clipProperty.objectReferenceValue)));
            }
        }

        string[] clipGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { AudioRootFolder });
        int addedCount = 0;

        for (int i = 0; i < clipGuids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(clipGuids[i]);
            if (existingClipIds.Contains(clipGuids[i]))
            {
                continue;
            }

            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            if (clip == null)
            {
                continue;
            }

            int newIndex = entriesProperty.arraySize;
            entriesProperty.arraySize++;
            SerializedProperty entryProperty = entriesProperty.GetArrayElementAtIndex(newIndex);

            string fileName = Path.GetFileNameWithoutExtension(assetPath);
            string entryId = BuildEntryId(assetPath, fileName);
            entryProperty.FindPropertyRelative("entryId").stringValue = entryId;
            entryProperty.FindPropertyRelative("displayName").stringValue = fileName;
            entryProperty.FindPropertyRelative("category").enumValueIndex = (int)ResolveCategory(assetPath);
            entryProperty.FindPropertyRelative("clip").objectReferenceValue = clip;
            entryProperty.FindPropertyRelative("bindToEvent").boolValue = false;
            entryProperty.FindPropertyRelative("bindToBgm").boolValue = false;
            entryProperty.FindPropertyRelative("volume").floatValue = 1f;
            entryProperty.FindPropertyRelative("pitch").floatValue = 1f;
            entryProperty.FindPropertyRelative("loop").boolValue = assetPath.Contains("/BGM/");
            entryProperty.FindPropertyRelative("track").enumValueIndex = assetPath.Contains("/BGM/")
                ? (int)MMSoundManager.MMSoundManagerTracks.Music
                : (int)MMSoundManager.MMSoundManagerTracks.Sfx;
            entryProperty.isExpanded = false;
            addedCount++;
        }

        serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
        catalog.RebuildCaches();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();

        Debug.Log($"[GameAudioCatalog] 扫描完成，新增 {addedCount} 条音频。");
    }

    private static void AddDefaultEntries(SerializedProperty entriesProperty)
    {
        AddEventEntry(entriesProperty, "ui.mouse_click", "鼠标点击", GameAudioCategory.UI,
            LoadClip("Assets/Feel/MMFeedbacks/Demos/SequencingDemo/Sounds/MMSequencingClick.wav"),
            GameAudioEventType.MouseClick, MMSoundManager.MMSoundManagerTracks.UI);

        AddEventEntry(entriesProperty, "ui.button_hover_enter", "按钮悬停进入", GameAudioCategory.UI,
            LoadClip("Assets/SE & BGM/按钮进出.mp3"),
            GameAudioEventType.ButtonHoverEnter, MMSoundManager.MMSoundManagerTracks.UI);

        AddEventEntry(entriesProperty, "ui.button_hover_exit", "按钮悬停离开", GameAudioCategory.UI,
            LoadClip("Assets/SE & BGM/按钮进出.mp3"),
            GameAudioEventType.ButtonHoverExit, MMSoundManager.MMSoundManagerTracks.UI);

        AddEventEntry(entriesProperty, "ui.level_scroll", "关卡滚动", GameAudioCategory.UI,
            LoadClip("Assets/SE & BGM/SE/切换页面.wav"),
            GameAudioEventType.LevelScroll, MMSoundManager.MMSoundManagerTracks.UI);

        AddEventEntry(entriesProperty, "combat.damage", "受到伤害", GameAudioCategory.Combat,
            LoadClip("Assets/SE & BGM/SE/击中.wav"),
            GameAudioEventType.CombatDamage, MMSoundManager.MMSoundManagerTracks.Sfx, useTargetPosition: true);

        AddEventEntry(entriesProperty, "combat.buff_gain", "获得 Buff", GameAudioCategory.Combat,
            LoadClip("Assets/SE & BGM/SE/Buff.wav"),
            GameAudioEventType.CombatBuffGain, MMSoundManager.MMSoundManagerTracks.Sfx, useTargetPosition: true);

        AddEventEntry(entriesProperty, "combat.debuff_gain", "获得 Debuff", GameAudioCategory.Combat,
            LoadClip("Assets/SE & BGM/SE/Debuff.wav"),
            GameAudioEventType.CombatDebuffGain, MMSoundManager.MMSoundManagerTracks.Sfx, useTargetPosition: true);

        AddBgmEntry(entriesProperty, "bgm.lobby", "主界面 BGM", BGMPlayer.BGMType.Lobby,
            LoadClip("Assets/SE & BGM/BGM/主界面.mp3"), 0.8f);

        AddBgmEntry(entriesProperty, "bgm.battle", "通用战斗 BGM", BGMPlayer.BGMType.Battle,
            LoadClip("Assets/SE & BGM/BGM/通用战斗音乐_01.mp3"), 0.8f);

        AddBgmEntry(entriesProperty, "bgm.backpack", "背包 BGM", BGMPlayer.BGMType.Backpack,
            LoadClip("Assets/SE & BGM/BGM/主界面.mp3"), 0.8f);

        AddBgmEntry(entriesProperty, "bgm.shop", "商店 BGM", BGMPlayer.BGMType.Shop,
            LoadClip("Assets/SE & BGM/BGM/主界面.mp3"), 0.8f);

        AddBgmEntry(entriesProperty, "bgm.result", "结算 BGM", BGMPlayer.BGMType.Result,
            LoadClip("Assets/SE & BGM/BGM/主界面.mp3"), 0.8f);
    }

    private static void AddEventEntry(
        SerializedProperty entriesProperty,
        string entryId,
        string displayName,
        GameAudioCategory category,
        AudioClip clip,
        GameAudioEventType eventType,
        MMSoundManager.MMSoundManagerTracks track,
        bool useTargetPosition = false,
        float volume = 1f)
    {
        int index = entriesProperty.arraySize;
        entriesProperty.arraySize++;
        SerializedProperty entryProperty = entriesProperty.GetArrayElementAtIndex(index);

        entryProperty.FindPropertyRelative("entryId").stringValue = entryId;
        entryProperty.FindPropertyRelative("displayName").stringValue = displayName;
        entryProperty.FindPropertyRelative("category").enumValueIndex = (int)category;
        entryProperty.FindPropertyRelative("clip").objectReferenceValue = clip;
        entryProperty.FindPropertyRelative("bindToEvent").boolValue = true;
        entryProperty.FindPropertyRelative("eventType").enumValueIndex = (int)eventType;
        entryProperty.FindPropertyRelative("bindToBgm").boolValue = false;
        entryProperty.FindPropertyRelative("track").enumValueIndex = (int)track;
        entryProperty.FindPropertyRelative("volume").floatValue = volume;
        entryProperty.FindPropertyRelative("pitch").floatValue = 1f;
        entryProperty.FindPropertyRelative("useTargetPosition").boolValue = useTargetPosition;
        entryProperty.FindPropertyRelative("loop").boolValue = false;
    }

    private static void AddBgmEntry(
        SerializedProperty entriesProperty,
        string entryId,
        string displayName,
        BGMPlayer.BGMType bgmType,
        AudioClip clip,
        float volume)
    {
        int index = entriesProperty.arraySize;
        entriesProperty.arraySize++;
        SerializedProperty entryProperty = entriesProperty.GetArrayElementAtIndex(index);

        entryProperty.FindPropertyRelative("entryId").stringValue = entryId;
        entryProperty.FindPropertyRelative("displayName").stringValue = displayName;
        entryProperty.FindPropertyRelative("category").enumValueIndex = (int)GameAudioCategory.BGM;
        entryProperty.FindPropertyRelative("clip").objectReferenceValue = clip;
        entryProperty.FindPropertyRelative("bindToEvent").boolValue = false;
        entryProperty.FindPropertyRelative("bindToBgm").boolValue = true;
        entryProperty.FindPropertyRelative("bgmType").enumValueIndex = (int)bgmType;
        entryProperty.FindPropertyRelative("track").enumValueIndex = (int)MMSoundManager.MMSoundManagerTracks.Music;
        entryProperty.FindPropertyRelative("volume").floatValue = volume;
        entryProperty.FindPropertyRelative("pitch").floatValue = 1f;
        entryProperty.FindPropertyRelative("useTargetPosition").boolValue = false;
        entryProperty.FindPropertyRelative("loop").boolValue = true;
    }

    private static void WireSharedPrefab(GameAudioCatalog catalog)
    {
        GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(SharedPrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogWarning($"[GameAudioCatalog] 未找到通用物体 Prefab：{SharedPrefabPath}");
            return;
        }

        string prefabPath = SharedPrefabPath;
        GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
        bool changed = false;

        GameAudioEventSoundPlayer soundPlayer = prefabContents.GetComponentInChildren<GameAudioEventSoundPlayer>(true);
        if (soundPlayer != null)
        {
            SerializedObject serializedPlayer = new SerializedObject(soundPlayer);
            serializedPlayer.FindProperty("catalog").objectReferenceValue = catalog;
            serializedPlayer.ApplyModifiedPropertiesWithoutUndo();
            changed = true;
        }

        BGMPlayer bgmPlayer = prefabContents.GetComponentInChildren<BGMPlayer>(true);
        if (bgmPlayer != null)
        {
            SerializedObject serializedBgm = new SerializedObject(bgmPlayer);
            serializedBgm.FindProperty("catalog").objectReferenceValue = catalog;
            serializedBgm.ApplyModifiedPropertiesWithoutUndo();
            changed = true;
        }

        if (changed)
        {
            PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
            Debug.Log("[GameAudioCatalog] 已把配置表引用写入通用物体 Prefab。");
        }

        PrefabUtility.UnloadPrefabContents(prefabContents);
    }

    private static AudioClip LoadClip(string assetPath)
    {
        return AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
    }

    private static GameAudioCategory ResolveCategory(string assetPath)
    {
        string normalized = assetPath.Replace('\\', '/');
        if (normalized.Contains("/BGM/"))
        {
            return GameAudioCategory.BGM;
        }

        if (normalized.Contains("/SE/"))
        {
            return GameAudioCategory.Combat;
        }

        return GameAudioCategory.Other;
    }

    private static string BuildEntryId(string assetPath, string fileName)
    {
        string normalized = assetPath.Replace('\\', '/');
        if (normalized.Contains("/BGM/"))
        {
            return $"bgm.{SanitizeId(fileName)}";
        }

        if (normalized.Contains("/SE/"))
        {
            return $"se.{SanitizeId(fileName)}";
        }

        return $"audio.{SanitizeId(fileName)}";
    }

    private static string SanitizeId(string value)
    {
        return value.Replace(' ', '_').ToLowerInvariant();
    }

    private static void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        string folderName = Path.GetFileName(folderPath);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolderExists(parent);
        }

        AssetDatabase.CreateFolder(parent, folderName);
    }
}
#endif
