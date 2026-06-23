using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System;
using UnityEngine;

public enum StateDurationType
{
    Turn,
    ActionValue,
    Special
}

public enum StateCombatEventType
{
    DamageSkillUsed,
    DotTriggered,
    CriticalHit,
    EnemyKilled
}

public enum StateType
{
    PursuitPunish,//追惩
    PunishMark,//惩戒标记
    PersistentTorment,//持续煎熬
    SeqFlame,//序焰
    Daze,//震慑
    Ice,//冰寒
    Corrosion,//腐蚀
    Wind,//风蚀
    ElementalDetonation,//元素爆发
    CounterCharge,//蓄势逆击
    Resist,//抵御
    Taunt,//嘲讽
    Charge,//蓄势
    Attract,//瞩目
    Poison,//毒
    Chaos,//混沌（整合状态，层数=混沌值）
    None,
    BerserkFeast,//狂暴盛宴
    BurningBlood,//燃血
    CritRhythm,//暴击律动
    DesperationMark,//裁决印记
    HealingSpring,//回复之泉
    RestorationSurge,//复苏锋芒
    DeadlyArmor,//致命穿甲
    BloodBath,//浴血
    Vulnerable,//易伤
    ArmorBreak,//破甲
    BloodSurgeHeal,//浴血反哺
    ExploderProcess,//自爆流程
    Weakened,//虚弱
    ChessKingMark,//王棋标记
    ChessRookMark,//车棋标记
    ChessExhaustion,//力竭
    DragonBreath,//龙息（Dot）
    EternalFlame,//不灭之焰
    InstantDeath,//即死
    DamageChange,//伤害变化
    ActionWeakened,//单回合减攻
    SpeedChange,//速度变化
    SwordsmanBrightSword,//亮剑姿态
    SwordsmanDefense,//防御姿态
    SwordsmanGuerrilla,//游击姿态
    SwordsmanLastStand,//背水一战
    SwordsmanStagger,//失衡
    SwordsmanElegance,//优雅体态
    ChessPawnPromotion,//兵棋升变
    Prestige//威望

}

[CreateAssetMenu(fileName = "State", menuName = "状态/新状态")]
public class State : ScriptableObject
{
    public static event Action OnDamageEventSettled;

    public static void NotifyDamageEventSettled()
    {
        OnDamageEventSettled?.Invoke();
    }
    private static UnitCombatant s_activeDotEventUnit;
    private static bool s_activeDotEventHasDamage;
    private static List<UnitCombatant> s_activeDotDamagedUnits;

    [Header("状态配置")]
    [SerializeField] private StateDurationType durationType = StateDurationType.Turn;

    [Tooltip("默认持续回合数")]
    [Min(1)]
    [SerializeField]
    private int defaultTurns = 1;

    [Tooltip("默认持续行动值")]
    [Min(1)]
    [SerializeField]
    private int defaultActionValue = 100;

    [Tooltip("剩余回合数（运行时）")]
    private int remainingTurns;

    [Tooltip("剩余行动值（运行时）")]
    private int remainingActionValue;

    [Tooltip("最大有效层数")]
    [Min(1)]
    [SerializeField] private int maxStacks = 1;

    private int stackCount = 1;

    [System.NonSerialized] private IStateBehavior m_behavior;
    [System.NonSerialized] private float runtimeRecordValue;

    public UnitCombatant owner { get; private set; }
    public UnitCombatant giver { get; private set; }
    public int RemainingTurns => remainingTurns;
    public int RemainingActionValue => remainingActionValue;
    public float RuntimeRecordValue => runtimeRecordValue;
    public int StackCount => stackCount;
    public int MaxStacks => maxStacks;
    public int Stacks
    {
        get => stackCount;
        set => ChangeStackCount(value);
    }
    public StateDurationType DurationType => durationType;

    [Header("标签")]
    public StateType stateType;
    public bool isDot;
    public bool isDebuff;

    [Tooltip("是否可以重复施加（跳过已有检查，直接添加新实例）")]
    public bool canStack;

    [Header("排序")]
    [Tooltip("状态图标显示优先级，数值越小越靠前")]
    public int priority;

    [Header("显示与基础配置")]
    [Tooltip("状态图标")]
    public Sprite icon;
    [TextArea(2, 5)] public string description;
    public float skillCoef;
    public float baseExtraData1;
    public float baseExtraData2;
    public float baseExtraData3;
    public float baseExtraData4;

    [Header("Dot:快照属性")]
    [InspectorReadOnly] public float atkT;

    [Header("额外参数(用于施加时动态传入)")]
    [SerializeField, InspectorReadOnly] private float extraData = 0;

    private IStateBehavior Behavior
    {
        get
        {
            if (m_behavior == null)
            {
                m_behavior = StateBehaviorFactory.Create(stateType);
                m_behavior.Initialize(this);
            }

            return m_behavior;
        }
    }

    private void OnEnable()
    {
        m_behavior = null;
    }

    private void OnValidate()
    {
        m_behavior = null;
    }
    public float GetSpeedModifier()
    {
        return m_behavior.GetSpeedMultiplier();
    }
    public int GetSpeedExtraNum()
    {
        return m_behavior.GetSpeedExtraNum();
    }
    public void Mount(
        UnitCombatant owner,
        UnitCombatant giver,
        int duration = -1,
        int stacks = -1, float extraData = 0f)
    {
        atkT = giver != null ? giver.attack : 0f;
        this.owner = owner;
        this.giver = giver;
        this.extraData = extraData;

        switch (DurationType)
        {
            case StateDurationType.Turn:
                remainingTurns = duration > 0 ? duration : defaultTurns;
                break;
            case StateDurationType.ActionValue:
                remainingActionValue = duration > 0 ? duration : defaultActionValue;
                break;
        }

        ChangeStackCount(stacks > 0 ? stacks : 1);
        Behavior.OnStateApply();
    }

    public void UpdateState(
        int atkT,
        int extraDuration,
        int extraStacks,
        bool ifChangeStackByExtraStacks = true)
    {
        if (this.atkT < atkT)
        {
            this.atkT = atkT;
        }


        if (durationType == StateDurationType.Turn)
        {
            int targetTurns = extraDuration > 0 ? extraDuration : defaultTurns;
            remainingTurns = Mathf.Max(remainingTurns, targetTurns);
        }
        else
        {
            int targetActionValue = extraDuration > 0 ? extraDuration : defaultActionValue;
            remainingActionValue = Mathf.Max(remainingActionValue, targetActionValue);
        }
        if (ifChangeStackByExtraStacks)
        {
            ChangeStackCount(stackCount + extraStacks);
        }
        else
        {
            ChangeStackCount(extraStacks);
        }
    }

    public void TickOnTurnEnd()
    {
        if (durationType != StateDurationType.Turn)
        {
            return;
        }

        ChangeDuration(Mathf.Max(0, remainingTurns - 1));
    }
    public Coroutine OnOwnerTurnStart()
    {
        var coroutine = CoroutineHelper.GetHelper().StartCoroutine(Behavior.OnOwnerTurnStart());
        return coroutine;
    }

    public void OnOwnerTurnEnd()
    {
        Behavior.OnOwnerTurnEnd();
    }

    public void TickByActionValue(int actionValueCost)
    {
        if (durationType != StateDurationType.ActionValue)
        {
            return;
        }

        ChangeDuration(Mathf.Max(0, remainingActionValue - actionValueCost));
    }
    void ChangeDuration(int newDuration)
    {
        if (durationType == StateDurationType.Turn)
        {
            remainingTurns = Mathf.Max(0, newDuration);
            if (remainingTurns <= 0)
            {
                Debug.Log($"状态 {stateType} 在 {owner.gameObject.name} 上持续回合数已耗尽");
                EndState();
            }
        }
        else
        {
            remainingActionValue = Mathf.Max(0, newDuration);
            if (remainingActionValue <= 0)
            {
                Debug.Log($"状态 {stateType} 在 {owner.gameObject.name} 上持续行动值已耗尽");
                EndState();
            }
        }
    }
    public void EndState()
    {
        Behavior.OnStateEnd();

        if (owner != null)
        {
            owner.States.Remove(this);
        }

        Destroy(this);
    }

