using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 西洋剑客韧性点数据提供接口，供 UI 层读取韧性点状态
/// </summary>
public interface ISwordsmanTenacityProvider
{
    int CurrentTenacity { get; }
    int MaxTenacity { get; }
    event System.Action TenacityChanged;
}

/// <summary>
/// 西洋剑客Boss — 三姿态循环、韧性点眩晕、背水一战
/// 姿态管理使用状态机模式
/// </summary>
public class SwordsmanEnemy : Enemy, ISwordsmanTenacityProvider
{
    [Header("姿态配置")]
    [SerializeField] private float phaseTwoHpThreshold = 0.5f;
    [SerializeField] private float phaseThreeHpThreshold = 0.25f;

    [Header("韧性配置")]
    private const int maxTenacity = 20;
    private const int staggerDuration = 120;

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
            BattleDialogEvents.Raise(BattleDialogEventType.SwordsmanStanceBright, enemy: owner);
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
            BattleDialogEvents.Raise(BattleDialogEventType.SwordsmanStanceDefense, enemy: owner);
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
            BattleDialogEvents.Raise(BattleDialogEventType.SwordsmanStanceGuerrilla, enemy: owner);
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
    private bool m_suppressStaggerExitCallback;

    // ============ 指挥点奖励 ============
    private const float SwordsmanGuaranteeThreshold = 200f;
    private bool m_battleStartRewarded;
    private bool m_phaseThreeRewarded;

