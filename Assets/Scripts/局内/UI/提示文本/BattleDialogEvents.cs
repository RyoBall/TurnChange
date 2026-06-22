using System;
using UnityEngine;

/// <summary>
/// 战斗对话事件类型枚举
/// </summary>
public enum BattleDialogEventType
{
    // ============ 通用 ============
    /// <summary>角色换人完成（切出角色, 切入角色）</summary>
    CharacterSwapped,

    /// <summary>角色血量发生变化（ExtraFloat 携带当前血量百分比 0~1）</summary>
    CharacterHealthChanged,

    /// <summary>角色混沌值达到5点</summary>
    CharacterChaosMaxReached,

    /// <summary>角色或敌人因震慑、混沌等原因跳过回合</summary>
    CombatantTurnSkipped,

    // ============ 西洋象棋 Boss ============
    /// <summary>兵棋入场</summary>
    ChessPawnsEnter,

    /// <summary>兵棋即将升变（倒数第二次行动后）</summary>
    ChessPawnsAboutToPromote,

    /// <summary>皇后标记王棋（三后袭王座）</summary>
    ChessQueenMarkingKing,

    /// <summary>角色被标记为王棋</summary>
    ChessKingMarked,

    /// <summary>角色被标记为车棋</summary>
    ChessRookMarked,

    /// <summary>皇后蓄力警告</summary>
    ChessQueenCharging,

    /// <summary>皇后力竭（蓄力技释放后）</summary>
    ChessQueenExhausted,

    /// <summary>皇后王权加冕前兆（30%血量）</summary>
    ChessQueenCoronationImminent,

    /// <summary>皇后加冕后耗尽威望</summary>
    ChessQueenPrestigeDepleted,

    /// <summary>皇后后袭王座落空（王棋丢失）</summary>
    ChessQueenThroneMissed,

    // ============ 三头龙 Boss ============
    /// <summary>三头龙入场提示</summary>
    DragonEnter,

    /// <summary>一头龙被击杀（黑雾被吸收）</summary>
    DragonFirstDeath,

    /// <summary>剩余龙首技能强化</summary>
    DragonSkillReinforced,

    /// <summary>第二头龙死亡（进入暴怒）</summary>
    DragonSecondDeath,

    /// <summary>独龙力量增强</summary>
    DragonLastStand,

    /// <summary>Dot龙·无尽炼狱触发</summary>
    DragonDotUltimate,

    /// <summary>Dot龙·净化吞噬清除负面</summary>
    DragonDotPurify,

    /// <summary>直伤龙·即死前兆</summary>
    DragonInstantDeathWarning,

    /// <summary>直伤龙·即死触发</summary>
    DragonInstantDeathTriggered,

    /// <summary>混沌龙·震慑溃灭</summary>
    DragonChaosUltimate,

    /// <summary>混沌龙·混沌跃动行动提前</summary>
    DragonChaosLeap,

    // ============ 西洋剑剑客 Boss ============
    /// <summary>剑客入场</summary>
    SwordsmanEnter,

    /// <summary>切换至亮剑姿态</summary>
    SwordsmanStanceBright,

    /// <summary>切换至防御姿态</summary>
    SwordsmanStanceDefense,

    /// <summary>切换至游击姿态</summary>
    SwordsmanStanceGuerrilla,

    /// <summary>韧性击破·进入失衡</summary>
    SwordsmanStaggerEnter,

    /// <summary>失衡结束·韧性恢复</summary>
    SwordsmanStaggerExit,

    /// <summary>血量降至50%·阶段二</summary>
    SwordsmanPhaseTwo,

    /// <summary>血量降至25%·背水一战</summary>
    SwordsmanPhaseThree,

    /// <summary>优雅体态提醒</summary>
    SwordsmanEleganceReminder,
}

/// <summary>
/// 战斗对话事件数据
/// </summary>
public class BattleDialogEventData
{
    public BattleDialogEventType EventType;

    /// <summary>关联的角色（可为null）</summary>
    public Character RelatedCharacter;

    /// <summary>关联的敌人（可为null）</summary>
    public Enemy RelatedEnemy;

    /// <summary>关联的 Transform（用于世界坐标定位）</summary>
    public Transform RelatedTransform;

    /// <summary>额外文本参数（用于格式化）</summary>
    public string ExtraText;

    /// <summary>额外数值参数</summary>
    public int ExtraInt;

    /// <summary>额外浮点参数</summary>
    public float ExtraFloat;

    public static BattleDialogEventData Create(BattleDialogEventType eventType)
    {
        return new BattleDialogEventData { EventType = eventType };
    }

    public BattleDialogEventData WithCharacter(Character character)
    {
        RelatedCharacter = character;
        if (character != null) RelatedTransform = character.transform;
        return this;
    }

    public BattleDialogEventData WithEnemy(Enemy enemy)
    {
        RelatedEnemy = enemy;
        if (enemy != null) RelatedTransform = enemy.transform;
        return this;
    }

    public BattleDialogEventData WithTransform(Transform transform)
    {
        RelatedTransform = transform;
        return this;
    }

    public BattleDialogEventData WithExtraText(string text)
    {
        ExtraText = text;
        return this;
    }

    public BattleDialogEventData WithExtraInt(int value)
    {
        ExtraInt = value;
        return this;
    }

    public BattleDialogEventData WithExtraFloat(float value)
    {
        ExtraFloat = value;
        return this;
    }
}

/// <summary>
/// 战斗对话事件系统 — 静态事件定义
/// 各系统通过 Raise 方法触发事件，BattleDialogController 响应并弹出对话
/// </summary>
public static class BattleDialogEvents
{
    /// <summary>战斗对话事件</summary>
    public static event Action<BattleDialogEventData> DialogEvent;

    /// <summary>触发对话事件</summary>
    public static void Raise(BattleDialogEventData data)
    {
        DialogEvent?.Invoke(data);
    }

    /// <summary>快捷触发方法</summary>
    public static void Raise(BattleDialogEventType eventType, Character character = null, Enemy enemy = null, string extraText = null)
    {
        DialogEvent?.Invoke(new BattleDialogEventData
        {
            EventType = eventType,
            RelatedCharacter = character,
            RelatedEnemy = enemy,
            RelatedTransform = character != null ? character.transform : (enemy != null ? enemy.transform : null),
            ExtraText = extraText,
        });
    }
}