    public void OnAnyDamageSettled(UnitCombatant source, UnitCombatant target, int damage, bool isDotDamage, bool isTrueDamage)
    {
        if (owner == null || target == null)
        {
            return;
        }

        Behavior.OnAnyDamageSettled(source, target, damage, isDotDamage, isTrueDamage);
    }

    public void OnCombatEventTriggered(UnitCombatant triggerUnit, StateCombatEventType eventType, IReadOnlyList<UnitCombatant> damagedUnits)
    {
        if (owner == null || triggerUnit == null)
        {
            return;
        }

        Behavior.OnCombatEventTriggered(triggerUnit, eventType, damagedUnits);
    }

    public void OnOwnerSwappedOut(UnitCombatant newOwner)
    {
        Behavior.OnOwnerSwappedOut(newOwner);
    }

    public bool CanBePurged()
    {
        return Behavior.CanBePurged();
    }

    public void OnDebuffApplied(UnitCombatant target, UnitCombatant debuffGiver)
    {
        if (owner == null || target == null)
        {
            return;
        }

        Behavior.OnDebuffApplied(target, debuffGiver);
    }

    public float GetIncomingDamageMultiplier(bool isDotDamage, bool isTrueDamage)
    {
        return Behavior.GetIncomingDamageMultiplier(isDotDamage, isTrueDamage);
    }

    public float GetOutgoingDamageMultiplier(bool isDotDamage)
    {
        return Behavior.GetOutgoingDamageMultiplier(isDotDamage);
    }

    public bool CausesOutgoingTrueDamage(bool isDotDamage)
    {
        return Behavior.CausesOutgoingTrueDamage(isDotDamage);
    }

    public float GetAttractMultiplier(UnitCombatant source)
    {
        return Behavior.GetAttractMultiplier(source);
    }

    public bool CanActThisTurn()
    {
        return Behavior.CanActThisTurn();
    }

    public void ExtendTurns(int extraTurns)
    {
        if (extraTurns <= 0 || durationType != StateDurationType.Turn)
        {
            return;
        }

        ChangeDuration(remainingTurns + extraTurns);
    }


    public virtual void DotTrigger(float damageMultiplier = 1f)
    {
        if (!isDot)
        {
            return;
        }

        Behavior.DotTrigger(damageMultiplier);
    }

    public void AddRecordedValue(float delta)
    {
        runtimeRecordValue += Mathf.Max(0f, delta);
    }

    public void ResetRecordedValue()
    {
        runtimeRecordValue = 0f;
    }

    internal void ChangeStackCount(int count)
    {
        stackCount = Mathf.Clamp(count, 0, Mathf.Max(1, maxStacks));
        Behavior.OnStackChange();
        if (stackCount <= 0)
        {
            EndState();
        }
    }

    internal int ClampStackCount(int count)
    {
        return Mathf.Clamp(count, 0, Mathf.Max(1, maxStacks));
    }
    #region 伤害结算相关

    private static void NotifyCombatEvent(UnitCombatant triggerUnit, StateCombatEventType eventType, IReadOnlyList<UnitCombatant> damagedUnits)
    {
        if (triggerUnit == null || TurnManager.Instance == null)
        {
            return;
        }

        damagedUnits ??= System.Array.Empty<UnitCombatant>();

        foreach (var com in TurnManager.Instance.CurrentTurnOrder.ToList())
        {
            var unit = com as UnitCombatant;
            if (unit == null)
            {
                continue;
            }

            for (int i = 0; i < unit.States.Count; i++)
            {
                State state = unit.States[i];
                if (state == null)
                {
                    continue;
                }

                state.OnCombatEventTriggered(triggerUnit, eventType, damagedUnits);
            }
        }

    }
    public static void NotifyDamageSkillUsed(UnitCombatant triggerUnit, IReadOnlyList<UnitCombatant> damagedUnits)
    {
        NotifyCombatEvent(triggerUnit, StateCombatEventType.DamageSkillUsed, damagedUnits);
    }

    public static void RunBatchedDotEvent(UnitCombatant triggerUnit, System.Action triggerAction)
    {
        if (triggerUnit == null || triggerAction == null)
        {
            return;
        }

        UnitCombatant previousTriggerUnit = s_activeDotEventUnit;
        bool previousHasDamage = s_activeDotEventHasDamage;
        List<UnitCombatant> previousDamagedUnits = s_activeDotDamagedUnits;
        s_activeDotEventUnit = triggerUnit;
        s_activeDotEventHasDamage = false;
        s_activeDotDamagedUnits = new List<UnitCombatant>();

        try
        {
            triggerAction();
        }
        finally
        {
            FinishBatchedDotEvent(triggerUnit, previousTriggerUnit, previousHasDamage, previousDamagedUnits);
        }
    }

    public static IEnumerator RunBatchedDotEvent(UnitCombatant triggerUnit, IEnumerator triggerRoutine)
    {
        if (triggerUnit == null || triggerRoutine == null)
        {
            yield break;
        }

        UnitCombatant previousTriggerUnit = s_activeDotEventUnit;
        bool previousHasDamage = s_activeDotEventHasDamage;
        List<UnitCombatant> previousDamagedUnits = s_activeDotDamagedUnits;
        s_activeDotEventUnit = triggerUnit;
        s_activeDotEventHasDamage = false;
        s_activeDotDamagedUnits = new List<UnitCombatant>();

        try
        {
            while (triggerRoutine.MoveNext())
            {
                yield return triggerRoutine.Current;
            }
        }
        finally
        {
            FinishBatchedDotEvent(triggerUnit, previousTriggerUnit, previousHasDamage, previousDamagedUnits);
        }
    }

    internal static void RecordBatchedDotDamage(UnitCombatant target, int damage, bool isDotDamage)
    {
        if (!isDotDamage || s_activeDotEventUnit == null || target == null || damage <= 0)
        {
            return;
        }

        s_activeDotEventHasDamage = true;
        if (s_activeDotDamagedUnits == null)
        {
            s_activeDotDamagedUnits = new List<UnitCombatant>();
        }

        if (!s_activeDotDamagedUnits.Contains(target))
        {
            s_activeDotDamagedUnits.Add(target);
        }
    }

    private static void FinishBatchedDotEvent(UnitCombatant triggerUnit, UnitCombatant previousTriggerUnit, bool previousHasDamage, List<UnitCombatant> previousDamagedUnits)
    {
        bool hasDotDamage = s_activeDotEventHasDamage;
        List<UnitCombatant> damagedUnits = s_activeDotDamagedUnits ?? new List<UnitCombatant>();
        s_activeDotEventUnit = previousTriggerUnit;
        s_activeDotEventHasDamage = previousHasDamage || hasDotDamage;
        s_activeDotDamagedUnits = previousDamagedUnits;

        if (previousDamagedUnits != null)
        {
            for (int i = 0; i < damagedUnits.Count; i++)
            {
                UnitCombatant damagedUnit = damagedUnits[i];
                if (damagedUnit != null && !previousDamagedUnits.Contains(damagedUnit))
                {
                    previousDamagedUnits.Add(damagedUnit);
                }
            }
        }

        if (hasDotDamage)
        {
            NotifyCombatEvent(triggerUnit, StateCombatEventType.DotTriggered, damagedUnits);
        }
    }
    #endregion
    public static void TickAllStatesByActionValue(float passedActionValue)
    {
        int actionValueCost = Mathf.Max(0, Mathf.CeilToInt(passedActionValue));
        if (actionValueCost <= 0)
        {
            return;
        }

        foreach (var com in TurnManager.Instance.CurrentTurnOrder.ToList())
        {
            var unit = com as UnitCombatant;
            if (unit == null)
            {
                continue;
            }

            unit.ProcessStatesByActionValue(actionValueCost);
        }
    }
}

public interface IStateBehavior
{
    float GetSpeedMultiplier();
    int GetSpeedExtraNum();
    void Initialize(State state);
    void OnStateApply();
    IEnumerator OnOwnerTurnStart();
    void OnOwnerTurnEnd();
    void OnStateEnd();
    void OnCombatEventTriggered(UnitCombatant triggerUnit, StateCombatEventType eventType, IReadOnlyList<UnitCombatant> damagedUnits);
    void OnAnyDamageSettled(UnitCombatant source, UnitCombatant target, int damage, bool isDotDamage, bool isTrueDamage);
    void OnDebuffApplied(UnitCombatant target, UnitCombatant debuffGiver);
    float GetIncomingDamageMultiplier(bool isDotDamage, bool isTrueDamage);
    float GetOutgoingDamageMultiplier(bool isDotDamage);
    float GetAttractMultiplier(UnitCombatant source);
    bool CanActThisTurn();
    void OnStackChange();
    void DotTrigger(float damageMultiplier);
    bool CausesOutgoingTrueDamage(bool isDotDamage);
    /// <summary>角色交换回调：当持有者被换下时，将状态转移到新角色上</summary>
    void OnOwnerSwappedOut(UnitCombatant newOwner);
    bool CanBePurged();
}

