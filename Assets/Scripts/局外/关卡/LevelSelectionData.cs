using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelSelectionData", menuName = "关卡数据/Level Selection Data")]
public class LevelSelectionData : ScriptableObject//关卡数据
{
    public string levelId;
    public string levelName;
    public bool isUnlocked;
    public LevelSelectionButtonType buttonType;
    public LevelEventData eventData;
    public List<LevelEnemyWaveData> enemyWaves = new List<LevelEnemyWaveData>();
    [Min(1)] public int playerLevel = 1;
    [Min(0)] public int rewardExperience;
    [Min(0)] public int rewardGold;

    [Header("BGM")]
    [Tooltip("本关卡专属BGM（为空则从通用战斗BGM中随机选取）")]
    public AudioClip levelBgmClip;

    [Header("剧情对话")]
    [Tooltip("战前对话（在切入技之前播放）")]
    public List<BattleStoryDialogData> preBattleDialogs = new List<BattleStoryDialogData>();
    [Tooltip("战后对话（仅在胜利时播放，在结算界面之前）")]
    public List<BattleStoryDialogData> postBattleDialogs = new List<BattleStoryDialogData>();

    /// <summary>教程关（levelId 以 0- 开头，如 0-1、0-2、0-3）</summary>
    public bool IsTutorialLevel => IsTutorialLevelId(levelId);

    public static bool IsTutorialLevelId(string levelId)
    {
        return !string.IsNullOrEmpty(levelId) && levelId.StartsWith("0-");
    }

    public IReadOnlyList<LevelEnemyWaveData> GetEnemyWaves()
    {
        return enemyWaves != null ? enemyWaves : Array.Empty<LevelEnemyWaveData>();
    }

    public IReadOnlyList<LevelEnemyEntry> GetWaveEnemies(int waveIndex)
    {
        if (enemyWaves == null || waveIndex < 0 || waveIndex >= enemyWaves.Count)
        {
            return Array.Empty<LevelEnemyEntry>();
        }

        LevelEnemyWaveData waveData = enemyWaves[waveIndex];
        if (waveData == null || waveData.enemies == null)
        {
            return Array.Empty<LevelEnemyEntry>();
        }

        return waveData.enemies;
    }

    /// <summary>
    /// 汇总所有波次中不同等级和种类的敌人，相同敌人（同enemyID+同等级）只保留一条
    /// </summary>
    public List<LevelEnemyEntry> GetDistinctEnemies()
    {
        List<LevelEnemyEntry> result = new List<LevelEnemyEntry>();
        if (enemyWaves == null) return result;

        foreach (LevelEnemyWaveData wave in enemyWaves)
        {
            if (wave == null || wave.enemies == null) continue;

            foreach (LevelEnemyEntry entry in wave.enemies)
            {
                if (entry == null || entry.enemyData == null) continue;

                // 检查是否已存在相同enemyID且相同等级的敌人
                bool alreadyExists = false;
                foreach (LevelEnemyEntry existing in result)
                {
                    if (existing != null && existing.enemyData != null &&
                        existing.enemyData.enemyID == entry.enemyData.enemyID &&
                        existing.level == entry.level)
                    {
                        alreadyExists = true;
                        break;
                    }
                }

                if (!alreadyExists)
                {
                    result.Add(entry);
                }
            }
        }

        return result;
    }
}