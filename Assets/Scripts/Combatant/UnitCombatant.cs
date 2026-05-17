using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MoreMountains.Feedbacks;
public class UnitCombatant : Combatant
{
    [Header("标记")]
    protected bool dead = false;
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
    [SerializeField] protected MMF_Player mouseEnterFeedback;
    [SerializeField] protected MMF_Player mouseExitFeedback;

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
    protected virtual void OnDestroy() { }
    public struct DamageInfo
    {
        public int Damage;
        public UnitCombatant Source;
        public bool IsDotDamage;
        public bool IsTrueDamage;
        public StateType StateType;

        // 便捷构造
        public DamageInfo(int damage, UnitCombatant source = null)
        {
            Damage = damage;
            Source = source;
            IsDotDamage = false;
            IsTrueDamage = false;
            StateType = StateType.None;
        }

        // 链式配置（流畅接口）
        public DamageInfo AsDot(bool isDot = true) { IsDotDamage = isDot; return this; }
        public DamageInfo AsTrueDamage() { IsTrueDamage = true; return this; }
        public DamageInfo WithState(StateType state) { StateType = state; return this; }
    }
    public virtual void TakeDamage(DamageInfo damageInfo)
    {
        if (dead)
        {
            return;
        }
        if (damageInfo.Damage <= 0)
        {
            damageInfo.Damage = 0;
        }
        int finalDamage = damageInfo.Damage;
        //结算盾值
        finalDamage = ConsumeShield(finalDamage);
        hitFeedback?.PlayFeedbacks();
        OnDamaged(finalDamage, damageInfo.IsDotDamage, damageInfo.StateType);
        //如果伤害小于0直接结束
        if (finalDamage <= 0)
        {
            NotifyAnyDamageSettled(damageInfo.Source, this, 0, damageInfo.IsDotDamage, damageInfo.IsTrueDamage);
            return;
        }
        //扣血
        currentHP = Mathf.Max(0, currentHP - finalDamage);
        NotifyAnyDamageSettled(damageInfo.Source, this, finalDamage, damageInfo.IsDotDamage, damageInfo.IsTrueDamage);
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

    protected virtual void OnDamaged(int damage, bool isDotDamage = false, StateType stateType = StateType.None)
    {
        Debug.Log($"[{GetType().Name}] {gameObject.name} 受到 {damage} 点伤害");
        DamageTextPool.Instance?.ShowDamage(damage, transform.position, isDotDamage, StateDictionaryManager.GetStateName(stateType));
    }

    public virtual void Die()
    {
        if (dead)
        {
            return;
        }
        dead = true;
        TurnManager.Instance?.RemoveCombatant(this);
        hitFeedback?.StopFeedbacks();
        dieFeedback?.PlayFeedbacks();
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
    public State AddState(StateType stateType, UnitCombatant giver, int duration, int stacks = 1, float skillCoef = 1f)
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
        state.Mount(this, giver, skillCoef, duration, stacks);

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

    public IEnumerator ProcessStatesOnTurnStart()
    {
        for (int i = states.Count - 1; i >= 0; i--)
        {
            State state = states[i];
            if (state == null)
            {
                states.RemoveAt(i);
                continue;
            }

            yield return state.TickOnTurnStart();
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

            state.TickByActionValue(actionValueCost);
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

    public static void NotifyAnyDamageSettled(UnitCombatant source, UnitCombatant target, int damage, bool isDotDamage = false, bool isTrueDamage = false)
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

                state.OnAnyDamageSettled(source, target, damage, isDotDamage, isTrueDamage);
            }
        }
    }
    #endregion
}
