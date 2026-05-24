using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BattleEnemySpawnData
{
    public EnemyRosterData enemyData;
    public int level = 1;
    public ChessBossPendingData chessBossData;
}

[Serializable]
public class PendingBattleLevelData
{
    public string levelId;
    public string levelName;
    public List<BattleEnemySpawnData> enemies = new List<BattleEnemySpawnData>();
    public List<CharacterRosterData> selectedFieldCharacters = new List<CharacterRosterData>();
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
            enemies = new List<BattleEnemySpawnData>()
        };

        if (source.enemies != null)
        {
            for (int i = 0; i < source.enemies.Count; i++)
            {
                LevelEnemyEntry entry = source.enemies[i];
                if (entry == null || entry.enemyData == null)
                {
                    continue;
                }

                pendingData.enemies.Add(new BattleEnemySpawnData
                {
                    enemyData = entry.enemyData,
                    level = Mathf.Max(1, entry.level),
                    chessBossData = entry.isChessSeriesEnemy && entry.chessBossData != null
                        ? CreateChessBossPendingData(entry.chessBossData)
                        : null
                });
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

    private static ChessBossPendingData CreateChessBossPendingData(ChessBossPendingData source)
    {
        if (source == null)
        {
            return null;
        }

        ChessBossPendingData clonedData = source.Clone();
        clonedData.enabled = true;
        return clonedData;
    }
}