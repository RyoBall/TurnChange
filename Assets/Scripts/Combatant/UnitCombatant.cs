using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    public virtual void TakeDamage(int damage)
    {
        if (damage <= 0)
        {
            return;
        }

        currentHP = Mathf.Max(0, currentHP - damage);
        OnDamaged(damage);

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
        DamageTextPool.Instance?.Get().ShowDamage(damage, transform.position);
    }

    public virtual void Die()
    {
        Destroy(gameObject);
    }
    #region 状态相关
    [Header("状态列表")]
    [SerializeField] private List<State> states = new List<State>();

    public IReadOnlyList<State> States => states;

    public State AddState(StateType stateType, UnitCombatant giver,float skillCoef,int overrideTurns)
    {
        //先检测是否已经有对应的Dot状态，如果有则刷新持续回合数并返回
        foreach(var tstate in states)
        {
            if(tstate.stateType==stateType)
            {
                tstate.UpdateState(giver.GetAttackDamage(),overrideTurns);
                return tstate;
            }
        }
        State state = StateDictionaryManager.GetState(stateType);
        states.Add(state);
        state.Mount(this, giver, skillCoef, overrideTurns);
        return state;
    }

    public bool RemoveState(State state)
    {
        if (state == null)
        {
            return false;
        }

        if (!states.Remove(state))
        {
            return false;
        }

        state.EndState();
        Destroy(state);
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
            Debug.Log($"[ {gameObject.name} 状态 {state.stateType} 持续回合数剩余: {state.RemainingTurns}");
            if (shouldEnd)
            {
                states.RemoveAt(i);
                state.EndState();
                Destroy(state);
            }
        }
    }
    #endregion
}
