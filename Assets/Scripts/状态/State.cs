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
    ChaosStun//混沌眩晕
}

[CreateAssetMenu(fileName = "State", menuName = "状态/新状态")]
public class State : ScriptableObject
{
    [Header("状态配置")]
    [SerializeField] private StateDurationType durationType = StateDurationType.Turn;

    [Tooltip("默认持续回合数")]
    [Min(1)]
    private int defaultTurns = 1;

    [Tooltip("默认持续行动值")]
    [Min(1)]
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

    public UnitCombatant owner { get; private set; }
    public UnitCombatant giver { get; private set; }
    public int RemainingTurns => remainingTurns;
    public int RemainingActionValue => remainingActionValue;
    public int StackCount => stackCount;
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

    public void Mount(UnitCombatant owner, UnitCombatant giver, float skillCoef, int duration = -1, int overrideStacks = -1)
    {
        atkT = giver != null ? giver.attack : 0f;
        skillCoefT = skillCoef;
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

        ChangeStackCount(overrideStacks > 0 ? overrideStacks : 1);
        Behavior.OnStateApply();
    }

    public void UpdateState(int atkT, int extraDuration, int stacks)
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

        ChangeStackCount(stackCount + stacks);
    }

    public bool TickOnTurnStart()
    {
        Behavior.OnOwnerTurnStart();

        if (durationType != StateDurationType.Turn)
        {
            return false;
        }

        remainingTurns--;
        return remainingTurns <= 0;
    }

    public bool TickByActionValue(int actionValueCost)
    {
        if (durationType != StateDurationType.ActionValue)
        {
            return false;
        }

        remainingActionValue -= Mathf.Max(0, actionValueCost);
        return remainingActionValue <= 0;
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

        remainingTurns += extraTurns;
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
    void OnOwnerTurnStart();
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
}

public abstract class StateBehaviorBase : IStateBehavior
{
    protected State state;

    public virtual void Initialize(State state)
    {
        this.state = state;
    }

    public virtual void OnStateApply() { }
    public virtual void OnOwnerTurnStart() { }
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
}

public static class StateBehaviorFactory
{
    public static IStateBehavior Create(StateType stateType)
    {
        switch (stateType)
        {
            case StateType.BloodContract:
                return new BloodContractStateBehavior();
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

public class BloodContractStateBehavior : StateBehaviorBase
{
    public override void OnOwnerTurnStart()
    {
        if (state.owner == null)
        {
            return;
        }

        int selfDamage = Mathf.CeilToInt(state.owner.maxHP * 0.2f);
        state.owner.TakeDamage(selfDamage, state.owner, false, true);
        FloatingTipGenerator.Instance?.ShowTipAtObject(state.owner.transform, $"{state.owner.name}因血契失去{selfDamage}生命");
    }
}

public class PursuitPunishStateBehavior : StateBehaviorBase
{
    public override void OnAnyDamageSettled(UnitCombatant source, UnitCombatant target, int damage, bool isDotDamage, bool isTrueDamage)
    {
        int add = target == state.owner ? 3 : 1;
        state.ChangeStackCount(state.StackCount + add);
        if (state.StackCount >= 8)
        {
            TriggerCounterCharge();
            state.ChangeStackCount(0);
        }
    }

    public override void OnDebuffApplied(UnitCombatant target, UnitCombatant debuffGiver)
    {
        if(!(target is Enemy))
        {
            return;
        }
        int damage = Mathf.RoundToInt(state.owner.attack * 0.6f);
        target.TakeDamage(damage, state.owner, false, false);
        FloatingTipGenerator.Instance?.ShowTipAtObject(target.transform, $"{state.owner.name}触发追惩，对{target.name}追击");
    }

    public override float GetOutgoingDamageMultiplier(bool isDotDamage)
    {
        if (TurnManager.Instance != null && TurnManager.Instance.GetCurrentCombatant() != state.owner)
        {
            return 1.15f;
        }

        return 1f;
    }

    private void TriggerCounterCharge()
    {
        if (state.owner == null)
        {
            return;
        }

        if (state.owner is Character && CharacterManager.Instance != null)
        {
            int shieldValue = Mathf.RoundToInt(state.owner.maxHP * 0.2f + state.skillCoefT * 100f);
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

public class PersistentTormentStateBehavior : StateBehaviorBase
{
    public override void OnOwnerTurnStart()
    {
        if (state.owner == null)
        {
            return;
        }

        int validLayer = Mathf.Clamp(state.StackCount, 0, 5);
        float chance = Mathf.Min(0.5f, validLayer * 0.1f);
        if (Random.value <= chance)
        {
            state.owner.AddState(StateType.Daze, state.giver != null ? state.giver : state.owner, 1, 1);
            FloatingTipGenerator.Instance?.ShowTipAtObject(state.owner.transform, $"{state.owner.name}受到震慑");
        }
    }

    public override float GetOutgoingDamageMultiplier(bool isDotDamage)
    {
        return 1f - Mathf.Min(0.25f, 0.05f * state.StackCount);
    }
}

public abstract class DotStateBehaviorBase : StateBehaviorBase
{
    public override void OnOwnerTurnStart()
    {
        TriggerConfiguredDotDamage();
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
                int damage = DamageCounter.CountDotDamage(state, state.giver, state.owner);
                damage = Mathf.RoundToInt(damage * damageMultiplier);
                state.owner.TakeDamage(damage, state.giver, true, false);
                FloatingTipGenerator.Instance?.ShowTipAtObject(state.owner.transform, $"{state.owner.name}受到{damage}点持续伤害");
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
        return 1.4f;
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
        if (!isDotDamage || EnemyManager.Instance == null)
        {
            return;
        }

        int finalDamage = (int)(damage * 0.25f);
        foreach (var enemy in EnemyManager.Instance.AliveEnemies)
        {
            if (enemy == null || enemy == target)
            {
                continue;
            }

            enemy.TakeDamage(finalDamage, source, false, true);
        }
    }

    public override float GetIncomingDamageMultiplier(bool isDotDamage, bool isTrueDamage)
    {
        return isDotDamage ? 1.25f : 1f;
    }
}

public class CounterChargeStateBehavior : StateBehaviorBase
{
    public override void OnStateApply()
    {
        state.owner.AddState(StateType.Charge, state.owner, 99, 4);
    }

    public override void OnAnyDamageSettled(UnitCombatant source, UnitCombatant target, int damage, bool isDotDamage, bool isTrueDamage)
    {
        int count = target == state.owner ? 3 : 1;
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
        return 0.2f;
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

public class ChargeStateBehavior : StateBehaviorBase
{
    public override void OnStackChange()
    {
        if (state.StackCount >= 8)
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
            int shieldValue = Mathf.RoundToInt(state.owner.maxHP * 0.2f + state.skillCoefT * 100f);
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
        return 1.5f;
    }
}

public class PoisonStateBehavior : StateBehaviorBase
{
}

public class ChaosHalfStateBehavior : StateBehaviorBase
{
    public override float GetOutgoingDamageMultiplier(bool isDotDamage)
    {
        return 0.5f;
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
    public override void OnOwnerTurnStart()
    {
        if (hasStunned)
        {
            var cha= state.owner as Character;
            cha?.SetChaos(2);
            state.EndState();
        }
    }
}

public class InspectorReadOnlyAttribute : PropertyAttribute
{
    // 这个类不需要额外代码，只是作为一个标记存在
}