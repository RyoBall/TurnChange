using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

public enum StateDurationType
{
    Turn,
    ActionValue,
    Special
}

public enum StateType
{
        BloodContract,//血契
    PursuitPunish,//追惩
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
    ChaosHalf,//混沌半效
    ChaosStun,//混沌眩晕
    None,
    BerserkFeast,//狂暴盛宴
    BurningBlood,//燃血
    DeadlyArmor,//致命穿甲
    BloodBath,//浴血
    Vulnerable,//易伤
    ArmorBreak,//破甲
    BloodSurgeHeal,//浴血反哺
    CriticalGuard,//临界
    BloodGift,//血赐
    GiftWeak,//馈赠·弱
    GiftMid,//馈赠·中
    GiftStrong,//馈赠·强
    ExploderProcess,//自爆流程
    NextActionDamageBoost,//下次行动增伤
    ActionWeakened//单回合减攻

}

[CreateAssetMenu(fileName = "State", menuName = "状态/新状态")]
public class State : ScriptableObject
{
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

    [Header("显示与基础配置")]
    [TextArea(2, 5)] public string description;
    public float baseMultiplier;
    public float baseExtraData1;
    public float baseExtraData2;
    public float baseExtraData3;

    [Header("Dot:快照属性")]
    [InspectorReadOnly] public float atkT;
    [InspectorReadOnly] public float skillCoefT;

    [Header("Buff:增益倍率")]
    [SerializeField, InspectorReadOnly] private float buffMultiplier;

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

