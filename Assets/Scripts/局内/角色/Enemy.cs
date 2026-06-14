using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using MoreMountains.Feedbacks;
public enum ExplodeType
{
    None,
    Normal,
    hasStarted,
    ReadyToBurst
}

public class Enemy : UnitCombatant
{
    public static event System.Action OnEnemyActEvent;
    public string enemyID;
    [Header("动画覆盖")]
    [SerializeField] private Animator animator;
    protected Animator Anim => animator;
    [SerializeField] private EnemyAnimationOverrideDatabase animationOverrideDatabase;
    public float selectedScale = 1.1f;
    public float selectAnimDuration = 0.12f;

    private Vector3 m_defaultScale;
    private Tween m_scaleTween;
    private Coroutine m_hitAnimCoroutine;
    public List<EnemySkillType> skills = new List<EnemySkillType>();
    private List<EnemySkillBase> m_skillInstances = new List<EnemySkillBase>();
    private Dictionary<EnemySkillType, EnemySkillBase> m_skillInstanceMap = new Dictionary<EnemySkillType, EnemySkillBase>();
    protected bool m_runtimeInitialized;
    private bool m_isBattleVisible = true;
    private bool m_animationOverridesApplied;
    public virtual bool ShouldRegisterAtBattleStart => true;
    public bool IsBattleVisible => m_isBattleVisible;

    #region 自爆相关 因为项目比较小 所以先把自爆相关的状态和逻辑写在Enemy类里，后续如果需要的话再重构
    [HideInInspector] public ExplodeType explodeState = ExplodeType.None;
    #endregion
    protected virtual void Start()
    {
        ;
    }

    public virtual void ConfigureFromBattleSpawnData(BattleEnemySpawnData spawnData, int standPosition)
    {
        if (spawnData == null)
        {
            return;
        }

        OnConfigureFromBattleSpawnData(spawnData);
        ConfigureFromRosterData(spawnData.enemyData, standPosition, spawnData.level);
    }

    protected virtual void OnConfigureFromBattleSpawnData(BattleEnemySpawnData spawnData)
    {
    }

    public virtual void ConfigureFromRosterData(EnemyRosterData data, int standPosition, int level)
    {
        if (data == null)
        {
            return;
        }

        enemyID = data.enemyID;
        combatantName = string.IsNullOrEmpty(data.enemyName) ? data.enemyID : data.enemyName;
        skills = new List<EnemySkillType>(data.skills);
        this.standPosition = standPosition;
        this.level = Mathf.Max(1, level);
        participateInTurnLoopAtStart = ShouldRegisterAtBattleStart;
        InitializeAnimatorOverrides();
        InitializeEnemyRuntime();
    }

    private void InitializeAnimatorOverrides()
    {
        if (m_animationOverridesApplied)
        {
            return;
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator == null || animationOverrideDatabase == null)
        {
            return;
        }

        if (!animationOverrideDatabase.TryGetEnemyOverrides(enemyID, out List<AnimationClipOverrideEntry> clipOverrides))
        {
            return;
        }

        m_animationOverridesApplied = AnimatorOverrideUtility.TryApplyOverrides(animator, clipOverrides, out var m_animatorOverrideController);
    }

    protected virtual void InitializeEnemyRuntime()
    {
        if (m_runtimeInitialized)
        {
            return;
        }

        if (!ShouldRegisterAtBattleStart)
        {
            participateInTurnLoopAtStart = false;
        }

        InitializeSkill();
        LoadDataFromCSV();
        currentHP *= 2;
        maxHP *= 2;
        currentHP = Mathf.Min(currentHP, maxHP);
        m_defaultScale = transform.localScale;
        SetBattleVisibility(ShouldRegisterAtBattleStart);
        m_runtimeInitialized = true;
    }

    public virtual void InitializeFromPendingLevelData(PendingBattleLevelData pendingData, IReadOnlyList<Enemy> spawnedEnemies)
    {
    }