public abstract class StateBehaviorBase : IStateBehavior
{
    protected State state;
    public virtual int GetSpeedExtraNum()
    {
        return 0;
    }

    public virtual void Initialize(State state)
    {
        this.state = state;
    }
    public virtual void OnStateApply() { }
    public virtual IEnumerator OnOwnerTurnStart() { yield break; }//由于这是回合开始的行为，最好支持协程，以便实现一些需要等待的效果
    public virtual void OnOwnerTurnEnd() { }
    public virtual void OnStateEnd() { }
    public virtual void OnCombatEventTriggered(UnitCombatant triggerUnit, StateCombatEventType eventType, IReadOnlyList<UnitCombatant> damagedUnits) { }
    public virtual void OnAnyDamageSettled(UnitCombatant source, UnitCombatant target, int damage, bool isDotDamage, bool isTrueDamage) { }
    public virtual void OnDebuffApplied(UnitCombatant target, UnitCombatant debuffGiver) { }
    public virtual float GetSpeedMultiplier()
    {
        return 1f;
    }
    public virtual float GetIncomingDamageMultiplier(bool isDotDamage, bool isTrueDamage) { return 1f; }
    public virtual float GetOutgoingDamageMultiplier(bool isDotDamage) { return 1f; }
    public virtual float GetAttractMultiplier(UnitCombatant source) { return 1f; }
    public virtual bool CanActThisTurn() { return true; }
    public virtual void OnStackChange() { }
    public virtual void DotTrigger(float damageMultiplier) { }
    public virtual bool CausesOutgoingTrueDamage(bool isDotDamage) { return false; }
    public virtual void OnOwnerSwappedOut(UnitCombatant newOwner) { }
    public virtual bool CanBePurged() { return true; }
}

public static class StateBehaviorFactory
{
    public static IStateBehavior Create(StateType stateType)
    {
        switch (stateType)
        {
            case StateType.BerserkFeast:
                return new BerserkFeastStateBehavior();
            case StateType.BurningBlood:
                return new BurningBloodStateBehavior();
            case StateType.CritRhythm:
                return new CritRhythmStateBehavior();
            case StateType.DesperationMark:
                return new DesperationMarkStateBehavior();
            case StateType.HealingSpring:
                return new HealingSpringStateBehavior();
            case StateType.RestorationSurge:
                return new RestorationSurgeStateBehavior();
            case StateType.DeadlyArmor:
                return new DeadlyArmorStateBehavior();
            case StateType.BloodBath:
                return new BloodBathStateBehavior();
            case StateType.Vulnerable:
                return new VulnerableStateBehavior();
            case StateType.ArmorBreak:
                return new ArmorBreakStateBehavior();
            case StateType.BloodSurgeHeal:
                return new BloodSurgeHealStateBehavior();
            case StateType.ExploderProcess:
                return new ExploderProcessStateBehavior();
            case StateType.DamageChange:
                return new NextActionDamageBoostStateBehavior();
            case StateType.ActionWeakened:
                return new ActionWeakenedStateBehavior();
            case StateType.PursuitPunish:
                return new PursuitPunishStateBehavior();
            case StateType.PunishMark:
                return new DefaultStateBehavior();
            case StateType.PersistentTorment:
                return new PersistentTormentStateBehavior();
            case StateType.SeqFlame:
                return new SeqFlameStateBehavior();
            case StateType.Daze:
                return new DazeStateBehavior();
            case StateType.Ice:
                return new IceStateBehavior();
            case StateType.Corrosion:
                return new CorrosionStateBehavior();
            case StateType.Wind:
                return new WindStateBehavior();
            case StateType.ElementalDetonation:
                return new ElementalDetonationStateBehavior();
            case StateType.CounterCharge:
                return new CounterChargeStateBehavior();
            case StateType.Resist:
                return new ResistStateBehavior();
            case StateType.Taunt:
                return new TauntStateBehavior();
            case StateType.Charge:
                return new ChargeStateBehavior();
            case StateType.Attract:
                return new AttractStateBehavior();
            case StateType.Poison:
                return new PoisonStateBehavior();
            case StateType.Chaos:
                return new ChaosStateBehavior();
            case StateType.Weakened:
                return new WeakenedStateBehavior();
            case StateType.ChessKingMark:
                return new ChessKingMarkStateBehavior();
            case StateType.ChessRookMark:
                return new ChessRookMarkStateBehavior();
            case StateType.ChessExhaustion:
                return new ChessExhaustionStateBehavior();
            case StateType.DragonBreath:
                return new DragonBreathStateBehavior();
            case StateType.EternalFlame:
                return new EternalFlameStateBehavior();
            case StateType.InstantDeath:
                return new DefaultStateBehavior();
            case StateType.SwordsmanBrightSword:
                return new SwordsmanBrightSwordBehavior();
            case StateType.SwordsmanDefense:
                return new SwordsmanDefenseBehavior();
            case StateType.SwordsmanGuerrilla:
                return new SwordsmanGuerrillaBehavior();
            case StateType.SwordsmanLastStand:
                return new SwordsmanLastStandBehavior();
            case StateType.SwordsmanStagger:
                return new SwordsmanStaggerBehavior();
            case StateType.SwordsmanElegance:
                return new SwordsmanEleganceBehavior();
            case StateType.Prestige:
                return new PrestigeStateBehavior();
            default:
                return new DefaultStateBehavior();
        }
    }
}

public class DefaultStateBehavior : StateBehaviorBase
{
}

public class ExploderProcessStateBehavior : StateBehaviorBase
{
    public override void OnStateApply()
    {
        if (state.owner is Enemy enemy)
        {
            enemy.explodeState = ExplodeType.hasStarted;
        }
    }

    public override void OnStateEnd()
    {
        if (state.owner is Enemy enemy)
        {
            enemy.explodeState = ExplodeType.ReadyToBurst;
        }
    }
}

public class BloodContractStateBehavior : StateBehaviorBase
{
    public override void OnAnyDamageSettled(UnitCombatant source, UnitCombatant target, int damage, bool isDotDamage, bool isTrueDamage)
    {
        if (state.owner == null || state.giver == null || source != state.owner || damage <= 0)
        {
            return;
        }

        float healRatio = state.baseExtraData1;
        int healAmount = Mathf.RoundToInt(damage * healRatio);
        state.giver.Heal(healAmount);
    }
}

public class BerserkFeastStateBehavior : StateBehaviorBase
{
    private float critBonus;

    public override void OnStateApply()
    {
        if (state.owner == null)
        {
            return;
        }

        critBonus = state.baseExtraData2;
        state.owner.critRate += critBonus;
    }

    public override void OnStateEnd()
    {
        if (state.owner == null)
        {
            return;
        }

        state.owner.critRate = Mathf.Max(0f, state.owner.critRate - critBonus);
    }
}

public class BurningBloodStateBehavior : StateBehaviorBase
{
    private static readonly Dictionary<UnitCombatant, bool> s_killInTurn = new Dictionary<UnitCombatant, bool>();

    public override void OnStateApply()
    {
        if (state.owner != null)
        {
            s_killInTurn[state.owner] = false;
        }
    }

    public override void OnStateEnd()
    {
        if (state.owner != null)
        {
            s_killInTurn.Remove(state.owner);
        }
    }

    public override void OnAnyDamageSettled(UnitCombatant source, UnitCombatant target, int damage, bool isDotDamage, bool isTrueDamage)
    {
        if (state.owner == null || source != state.owner || target == null)
        {
            return;
        }

        if (target.currentHP <= 0 && target != source)
        {
            s_killInTurn[state.owner] = true;
        }
    }

