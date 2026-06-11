using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 西洋剑客Boss — 三姿态循环、韧性点眩晕、背水一战
/// 姿态管理使用状态机模式
/// </summary>
public class SwordsmanEnemy : Enemy
{
    [Header("姿态配置")]
    [SerializeField] private float phaseTwoSpeedBonus = 0.2f;
    [SerializeField] private float phaseTwoHpThreshold = 0.5f;
    [SerializeField] private float phaseThreeHpThreshold = 0.25f;

    [Header("韧性配置")]
    [SerializeField] private int maxTenacity = 10;
    [SerializeField] private int staggerDuration = 100;

    public enum SwordsmanStance
    {
        BrightSword,  // 亮剑
        Defense,      // 防御
        Guerrilla     // 游击
    }

    // ============ 姿态状态机 ============

    private abstract class StanceState
    {
        protected SwordsmanEnemy owner;
        public abstract SwordsmanStance Stance { get; }
        public void Initialize(SwordsmanEnemy swordsman) { owner = swordsman; }
        public virtual void OnEnter(SwordsmanStance fromStance) { }
        public virtual void OnExit(SwordsmanStance toStance) { }
        public virtual int GetTenacityReduction(bool isLastStand) => 1;
    }

    private sealed class BrightSwordState : StanceState
    {
        public override SwordsmanStance Stance => SwordsmanStance.BrightSword;
        public override void OnEnter(SwordsmanStance fromStance)
        {
            owner.AddState(StateType.SwordsmanBrightSword, owner, 99, 1);
            FloatingTipGenerator.Instance?.ShowTipAtObject(owner.transform, "亮剑姿态");
        }
        public override void OnExit(SwordsmanStance toStance)
        {
            owner.RemoveStateByType(StateType.SwordsmanBrightSword);
        }
        public override int GetTenacityReduction(bool isLastStand) => isLastStand ? 3 : 2;
    }

    private sealed class DefenseState : StanceState
    {
        public override SwordsmanStance Stance => SwordsmanStance.Defense;
        public override void OnEnter(SwordsmanStance fromStance)
        {
            owner.AddState(StateType.SwordsmanDefense, owner, 99, 1);
            FloatingTipGenerator.Instance?.ShowTipAtObject(owner.transform, "防御姿态");
        }
        public override void OnExit(SwordsmanStance toStance)
        {
            owner.RemoveStateByType(StateType.SwordsmanDefense);
        }
    }

    private sealed class GuerrillaState : StanceState
    {
        public override SwordsmanStance Stance => SwordsmanStance.Guerrilla;
        public override void OnEnter(SwordsmanStance fromStance)
        {
            owner.AddState(StateType.SwordsmanGuerrilla, owner, 99, 1);
            FloatingTipGenerator.Instance?.ShowTipAtObject(owner.transform, "游击姿态");
        }
        public override void OnExit(SwordsmanStance toStance)
        {
            owner.RemoveStateByType(StateType.SwordsmanGuerrilla);
        }
    }

    private readonly Dictionary<SwordsmanStance, StanceState> m_stanceStates = new Dictionary<SwordsmanStance, StanceState>();
    private StanceState m_currentStanceState;
    private SwordsmanStance m_currentStance;
    private int m_stanceSwitchCountdown = 2;
    private int m_currentTenacity;
    private bool m_isInStagger;
    private bool m_isLastStand;
    private bool m_phaseTwoTriggered;
    private bool m_phaseThreeTriggered;
    private int m_previousHp;

    protected override void Start()
    {
        base.Start();
        m_previousHp = currentHP;
        m_currentTenacity = maxTenacity;

        // 初始化姿态状态机
        m_stanceStates[SwordsmanStance.BrightSword] = new BrightSwordState();
        m_stanceStates[SwordsmanStance.Defense] = new DefenseState();
        m_stanceStates[SwordsmanStance.Guerrilla] = new GuerrillaState();
        foreach (var state in m_stanceStates.Values) state.Initialize(this);

        // 初始姿态：亮剑
        TransitionTo(SwordsmanStance.BrightSword);
        // 施加优雅体态
        AddState(StateType.SwordsmanElegance, this, 99, 1);
    }