    public override IEnumerator PerformTurn()
    {
        if (!m_isBattleVisible || dead)
        {
            yield break;
        }
        
        TickSkillCooldowns();
        OnTurnStartBeforeStateSettlement();
        yield return new WaitForSeconds(0.2f);
        yield return ProcessStatesOnTurnStart();
        //如果死亡了就直接结束回合
        if (dead)
        {
            yield break;
        }
        if (!CanActThisTurn())
        {
            FloatingTipGenerator.Instance?.ShowTipAtObject(transform, $"无法行动");
            yield break;
        }
        //执行行动
        yield return ActionCoroutine();
        OnEnemyActEvent?.Invoke();
    }

    public override float ConsumeTurnEndActionValue()
    {
        return TemporaryBattleModifierRuntimeManager.GetEnemyTurnEndActionValue(base.ConsumeTurnEndActionValue(), this);
    }

    protected virtual void OnTurnStartBeforeStateSettlement()
    {
    }

    public override void TakeDamage(DamageInfo damageInfo)
    {
        base.TakeDamage(damageInfo);
        PlayHitAnimation();
    }

    private void PlayHitAnimation()
    {
        if (animator == null) return;

        if (m_hitAnimCoroutine != null)
        {
            StopCoroutine(m_hitAnimCoroutine);
        }
        m_hitAnimCoroutine = StartCoroutine(HitAnimationCoroutine());
    }

    private IEnumerator HitAnimationCoroutine()
    {
        animator.SetTrigger("EnterGetAttack");
        yield return new WaitForSeconds(0.1f);
        animator.SetTrigger("ExitGetAttack");
        m_hitAnimCoroutine = null;
    }

    private IEnumerator ActionCoroutine()
    {
        if (m_skillInstances == null || m_skillInstances.Count == 0)
        {
            InitializeSkill();
        }

        if (m_skillInstances == null || m_skillInstances.Count == 0)
        {
            yield break;
        }

        EnemySkillBase skill = SelectSkillForTurn();
        if (skill == null)
        {
            FloatingTipGenerator.Instance?.ShowTipAtObject(transform, $"{combatantName}暂无可用技能");
            yield break;
        }

        yield return new WaitForSeconds(0.2f);
        FloatingTipGenerator.Instance?.ShowDefaultTip(skill.skillName);
        yield return new WaitForSeconds(0.5f);//进入回合动画
        yield return new WaitForSeconds(0.5f);
        SkillExecuteManager.ExecuteSkill(this, skill);
        yield return new WaitUntil(() => !SkillExecuteManager.s_isExecutingSkill);
        yield return WaitForDeathEvents();
        yield break;
    }

    public override void Die()
    {
        base.Die();
        EnemyManager.Instance?.UnregisterEnemy(this);
        if (currentHP <= 0)
        {
            Commander.GetInstance().NotifyEnemyKilled();
        }
        // 通知 LevelCharacterSpawner 释放站位
        LevelCharacterSpawner.Instance?.ReleaseEnemyStandPosition(standPosition);
    }

    protected override IEnumerator OnDeathEvent()
    {
        yield return base.OnDeathEvent();
        gameObject.SetActive(false);
    }
    #region 选敌相关
    private void OnMouseDown()
    {
        if (!m_isBattleVisible)
        {
            return;
        }

        // OnMouseDown 默认响应鼠标左键，这里把点击事件转发给选敌系统。
        SkillManager.Instance?.OnEnemyClicked(this);
        StopMouseHoverEffect();
    }
    private void OnMouseEnter()
    {
        if (m_isBattleVisible && SkillManager.Instance != null && SkillManager.Instance.IsSelectingEnemies)
        {
            PlayMouseHoverEffect();
        }
    }
    private void OnMouseExit()
    {
        if (m_isBattleVisible && SkillManager.Instance != null && SkillManager.Instance.IsSelectingEnemies)
        {
            StopMouseHoverEffect();
        }
    }