    public override void OnOwnerTurnEnd()
    {
        if (state.owner == null)
        {
            return;
        }

        if (ConsumeKillFlag(state.owner))
        {
            return;
        }

        float selfDamageRatio = state.baseExtraData1;
        int selfDamage = Mathf.RoundToInt(state.owner.maxHP * selfDamageRatio);
        // 若当前生命不足以承受自损，则降至1点而不会死亡
        if (state.owner.currentHP <= selfDamage)
        {
            state.owner.TakeDamage(new UnitCombatant.DamageInfo(state.owner.currentHP - 1, state.owner).AsTrueDamage());
        }
        else
        {
            state.owner.TakeDamage(new UnitCombatant.DamageInfo(selfDamage, state.owner).AsTrueDamage());
        }
    }

    public static bool ConsumeKillFlag(UnitCombatant owner)
    {
        if (owner == null)
        {
            return false;
        }

        if (!s_killInTurn.TryGetValue(owner, out bool killed) || !killed)
        {
            return false;
        }

        s_killInTurn[owner] = false;
        return true;
    }
}

public class CritRhythmStateBehavior : StateBehaviorBase
{
    public override void OnCombatEventTriggered(UnitCombatant triggerUnit, StateCombatEventType eventType, IReadOnlyList<UnitCombatant> damagedUnits)
    {
        if (state.owner == null || CharacterManager.Instance == null)
        {
            return;
        }

        if (eventType == StateCombatEventType.CriticalHit && triggerUnit != state.owner)
        {
            return;
        }

        if (eventType != StateCombatEventType.CriticalHit && eventType != StateCombatEventType.EnemyKilled)
        {
            return;
        }

        float teamAdvanceRatio = state.baseExtraData1;
        if (teamAdvanceRatio <= 0f)
        {
            return;
        }

        foreach (var ally in CharacterManager.Instance.fieldCharacters)
        {
            if (ally == null || ally.IsDead)
            {
                continue;
            }

            ally.ChangeActionValue(Mathf.Max(0f, ally.currentActionValue - ally.currentActionValue * teamAdvanceRatio));
        }
    }
}

public class DesperationMarkStateBehavior : StateBehaviorBase
{
    private static readonly HashSet<UnitCombatant> s_resolvingExtraDamage = new HashSet<UnitCombatant>();

    private bool transferredOnDeath;

    public override void OnStateApply()
    {
        RemoveDuplicateAliveMarks();
    }

    public override void OnAnyDamageSettled(UnitCombatant source, UnitCombatant target, int damage, bool isDotDamage, bool isTrueDamage)
    {
        if (state.owner == null || target != state.owner || damage <= 0 || transferredOnDeath)
        {
            return;
        }

        if (state.owner.currentHP <= 0)
        {
            TransferMark();
            return;
        }

        if (s_resolvingExtraDamage.Contains(state.owner))
        {
            return;
        }

        int extraTrueDamage = Mathf.RoundToInt(damage * state.baseExtraData1);
        if (extraTrueDamage <= 0)
        {
            return;
        }

        UnitCombatant extraDamageSource = source != null ? source : state.giver != null ? state.giver : state.owner;
        s_resolvingExtraDamage.Add(state.owner);
        try
        {
            state.owner.TakeDamage(
                new UnitCombatant.DamageInfo(extraTrueDamage, extraDamageSource)
                    .AsTrueDamage()
                    .WithState(StateType.DesperationMark));
        }
        finally
        {
            s_resolvingExtraDamage.Remove(state.owner);
        }

        if (state.owner.currentHP <= 0)
        {
            TransferMark();
        }
    }

    private void RemoveDuplicateAliveMarks()
    {
        if (EnemyManager.Instance == null || state.owner == null)
        {
            return;
        }

        IReadOnlyList<Enemy> aliveEnemies = EnemyManager.Instance.AliveEnemies;
        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            Enemy enemy = aliveEnemies[i];
            if (enemy == null || enemy == state.owner || enemy.currentHP <= 0 || enemy.IsDead)
            {
                continue;
            }

            State duplicate = enemy.GetState(StateType.DesperationMark);
            if (duplicate != null)
            {
                enemy.RemoveState(duplicate);
            }
        }
    }

    private void TransferMark()
    {
        if (transferredOnDeath || EnemyManager.Instance == null)
        {
            return;
        }

        transferredOnDeath = true;

        Enemy nextTarget = GetNextTarget();
        if (nextTarget == null)
        {
            return;
        }

        UnitCombatant giver = state.giver != null ? state.giver : state.owner;
        State transferredState = nextTarget.AddState(StateType.DesperationMark, giver, GetTransferDuration(), state.StackCount);
        if (transferredState == null)
        {
            return;
        }

        transferredState.baseExtraData1 = state.baseExtraData1;
        transferredState.baseExtraData2 = state.baseExtraData2;
        transferredState.baseExtraData3 = state.baseExtraData3;
        transferredState.baseExtraData4 = state.baseExtraData4;
    }

    private int GetTransferDuration()
    {
        switch (state.DurationType)
        {
            case StateDurationType.Turn:
                return Mathf.Max(1, state.RemainingTurns);
            case StateDurationType.ActionValue:
                return Mathf.Max(1, state.RemainingActionValue);
            default:
                return 99;
        }
    }

    private Enemy GetNextTarget()
    {
        Enemy bestTarget = null;
        float lowestHpRatio = float.MaxValue;
        IReadOnlyList<Enemy> aliveEnemies = EnemyManager.Instance.AliveEnemies;
        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            Enemy enemy = aliveEnemies[i];
            if (enemy == null || enemy == state.owner || enemy.currentHP <= 0 || enemy.maxHP <= 0 || enemy.IsDead)
            {
                continue;
            }

            float hpRatio = (float)enemy.currentHP / enemy.maxHP;
            if (bestTarget == null || hpRatio < lowestHpRatio)
            {
                bestTarget = enemy;
                lowestHpRatio = hpRatio;
            }
        }

        return bestTarget;
    }
}

public class HealingSpringStateBehavior : StateBehaviorBase
{
    public override float GetOutgoingDamageMultiplier(bool isDotDamage)
    {
        return state.baseExtraData1 > 0f ? state.baseExtraData1 : 1.2f;
    }

    public override void OnAnyDamageSettled(UnitCombatant source, UnitCombatant target, int damage, bool isDotDamage, bool isTrueDamage)
    {
        if (state.owner == null || source != state.owner || isDotDamage || target == null || target == source || damage <= 0 || CharacterManager.Instance == null)
        {
            return;
        }

        List<Character> reserveTargets = new List<Character>();
        for (int i = 0; i < CharacterManager.Instance.reserveCharacters.Count; i++)
        {
            Character reserveCharacter = CharacterManager.Instance.reserveCharacters[i];
            if (reserveCharacter == null || reserveCharacter.IsDead)
            {
                continue;
            }

            reserveTargets.Add(reserveCharacter);
        }

        if (reserveTargets.Count <= 0)
        {
            return;
        }

        int baseHeal = damage / reserveTargets.Count;
        int remainder = damage % reserveTargets.Count;
        for (int i = 0; i < reserveTargets.Count; i++)
        {
            int healAmount = baseHeal + (i < remainder ? 1 : 0);
            if (healAmount > 0)
            {
                reserveTargets[i].Heal(healAmount);
            }
        }
    }
}

public class RestorationSurgeStateBehavior : StateBehaviorBase
{
    public override float GetOutgoingDamageMultiplier(bool isDotDamage)
    {
        if (isDotDamage)
        {
            return 1f;
        }

        return state.baseExtraData1 + state.baseExtraData2 * GetTeamMissingHpRatio();
    }

    public override void OnAnyDamageSettled(UnitCombatant source, UnitCombatant target, int damage, bool isDotDamage, bool isTrueDamage)
    {
        if (source != state.owner || target == null || damage <= 0 || isDotDamage)
        {
            return;
        }

        state.ChangeStackCount(0);
    }
    private float GetTeamMissingHpRatio()
    {
        var allies = new List<Character>(CharacterManager.Instance.fieldCharacters);
        allies.AddRange(CharacterManager.Instance.reserveCharacters);
        float totalMaxHp = 0f;
        float totalMissingHp = 0f;
        for (int i = 0; i < allies.Count; i++)
        {
            Character ally = allies[i];
            if (ally == null || ally.maxHP <= 0)
            {
                continue;
            }

            totalMaxHp += ally.maxHP;
            totalMissingHp += Mathf.Max(0, ally.maxHP - ally.currentHP);
        }

        if (totalMaxHp <= 0f)
        {
            return 0f;
        }

        return Mathf.Clamp01(totalMissingHp / totalMaxHp);
    }

}

