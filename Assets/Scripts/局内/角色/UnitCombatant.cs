using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MoreMountains.Feedbacks;
public class UnitCombatant : Combatant
{
    [Header("标签")]
    [SerializeField] protected bool dead = false;
    public bool IsDead => dead;
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
    [SerializeField] protected MMF_Player hitFeedback;
    [SerializeField] protected MMF_Player dieFeedback;
    [SerializeField] protected MMF_Player healFeedback;
    [SerializeField] protected MMF_Player shieldFeedback;
    [SerializeField] protected MMF_Player buffAppliedFeedback;
    [SerializeField] protected MMF_Player debuffAppliedFeedback;
    [Header("悬停特效")]
    [SerializeField] protected ParticleSystem mouseHoverParticle;

    /// <summary>用于回合顺序条中显示的图标，子类可 override 从 RosterData 中获取</summary>
    public virtual Sprite TurnImageSprite => null;

    protected virtual void Awake()
    {
        CombatantDeathMonitor.Register(this);
        if (maxHP > 0 && currentHP <= 0)
        {
            currentHP = maxHP;
        }
    }

    public virtual int GetAttackDamage()
    {
        return Mathf.RoundToInt(attack);
    }
    protected virtual void OnDestroy()
    {
        CombatantDeathMonitor.Unregister(this);
    }
    public struct DamageInfo
    {
        public int Damage;
        public UnitCombatant Source;
        public bool IsDotDamage;
        public bool IsTrueDamage;
        public bool IsCriticalHit;
        public bool BypassShield;
        public DamageType DamageType;
        public StateType StateType;

        // 便捷构造
        public DamageInfo(int damage, UnitCombatant source = null, DamageType damageType = DamageType.Physical)
        {
            Damage = damage;
            Source = source;
            IsDotDamage = false;
            IsTrueDamage = false;
            IsCriticalHit = false;
            BypassShield = false;
            DamageType = damageType;
            StateType = StateType.None;
        }

        // 链式配置（流畅接口）
        public DamageInfo AsDot(bool isDot = true) { IsDotDamage = isDot; return this; }
        public DamageInfo AsTrueDamage() { IsTrueDamage = true; return this; }
        public DamageInfo AsCriticalHit() { IsCriticalHit = true; return this; }
        public DamageInfo BypassingShield() { BypassShield = true; return this; }
        public DamageInfo WithDamageType(DamageType damageType) { DamageType = damageType; return this; }
        public DamageInfo WithState(StateType state) { StateType = state; return this; }//用于注明伤害来自于哪个状态
    }
    public virtual void TakeDamage(DamageInfo damageInfo)
    {
        if (dead)
        {
            return;
        }
        if (damageInfo.Source != null && damageInfo.Source.DealsTrueDamage(damageInfo.IsDotDamage))
        {
            damageInfo = damageInfo.AsTrueDamage();
        }
        if (damageInfo.Damage <= 0)
        {
            damageInfo.Damage = 0;
        }

        int finalDamage = damageInfo.Damage;
        if (!damageInfo.BypassShield)
        {
            int shieldBefore = currentShield;
            //结算盾值
            finalDamage = ConsumeShield(finalDamage);
            if (shieldBefore > 0 && currentShield <= 0)
            {
                TemporaryBattleModifierRuntimeManager.NotifyShieldBroken(this, damageInfo.Source);
            }
        }
        hitFeedback?.PlayFeedbacks();
        OnDamaged(finalDamage, damageInfo.IsDotDamage, damageInfo.StateType, damageInfo.IsCriticalHit);
        //扣血  
        currentHP = Mathf.Max(0, currentHP - finalDamage);
        GameAudioEvents.Raise(GameAudioEventType.CombatDamage, damageInfo.Source, this, finalDamage);
        TemporaryBattleModifierRuntimeManager.NotifyDamageSettled(damageInfo.Source, this, finalDamage, damageInfo.IsDotDamage, damageInfo.IsTrueDamage, damageInfo.DamageType);
        NotifyAnyDamageSettled(damageInfo.Source, this, finalDamage, damageInfo.IsDotDamage, damageInfo.IsTrueDamage);

        if (damageInfo.Source is Character damageDealer && this is Enemy && finalDamage > 0)
        {
            CombatDamageTracker.RecordDamageDealt(damageDealer, finalDamage);
        }

        // 角色血量变化时发出事件，携带当前血量百分比
        NotifyHealthChanged();
    }

