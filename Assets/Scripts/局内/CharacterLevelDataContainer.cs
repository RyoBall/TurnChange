using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public struct CharacterLevelData
{
    public int maxHP;
    public int attack;
    public int defense;
    public float critRate;
    public float critDamage;
    public int speed;
    public int K;
    public CharacterLevelData(int maxHP, int attack, int defense, float critRate, float critDamage, int speed,int K)
    {
        this.maxHP = maxHP;
        this.attack = attack;
        this.defense = defense;
        this.critRate = critRate;
        this.critDamage = critDamage;
        this.speed = speed;
        this.K = K;
    }
}
[System.Serializable]
public struct EnemyLevelData
{
    public int maxHP;
    public int attack;
    public int defense;
    public int speed;
    public float K;
    public EnemyLevelData(int maxHP, int attack, int defense, int speed,float K)
    {
        this.maxHP = maxHP;
        this.attack = attack;
        this.defense = defense;
        this.speed = speed;
        this.K = K;
    }
}
public static class LevelDataContainer
{
    public static CharacterLevelDataContainer Asset => CharacterLevelDataContainer.LoadAsset();

    public static Dictionary<string, Dictionary<int, CharacterLevelData>> CharacterLevelData =>
        Asset != null ? Asset.CharacterLevelLookup : null;

    public static Dictionary<string, Dictionary<int, EnemyLevelData>> EnemyLevelData =>
        Asset != null ? Asset.EnemyLevelLookup : null;

    public static bool TryGetCharacterLevelData(string characterId, int level, out CharacterLevelData levelData)
    {
        if (Asset == null)
        {
            levelData = default;
            return false;
        }

        return Asset.TryGetCharacterLevelData(characterId, level, out levelData);
    }

    public static bool TryGetEnemyLevelData(string enemyId, int level, out EnemyLevelData levelData)
    {
        if (Asset == null)
        {
            levelData = default;
            return false;
        }

        return Asset.TryGetEnemyLevelData(enemyId, level, out levelData);
    }
}

[System.Serializable]
public class CharacterLevelDataEntryList
{
    public string descrip;
    public string characterId;
    public List<CharacterLevelDataEntry> levelEntries = new List<CharacterLevelDataEntry>();
}

[System.Serializable]
public class EnemyLevelDataEntryList
{
    public string descrip;
    public string enemyId;
    public List<EnemyLevelDataEntry> levelEntries = new List<EnemyLevelDataEntry>();
}
[System.Serializable]
public class CharacterLevelDataEntry
{
    public string descrip;
    public int level;
    public CharacterLevelData data;
}

[System.Serializable]
public class EnemyLevelDataEntry
{
    public string descrip;
    public int level;
    public EnemyLevelData data;
}

[CreateAssetMenu(fileName = "LevelDataContainer", menuName = "Config/LevelDataContainer")]
public class CharacterLevelDataContainer : ScriptableObject
{
    public const string AssetFileName = "LevelDataContainer";
    public const string AssetFolderPath = "Assets/Resources/配置可编程物体/等级数据";
    public const string AssetPath = AssetFolderPath + "/" + AssetFileName + ".asset";
    public const string ResourcePath = "配置可编程物体/等级数据/" + AssetFileName;

    [Header("Inspector可视化数据")]
    [SerializeField] private List<CharacterLevelDataEntryList> characterLevelEntriesList = new List<CharacterLevelDataEntryList>();
    [SerializeField] private List<EnemyLevelDataEntryList> enemyLevelEntriesList = new List<EnemyLevelDataEntryList>();

    private Dictionary<string, Dictionary<int, CharacterLevelData>> characterLevelLookup;
    private Dictionary<string, Dictionary<int, EnemyLevelData>> enemyLevelLookup;

    public Dictionary<string, Dictionary<int, CharacterLevelData>> CharacterLevelLookup
    {
        get
        {
            EnsureLookupsBuilt();
            return characterLevelLookup;
        }
    }

    public Dictionary<string, Dictionary<int, EnemyLevelData>> EnemyLevelLookup
    {
        get
        {
            EnsureLookupsBuilt();
            return enemyLevelLookup;
        }
    }