public class DeadlyArmorStateBehavior : StateBehaviorBase
{
    public override float GetOutgoingDamageMultiplier(bool isDotDamage)
    {
        return state.baseExtraData2;
    }
    public override bool CausesOutgoingTrueDamage(bool isDotDamage)
    {
        return true;
    }

    public override void OnOwnerTurnEnd()
    {
        state.ChangeStackCount(state.StackCount - 1);
    }

    public override void OnAnyDamageSettled(UnitCombatant source, UnitCombatant target, int damage, bool isDotDamage, bool isTrueDamage)
    {
        if (state.owner == null || source != state.owner || target == null || damage <= 0 || target.currentHP > 0)
        {
            return;
        }

        float teamAdvanceRatio = state.baseExtraData1;
        if (teamAdvanceRatio <= 0f || CharacterManager.Instance == null)
        {
            return;
        }

        foreach (var ally in CharacterManager.Instance.fieldCharacters)
        {
            if (ally == null || ally.IsDead)
            {
                continue;
            }

            ally.ChangeActionValue(Mathf.Max(0f, ally.currentActionValue - ally.BaseActionValue * teamAdvanceRatio));
        }
    }
}

public class DesperationFieldStateBehavior : StateBehaviorBase
{
    public override float GetOutgoingDamageMultiplier(bool isDotDamage)
    {
        if (state.owner == null || state.owner.maxHP <= 0)
        {
            return 1f;
        }

        float lostHpRatio = (state.owner.maxHP - state.owner.currentHP) / Mathf.Max(1f, state.owner.maxHP);
        return 1f + Mathf.Clamp01(lostHpRatio) * 0.25f;
    }
}

public class BloodBathStateBehavior : StateBehaviorBase
{
    public override void OnStateApply()
    {
        state.ResetRecordedValue();
    }

    public override void OnAnyDamageSettled(UnitCombatant source, UnitCombatant target, int damage, bool isDotDamage, bool isTrueDamage)
    {
        if (target == state.owner && damage > 0)
        {
            state.AddRecordedValue(damage);
        }
    }
}

public class VulnerableStateBehavior : StateBehaviorBase
{
    public override float GetIncomingDamageMultiplier(bool isDotDamage, bool isTrueDamage)
    {
        return state.baseExtraData2;
    }

    public override void OnAnyDamageSettled(UnitCombatant source, UnitCombatant target, int damage, bool isDotDamage, bool isTrueDamage)
    {
        if (target != state.owner || damage <= 0)
        {
            return;
        }

        int consumeStacks = Mathf.RoundToInt(state.baseExtraData1);
        state.ChangeStackCount(state.StackCount - consumeStacks);
    }
}

public class ArmorBreakStateBehavior : StateBehaviorBase
{
    public override float GetIncomingDamageMultiplier(bool isDotDamage, bool isTrueDamage)
    {
        if (state.owner == null || state.owner.K <= 0f)
        {
            return 1f;
        }

        int validStacks = Mathf.Clamp(state.StackCount, 0, 2);
        if (validStacks <= 0)
        {
            return 1f;
        }

        float def = Mathf.Max(0f, state.owner.defense);
        float oldFactor = state.owner.K / (state.owner.K + def);
        float reductionPerStack = state.baseExtraData1;
        float newDef = def * (1f - reductionPerStack * validStacks);
        float newFactor = state.owner.K / (state.owner.K + Mathf.Max(0f, newDef));
        if (oldFactor <= 0f)
        {
            return 1f;
        }

        return newFactor / oldFactor;
    }
}

public class BloodSurgeHealStateBehavior : StateBehaviorBase
{
    public override void OnCombatEventTriggered(UnitCombatant triggerUnit, StateCombatEventType eventType, IReadOnlyList<UnitCombatant> damagedUnits)
    {
        if (eventType != StateCombatEventType.DamageSkillUsed
            || state.owner == null
            || triggerUnit != state.owner
            || state.StackCount <= 0
            || !DamagedAnyEnemy(state.owner, damagedUnits))
        {
            return;
        }

        float healRatio = state.baseExtraData2;
        float critBonusRatio = state.baseExtraData1;
        int heal = Mathf.RoundToInt(state.owner.maxHP * healRatio);
        bool critHeal = UnityEngine.Random.value < Mathf.Clamp01(state.owner.critRate);
        if (critHeal)
        {
            heal += Mathf.RoundToInt(state.owner.maxHP * critBonusRatio);
        }

        state.owner.Heal(heal);
        state.ChangeStackCount(state.StackCount - 1);
    }

    private static bool DamagedAnyEnemy(UnitCombatant owner, IReadOnlyList<UnitCombatant> damagedUnits)
    {
        if (damagedUnits == null)
        {
            return false;
        }

        for (int i = 0; i < damagedUnits.Count; i++)
        {
            UnitCombatant unit = damagedUnits[i];
            if (unit == null || unit == owner || unit.IsDead || unit is not Enemy)
            {
                continue;
            }

            return true;
        }

        return false;
    }
}

public class CriticalGuardStateBehavior : StateBehaviorBase
{
    private bool usedFatalHeal;

    public override float GetOutgoingDamageMultiplier(bool isDotDamage)
    {
        if (state.owner == null || state.owner.maxHP <= 0)
        {
            return 1f;
        }

        float hpThreshold = state.baseExtraData1;
        float damageMultiplier = state.baseExtraData3;
        return state.owner.currentHP <= Mathf.RoundToInt(state.owner.maxHP * hpThreshold) ? damageMultiplier : 1f;
    }

    public override void OnAnyDamageSettled(UnitCombatant source, UnitCombatant target, int damage, bool isDotDamage, bool isTrueDamage)
    {
        if (target != state.owner || state.owner == null)
        {
            return;
        }

        if (state.owner.currentHP > 0)
        {
            return;
        }

        if (source == state.owner)
        {
            state.owner.currentHP = 1;
            return;
        }

        if (usedFatalHeal)
        {
            return;
        }

        float healRatio = state.baseExtraData2;
        int heal = state.giver != null ? Mathf.RoundToInt(state.giver.maxHP * healRatio) : Mathf.RoundToInt(state.owner.maxHP * healRatio);
        state.owner.currentHP = Mathf.Clamp(heal, 1, state.owner.maxHP);
        usedFatalHeal = true;
    }
}

public class BloodGiftStateBehavior : StateBehaviorBase
{
    public override float GetOutgoingDamageMultiplier(bool isDotDamage)
    {
        return state.baseExtraData4;
    }

    public override IEnumerator OnOwnerTurnStart()
    {
        if (state.owner == null || state.giver == null)
        {
            yield break;
        }

        float lowHpThreshold = state.baseExtraData1;
        float lowHpHealPercent = state.baseExtraData2;
        float normalHealPercent = state.baseExtraData3;
        float healPercent = state.owner.currentHP <= Mathf.RoundToInt(state.owner.maxHP * lowHpThreshold) ? lowHpHealPercent : normalHealPercent;
        int heal = Mathf.RoundToInt(state.giver.maxHP * healPercent);
        state.owner.Heal(heal);
    }
}

public abstract class GiftStateBehaviorBase : StateBehaviorBase
{
    protected void ConsumeActionCount()
    {
        state.ChangeStackCount(state.StackCount - 1);
    }
}

public class GiftWeakStateBehavior : GiftStateBehaviorBase
{
    public override float GetOutgoingDamageMultiplier(bool isDotDamage)
    {
        return state.baseExtraData1;
    }

    public override IEnumerator OnOwnerTurnStart()
    {
        ConsumeActionCount();
        yield break;
    }
}

public class GiftMidStateBehavior : GiftStateBehaviorBase
{
    public override float GetOutgoingDamageMultiplier(bool isDotDamage)
    {
        return state.baseExtraData2;
    }

    public override IEnumerator OnOwnerTurnStart()
    {
        if (state.owner != null)
        {
            float healRatio = state.baseExtraData1;
            int heal = Mathf.RoundToInt(state.owner.maxHP * healRatio);
            state.owner.Heal(heal);
        }

        ConsumeActionCount();
        yield break;
    }
}

public class GiftStrongStateBehavior : GiftStateBehaviorBase
{
    private bool triggered;

