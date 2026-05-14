using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MoreMountains.Feedbacks;
public class UnitCombatant : Combatant
{

    [Header("属性")]
    public int level;//先写在这里，理论上应该写在角色管理器
    public int maxHP;
    public int currentHP;
    public float attack;
    public int defense;
    public float critRate;
    public float critDamage;
    public float K;

    [Header("防护")]
    public int currentShield;
    [Header("MMF引用")]
    [SerializeField] protected MMF_Player enterFeedback;
    [SerializeField] protected MMF_Player hitFeedback;
    [SerializeField] protected MMF_Player actionFeedback;
    [SerializeField] protected MMF_Player dieFeedback;

    protected virtual void Awake()
    {
        if (maxHP > 0 && currentHP <= 0)
        {
            currentHP = maxHP;
        }
    }

    public virtual int GetAttackDamage()
    {
        return Mathf.RoundToInt(attack);
    }
    protected virtual void OnDestroy(){}
    public virtual void TakeDamage(int damage, UnitCombatant source = null, bool isDotDamage = false, bool isTrueDamage = false)
    {
        if (damage <= 0)
        {
            return;
        }

        int finalDamage = damage;
        //结算盾值
        finalDamage = ConsumeShield(finalDamage);
        //如果伤害小于0直接结束
        if (finalDamage <= 0)
        {
            NotifyAnyDamageSettled(source, this, 0);
            return;
        }
        //扣血
        currentHP = Mathf.Max(0, currentHP - finalDamage);
        OnDamaged(finalDamage);
        hitFeedback?.PlayFeedbacks();
        NotifyAnyDamageSettled(source, this, finalDamage,isDotDamage,isTrueDamage);
        //检查血量
        if (currentHP <= 0)
        {
            Die();
        }
    }

    public virtual void Heal(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentHP = Mathf.Min(maxHP, currentHP + amount);
    }

    protected virtual void OnDamaged(int damage)
    {
        Debug.Log($"[{GetType().Name}] {gameObject.name} 受到 {damage} 点伤害");
        DamageTextPool.Instance?.ShowDamage(damage, transform.position);
    }

    public virtual void Die()
    {
        TurnManager.Instance?.RemoveCombatant(this);
        Destroy(gameObject);
    }

    public virtual void AddShield(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentShield += amount;
    }



    #region 状态相关
    [Header("状态列表")]
    [SerializeField] protected List<State> states = new List<State>();

    public List<State> States => states;
    public State AddState(StateType stateType, UnitCombatant giver, int duration,int stacks=1,float skillCoef = 1f)
    {
        foreach (var tstate in states)
        {
            if (tstate != null && tstate.stateType == stateType)
            {

                tstate.UpdateState(giver != null ? giver.GetAttackDamage() : 0, duration, stacks);

                if (tstate.isDebuff)
                {
                    NotifyDebuffApplied(this, giver);
                }

                return tstate;
            }
        }

        State stateTemplate = StateDictionaryManager.GetState(stateType);
        if (stateTemplate == null)
        {
            return null;
        }

        State state = Instantiate(stateTemplate);
        state.name = stateTemplate.name;
        states.Add(state);
        state.Mount(this, giver, skillCoef, duration,stacks);

        if (state.isDebuff)
        {
            NotifyDebuffApplied(this, giver);
        }

        return state;
    }


    public bool RemoveState(State state)
    {
        if (state == null)
        {
            return false;
        }
        
        state.EndState();
        return true;
    }

    public void ProcessStatesOnTurnStart()
    {
        for (int i = states.Count - 1; i >= 0; i--)
        {
            State state = states[i];
            if (state == null)
            {
                states.RemoveAt(i);
                continue;
            }

            bool shouldEnd = state.TickOnTurnStart();
            if (state.DurationType == StateDurationType.Turn)
            {
                Debug.Log($"[ {gameObject.name} 状态 {state.stateType} 持续回合数剩余: {state.RemainingTurns}");
            }
            else
            {
                Debug.Log($"[ {gameObject.name} 状态 {state.stateType} 持续行动值剩余: {state.RemainingActionValue}");
            }

            if (shouldEnd)
            {
                state.EndState();
            }
        }
    }

    public void ProcessStatesByActionValue(int actionValueCost)
    {
        for (int i = states.Count - 1; i >= 0; i--)
        {
            State state = states[i];
            if (state == null)
            {
                states.RemoveAt(i);
                continue;
            }

            bool shouldEnd = state.TickByActionValue(actionValueCost);
            if (shouldEnd)
            {
                state.EndState();
            }
        }
    }
    #endregion
    #region 状态接口
    private int ConsumeShield(int damage)//盾值结算
    {
        if (currentShield <= 0 || damage <= 0)
        {
            return damage;
        }

        int absorbed = Mathf.Min(currentShield, damage);
        currentShield -= absorbed;
        return damage - absorbed;
    }
    public float GetOutgoingDamageMultiplier(bool isDotDamage)
    {
        float multiplier = 1f;
        for (int i = 0; i < states.Count; i++)
        {
            State state = states[i];
            if (state == null)
            {
                continue;
            }

            multiplier *= state.GetOutgoingDamageMultiplier(isDotDamage);
        }

        return Mathf.Max(0f, multiplier);
    }

    public float GetIncomingDamageMultiplier(bool isDotDamage, bool isTrueDamage)
    {
        float multiplier = 1f;
        for (int i = 0; i < states.Count; i++)
        {
            State state = states[i];
            if (state == null)
            {
                continue;
            }

            multiplier *= state.GetIncomingDamageMultiplier(isDotDamage, isTrueDamage);
        }

        return Mathf.Max(0f, multiplier);
    }

    public bool CanActThisTurn()
    {
        for (int i = 0; i < states.Count; i++)
        {
            State state = states[i];
            if (state == null)
            {
                continue;
            }

            if (!state.CanActThisTurn())
            {
                return false;
            }
        }

        return true;
    }

    public bool HasState(StateType stateType)
    {
        return GetState(stateType) != null;
    }

    public State GetState(StateType stateType)
    {
        for (int i = 0; i < states.Count; i++)
        {
            if (states[i] != null && states[i].stateType == stateType)
            {
                return states[i];
            }
        }

        return null;
    }

    public static void NotifyDebuffApplied(UnitCombatant target, UnitCombatant debuffGiver)
    {
        foreach (var com in TurnManager.Instance.CurrentTurnOrder.ToList())
        {
            var unit = com as UnitCombatant;
            if (unit == null)
            {
                continue;
            }

            for (int i = 0; i < unit.states.Count; i++)
            {
                State state = unit.states[i];
                if (state == null)
                {
                    continue;
                }

                state.OnDebuffApplied(target, debuffGiver);
            }
        }
    }

    public static void NotifyAnyDamageSettled(UnitCombatant source, UnitCombatant target, int damage,bool isDotDamage=false,bool isTrueDamage=false)
    {
        foreach (var com in TurnManager.Instance.CurrentTurnOrder.ToList())
        {
            var unit = com as UnitCombatant;
            if (unit == null)
            {
                continue;
            }

            for (int i = 0; i < unit.states.Count; i++)
            {
                State state = unit.states[i];
                if (state == null)
                {
                    continue;
                }

                state.OnAnyDamageSettled(source, target, damage,isDotDamage,isTrueDamage);
            }
        }
    }
    #endregion
}