    private void OnEnable()
    {
        EnsureLookupsBuilt();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureLookupsBuilt();
    }
#endif

    public static CharacterLevelDataContainer LoadAsset()
    {
        return Resources.Load<CharacterLevelDataContainer>(ResourcePath);
    }

    [ContextMenu("从序列化列表重建查询字典")]
    public void RebuildLookupDictionaries()
    {
        characterLevelLookup = BuildCharacterLookup(characterLevelEntriesList);
        enemyLevelLookup = BuildEnemyLookup(enemyLevelEntriesList);
    }

    public void ImportCharacterData(Dictionary<string, Dictionary<int, CharacterLevelData>> importedData)
    {
        characterLevelEntriesList = CreateCharacterEntryLists(importedData);
        RebuildLookupDictionaries();
    }

    public void ImportEnemyData(Dictionary<string, Dictionary<int, EnemyLevelData>> importedData)
    {
        enemyLevelEntriesList = CreateEnemyEntryLists(importedData);
        RebuildLookupDictionaries();
    }

    public bool TryGetCharacterLevelData(string characterId, int level, out CharacterLevelData levelData)
    {
        EnsureLookupsBuilt();
        levelData = default;
        if (string.IsNullOrWhiteSpace(characterId) || characterLevelLookup == null)
        {
            return false;
        }

        if (!characterLevelLookup.TryGetValue(characterId, out Dictionary<int, CharacterLevelData> levelDataByLevel))
        {
            return false;
        }

        return levelDataByLevel.TryGetValue(level, out levelData);
    }

    public bool TryGetEnemyLevelData(string enemyId, int level, out EnemyLevelData levelData)
    {
        EnsureLookupsBuilt();
        levelData = default;
        if (string.IsNullOrWhiteSpace(enemyId) || enemyLevelLookup == null)
        {
            return false;
        }

        if (!enemyLevelLookup.TryGetValue(enemyId, out Dictionary<int, EnemyLevelData> levelDataByLevel))
        {
            return false;
        }

        return levelDataByLevel.TryGetValue(level, out levelData);
    }

    private void EnsureLookupsBuilt()
    {
        if (characterLevelLookup == null || enemyLevelLookup == null)
        {
            RebuildLookupDictionaries();
        }
    }

    private static Dictionary<string, Dictionary<int, CharacterLevelData>> BuildCharacterLookup(List<CharacterLevelDataEntryList> entryLists)
    {
        var lookup = new Dictionary<string, Dictionary<int, CharacterLevelData>>();
        if (entryLists == null)
        {
            return lookup;
        }

        for (int i = 0; i < entryLists.Count; i++)
        {
            CharacterLevelDataEntryList entryList = entryLists[i];
            if (entryList == null || string.IsNullOrWhiteSpace(entryList.characterId))
            {
                continue;
            }

            var levelLookup = new Dictionary<int, CharacterLevelData>();
            if (entryList.levelEntries != null)
            {
                for (int j = 0; j < entryList.levelEntries.Count; j++)
                {
                    CharacterLevelDataEntry levelEntry = entryList.levelEntries[j];
                    if (levelEntry == null)
                    {
                        continue;
                    }

                    levelLookup[levelEntry.level] = levelEntry.data;
                }
            }

            lookup[entryList.characterId] = levelLookup;
        }

        return lookup;
    }

    private static Dictionary<string, Dictionary<int, EnemyLevelData>> BuildEnemyLookup(List<EnemyLevelDataEntryList> entryLists)
    {
        var lookup = new Dictionary<string, Dictionary<int, EnemyLevelData>>();
        if (entryLists == null)
        {
            return lookup;
        }

        for (int i = 0; i < entryLists.Count; i++)
        {
            EnemyLevelDataEntryList entryList = entryLists[i];
            if (entryList == null || string.IsNullOrWhiteSpace(entryList.enemyId))
            {
                continue;
            }

            var levelLookup = new Dictionary<int, EnemyLevelData>();
            if (entryList.levelEntries != null)
            {
                for (int j = 0; j < entryList.levelEntries.Count; j++)
                {
                    EnemyLevelDataEntry levelEntry = entryList.levelEntries[j];
                    if (levelEntry == null)
                    {
                        continue;
                    }

                    levelLookup[levelEntry.level] = levelEntry.data;
                }
            }

            lookup[entryList.enemyId] = levelLookup;
        }

        return lookup;
    }