    public override float GetOutgoingDamageMultiplier(bool isDotDamage)
    {
        return state.baseExtraData2;
    }

    public override void OnAnyDamageSettled(UnitCombatant source, UnitCombatant target, int damage, bool isDotDamage, bool isTrueDamage)
    {
        if (triggered || source != state.owner || state.owner == null)
        {
            return;
        }

        float healRatio = state.baseExtraData1;
        int heal = state.giver != null
            ? Mathf.RoundToInt(state.giver.maxHP * healRatio)
            : Mathf.RoundToInt(state.owner.maxHP * healRatio);
        state.owner.Heal(heal);
        triggered = true;
    }

    public override IEnumerator OnOwnerTurnStart()
    {
        ConsumeActionCount();
        yield break;
    }
}

public class PursuitPunishStateBehavior : StateBehaviorBase
{
    public override void OnDebuffApplied(UnitCombatant target, UnitCombatant debuffGiver)
    {
        Character ownerCharacter = state.owner as Character;
        if (!(target is Enemy enemy) || ownerCharacter == null)
        {
            return;
        }

        // 仅响应友方施加的减益；排除追惩持有者自身与敌人自挂减益（如剑客失衡）
        if (debuffGiver == state.owner || debuffGiver == target || !(debuffGiver is Character))
        {
            return;
        }

        bool hadMark = enemy.HasState(StateType.PunishMark);
        State punishMark = enemy.AddState(StateType.PunishMark, state.owner, 1, 1);
        if (punishMark == null)
        {
            return;
        }

        // 已有惩戒标记时仅刷新层数/回合，不重复插入追加回合
        if (hadMark)
        {
            return;
        }

        // 同一角色同时最多存在一个待处理的追加回合
        if (HasPendingAdditionalTurn(ownerCharacter))
        {
            return;
        }

        CharacterSkillBase additionalSkill = ResolvePursuitPunishAdditionalSkill(ownerCharacter);
        if (additionalSkill == null)
        {
            Debug.LogWarning($"[PursuitPunish] {ownerCharacter.combatantName} 缺少追惩追加技能，无法插入追加回合");
            return;
        }

        TurnManager.Instance?.AdditionalTurnInsert(ownerCharacter, additionalSkill, null);
    }

    private static bool HasPendingAdditionalTurn(Character ownerCharacter)
    {
        if (ownerCharacter == null || TurnManager.Instance == null)
        {
            return false;
        }

        foreach (var combatant in TurnManager.Instance.CurrentTurnOrder)
        {
            if (combatant is AdditionalCharacter additionalTurnCombatant && additionalTurnCombatant.character == ownerCharacter)
            {
                return true;
            }
        }

        return false;
    }

    private static CharacterSkillBase ResolvePursuitPunishAdditionalSkill(Character ownerCharacter)
    {
        CharacterSkillBase additionalSkill = ownerCharacter.GetAdditionalSkillInstance();
        if (additionalSkill != null && additionalSkill.skillType == CharacterSkillType.PursuitPunishAdditional)
        {
            return additionalSkill;
        }

        return SkillDictionaryManager.GetSkill(CharacterSkillType.PursuitPunishAdditional);
    }

    public override float GetOutgoingDamageMultiplier(bool isDotDamage)
    {
        if (TurnManager.Instance != null && TurnManager.Instance.GetCurrentCombatant() != state.owner)
        {
            float bonus = state.baseExtraData1;
            return 1f + bonus;
        }

        return 1f;
    }

}

public class PersistentTormentStateBehavior : StateBehaviorBase
{
    public override IEnumerator OnOwnerTurnStart()
    {
        if (state.owner == null)
        {
            yield break;
        }

        int maxStacks = Mathf.Max(1, state.MaxStacks);
        float chancePerStack = state.baseExtraData2;
        int stunDuration = Mathf.RoundToInt(state.baseExtraData3);
        int validLayer = Mathf.Clamp(state.StackCount, 0, maxStacks);
        float chance = Mathf.Min(maxStacks * chancePerStack, validLayer * chancePerStack);
        if (UnityEngine.Random.value <= chance)
        {
            state.owner.AddState(StateType.Daze, state.giver != null ? state.giver : state.owner, stunDuration, 1);
            FloatingTipGenerator.Instance?.ShowTipAtObject(state.owner.transform, $"{state.owner.name}受到震慑");
        }
    }

    public override float GetOutgoingDamageMultiplier(bool isDotDamage)
    {
        int maxStacks = Mathf.Max(1, state.MaxStacks);
        float reductionPerStack = state.baseExtraData1;
        return 1f - Mathf.Min(maxStacks * reductionPerStack, reductionPerStack * state.StackCount);
    }
}

public abstract class DotStateBehaviorBase : StateBehaviorBase
{
    public override IEnumerator OnOwnerTurnStart()
    {
        TriggerConfiguredDotDamage();
        yield return new WaitForSeconds(0.1f);
    }

    public override void DotTrigger(float damageMultiplier)
    {
        TriggerConfiguredDotDamage(damageMultiplier);
    }

    protected void TriggerConfiguredDotDamage(float damageMultiplier = 1f)
    {
        if (!state.isDot)
        {
            return;
        }

        Debug.Log($"状态 {state.stateType} 造成伤害，伤害倍率: {damageMultiplier}");
        var damageInfo = DamageCounter.CountDotDamage(state, state.giver, state.owner);
        damageInfo.Damage = Mathf.RoundToInt(damageInfo.Damage * damageMultiplier);
        state.owner.TakeDamage(damageInfo);
    }
}

public class SeqFlameStateBehavior : DotStateBehaviorBase
{
}

public class IceStateBehavior : DotStateBehaviorBase
{
}

public class CorrosionStateBehavior : DotStateBehaviorBase
{
}

public class WindStateBehavior : DotStateBehaviorBase
{
}

public class DazeStateBehavior : StateBehaviorBase
{
    public override float GetIncomingDamageMultiplier(bool isDotDamage, bool isTrueDamage)
    {
        return state.baseExtraData1;
    }

    public override bool CanActThisTurn()
    {
        return false;
    }
}

public class ElementalDetonationStateBehavior : StateBehaviorBase
{
    public override void OnAnyDamageSettled(UnitCombatant source, UnitCombatant target, int damage, bool isDotDamage, bool isTrueDamage)
    {
        if (!isDotDamage || damage <= 0 || target != state.owner || target.currentHP > 0 || EnemyManager.Instance == null)
        {
            return;
        }

        int finalDamage = GetTotalDotDetonationDamage(target);
        if (finalDamage <= 0)
        {
            return;
        }

        UnitCombatant damageSource = source != null ? source : state.giver != null ? state.giver : state.owner;
        var enemies = new List<Enemy>(EnemyManager.Instance.AliveEnemies);

        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy == target)
            {
                continue;
            }

            enemy.TakeDamage(DamageCounter.CountDamage(state.giver, enemy, 0, finalDamage, DamageType.Magical, true, false));
        }
    }

    private int GetTotalDotDetonationDamage(UnitCombatant target)
    {
        int totalDamage = 0;
        var targetStates = new List<State>(target.States);
        foreach (var targetState in targetStates)
        {
            if (targetState == null || !targetState.isDot)
            {
                continue;
            }

            totalDamage += Mathf.Max(0, DamageCounter.CountDotDamage(targetState, targetState.giver, target).Damage);
        }

        return totalDamage;
    }

    public override float GetIncomingDamageMultiplier(bool isDotDamage, bool isTrueDamage)
    {
        return isDotDamage ? state.baseExtraData1 : 1f;
    }
}

public class CounterChargeStateBehavior : StateBehaviorBase
{
    public override void OnStateApply()
    {
        int initialCharge = Mathf.RoundToInt(state.baseExtraData1);
        state.owner.AddState(StateType.Charge, state.owner, 99, initialCharge);
    }

    public override void OnCombatEventTriggered(UnitCombatant triggerUnit, StateCombatEventType eventType, IReadOnlyList<UnitCombatant> damagedUnits)
    {
        int selfGain = Mathf.RoundToInt(state.baseExtraData2);
        int otherGain = Mathf.RoundToInt(state.baseExtraData3);
        bool isSelfTriggered = false;
        foreach (var unit in damagedUnits)
        {
            if (unit == state.owner)
            {
                isSelfTriggered = true;
                break;
            }
        }
        int count = isSelfTriggered ? selfGain : otherGain;
        AddOrUpdateChargeState(count);
    }

