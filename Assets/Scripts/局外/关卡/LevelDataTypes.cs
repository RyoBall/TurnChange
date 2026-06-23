using System;
using System.Collections.Generic;
using UnityEngine;

public enum LevelSelectionButtonType
{
    BattleLevel,
    EventLevel,
    NextFloor,
    CreditsLevel
}

public enum LevelEventOptionType
{
    None,
    WorshipSpeedGod,
    WorshipPowerGod,
    TakeAllIncenseMoney,
    SwapForProfit,
    CashOutSwap,
    TakeWindingPath,
    TakeBroadRoad
}

[Serializable]
public class LevelEnemyEntry//关卡数据单元
{
    public EnemyRosterData enemyData;
    public int level = 1;
}

[Serializable]
public class LevelEnemyWaveData//关卡敌人波次
{
    public List<LevelEnemyEntry> enemies = new List<LevelEnemyEntry>();
}

[Serializable]
public class LevelSelectionFloorData
{
    public List<LevelSelectionData> levels = new List<LevelSelectionData>();

    public IReadOnlyList<LevelSelectionData> GetLevels()
    {
        return levels != null ? levels : Array.Empty<LevelSelectionData>();
    }
}