    public override void TakeDamage(DamageInfo damageInfo)
    {
        int hpBefore = currentHP;
        base.TakeDamage(damageInfo);

        // 韧性点扣除（非Dot伤害）
        if (!damageInfo.IsDotDamage && damageInfo.Damage > 0)
        {
            ReduceTenacity(damageInfo);
        }

        // 阶段检测
        CheckPhaseTransition();
    }

    protected override void OnTurnStartBeforeStateSettlement()
    {
        base.OnTurnStartBeforeStateSettlement();

        // 姿态切换倒计时
        if (!m_isInStagger && !m_isLastStand)
        {
            m_stanceSwitchCountdown--;
            if (m_stanceSwitchCountdown <= 0)
            {
                SwitchToNextStance();
                m_stanceSwitchCountdown = 2;
            }
        }
    }

    public override bool CanUseEnemySkill(EnemySkillBase skill)
    {
        if (!base.CanUseEnemySkill(skill)) return false;
        if (m_isInStagger) return false;

        switch (skill.enemySkillType)
        {
            case EnemySkillType.SwordsmanThrust:
                return m_currentStance == SwordsmanStance.BrightSword && GetAliveFieldCharacters().Count > 0;
            case EnemySkillType.SwordsmanDance:
                return m_currentStance == SwordsmanStance.BrightSword && GetAliveFieldCharacters().Count > 0;
            case EnemySkillType.SwordsmanBlock:
                return m_currentStance == SwordsmanStance.Defense && GetAliveFieldCharacters().Count > 0;
            case EnemySkillType.SwordsmanSteady:
                return m_currentStance == SwordsmanStance.Defense;
            case EnemySkillType.SwordsmanDisrupt:
                return m_currentStance == SwordsmanStance.Guerrilla && GetAliveFieldCharacters().Count > 0;
            case EnemySkillType.SwordsmanShadow:
                return m_currentStance == SwordsmanStance.Guerrilla && GetAliveFieldCharacters().Count > 0;
            default:
                return true;
        }
    }

    protected override EnemySkillBase GetForcedSkillForTurn()
    {
        if (m_isInStagger) return null;

        List<EnemySkillBase> stanceSkills = GetSkillsForCurrentStance();
        if (stanceSkills.Count == 0) return null;

        return stanceSkills[Random.Range(0, stanceSkills.Count)];
    }

    // ============ 姿态状态机 ============

    /// <summary>状态机驱动的姿态切换</summary>
    private void TransitionTo(SwordsmanStance newStance)
    {
        SwordsmanStance oldStance = m_currentStance;
        StanceState oldState = m_currentStanceState;

        // 离开旧姿态
        oldState?.OnExit(newStance);

        // 进入新姿态
        m_currentStance = newStance;
        if (m_stanceStates.TryGetValue(newStance, out StanceState newState))
        {
            m_currentStanceState = newState;
            newState.OnEnter(oldStance);
        }
    }

    private void SwitchToNextStance()
    {
        SwordsmanStance nextStance;
        switch (m_currentStance)
        {
            case SwordsmanStance.BrightSword:
                nextStance = SwordsmanStance.Defense;
                break;
            case SwordsmanStance.Defense:
                nextStance = SwordsmanStance.Guerrilla;
                break;
            default:
                nextStance = SwordsmanStance.BrightSword;
                break;
        }
        TransitionTo(nextStance);
    }

    private void RemoveStateByType(StateType type)
    {
        State state = GetState(type);
        if (state != null) RemoveState(state);
    }

    // ============ 韧性系统 ============

