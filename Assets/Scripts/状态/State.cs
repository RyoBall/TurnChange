using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum StateType
{
    Burn,
    StrengthenSelf,
    // 可以根据需要添加更多状态类型
}
[CreateAssetMenu(fileName = "State", menuName = "状态/新状态")]
public class State : ScriptableObject
{
    [Header("状态配置")]
    [Tooltip("默认持续回合数")]
    [Min(1)]
    [SerializeField] private int defaultTurns = 1;

    [Tooltip("剩余回合数（运行时）")]
    private int remainingTurns;

    public Combatant Owner { get; private set; }
    public Combatant Giver { get; private set; }
    public int RemainingTurns => remainingTurns;
    [Header("标签")]
    public StateType stateType;
    public bool isDot;
    public bool isBuff;
    [Header("Dot:快照属性")]
    public float atkT;
    public float skillCoefT;
    [Header("Buff:增益倍率")]
    [SerializeField] float buffMultiplier;
    public void Mount(UnitCombatant owner, UnitCombatant giver,float skillCoef, int overrideTurns = -1)//施加
    {
        atkT = giver.attack;
        skillCoefT = skillCoef;
        Owner = owner;
        Giver = giver;
        remainingTurns = overrideTurns > 0 ? overrideTurns : defaultTurns;
        OnStateApply();
    }
    public void UpdateState(int atkT,float extraTurn=0)//刷新持续回合数和快照属性
    {
        if(this.atkT<atkT)
        this.atkT = atkT;
        remainingTurns = Mathf.Max(remainingTurns, defaultTurns + Mathf.RoundToInt(extraTurn));
    }
    public bool TickOnTurnStart()
    {
        OnOwnerTurnStart();
        remainingTurns--;
        return remainingTurns <= 0;
    }

    public void EndState()
    {
        OnStateEnd();
    }

    protected virtual void OnStateApply()
    {
        switch (stateType)
        {
            case StateType.Burn:
                break;
        }
    }

    protected virtual void OnOwnerTurnStart()
    {
        switch (stateType)
        {
            case StateType.Burn:
                DotTrigger();
                break;
        }
    }

    protected virtual void OnStateEnd()
    {
        switch (stateType)
        {
            case StateType.Burn:
                break;
        }
    }
#region Dot相关
    public virtual void DotTrigger(float damageMultiplier = 1f)
    {
        Debug.Log($"状态 {stateType} 触发Dot，剩余回合数: {remainingTurns}");
        if(!isDot)
        {
            return;
        }
        Debug.Log($"状态 {stateType} 造成伤害，伤害倍率: {damageMultiplier}");
        switch (stateType)
        {
            case StateType.Burn:
                UnitCombatant attacker = Giver as UnitCombatant;
                UnitCombatant target = Owner as UnitCombatant;
                int damage = DamageCounter.CountDotDamage(this, attacker, target);
                target.TakeDamage(damage);
                FloatingTipGenerator.Instance.ShowDefaultTip($"{Owner.name}受到{damage}点燃烧伤害");
                break;
        }
    }
#endregion
#region Buff相关
    public virtual float GetBuffMultiplier()
    {
        if (!isBuff)
        {
            return 0f;
        }
        switch (stateType)
        {
            case StateType.StrengthenSelf:
                if(TurnManager.Instance.GetCurrentCombatant()!=Owner)
                return buffMultiplier;
                else
                return 0f;
            default:
                return 0f;
        }
    }
#endregion
    public void ChangeRemainingTurns(int turns)//用于外部强制修改状态回合数
    {
        remainingTurns = turns;
    }
}
