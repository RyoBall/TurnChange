using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
public class LevelDataContainer
{
    public static Dictionary<string, Dictionary<int, CharacterLevelData>> CharacterLevelData;
    public static Dictionary<string, Dictionary<int, EnemyLevelData>> EnemyLevelData;
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

public class CharacterLevelDataContainer : MonoBehaviour
{
    [Header("Inspector可视化数据")]
    [SerializeField] private List<CharacterLevelDataEntryList> characterLevelEntriesList = new List<CharacterLevelDataEntryList>();
    [SerializeField] private List<EnemyLevelDataEntryList> enemyLevelEntriesList = new List<EnemyLevelDataEntryList>();

    private void Awake()
    {
        SyncFromRuntimeDictionaries();
    }

    /*[ContextMenu("从Inspector列表重建运行时字典")]
    public void ApplyToRuntimeDictionaries()
    {
        var characterDict = new Dictionary<string, Dictionary<int, CharacterLevelData>>();
        for (int i = 0; i < characterLevelEntries.Count; i++)
        {
            var entry = characterLevelEntries[i];
            if (string.IsNullOrEmpty(entry.characterId))
            {
                continue;
            }

            if (!characterDict.ContainsKey(entry.characterId))
            {
                characterDict[entry.characterId] = new Dictionary<int, CharacterLevelData>();
            }

            characterDict[entry.characterId][entry.level] = entry.data;
        }

        var enemyDict = new Dictionary<string, Dictionary<int, EnemyLevelData>>();
        for (int i = 0; i < enemyLevelEntries.Count; i++)
        {
            var entry = enemyLevelEntries[i];
            if (string.IsNullOrEmpty(entry.enemyId))
            {
                continue;
            }

            if (!enemyDict.ContainsKey(entry.enemyId))
            {
                enemyDict[entry.enemyId] = new Dictionary<int, EnemyLevelData>();
            }

            enemyDict[entry.enemyId][entry.level] = entry.data;
        }

        LevelDataContainer.CharacterLevelData = characterDict;
        LevelDataContainer.EnemyLevelData = enemyDict;
    }*/

    [ContextMenu("从运行时字典刷新Inspector列表")]
    public void SyncFromRuntimeDictionaries()
    {
        characterLevelEntriesList.Clear();
        if (LevelDataContainer.CharacterLevelData != null)
        {
            foreach (var characterPair in LevelDataContainer.CharacterLevelData)
            {
                var characterLevelEntries = new CharacterLevelDataEntryList
                {
                    descrip = characterPair.Key,
                    characterId = characterPair.Key,
                    levelEntries = new List<CharacterLevelDataEntry>()
                };

                foreach (var levelPair in characterPair.Value)
                {
                    characterLevelEntries.levelEntries.Add(new CharacterLevelDataEntry
                    {
                        descrip = $"{characterPair.Key} Level {levelPair.Key}",
                        level = levelPair.Key,
                        data = levelPair.Value
                    });
                }

                characterLevelEntriesList.Add(characterLevelEntries);
            }
        }

        enemyLevelEntriesList.Clear();
        if (LevelDataContainer.EnemyLevelData != null)
        {
            foreach (var enemyPair in LevelDataContainer.EnemyLevelData)
            {
                var enemyLevelEntries = new EnemyLevelDataEntryList
                {
                    descrip = enemyPair.Key,
                    enemyId = enemyPair.Key,
                    levelEntries = new List<EnemyLevelDataEntry>()
                };

                foreach (var levelPair in enemyPair.Value)
                {
                    enemyLevelEntries.levelEntries.Add(new EnemyLevelDataEntry
                    {
                        descrip = $"{enemyPair.Key} Level {levelPair.Key}",
                        level = levelPair.Key,
                        data = levelPair.Value
                    });
                }

                enemyLevelEntriesList.Add(enemyLevelEntries);
            }
        }
    }
}