    protected override void Start()
    {
        base.Start();
        m_previousHp = currentHP;
        m_currentTenacity = maxTenacity;
        NotifyTenacityChanged();

        // 初始化姿态状态机
        m_stanceStates[SwordsmanStance.BrightSword] = new BrightSwordState();
        m_stanceStates[SwordsmanStance.Defense] = new DefenseState();
        m_stanceStates[SwordsmanStance.Guerrilla] = new GuerrillaState();
        foreach (var state in m_stanceStates.Values) state.Initialize(this);

        // 初始姿态：亮剑
        TransitionTo(SwordsmanStance.BrightSword);
        // 施加优雅体态
        AddState(StateType.SwordsmanElegance, this, 99, 1);

        // 入场对话
        BattleDialogEvents.Raise(BattleDialogEventType.SwordsmanEnter);
        // 优雅体态提醒
        BattleDialogEvents.Raise(BattleDialogEventType.SwordsmanEleganceReminder);

        // 指挥点奖励：战斗开始 +1，低保200AV
        if (!m_battleStartRewarded)
        {
            m_battleStartRewarded = true;
            Commander.GetInstance().RecoverCommandPoints(1, "剑客Boss战斗开始+1");
            Commander.GetInstance().SetBossGuaranteeThreshold(SwordsmanGuaranteeThreshold);
        }
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
        EnsureStaggerConsistency();

        // 姿态切换倒计时（失衡、背水一战中暂停）
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
        if (stanceSkills.Count > 0)
        {
            return stanceSkills[Random.Range(0, stanceSkills.Count)];
        }

        // 当前姿态技能全部不可用时，回退到基类通用收集逻辑
        return base.GetForcedSkillForTurn();
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

        // 同步 Animator 的 Stance 参数，驱动 Idle 动画切换
        if (Anim != null)
        {
            Anim.SetInteger("Stance", (int)newStance);
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
        NotifyTenacityChanged();

        if (m_currentTenacity <= 0)
        {
            EnterStagger();
        }
    }

    private void EnterStagger()
    {
        if (m_isInStagger) return;

        // 先离开当前姿态 buff；游击免疫会拦截 debuff，必须在施加失衡前清掉。动画/枚举保持进入失衡前的姿态。
        m_currentStanceState?.OnExit(m_currentStance);
        m_currentStanceState = null;

        m_isInStagger = true;
        NotifyTenacityChanged();

        State staggerState = AddState(StateType.SwordsmanStagger, this, staggerDuration, 1);
        if (staggerState == null)
        {
            Debug.LogWarning($"[SwordsmanEnemy] {combatantName} 失衡状态施加失败，回退至防御姿态");
            ExitStagger();
            return;
        }

        FloatingTipGenerator.Instance?.ShowTipAtObject(transform, "失衡！");
        BattleDialogEvents.Raise(BattleDialogEventType.SwordsmanStaggerEnter, enemy: this);

        // 指挥点奖励：每次进入失衡 +1
        Commander.GetInstance().RecoverCommandPoints(1, "剑客失衡+1");
    }

    public void ExitStagger()
    {
        if (m_suppressStaggerExitCallback)
        {
            return;
        }

        m_isInStagger = false;
        m_currentTenacity = maxTenacity;
        NotifyTenacityChanged();
        TransitionTo(m_isLastStand ? SwordsmanStance.BrightSword : SwordsmanStance.Defense);
        m_stanceSwitchCountdown = 2;
        BattleDialogEvents.Raise(BattleDialogEventType.SwordsmanStaggerExit, enemy: this);
        BattleDialogEvents.Raise(BattleDialogEventType.SwordsmanEleganceReminder, enemy: this);
    }

    /// <summary>背水一战：强制清除失衡 debuff，随后由 EnterLastStand 切入亮剑。</summary>
    private void ForceEndStaggerForLastStand()
    {
        if (!m_isInStagger && !HasState(StateType.SwordsmanStagger))
        {
            return;
        }

        m_isInStagger = false;
        m_currentTenacity = maxTenacity;
        NotifyTenacityChanged();

        State staggerState = GetState(StateType.SwordsmanStagger);
        if (staggerState == null)
        {
            return;
        }

        m_suppressStaggerExitCallback = true;
        RemoveState(staggerState);
        m_suppressStaggerExitCallback = false;
    }

    /// <summary>修复 m_isInStagger 与失衡状态不同步的软锁（如游击姿态误判导致 debuff 未挂上）</summary>
    private void EnsureStaggerConsistency()
    {
        bool hasStaggerState = HasState(StateType.SwordsmanStagger);
        if (m_isInStagger && !hasStaggerState)
        {
            Debug.LogWarning($"[SwordsmanEnemy] {combatantName} 失衡标记与状态不一致，自动恢复");
            ExitStagger();
            return;
        }

        if (!m_isInStagger && hasStaggerState)
        {
            m_isInStagger = true;
        }
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
            BattleDialogEvents.Raise(BattleDialogEventType.SwordsmanPhaseTwo, enemy: this);
        }

        // 阶段三：血量低于25% — 背水一战锁定亮剑，若处于失衡则强制结束失衡
        if (!m_phaseThreeTriggered && hpRatio <= phaseThreeHpThreshold)
        {
            m_phaseThreeTriggered = true;
            m_isLastStand = true;
            ForceEndStaggerForLastStand();
            TransitionTo(SwordsmanStance.BrightSword);
            AddState(StateType.SwordsmanLastStand, this, 99, 1);
            FloatingTipGenerator.Instance?.ShowTipAtObject(transform, "背水一战！");
            BattleDialogEvents.Raise(BattleDialogEventType.SwordsmanPhaseThree, enemy: this);

            // 指挥点奖励：进入三阶段背水一战 +1
            if (!m_phaseThreeRewarded)
            {
                m_phaseThreeRewarded = true;
                Commander.GetInstance().RecoverCommandPoints(1, "剑客背水一战+1");
            }
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

    protected override bool CanReceiveState(StateType stateType, UnitCombatant giver)
    {
        // 自身失衡 debuff 不受游击免疫影响（EnterStagger 前已清姿态，此处作兜底）
        if (stateType == StateType.SwordsmanStagger && giver == this)
        {
            return base.CanReceiveState(stateType, giver);
        }

        // 追惩惩戒标记由友方施加，不受游击免疫影响
        if (stateType == StateType.PunishMark && giver is Character)
        {
            return base.CanReceiveState(stateType, giver);
        }

        if (m_currentStanceState != null && m_currentStanceState.Stance == SwordsmanStance.Guerrilla)
        {
            State stateTemplate = StateDictionaryManager.GetState(stateType);
            if (stateTemplate != null && stateTemplate.isDebuff)
            {
                FloatingTipGenerator.Instance?.ShowTipAtObject(transform, $"{combatantName}免疫{StateDictionaryManager.GetStateName(stateType)}");
                return false;
            }
        }

        return base.CanReceiveState(stateType, giver);
    }

    public bool IsInStagger => m_isInStagger;
    public SwordsmanStance CurrentStance => m_currentStance;
    public bool IsLastStand => m_isLastStand;

    // ============ ISwordsmanTenacityProvider 实现 ============

    public int CurrentTenacity => m_currentTenacity;
    public int MaxTenacity => maxTenacity;
    public event System.Action TenacityChanged;

    private void NotifyTenacityChanged()
    {
        TenacityChanged?.Invoke();
    }

    /// <summary>
    /// Dot伤害批次结算时扣除1点韧性（同一批次多次Dot只扣1点）
    /// 由 SwordsmanEleganceBehavior.OnCombatEventTriggered 在 DotTriggered 事件中调用
    /// </summary>
    public void ReduceTenacityByDot()
    {
        if (m_isInStagger) return;

        m_currentTenacity = Mathf.Max(0, m_currentTenacity - 1);
        NotifyTenacityChanged();

        if (m_currentTenacity <= 0)
        {
            EnterStagger();
        }
    }

#if UNITY_EDITOR
    /// <summary>调试：空格键将韧性点设为 1，便于测试失衡。</summary>
    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Space) || dead)
        {
            return;
        }

        m_currentTenacity = 1;
        NotifyTenacityChanged();
        FloatingTipGenerator.Instance?.ShowTipAtObject(transform, "Debug: 韧性→1");
    }
#endif
}