    private void AddOrUpdateChargeState(int count)
    {
        if (state.owner == null)
        {
            return;
        }

        var currentStates = new List<State>(state.owner.States);
        for (int i = 0; i < currentStates.Count; i++)
        {
            State item = currentStates[i];
            if (item == null || item.stateType != StateType.Charge)
            {
                continue;
            }

            item.ChangeStackCount(item.StackCount + count);
            return;
        }

        state.owner.AddState(StateType.Charge, state.owner, 99, count);
    }
}

public class ResistStateBehavior : StateBehaviorBase
{
    public override float GetIncomingDamageMultiplier(bool isDotDamage, bool isTrueDamage)
    {
        return state.baseExtraData1;
    }

    public override void OnCombatEventTriggered(UnitCombatant triggerUnit, StateCombatEventType eventType, IReadOnlyList<UnitCombatant> damagedUnits)
    {
        if (eventType != StateCombatEventType.DamageSkillUsed || state.StackCount <= 0)
        {
            return;
        }

        if (damagedUnits == null)
        {
            return;
        }

        foreach (var unit in damagedUnits)
        {
            if (unit == state.owner)
            {
                state.ChangeStackCount(state.StackCount - 1);
                return;
            }
        }
    }
}

public class TauntStateBehavior : StateBehaviorBase
{
    /// <summary>
    /// 嘲讽单位选目标：施加者（盾手）仍在场上则强制锁定；否则清除嘲讽并返回 false，由调用方走正常随机。
    /// </summary>
    public static bool TryResolveForcedTarget(Enemy attacker, out Character forcedTarget)
    {
        forcedTarget = null;
        if (attacker == null)
        {
            return false;
        }

        State tauntState = attacker.GetState(StateType.Taunt);
        if (tauntState == null)
        {
            return false;
        }

        Character tauntGiver = tauntState.giver as Character;
        if (IsValidFieldTarget(tauntGiver))
        {
            forcedTarget = tauntGiver;
            return true;
        }

        attacker.RemoveState(tauntState);
        return false;
    }

    private static bool IsValidFieldTarget(Character character)
    {
        if (character == null || character.IsDead || CharacterManager.Instance == null)
        {
            return false;
        }

        return CharacterManager.Instance.fieldCharacters.Contains(character);
    }
}

public class NextActionDamageBoostStateBehavior : StateBehaviorBase//盾手专用增伤
{
    public override float GetOutgoingDamageMultiplier(bool isDotDamage)
    {
        return state.baseExtraData1;
    }

    public override void OnOwnerTurnEnd()
    {
        state.ChangeStackCount(state.StackCount - 1);
    }
}

public class ActionWeakenedStateBehavior : StateBehaviorBase//盾手专用减伤
{
    public override float GetOutgoingDamageMultiplier(bool isDotDamage)
    {
        return state.baseExtraData1;
    }

    public override void OnOwnerTurnEnd()
    {
        state.ChangeStackCount(state.StackCount - 1);
    }
}

public class ChargeStateBehavior : StateBehaviorBase
{
    private const float BurstDamageSkillBase = 20f;
    private const float BurstShieldTargetCapRatio = 0.3f;

    public static event Action<UnitCombatant> OnCounterChargeTriggered;
    public override void OnStackChange()
    {
        int threshold = Mathf.RoundToInt(state.baseExtraData2);
        if (state.StackCount >= threshold)
        {
            state.ChangeStackCount(0);
            TriggerCounterCharge();
        }
    }

    private void TriggerCounterCharge()
    {
        if (state.owner == null)
        {
            return;
        }

        if (state.owner is Character && CharacterManager.Instance != null)
        {
            float shieldHpRatio = state.baseExtraData1;
            float fixedShieldScale = state.baseExtraData3;
            float enemyDelayRatio = state.baseExtraData4 > 0f ? state.baseExtraData4 : 0.2f;
            foreach (var ally in CharacterManager.Instance.fieldCharacters)
            {
                if (ally == null)
                {
                    continue;
                }

                int shieldValue = CalculateBurstShieldGrant(state.owner, ally, shieldHpRatio, fixedShieldScale, BurstShieldTargetCapRatio);
                if (shieldValue > 0)
                {
                    ally.AddShield(shieldValue);
                }
            }

            if (EnemyManager.Instance != null)
            {
                foreach (var enemy in EnemyManager.Instance.AliveEnemies)
                {
                    if (enemy == null)
                    {
                        continue;
                    }

                    enemy.TakeDamage(DamageCounter.CountDamage(
                        state.owner,
                        enemy,
                        state.skillCoef,
                        BurstDamageSkillBase,
                        DamageType.Physical,
                        false,
                        false,
                        true));
                    enemy.ChangeActionValue(enemy.currentActionValue + enemy.BaseActionValue * enemyDelayRatio);
                }

                State.NotifyDamageSkillUsed(state.owner, new List<UnitCombatant>(EnemyManager.Instance.AliveEnemies));
            }
        }

        TurnManager.Instance?.ExtraTurnInsert(state.owner as Character);
        FloatingTipGenerator.Instance?.ShowTipAtObject(state.owner.transform, $"{state.owner.name}触发蓄势逆击");
        OnCounterChargeTriggered?.Invoke(state.owner);
    }

    private static int CalculateBurstShieldGrant(UnitCombatant owner, UnitCombatant target, float hpRatio, float fixedScale, float perTargetCapRatio)
    {
        if (owner == null || target == null)
        {
            return 0;
        }

        int rawShield = Mathf.RoundToInt(owner.maxHP * hpRatio + fixedScale);
        int perTargetCap = Mathf.RoundToInt(target.maxHP * perTargetCapRatio);
        return Mathf.Min(rawShield, perTargetCap);
    }
}

public class AttractStateBehavior : StateBehaviorBase
{
    public override float GetAttractMultiplier(UnitCombatant source)
    {
        return state.baseExtraData1;
    }
}

public class PoisonStateBehavior : DotStateBehaviorBase
{
}

/// <summary>
/// 混沌整合状态：层数 = 混沌值(1-5)
/// - 1-2层：每层减少10%造成伤害
/// - 3-4层：每层增加20点行动冷却（合计-20%~-40%伤害、+20~+40冷却）
/// - 5层：眩晕1回合并扣除当前生命值30%（不消耗护盾），下回合混沌值重置为2层
/// </summary>
public class ChaosStateBehavior : StateBehaviorBase
{
    private const float ChaosBurstCurrentHpLossRatio = 0.3f;

    private bool m_pendingChaosReset;

    public override float GetOutgoingDamageMultiplier(bool isDotDamage)
    {
        // 每层减少10%伤害
        float multiplier = 1f - state.StackCount * 0.1f;
        return Mathf.Max(0f, multiplier);
    }

    public override int GetSpeedExtraNum()
    {
        // 3-4层：每层+20行动冷却；1-2层不加；5层不加（因为眩晕不行动）
        int stacks = state.StackCount;
        if (stacks >= 3 && stacks <= 4)
        {
            return stacks * 20;
        }
        return 0;
    }

    public override bool CanActThisTurn()
    {
        // 5层时眩晕
        return state.StackCount < 5;
    }

    public override IEnumerator OnOwnerTurnStart()
    {
        var cha = state.owner as Character;
        if (cha == null || cha.IsDead)
        {
            yield break;
        }

        if (state.StackCount >= 5 && !m_pendingChaosReset)
        {
            int hpLoss = Mathf.RoundToInt(cha.currentHP * ChaosBurstCurrentHpLossRatio);
            if (hpLoss > 0)
            {
                cha.TakeDamage(new UnitCombatant.DamageInfo(hpLoss, cha)
                    .AsTrueDamage()
                    .BypassingShield()
                    .WithState(StateType.Chaos));
            }

            m_pendingChaosReset = true;
            FloatingTipGenerator.Instance?.ShowTipAtObject(cha.transform, $"{cha.name}混沌爆发，扣除{hpLoss}生命");
            yield break;
        }

        if (m_pendingChaosReset)
        {
            m_pendingChaosReset = false;
            cha.SetChaos(2);
            FloatingTipGenerator.Instance?.ShowTipAtObject(cha.transform, $"{cha.name}眩晕解除，混沌回落至2");
        }

        yield break;
    }
}
public class WeakenedStateBehavior : StateBehaviorBase
{
    public override float GetOutgoingDamageMultiplier(bool isDotDamage)
    {
        state.ChangeStackCount(state.StackCount - 1);
        return state.baseExtraData1;
    }
}
public class ChessKingMarkStateBehavior : StateBehaviorBase
{
    public override void OnStateApply()
    {
        // 王棋：自身伤害+30%，受到伤害+30%
    }

