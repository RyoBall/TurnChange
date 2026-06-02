using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BattleEnemySpawnData
{
    public EnemyRosterData enemyData;
    public int level = 1;
    public bool isChessSeriesEnemy;
}

[Serializable]
public class BattleEnemyWaveData
{
    public List<BattleEnemySpawnData> enemies = new List<BattleEnemySpawnData>();
}

[Serializable]
public class PendingBattleLevelData
{
    public string levelId;
    public string levelName;
    public int rewardExperience;
    public int rewardGold;
    public List<BattleEnemyWaveData> enemyWaves = new List<BattleEnemyWaveData>();
    public List<CharacterRosterData> selectedFieldCharacters = new List<CharacterRosterData>();

    public int WaveCount => enemyWaves != null ? enemyWaves.Count : 0;

    public IReadOnlyList<BattleEnemySpawnData> GetWaveEnemies(int waveIndex)
    {
        if (enemyWaves == null || waveIndex < 0 || waveIndex >= enemyWaves.Count)
        {
            return Array.Empty<BattleEnemySpawnData>();
        }

        BattleEnemyWaveData waveData = enemyWaves[waveIndex];
        if (waveData == null || waveData.enemies == null)
        {
            return Array.Empty<BattleEnemySpawnData>();
        }

        return waveData.enemies;
    }
}

public static class BattleLaunchContext
{
    private static PendingBattleLevelData s_pendingLevelData;

    public static bool HasPendingLevelData => s_pendingLevelData != null;

    public static void SetPendingLevelData(LevelSelectionData source, IReadOnlyList<CharacterRosterData> selectedFieldCharacters = null)
    {
        if (source == null)
        {
            s_pendingLevelData = null;
            return;
        }

        var pendingData = new PendingBattleLevelData
        {
            levelId = source.levelId,
            levelName = source.levelName,
            rewardExperience = Mathf.Max(0, source.rewardExperience),
            rewardGold = Mathf.Max(0, source.rewardGold),
            enemyWaves = new List<BattleEnemyWaveData>()
        };

        IReadOnlyList<LevelEnemyWaveData> sourceWaves = source.GetEnemyWaves();
        if (sourceWaves != null)
        {
            for (int i = 0; i < sourceWaves.Count; i++)
            {
                LevelEnemyWaveData sourceWave = sourceWaves[i];
                if (sourceWave == null)
                {
                    continue;
                }

                var battleWave = new BattleEnemyWaveData
                {
                    enemies = new List<BattleEnemySpawnData>()
                };

                if (sourceWave.enemies != null)
                {
                    for (int j = 0; j < sourceWave.enemies.Count; j++)
                    {
                        LevelEnemyEntry entry = sourceWave.enemies[j];
                        if (entry == null || entry.enemyData == null)
                        {
                            continue;
                        }

                        battleWave.enemies.Add(new BattleEnemySpawnData
                        {
                            enemyData = entry.enemyData,
                            level = Mathf.Max(1, entry.level),
                        });
                    }
                }

                if (battleWave.enemies.Count > 0)
                {
                    pendingData.enemyWaves.Add(battleWave);
                }
            }
        }

        if (selectedFieldCharacters != null)
        {
            for (int i = 0; i < selectedFieldCharacters.Count; i++)
            {
                CharacterRosterData rosterData = selectedFieldCharacters[i];
                if (rosterData != null)
                {
                    pendingData.selectedFieldCharacters.Add(rosterData);
                }
            }
        }

        s_pendingLevelData = pendingData;
    }

    public static PendingBattleLevelData ConsumePendingLevelData()
    {
        PendingBattleLevelData pendingData = s_pendingLevelData;
        s_pendingLevelData = null;
        return pendingData;
    }

    public static void ClearPendingLevelData()
    {
        s_pendingLevelData = null;
    }
}