    /// <summary>角色血量变化时通知对话系统（仅对 Character）</summary>
    private void NotifyHealthChanged()
    {
        Character character = this as Character;
        if (character == null || character.IsDead) return;

        float hpRatio = (float)currentHP / maxHP;
        BattleDialogEvents.Raise(new BattleDialogEventData
        {
            EventType = BattleDialogEventType.CharacterHealthChanged,
            RelatedCharacter = character,
            ExtraFloat = hpRatio,
        });
    }
    public virtual void Heal(int amount)
    {
        if (amount <= 0)
        {
            return;
        }
        DamageTextPool.Instance?.ShowHeal(amount, transform.position);
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        healFeedback?.PlayFeedbacks();
        TemporaryBattleModifierRuntimeManager.NotifyUnitHealed(this, amount);
    }

    protected virtual void OnDamaged(int damage, bool isDotDamage = false, StateType stateType = StateType.None, bool isCriticalHit = false)
    {
        Debug.Log($"[{GetType().Name}] {gameObject.name} 受到 {damage} 点伤害");
        DamageTextPool.Instance?.ShowDamage(damage, transform.position, isDotDamage, isCriticalHit);
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

    protected void PlayMouseHoverEffect()
    {
        if (mouseHoverParticle == null)
        {
            return;
        }

        if (!mouseHoverParticle.isPlaying)
        {
            mouseHoverParticle.Play();
        }
    }

    protected void StopMouseHoverEffect()
    {
        if (mouseHoverParticle == null)
        {
            return;
        }

        mouseHoverParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public IEnumerator ExecuteDeathEvent()
    {
        yield return OnDeathEvent();
    }

    public static IEnumerator WaitForPendingDeaths()
    {
        yield return CombatantDeathMonitor.CheckDeathsAndWait();
    }

    protected IEnumerator WaitForDeathEvents()
    {
        yield return WaitForPendingDeaths();
    }

    public virtual void AddShield(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        const float maxTotalShieldRatio = 0.5f;
        int maxTotalShield = Mathf.RoundToInt(maxHP * maxTotalShieldRatio);
        amount = Mathf.Min(amount, Mathf.Max(0, maxTotalShield - currentShield));
        if (amount <= 0)
        {
            return;
        }

        DamageTextPool.Instance?.ShowCustomText($"获得护盾 {amount}", transform.position, Color.cyan);
        shieldFeedback?.PlayFeedbacks();
        currentShield += amount;
        TemporaryBattleModifierRuntimeManager.NotifyShieldAdded(this, amount);
    }

    public override void ChangeActionValue(float delta, bool ifChangePos = true)
    {
        if (dead)
        {
            return;
        }
        base.ChangeActionValue(delta, ifChangePos);
    }

    #region 状态相关
    [Header("状态列表")]
    [SerializeField] protected List<State> states = new List<State>();

    public List<State> States => states;
    protected override float GetSpeed()
    {
        float modifiedSpeed = speed;
        for (int i = 0; i < states.Count; i++)
        {
            State state = states[i];
            if (state == null)
            {
                continue;
            }

            modifiedSpeed *= state.GetSpeedModifier();
            modifiedSpeed += state.GetSpeedExtraNum();
        }

        return Mathf.Max(1f, modifiedSpeed);
    }
    public State AddState(
        StateType stateType,
        UnitCombatant giver,
        int duration,
        int stacks = 1,bool ifChangeStackByExtraStacks = true,float extraData=0f)
    {
        if (!CanReceiveState(stateType, giver))
        {
            return null;
        }

        // 先获取模板以检查 canStack（只读，不需要 Instantiate）
        State stateTemplate = StateDictionaryManager.GetStateTemplate(stateType);
        if (stateTemplate == null)
        {
            return null;
        }

        // 如果不允许重复施加，则查找已有状态进行更新
        if (!stateTemplate.canStack)
        {
            foreach (var tstate in states)
            {
                if (tstate != null && tstate.stateType == stateType)
                {
                    tstate.UpdateState(giver != null ? giver.GetAttackDamage() : 0, duration, stacks, ifChangeStackByExtraStacks);
                    DamageTextPool.Instance?.ShowStateTipAtObject(transform, tstate);
                    GameAudioEvents.Raise(
                        tstate.isDebuff ? GameAudioEventType.CombatDebuffGain : GameAudioEventType.CombatBuffGain,
                        giver,
                        this,
                        stacks);
                    if (tstate.isDebuff)
                    {
                        NotifyDebuffApplied(this, giver);
                    }

                    return tstate;
                }
            }
        }

        State state = Instantiate(stateTemplate);
        state.name = stateTemplate.name;
        states.Add(state);
        state.Mount(this, giver, duration, stacks,extraData);

        if (state.isDebuff)
        {
            NotifyDebuffApplied(this, giver);
            debuffAppliedFeedback?.PlayFeedbacks();
        }
        else
        {
            buffAppliedFeedback?.PlayFeedbacks();
        }

        GameAudioEvents.Raise(
            state.isDebuff ? GameAudioEventType.CombatDebuffGain : GameAudioEventType.CombatBuffGain,
            giver,
            this,
            stacks);
        DamageTextPool.Instance?.ShowStateTipAtObject(transform, state);
        return state;
    }

    /// <summary>通知所有状态的 OnOwnerSwappedOut（角色被换下时）</summary>
    public void NotifyStatesOwnerSwappedOut(UnitCombatant newOwner)
    {
        for (int i = states.Count - 1; i >= 0; i--)
        {
            State state = states[i];
            if (state == null) continue;
            state.OnOwnerSwappedOut(newOwner);
        }
    }

    public bool RemoveState(State state)
    {
        if (state == null)
        {
            return false;
        }

        if (!state.CanBePurged())
        {
            return false;
        }

        state.EndState();
        return true;
    }

    /// <summary>
    /// 清除所有负面状态（isDebuff == true）
    /// </summary>
    public void ClearAllDebuffs()
    {
        for (int i = states.Count - 1; i >= 0; i--)
        {
            State state = states[i];
            if (state != null && state.isDebuff)
            {
                state.EndState();
            }
        }
    }

    protected IEnumerator ProcessStatesOnTurnStart()
    {
        yield return State.RunBatchedDotEvent(this, ProcessTurnStartStatesInternal());
    }

    private IEnumerator ProcessTurnStartStatesInternal()
    {
        for (int i = states.Count - 1; i >= 0; i--)
        {
            State state = states[i];
            if (state == null)
            {
                states.RemoveAt(i);
                continue;
            }

            yield return state.OnOwnerTurnStart();
        }

        yield return WaitForDeathEvents();
    }

    public void ProcessStatesOnTurnEnd()
    {
        for (int i = states.Count - 1; i >= 0; i--)
        {
            State state = states[i];
            if (state == null)
            {
                states.RemoveAt(i);
                continue;
            }
            state.TickOnTurnEnd();
            state.OnOwnerTurnEnd();
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

    public bool DealsTrueDamage(bool isDotDamage, bool forceTrueDamage = false)
    {
        if (forceTrueDamage)
        {
            return true;
        }

        for (int i = 0; i < states.Count; i++)
        {
            State state = states[i];
            if (state == null)
            {
                continue;
            }

            if (state.CausesOutgoingTrueDamage(isDotDamage))
            {
                return true;
            }
        }

        return false;
    }

    public virtual float GetIncomingDamageMultiplier(bool isDotDamage, bool isTrueDamage)
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

    /// <summary>因震慑、混沌等状态无法行动时，播放音效并触发战斗对话。</summary>
    protected void NotifyTurnSkipped()
    {
        GameAudioEvents.Raise(GameAudioEventType.CombatTurnSkipped, this, this);

        if (this is Character character)
        {
            BattleDialogEvents.Raise(BattleDialogEventType.CombatantTurnSkipped, character: character);
            return;
        }

        if (this is Enemy enemy)
        {
            BattleDialogEvents.Raise(BattleDialogEventType.CombatantTurnSkipped, enemy: enemy);
        }
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

    protected virtual bool CanReceiveState(StateType stateType, UnitCombatant giver)
    {
        return !dead;
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
        State.RecordBatchedDotDamage(target, damage, isDotDamage);

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

        if (damage > 0)
        {
            State.NotifyDamageEventSettled();
        }
    }

    protected virtual IEnumerator OnDeathEvent()
    {
        while (dieFeedback != null && dieFeedback.IsPlaying)
        {
            yield return null;
        }
    }
    #endregion
}