    private void ReduceTenacity(DamageInfo damageInfo)
    {
        int reduction = m_currentStanceState != null
            ? m_currentStanceState.GetTenacityReduction(m_isLastStand)
            : 1;

        // 游击姿态：被暴击时额外减1
        if (m_currentStance == SwordsmanStance.Guerrilla
            && Random.value < (damageInfo.Source != null ? damageInfo.Source.critRate : 0f))
        {
            reduction += 1;
        }

        m_currentTenacity = Mathf.Max(0, m_currentTenacity - reduction);

        if (m_currentTenacity <= 0)
        {
            EnterStagger();
        }
    }

    private void EnterStagger()
    {
        m_isInStagger = true;
        m_currentTenacity = maxTenacity;

        // 通过状态机离开当前姿态
        m_currentStanceState?.OnExit(SwordsmanStance.Defense);
        m_currentStanceState = null;

        AddState(StateType.SwordsmanStagger, this, staggerDuration, 1);
        FloatingTipGenerator.Instance?.ShowTipAtObject(transform, "失衡！");
    }

    public void ExitStagger()
    {
        m_isInStagger = false;
        // 强制切换至防御姿态
        TransitionTo(SwordsmanStance.Defense);
        m_stanceSwitchCountdown = 2;
    }

    // ============ 阶段检测 ============

    private void CheckPhaseTransition()
    {
        float hpRatio = (float)currentHP / maxHP;

        // 阶段二：血量低于50%
        if (!m_phaseTwoTriggered && hpRatio <= phaseTwoHpThreshold)
        {
            m_phaseTwoTriggered = true;
            AddState(StateType.SpeedChange, this, 99, 1);
            FloatingTipGenerator.Instance?.ShowTipAtObject(transform, "剑客速度提升！");
        }

        // 阶段三：血量低于25%
        if (!m_phaseThreeTriggered && hpRatio <= phaseThreeHpThreshold)
        {
            m_phaseThreeTriggered = true;
            m_isLastStand = true;
            TransitionTo(SwordsmanStance.BrightSword);
            AddState(StateType.SwordsmanLastStand, this, 99, 1);
            FloatingTipGenerator.Instance?.ShowTipAtObject(transform, "背水一战！");
        }
    }

    // ============ 辅助方法 ============

    private List<EnemySkillBase> GetSkillsForCurrentStance()
    {
        List<EnemySkillBase> result = new List<EnemySkillBase>();
        EnemySkillType[] skillTypes;

        switch (m_currentStance)
        {
            case SwordsmanStance.BrightSword:
                skillTypes = new[] { EnemySkillType.SwordsmanThrust, EnemySkillType.SwordsmanDance };
                break;
            case SwordsmanStance.Defense:
                skillTypes = new[] { EnemySkillType.SwordsmanBlock, EnemySkillType.SwordsmanSteady };
                break;
            case SwordsmanStance.Guerrilla:
                skillTypes = new[] { EnemySkillType.SwordsmanDisrupt, EnemySkillType.SwordsmanShadow };
                break;
            default:
                return result;
        }

        for (int i = 0; i < skillTypes.Length; i++)
        {
            EnemySkillBase skill = GetSkillInstance(skillTypes[i]);
            if (skill != null && skill.CanUse(this))
            {
                result.Add(skill);
            }
        }
        return result;
    }

    private List<Character> GetAliveFieldCharacters()
    {
        List<Character> alive = new List<Character>();
        if (CharacterManager.Instance == null) return alive;
        for (int i = 0; i < CharacterManager.Instance.fieldCharacters.Count; i++)
        {
            Character c = CharacterManager.Instance.fieldCharacters[i];
            if (c != null && !c.IsDead) alive.Add(c);
        }
        return alive;
    }

    public bool IsInStagger => m_isInStagger;
    public SwordsmanStance CurrentStance => m_currentStance;
    public bool IsLastStand => m_isLastStand;
}