    private static List<CharacterLevelDataEntryList> CreateCharacterEntryLists(Dictionary<string, Dictionary<int, CharacterLevelData>> importedData)
    {
        var entryLists = new List<CharacterLevelDataEntryList>();
        if (importedData == null)
        {
            return entryLists;
        }

        var characterIds = new List<string>(importedData.Keys);
        characterIds.Sort(StringComparer.Ordinal);
        for (int i = 0; i < characterIds.Count; i++)
        {
            string characterId = characterIds[i];
            if (!importedData.TryGetValue(characterId, out Dictionary<int, CharacterLevelData> levelDataByLevel) || levelDataByLevel == null)
            {
                continue;
            }

            var levelEntries = new List<CharacterLevelDataEntry>();
            var levels = new List<int>(levelDataByLevel.Keys);
            levels.Sort();
            for (int j = 0; j < levels.Count; j++)
            {
                int level = levels[j];
                levelEntries.Add(new CharacterLevelDataEntry
                {
                    descrip = $"{characterId} Level {level}",
                    level = level,
                    data = levelDataByLevel[level]
                });
            }

            entryLists.Add(new CharacterLevelDataEntryList
            {
                descrip = characterId,
                characterId = characterId,
                levelEntries = levelEntries
            });
        }

        return entryLists;
    }

    private static List<EnemyLevelDataEntryList> CreateEnemyEntryLists(Dictionary<string, Dictionary<int, EnemyLevelData>> importedData)
    {
        var entryLists = new List<EnemyLevelDataEntryList>();
        if (importedData == null)
        {
            return entryLists;
        }

        var enemyIds = new List<string>(importedData.Keys);
        enemyIds.Sort(StringComparer.Ordinal);
        for (int i = 0; i < enemyIds.Count; i++)
        {
            string enemyId = enemyIds[i];
            if (!importedData.TryGetValue(enemyId, out Dictionary<int, EnemyLevelData> levelDataByLevel) || levelDataByLevel == null)
            {
                continue;
            }

            var levelEntries = new List<EnemyLevelDataEntry>();
            var levels = new List<int>(levelDataByLevel.Keys);
            levels.Sort();
            for (int j = 0; j < levels.Count; j++)
            {
                int level = levels[j];
                levelEntries.Add(new EnemyLevelDataEntry
                {
                    descrip = $"{enemyId} Level {level}",
                    level = level,
                    data = levelDataByLevel[level]
                });
            }

            entryLists.Add(new EnemyLevelDataEntryList
            {
                descrip = enemyId,
                enemyId = enemyId,
                levelEntries = levelEntries
            });
        }

        return entryLists;
    }

#if UNITY_EDITOR
    public static CharacterLevelDataContainer GetOrCreateAsset()
    {
        CharacterLevelDataContainer asset = AssetDatabase.LoadAssetAtPath<CharacterLevelDataContainer>(AssetPath);
        if (asset != null)
        {
            return asset;
        }

        EnsureFolderExists(AssetFolderPath);
        asset = CreateInstance<CharacterLevelDataContainer>();
        asset.name = AssetFileName;
        AssetDatabase.CreateAsset(asset, AssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return asset;
    }

    private static void EnsureFolderExists(string assetFolderPath)
    {
        string normalizedPath = assetFolderPath.Replace("\\", "/").TrimEnd('/');
        if (AssetDatabase.IsValidFolder(normalizedPath))
        {
            return;
        }

        string[] segments = normalizedPath.Split('/');
        if (segments.Length <= 1)
        {
            return;
        }

        string currentPath = segments[0];
        for (int i = 1; i < segments.Length; i++)
        {
            string nextPath = $"{currentPath}/{segments[i]}";
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, segments[i]);
            }

            currentPath = nextPath;
        }
    }
#endif
}
