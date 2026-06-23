using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战斗对话控制器 — 响应 BattleDialogEvents 并决定弹出哪些对话
/// 挂载到 Fight 场景的 Canvas 下
/// </summary>
public class BattleDialogController : MonoBehaviour
{
    public static BattleDialogController Instance { get; private set; }

    [Header("对话时间参数")]
    [Tooltip("普通对话持续时间（秒）")]
    [SerializeField] private float defaultDialogDuration = 2.5f;
    [Tooltip("多条对话之间的间隔（秒）")]
    [SerializeField] private float dialogInterval = 0.4f;
    [Tooltip("重要对话持续时间（秒）")]
    [SerializeField] private float importantDialogDuration = 3.5f;

    [Header("对话外观")]
    [Tooltip("战斗提示对话字体大小")]
    [SerializeField] private int battleHintFontSize = 32;

    [Header("低血量检测")]
    [Tooltip("低血量阈值（比例）")]
    [SerializeField, Range(0f, 1f)] private float lowHealthThreshold = 0.4f;

    // 已触发的一次性事件追踪
    private readonly HashSet<BattleDialogEventType> m_oncePerBattleEvents = new HashSet<BattleDialogEventType>();
    private readonly HashSet<string> m_characterLowHealthTriggered = new HashSet<string>();
    private readonly HashSet<string> m_characterChaosMaxTriggered = new HashSet<string>();
    private readonly HashSet<string> m_dragonDeathTriggered = new HashSet<string>();
    private readonly List<BattleDialogEventData> m_pendingDialogEvents = new List<BattleDialogEventData>();
    private bool m_battleHintsAllowed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        BattleDialogEvents.DialogEvent += OnDialogEvent;
        TurnManager.BattleStarted += OnBattleStarted;
    }

    private void OnDisable()
    {
        BattleDialogEvents.DialogEvent -= OnDialogEvent;
        TurnManager.BattleStarted -= OnBattleStarted;
    }

    private void OnBattleStarted()
    {
        ResetForNewBattle();
    }

    /// <summary>允许战斗提示对话显示，并处理战前暂存的事件（在角色入场技前调用）</summary>
    public void EnableBattleHints()
    {
        if (m_battleHintsAllowed)
        {
            return;
        }

        m_battleHintsAllowed = true;
        StartCoroutine(ProcessPendingDialogEvents());
    }

    private IEnumerator ProcessPendingDialogEvents()
    {
        for (int i = 0; i < m_pendingDialogEvents.Count; i++)
        {
            ProcessDialogEvent(m_pendingDialogEvents[i]);
            if (i < m_pendingDialogEvents.Count - 1)
            {
                yield return new WaitForSeconds(dialogInterval);
            }
        }

        m_pendingDialogEvents.Clear();
    }

    private void OnDialogEvent(BattleDialogEventData data)
    {
        if (data == null) return;

        if (!m_battleHintsAllowed)
        {
            m_pendingDialogEvents.Add(data);
            return;
        }

        ProcessDialogEvent(data);
    }

    private void ProcessDialogEvent(BattleDialogEventData data)
    {
        switch (data.EventType)
        {
            // ============ 通用 ============
            case BattleDialogEventType.CharacterSwapped:
                HandleCharacterSwapped(data);
                break;
            case BattleDialogEventType.CharacterHealthChanged:
                HandleCharacterHealthChanged(data);
                break;
            case BattleDialogEventType.CharacterChaosMaxReached:
                HandleCharacterChaosMaxReached(data);
                break;
            case BattleDialogEventType.CombatantTurnSkipped:
                HandleCombatantTurnSkipped(data);
                break;

            // ============ 西洋象棋 ============
            case BattleDialogEventType.ChessPawnsEnter:
                ShowOncePerBattle(data.EventType, "四枚兵棋整齐列阵，开始向前推进——");
                break;
            case BattleDialogEventType.ChessPawnsAboutToPromote:
                ShowCenterDialog("残存的兵棋将在下一回合后升变！", importantDialogDuration);
                break;
            case BattleDialogEventType.ChessQueenMarkingKing:
                ShowCenterDialog("皇后正在锁定目标……速度最低者将被标记为王棋！", importantDialogDuration);
                break;
            case BattleDialogEventType.ChessKingMarked:
                HandleChessKingMarked(data);
                break;
            case BattleDialogEventType.ChessRookMarked:
                HandleChessRookMarked(data);
                break;
            case BattleDialogEventType.ChessQueenCharging:
                ShowCenterDialog("皇后蓄力中，下一回合将对王棋发动致命一击！", importantDialogDuration);
                break;
            case BattleDialogEventType.ChessQueenExhausted:
                ShowCenterDialog("皇后释放重击后陷入力竭——趁现在全力输出！", importantDialogDuration);
                break;
            case BattleDialogEventType.ChessQueenCoronationImminent:
                ShowCenterDialog("皇后濒临绝境，正在凝聚全部威望——周身散发出不详的气息……", importantDialogDuration);
                break;
            case BattleDialogEventType.ChessQueenPrestigeDepleted:
                ShowCenterDialog("皇后已耗尽所有威望层数，强度极大被削弱！", importantDialogDuration);
                break;
            case BattleDialogEventType.ChessQueenThroneMissed:
                ShowCenterDialog("由于王棋丢失，皇后的攻击落空了", importantDialogDuration);
                break;

            // ============ 三头龙 ============
            case BattleDialogEventType.DragonEnter:
                ShowOncePerBattle(data.EventType, "三颗龙首彼此相连，似乎共享着力量，逐一击破或许并非上策……");
                break;
            case BattleDialogEventType.DragonFirstDeath:
                ShowCenterDialog("倒下的龙首化为黑雾，被残存的头颅吸入", defaultDialogDuration);
                break;
            case BattleDialogEventType.DragonSkillReinforced:
                ShowCenterDialog("剩余龙首的技能已强化！", defaultDialogDuration);
                break;
            case BattleDialogEventType.DragonSecondDeath:
                ShowCenterDialog("最后一颗头颅吞噬了所有残骸——进入暴怒状态！", importantDialogDuration);
                break;
            case BattleDialogEventType.DragonLastStand:
                ShowCenterDialog("独龙的力量大幅增强……小心应对", importantDialogDuration);
                break;
            case BattleDialogEventType.DragonDotUltimate:
                ShowCenterDialog("漆黑的火焰席卷全场——全体受到【不灭之焰】！", importantDialogDuration);
                break;
            case BattleDialogEventType.DragonDotPurify:
                ShowCenterDialog("三头龙的负面状态被清除了！", defaultDialogDuration);
                break;
            case BattleDialogEventType.DragonInstantDeathWarning:
                ShowCenterDialog("死亡宣告落下……龙首将降下【即死】！", importantDialogDuration);
                break;
            case BattleDialogEventType.DragonInstantDeathTriggered:
                ShowCenterDialog("即死效果触发！", defaultDialogDuration);
                break;
            case BattleDialogEventType.DragonChaosUltimate:
                ShowCenterDialog("混沌吐息侵蚀全场——全体混沌值上升，陷入混沌的角色将承受重击。", importantDialogDuration);
                break;
            case BattleDialogEventType.DragonChaosLeap:
                ShowCenterDialog("三头龙的行动提前了！", defaultDialogDuration);
                break;

            // ============ 西洋剑剑客 ============
            case BattleDialogEventType.SwordsmanEnter:
                ShowOncePerBattle(data.EventType, "剑客摆出优雅的架势，步法轻灵，剑招已至眼前");
                break;
            case BattleDialogEventType.SwordsmanStanceBright:
                ShowCenterDialog("剑客踏前一步，摆出进攻架势——攻击性增强，但破绽也更大", defaultDialogDuration);
                break;
            case BattleDialogEventType.SwordsmanStanceDefense:
                ShowCenterDialog("剑客横剑护身，转入守势——攻击欲望降低，难以撼动", defaultDialogDuration);
                break;
            case BattleDialogEventType.SwordsmanStanceGuerrilla:
                ShowCenterDialog("剑客步伐轻盈，身形飘忽——行动捉摸不透", defaultDialogDuration);
                break;
            case BattleDialogEventType.SwordsmanStaggerEnter:
                ShowCenterDialog("剑客的架势崩解失衡！抓住机会全力攻击！", importantDialogDuration);
                break;
            case BattleDialogEventType.SwordsmanStaggerExit:
                ShowCenterDialog("剑客重整姿态，摆出防御架势——韧性条已恢复", defaultDialogDuration);
                break;
            case BattleDialogEventType.SwordsmanPhaseTwo:
                ShowCenterDialog("剑客的呼吸变得急促，但剑招反而更加凌厉……姿态切换加快！", importantDialogDuration);
                break;
            case BattleDialogEventType.SwordsmanPhaseThree:
                ShowCenterDialog("剑客发出低吼，抛开一切防御姿态——不再切换姿态，决定背水一战！", importantDialogDuration);
                break;
            case BattleDialogEventType.SwordsmanEleganceReminder:
                ShowCenterDialog("剑客身姿优雅，未失衡时大幅减免所受伤害", defaultDialogDuration);
                break;
        }
    }

    #region 通用事件处理

    private void HandleCharacterSwapped(BattleDialogEventData data)
    {
        if (data.RelatedCharacter == null) return;

        // 格式: "（角色B）切出，混沌值已清除。（角色A）切入，切入技发动！"
        // data.RelatedCharacter 是切入的角色，data.ExtraText 是切出的角色名
        string oldName = data.ExtraText ?? "未知角色";
        string newName = data.RelatedCharacter.combatantName;
        ShowCenterDialog($"{oldName}切出，混沌值已清除。{newName}切入，切入技发动！", importantDialogDuration);
    }

    private void HandleCharacterHealthChanged(BattleDialogEventData data)
    {
        if (data.RelatedCharacter == null) return;

        float hpRatio = data.ExtraFloat;
        if (hpRatio <= 0f || hpRatio > lowHealthThreshold) return;

        string charId = data.RelatedCharacter.characterID;
        if (m_characterLowHealthTriggered.Contains(charId)) return;

        m_characterLowHealthTriggered.Add(charId);
        ShowCenterDialog($"{data.RelatedCharacter.combatantName}生命垂危！", importantDialogDuration);
    }

    private void HandleCombatantTurnSkipped(BattleDialogEventData data)
    {
        string combatantName = ResolveCombatantName(data);
        ShowCenterDialog($"{combatantName}动弹不得!", defaultDialogDuration);
    }

    private void HandleCharacterChaosMaxReached(BattleDialogEventData data)
    {
        if (data.RelatedCharacter == null) return;

        // 混沌值每次达到5点都可以触发（不需要去重），但加一个短冷却避免刷屏
        string charId = data.RelatedCharacter.characterID;
        if (m_characterChaosMaxTriggered.Contains(charId)) return;

        m_characterChaosMaxTriggered.Add(charId);
        ShowCenterDialog($"{data.RelatedCharacter.combatantName}陷入混沌！下回合将无法行动并受到大量伤害", importantDialogDuration);

        // 延迟清除标记，允许下次混沌值再满时重新触发
        StartCoroutine(ClearChaosMaxFlagAfterDelay(charId, 3f));
    }

    private IEnumerator ClearChaosMaxFlagAfterDelay(string charId, float delay)
    {
        yield return new WaitForSeconds(delay);
        m_characterChaosMaxTriggered.Remove(charId);
    }

    private void HandleChessKingMarked(BattleDialogEventData data)
    {
        string charName = data.RelatedCharacter != null ? data.RelatedCharacter.combatantName : "未知角色";
        ShowCenterDialog($"{charName}被标记为\"王棋\"——即将承受巨额伤害！", importantDialogDuration);
    }

    private void HandleChessRookMarked(BattleDialogEventData data)
    {
        string charName = data.RelatedCharacter != null ? data.RelatedCharacter.combatantName : "未知角色";
        ShowCenterDialog($"{charName}被标记为\"车棋\"", defaultDialogDuration);
    }

    #endregion

    #region 辅助方法

    /// <summary>每场战斗只显示一次的对话</summary>
    private void ShowOncePerBattle(BattleDialogEventType eventType, string message)
    {
        if (m_oncePerBattleEvents.Contains(eventType)) return;
        m_oncePerBattleEvents.Add(eventType);
        ShowCenterDialog(message, importantDialogDuration);
    }

    private static string ResolveCombatantName(BattleDialogEventData data)
    {
        if (data.RelatedCharacter != null)
        {
            return data.RelatedCharacter.combatantName;
        }

        if (data.RelatedEnemy != null)
        {
            return data.RelatedEnemy.combatantName;
        }

        return "未知单位";
    }

    /// <summary>在屏幕中央显示对话</summary>
    private void ShowCenterDialog(string message, float duration)
    {
        if (FloatingTipGenerator.Instance != null)
        {
            FloatingTipGenerator.Instance.ShowCenterDialog(message, duration, battleHintFontSize);
        }
    }

    /// <summary>重置所有一次性事件追踪（新战斗开始时调用）</summary>
    public void ResetForNewBattle()
    {
        m_battleHintsAllowed = false;
        m_oncePerBattleEvents.Clear();
        m_characterLowHealthTriggered.Clear();
        m_characterChaosMaxTriggered.Clear();
        m_dragonDeathTriggered.Clear();
    }

    #endregion
}