    public void SetSelectedVisual(bool selected)//被选中可视化函数
    {
        if (m_scaleTween != null)
        {
            m_scaleTween.Kill();
            m_scaleTween = null;
        }

        var targetScale = selected ? m_defaultScale * selectedScale : m_defaultScale;
        m_scaleTween = transform.DOScale(targetScale, selectAnimDuration).SetEase(Ease.OutQuad);
    }
    #endregion

    #region 读取数据
    public void LoadDataFromCSV()
    {
        if (string.IsNullOrEmpty(enemyID))
        {
            Debug.Log("EnemyID is null or empty for " + gameObject.name);
            return;
        }

        if (!LevelDataContainer.TryGetEnemyLevelData(enemyID, level, out EnemyLevelData levelData))
        {
            Debug.LogError($"未找到敌人数据: {enemyID} 等级: {level}");
            return;
        }

        maxHP = levelData.maxHP;
        currentHP = maxHP;
        attack = levelData.attack;
        defense = levelData.defense;
        speed = levelData.speed;
        K = levelData.K;
    }
    #endregion

    public void InitializeSkill()
    {
        CleanupSkillInstances();
        m_skillInstances.Clear();
        m_skillInstanceMap.Clear();

        if (skills == null)
        {
            return;
        }

        foreach (var skillType in skills)
        {
            EnemySkillBase skill = CreateSkillInstance(skillType);
            if (skill == null)
            {
                continue;
            }

            m_skillInstances.Add(skill);
            m_skillInstanceMap[skillType] = skill;
        }
    }

    public EnemySkillBase GetSkillInstance(EnemySkillType skillType)
    {
        if (m_skillInstanceMap == null || m_skillInstanceMap.Count == 0)
        {
            InitializeSkill();
        }

        m_skillInstanceMap.TryGetValue(skillType, out EnemySkillBase skill);
        return skill;
    }

    private EnemySkillBase CreateSkillInstance(EnemySkillType skillType)
    {
        EnemySkillBase template = EnemySkillDictionaryManager.GetEnemySkill(skillType);
        if (template == null)
        {
            return null;
        }

        EnemySkillBase instance = Instantiate(template);
        instance.name = template.name;
        return instance;
    }

    private void CleanupSkillInstances()
    {
        if (m_skillInstances == null)
        {
            return;
        }

        for (int i = 0; i < m_skillInstances.Count; i++)
        {
            if (m_skillInstances[i] == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(m_skillInstances[i]);
            }
            else
            {
                DestroyImmediate(m_skillInstances[i]);
            }
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        CleanupSkillInstances();
    }

    public virtual bool CanUseEnemySkill(EnemySkillBase skill)
    {
        return skill != null && m_isBattleVisible && !dead;
    }

    protected virtual EnemySkillBase SelectSkillForTurn()
    {
        EnemySkillBase forcedSkill = GetForcedSkillForTurn();
        if (forcedSkill != null)
        {
            return forcedSkill;
        }

        List<EnemySkillBase> availableSkills = new List<EnemySkillBase>();
        for (int i = 0; i < m_skillInstances.Count; i++)
        {
            EnemySkillBase skill = m_skillInstances[i];
            if (skill == null || !skill.CanUse(this))
            {
                continue;
            }

            availableSkills.Add(skill);
        }

        if (availableSkills.Count == 0)
        {
            return null;
        }

        return availableSkills[Random.Range(0, availableSkills.Count)];
    }

    protected virtual EnemySkillBase GetForcedSkillForTurn()
    {
        return null;
    }

    protected void TickSkillCooldowns()
    {
        if (m_skillInstances == null)
        {
            return;
        }

        for (int i = 0; i < m_skillInstances.Count; i++)
        {
            if (m_skillInstances[i] != null)
            {
                m_skillInstances[i].TickCooldown();
            }
        }
    }

    protected void SetBattleVisibility(bool visible)
    {
        m_isBattleVisible = visible;
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = visible;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = visible;
        }

        Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            canvases[i].enabled = visible;
        }
    }
}