    public void Mount(
        UnitCombatant owner,
        UnitCombatant giver,
        float skillCoef,
        int duration = -1,
        int stacks = -1)
    {
        atkT = giver != null ? giver.attack : 0f;
        skillCoefT = skillCoef > 0f ? skillCoef : baseMultiplier;
        this.owner = owner;
        this.giver = giver;

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
        float skillCoef = 0f)
    {
        if (this.atkT < atkT)
        {
            this.atkT = atkT;
        }

        if (skillCoef > this.skillCoefT)
        {
            this.skillCoefT = skillCoef;
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

        ChangeStackCount(stackCount + extraStacks);
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

    public bool TryConsumeResist()
    {
        return Behavior.TryConsumeResist();
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
    void Initialize(State state);
    void OnStateApply();
    IEnumerator OnOwnerTurnStart();
    void OnOwnerTurnEnd();
    void OnStateEnd();
    void OnAnyDamageSettled(UnitCombatant source, UnitCombatant target, int damage, bool isDotDamage, bool isTrueDamage);
    void OnDebuffApplied(UnitCombatant target, UnitCombatant debuffGiver);
    float GetIncomingDamageMultiplier(bool isDotDamage, bool isTrueDamage);
    float GetOutgoingDamageMultiplier(bool isDotDamage);
    float GetAttractMultiplier(UnitCombatant source);
    bool CanActThisTurn();
    void OnStackChange();
    bool TryConsumeResist();
    void DotTrigger(float damageMultiplier);
    bool CausesOutgoingTrueDamage(bool isDotDamage);
}

public abstract class StateBehaviorBase : IStateBehavior
{
    protected State state;

    public virtual void Initialize(State state)
    {
        this.state = state;
    }

    public virtual void OnStateApply() { }
    public virtual IEnumerator OnOwnerTurnStart() { yield break; }//由于这是回合开始的行为，最好支持协程，以便实现一些需要等待的效果
    public virtual void OnOwnerTurnEnd() { }
    public virtual void OnStateEnd() { }
    public virtual void OnAnyDamageSettled(UnitCombatant source, UnitCombatant target, int damage, bool isDotDamage, bool isTrueDamage) { }
    public virtual void OnDebuffApplied(UnitCombatant target, UnitCombatant debuffGiver) { }
    public virtual float GetIncomingDamageMultiplier(bool isDotDamage, bool isTrueDamage) { return 1f; }
    public virtual float GetOutgoingDamageMultiplier(bool isDotDamage) { return 1f; }
    public virtual float GetAttractMultiplier(UnitCombatant source) { return 1f; }
    public virtual bool CanActThisTurn() { return true; }
    public virtual void OnStackChange() { }
    public virtual bool TryConsumeResist() { return false; }
    public virtual void DotTrigger(float damageMultiplier) { }
    public virtual bool CausesOutgoingTrueDamage(bool isDotDamage) { return false; }
}

public static class StateBehaviorFactory
{
    public static IStateBehavior Create(StateType stateType)
    {
        switch (stateType)
        {
            case StateType.BloodContract:
                return new BloodContractStateBehavior();
            case StateType.BerserkFeast:
                return new BerserkFeastStateBehavior();
            case StateType.BurningBlood:
                return new BurningBloodStateBehavior();
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
            case StateType.CriticalGuard:
                return new CriticalGuardStateBehavior();
            case StateType.BloodGift:
                return new BloodGiftStateBehavior();
            case StateType.GiftWeak:
                return new GiftWeakStateBehavior();
            case StateType.GiftMid:
                return new GiftMidStateBehavior();
            case StateType.GiftStrong:
                return new GiftStrongStateBehavior();
            case StateType.ExploderProcess:
                return new ExploderProcessStateBehavior();
            case StateType.NextActionDamageBoost:
                return new NextActionDamageBoostStateBehavior();
            case StateType.ActionWeakened:
                return new ActionWeakenedStateBehavior();
            case StateType.PursuitPunish:
                return new PursuitPunishStateBehavior();
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
            case StateType.ChaosHalf:
                return new ChaosHalfStateBehavior();
            case StateType.ChaosStun:
                return new ChaosStunStateBehavior();
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

        float healRatio = state.skillCoefT > 0f ? state.skillCoefT : 0.2f;
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

        critBonus = state.skillCoefT > 0f ? state.skillCoefT : 0.25f;
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

        float selfDamageRatio = state.skillCoefT > 0f ? state.skillCoefT : 0.25f;
        int selfDamage = Mathf.RoundToInt(state.owner.maxHP * selfDamageRatio);
        state.owner.TakeDamage(new UnitCombatant.DamageInfo(selfDamage, state.owner).AsTrueDamage());
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

public class DeadlyArmorStateBehavior : StateBehaviorBase
{
    public override float GetOutgoingDamageMultiplier(bool isDotDamage)
    {
        return state.skillCoefT > 0f ? state.skillCoefT : 1.4f;
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

        float teamAdvanceRatio = state.baseExtraData1 > 0f ? state.baseExtraData1 : 0f;
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
        return state.skillCoefT > 0f ? state.skillCoefT : 1.25f;
    }

    public override void OnAnyDamageSettled(UnitCombatant source, UnitCombatant target, int damage, bool isDotDamage, bool isTrueDamage)
    {
        if (target != state.owner || damage <= 0)
        {
            return;
        }

        state.ChangeStackCount(state.StackCount - 1);
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
        float reductionPerStack = state.skillCoefT > 0f ? state.skillCoefT : 0.2f;
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
    public override void OnAnyDamageSettled(UnitCombatant source, UnitCombatant target, int damage, bool isDotDamage, bool isTrueDamage)
    {
        if (state.owner == null || source != state.owner || damage <= 0 || state.StackCount <= 0)
        {
            return;
        }

        float healRatio = state.skillCoefT > 0f ? state.skillCoefT : 0.2f;
        float critBonusRatio = state.baseExtraData1 > 0f ? state.baseExtraData1 : 0.1f;
        int heal = Mathf.RoundToInt(state.owner.maxHP * healRatio);
        bool critHeal = UnityEngine.Random.value < Mathf.Clamp01(state.owner.critRate);
        if (critHeal)
        {
            heal += Mathf.RoundToInt(state.owner.maxHP * critBonusRatio);
        }

        state.owner.Heal(heal);
        state.ChangeStackCount(state.StackCount - 1);
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

        float hpThreshold = state.baseExtraData1 > 0f ? state.baseExtraData1 : 0.5f;
        float damageMultiplier = state.skillCoefT > 0f ? state.skillCoefT : 1.2f;
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

        float healRatio = state.baseExtraData2 > 0f ? state.baseExtraData2 : 0.3f;
        int heal = state.giver != null ? Mathf.RoundToInt(state.giver.maxHP * healRatio) : Mathf.RoundToInt(state.owner.maxHP * healRatio);
        state.owner.currentHP = Mathf.Clamp(heal, 1, state.owner.maxHP);
        usedFatalHeal = true;
    }
}

public class BloodGiftStateBehavior : StateBehaviorBase
{
    public override float GetOutgoingDamageMultiplier(bool isDotDamage)
    {
        return state.skillCoefT > 0f ? state.skillCoefT : 1.2f;
    }

    public override IEnumerator OnOwnerTurnStart()
    {
        if (state.owner == null || state.giver == null)
        {
            yield break;
        }

        float lowHpThreshold = state.baseExtraData1 > 0f ? state.baseExtraData1 : 0.5f;
        float lowHpHealPercent = state.baseExtraData2 > 0f ? state.baseExtraData2 : 0.30f;
        float normalHealPercent = state.baseExtraData3 > 0f ? state.baseExtraData3 : 0.15f;
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
        return state.skillCoefT > 0f ? state.skillCoefT : 1.2f;
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
        return state.skillCoefT > 0f ? state.skillCoefT : 1.3f;
    }

    public override IEnumerator OnOwnerTurnStart()
    {
        if (state.owner != null)
        {
            float healRatio = state.baseExtraData1 > 0f ? state.baseExtraData1 : 0.1f;
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
        return state.skillCoefT > 0f ? state.skillCoefT : 1.4f;
    }

    public override void OnAnyDamageSettled(UnitCombatant source, UnitCombatant target, int damage, bool isDotDamage, bool isTrueDamage)
    {
        if (triggered || source != state.owner || state.owner == null)
        {
            return;
        }

        float healRatio = state.baseExtraData1 > 0f ? state.baseExtraData1 : 0.2f;
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
        if (!(target is Enemy) || TurnManager.Instance?.GetCurrentCombatant() == state.owner)
        {
            return;
        }
        float pursuitCoef = state.skillCoefT > 0f ? state.skillCoefT : 0.6f;
        var damageInfo = DamageCounter.CountDamage(state.owner, target, pursuitCoef, 0f, false, false, false)
            .WithState(state.stateType);
        target.TakeDamage(damageInfo);
    }

    public override float GetOutgoingDamageMultiplier(bool isDotDamage)
    {
        if (TurnManager.Instance != null && TurnManager.Instance.GetCurrentCombatant() != state.owner)
        {
            float bonus = state.baseExtraData1 > 0f ? state.baseExtraData1 : 0.15f;
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
        float chancePerStack = state.baseExtraData2 > 0f ? state.baseExtraData2 : 0.1f;
        int stunDuration = state.baseExtraData3 > 0f ? Mathf.RoundToInt(state.baseExtraData3) : 1;
        int validLayer = Mathf.Clamp(state.StackCount, 0, maxStacks);
        float chance = Mathf.Min(maxStacks * chancePerStack, validLayer * chancePerStack);
        if (Random.value <= chance)
        {
            state.owner.AddState(StateType.Daze, state.giver != null ? state.giver : state.owner, stunDuration, 1);
            FloatingTipGenerator.Instance?.ShowTipAtObject(state.owner.transform, $"{state.owner.name}受到震慑");
        }
    }

    public override float GetOutgoingDamageMultiplier(bool isDotDamage)
    {
        int maxStacks = Mathf.Max(1, state.MaxStacks);
        float reductionPerStack = state.baseExtraData1 > 0f ? state.baseExtraData1 : 0.05f;
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
        Debug.Log($"状态 {state.stateType} 触发Dot，剩余回合数: {state.RemainingTurns}");
        if (!state.isDot)
        {
            return;
        }

        Debug.Log($"状态 {state.stateType} 造成伤害，伤害倍率: {damageMultiplier}");
        switch (state.stateType)
        {
            case StateType.SeqFlame:
            case StateType.Ice:
            case StateType.Corrosion:
            case StateType.Wind:
                var damageInfo = DamageCounter.CountDotDamage(state, state.giver, state.owner);
                damageInfo.Damage = Mathf.RoundToInt(damageInfo.Damage * damageMultiplier);
                state.owner.TakeDamage(damageInfo);
                break;
        }
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
        return state.skillCoefT > 0f ? state.skillCoefT : 1.4f;
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

            enemy.TakeDamage(new UnitCombatant.DamageInfo(finalDamage, damageSource).AsTrueDamage());
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
        return isDotDamage ? (state.skillCoefT > 0f ? state.skillCoefT : 1.3f) : 1f;
    }
}

public class CounterChargeStateBehavior : StateBehaviorBase
{
    public override void OnStateApply()
    {
        int initialCharge = state.skillCoefT > 0f ? Mathf.RoundToInt(state.skillCoefT) : (state.baseExtraData1 > 0f ? Mathf.RoundToInt(state.baseExtraData1) : 4);
        state.owner.AddState(StateType.Charge, state.owner, 99, initialCharge);
    }

    public override void OnAnyDamageSettled(UnitCombatant source, UnitCombatant target, int damage, bool isDotDamage, bool isTrueDamage)
    {
        int selfGain = state.baseExtraData2 > 0f ? Mathf.RoundToInt(state.baseExtraData2) : 3;
        int otherGain = state.baseExtraData3 > 0f ? Mathf.RoundToInt(state.baseExtraData3) : 1;
        int count = target == state.owner ? selfGain : otherGain;
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
        return state.skillCoefT > 0f ? state.skillCoefT : 0.2f;
    }

    public override bool TryConsumeResist()
    {
        if (state.StackCount <= 0)
        {
            return false;
        }

        state.ChangeStackCount(state.StackCount - 1);
        return true;
    }
}

public class TauntStateBehavior : StateBehaviorBase
{
}

public class NextActionDamageBoostStateBehavior : StateBehaviorBase
{
    public override float GetOutgoingDamageMultiplier(bool isDotDamage)
    {
        return state.skillCoefT > 0f ? state.skillCoefT : 1.2f;
    }

    public override void OnOwnerTurnEnd()
    {
        state.ChangeStackCount(state.StackCount - 1);
    }
}

public class ActionWeakenedStateBehavior : StateBehaviorBase
{
    public override float GetOutgoingDamageMultiplier(bool isDotDamage)
    {
        return state.skillCoefT > 0f ? state.skillCoefT : 0.4f;
    }

    public override void OnOwnerTurnEnd()
    {
        state.ChangeStackCount(state.StackCount - 1);
    }
}

public class ChargeStateBehavior : StateBehaviorBase
{
    public override void OnStackChange()
    {
        int threshold = state.baseExtraData2 > 0f ? Mathf.RoundToInt(state.baseExtraData2) : 8;
        if (state.StackCount >= threshold)
        {
            TriggerCounterCharge();
            state.ChangeStackCount(0);
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
            float shieldHpRatio = state.baseExtraData1 > 0f ? state.baseExtraData1 : 0.2f;
            int shieldValue = Mathf.RoundToInt(state.owner.maxHP * shieldHpRatio + state.skillCoefT * 100f);
            foreach (var ally in CharacterManager.Instance.fieldCharacters)
            {
                if (ally == null)
                {
                    continue;
                }

                ally.AddShield(shieldValue);
            }
        }

        TurnManager.Instance?.ExtraTurnInsert(state.owner as Character);
        FloatingTipGenerator.Instance?.ShowTipAtObject(state.owner.transform, $"{state.owner.name}触发蓄势逆击");
    }
}

public class AttractStateBehavior : StateBehaviorBase
{
    public override float GetAttractMultiplier(UnitCombatant source)
    {
        return state.skillCoefT > 0f ? state.skillCoefT : 1.5f;
    }
}

public class PoisonStateBehavior : DotStateBehaviorBase
{
}

public class ChaosHalfStateBehavior : StateBehaviorBase
{
    public override float GetOutgoingDamageMultiplier(bool isDotDamage)
    {
        return state.skillCoefT > 0f ? state.skillCoefT : 0.5f;
    }
}

public class ChaosStunStateBehavior : StateBehaviorBase
{
    bool hasStunned;
    public override bool CanActThisTurn()
    {
        hasStunned = true;
        return false;
    }
    public override IEnumerator OnOwnerTurnStart()
    {
        if (hasStunned)
        {
            var cha = state.owner as Character;
            int recoverChaosValue = state.baseExtraData1 > 0f ? Mathf.RoundToInt(state.baseExtraData1) : 2;
            cha?.SetChaos(recoverChaosValue);
            state.EndState();
        }
        yield break;
    }
}

public class InspectorReadOnlyAttribute : PropertyAttribute
{
    // 这个类不需要额外代码，只是作为一个标记存在
}