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
    [Min(0)] public int rewardExperience;
    [Min(0)] public int rewardGold;

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
}