    public override float GetOutgoingDamageMultiplier(bool isDotDamage)
    {
        return 1.3f;
    }

    public override float GetIncomingDamageMultiplier(bool isDotDamage, bool isTrueDamage)
    {
        return 1.3f;
    }

    public override void OnOwnerSwappedOut(UnitCombatant newOwner)
    {
        // 角色被换下时，将王棋状态转移到新角色
        if (newOwner != null && state.owner != null)
        {
            newOwner.AddState(StateType.ChessKingMark, state.giver, 99, state.RemainingTurns);
            state.EndState();
        }
    }
}

public class ChessRookMarkStateBehavior : StateBehaviorBase
{
    public override void OnStateApply()
    {
        // 车棋：自身受到伤害-30%
    }

    public override float GetIncomingDamageMultiplier(bool isDotDamage, bool isTrueDamage)
    {
        return 0.7f;
    }

    public override void OnOwnerSwappedOut(UnitCombatant newOwner)
    {
        // 角色被换下时，将车棋状态转移到新角色
        if (newOwner != null && state.owner != null)
        {
            newOwner.AddState(StateType.ChessRookMark, state.giver, 99, state.RemainingTurns);
            state.EndState();
        }
    }
}

public class ChessExhaustionStateBehavior : StateBehaviorBase
{
    public override bool CanActThisTurn()
    {
        return false;
    }
}

/// <summary>
/// 威望：每层提供 baseExtraData2 输出倍率与 baseExtraData1 承伤减免（默认各 10%）。
/// </summary>
public class PrestigeStateBehavior : StateBehaviorBase
{
    public override float GetOutgoingDamageMultiplier(bool isDotDamage)
    {
        float perStackBonus = state.baseExtraData2 > 0f ? state.baseExtraData2 : 0.1f;
        return 1f + state.StackCount * perStackBonus;
    }

    public override float GetIncomingDamageMultiplier(bool isDotDamage, bool isTrueDamage)
    {
        float reductionPerStack = state.baseExtraData1 > 0f ? state.baseExtraData1 : 0.1f;
        return 1f - Mathf.Min(1f, state.StackCount * reductionPerStack);
    }
}

public class DragonBreathStateBehavior : StateBehaviorBase
{
    public override IEnumerator OnOwnerTurnStart()
    {
        TriggerDragonBreathDot();
        DecayStacksAfterTrigger();
        yield return new WaitForSeconds(0.1f);
    }

    public override void DotTrigger(float damageMultiplier)
    {
        TriggerDragonBreathDot(damageMultiplier);
        DecayStacksAfterTrigger();
    }

    public override void OnOwnerSwappedOut(UnitCombatant newOwner)
    {
        state.EndState();
    }

    private void TriggerDragonBreathDot(float damageMultiplier = 1f)
    {
        if (state.owner == null || state.giver == null || state.StackCount <= 0)
        {
            return;
        }

        float coef = state.skillCoef > 0f ? state.skillCoef : 0.3f;
        var damageInfo = DamageCounter.CountDotDamage(state, state.giver, state.owner);
        damageInfo.Damage = Mathf.RoundToInt(damageInfo.Damage * state.StackCount * damageMultiplier);
        state.owner.TakeDamage(damageInfo);
    }

    private void DecayStacksAfterTrigger()
    {
        if (state.StackCount <= 1)
        {
            state.EndState();
            return;
        }

        state.ChangeStackCount(state.StackCount - 1);
    }
}

public class EternalFlameStateBehavior : StateBehaviorBase
{
    public override bool CanBePurged()
    {
        return false;
    }

    public override IEnumerator OnOwnerTurnStart()
    {
        TriggerEternalFlameDot();
        yield return new WaitForSeconds(0.1f);
    }

    public override void DotTrigger(float damageMultiplier)
    {
        TriggerEternalFlameDot(damageMultiplier);
    }

    public override void OnOwnerSwappedOut(UnitCombatant newOwner)
    {
        state.EndState();
    }

    private void TriggerEternalFlameDot(float damageMultiplier = 1f)
    {
        if (state.owner == null || state.giver == null || state.StackCount <= 0)
        {
            return;
        }

        float coef = state.skillCoef > 0f ? state.skillCoef : 0.5f;
        float scaledCoef = coef * state.StackCount * damageMultiplier;
        var damageInfo = DamageCounter.CountDamage(state.giver, state.owner, scaledCoef, 0f, DamageType.Physical, false, false, false);
        damageInfo = damageInfo.AsDot();
        state.owner.TakeDamage(damageInfo);
    }
}

// ============ 西洋剑客状态行为 ============

public class SwordsmanBrightSwordBehavior : StateBehaviorBase
{
    public override float GetOutgoingDamageMultiplier(bool isDotDamage) { return 1.3f; }
    public override float GetIncomingDamageMultiplier(bool isDotDamage, bool isTrueDamage) { return 1.3f; }
}

public class SwordsmanDefenseBehavior : StateBehaviorBase
{
    public override float GetIncomingDamageMultiplier(bool isDotDamage, bool isTrueDamage)
    {
        return state.baseExtraData1 > 0f ? state.baseExtraData1 : 0.7f;
    }

    public override float GetSpeedMultiplier()
    {
        return state.baseExtraData2 > 0f ? state.baseExtraData2 : 1.2f;
    }
}

public class SwordsmanGuerrillaBehavior : StateBehaviorBase
{
    public override float GetOutgoingDamageMultiplier(bool isDotDamage) { return 1f; }
    public override float GetIncomingDamageMultiplier(bool isDotDamage, bool isTrueDamage) { return 1f; }

    public override float GetSpeedMultiplier()
    {
        return state.baseExtraData1 > 0f ? state.baseExtraData1 : 0.8f;
    }
}

public class SwordsmanLastStandBehavior : StateBehaviorBase
{
    public override float GetOutgoingDamageMultiplier(bool isDotDamage) { return 1.5f; }
    public override float GetIncomingDamageMultiplier(bool isDotDamage, bool isTrueDamage) { return 1.5f; }
}

public class SwordsmanStaggerBehavior : StateBehaviorBase
{
    public override float GetIncomingDamageMultiplier(bool isDotDamage, bool isTrueDamage) { return 1.4f; }
    public override bool CanActThisTurn() { return false; }

    public override void OnStateEnd()
    {
        // 失衡结束时通知剑客
        SwordsmanEnemy swordsman = state.owner as SwordsmanEnemy;
        swordsman?.ExitStagger();
    }
}

public class SwordsmanEleganceBehavior : StateBehaviorBase
{
    public override float GetIncomingDamageMultiplier(bool isDotDamage, bool isTrueDamage)
    {
        SwordsmanEnemy swordsman = state.owner as SwordsmanEnemy;
        if (swordsman != null && swordsman.IsInStagger) return 1f;
        return state.baseExtraData1 > 0f ? state.baseExtraData1 : 0.6f;
    }

    public override void OnCombatEventTriggered(UnitCombatant triggerUnit, StateCombatEventType eventType, IReadOnlyList<UnitCombatant> damagedUnits)
    {
        if (eventType != StateCombatEventType.DotTriggered) return;
        if (state.owner == null || damagedUnits == null) return;

        SwordsmanEnemy swordsman = state.owner as SwordsmanEnemy;
        if (swordsman == null) return;

        // 检查剑客是否在本批Dot伤害的目标中
        bool isSwordsmanDamaged = false;
        for (int i = 0; i < damagedUnits.Count; i++)
        {
            if (damagedUnits[i] == swordsman)
            {
                isSwordsmanDamaged = true;
                break;
            }
        }

        if (isSwordsmanDamaged)
        {
            swordsman.ReduceTenacityByDot();
        }
    }
}

public class InspectorReadOnlyAttribute : PropertyAttribute
{
    // 这个类不需要额外代码，只是作为一个标记存在
}