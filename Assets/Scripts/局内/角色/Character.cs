using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using MoreMountains.Feedbacks;
using System;

public class Character : UnitCombatant

{
    public event Action<Character> OnSwapCooldownAvailabilityChanged;
    public static event Action<Character> OnCharacterEnterTurn;
    public Transform spriteTransform;
    public string characterID;
    public CharacterType characterType;
    [Header("动画覆盖")]
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterAnimationOverrideDatabase animationOverrideDatabase;
    public List<CharacterSkillType> skills = new List<CharacterSkillType>();
    public CharacterSkillType enterSkill;//入场技能，回合开始时自动触发
    public CharacterSkillType additionalSkillType;
    public CharacterSkillBase additionalSkill;
    private List<CharacterSkillBase> m_skillInstances = new List<CharacterSkillBase>();
    private Dictionary<CharacterSkillType, CharacterSkillBase> m_skillInstanceMap = new Dictionary<CharacterSkillType, CharacterSkillBase>();
    private CharacterSkillBase m_enterSkillInstance;
    private AnimatorOverrideController m_animatorOverrideController;

    [Header("混沌值")]
    [SerializeField, Range(0, MaxChaosValue)] private int chaosValue = 0;
    private const int MaxChaosValue = 5;
    public int ChaosValue => chaosValue;
    public int MaxChaosValueConst => MaxChaosValue;
    [Header("换人冷却")]
    [SerializeField] private float switchCooldownRemaining;
    [SerializeField] private float switchCooldownMax = 200f;
    public float SwitchCooldownRemaining => switchCooldownRemaining;
    public float SwitchCooldownMax => switchCooldownMax;
    public bool IsSwapOnCooldown => switchCooldownRemaining > 0f;
    bool endTurn = false;
    [Header("选中效果")]
    public float selectedScale = 1.1f;
    public float selectAnimDuration = 0.12f;
    private Vector3 m_defaultScale;
    private Tween m_scaleTween;
    [Header("动画精灵")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private List<CanvasGroup> slidersCanvasGroups;
    [Header("位移动画")]
    [SerializeField] private float moveAnimDuration = 0.5f;
    [SerializeField] private Ease moveAnimEase = Ease.InOutSine;
    [SerializeField] private Vector3 targetPos = new Vector3(0, 0, -7);
    private Vector3 m_originalPosition;
    private void Start()
    {
        InitializeAnimatorOverrides();
        InitializeSkill();
        m_defaultScale = transform.localScale;
    }

    private void InitializeAnimatorOverrides()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator == null || animationOverrideDatabase == null)
        {
            return;
        }

        if (!animationOverrideDatabase.TryGetCharacterOverrides(characterID, out List<AnimationClipOverrideEntry> clipOverrides))
        {
            return;
        }

        AnimatorOverrideUtility.TryApplyOverrides(animator, clipOverrides, out m_animatorOverrideController);
    }

    public override IEnumerator PerformTurn()
    {
        endTurn = false;
        TickSkillCooldowns();
        //结算状态
        yield return ProcessStatesOnTurnStart();
        //如果死亡就结束回合
        if(dead)
        {
            yield break;
        }
        EnterMoveDOT();
        yield return new WaitForSeconds(moveAnimDuration);

        if (!CanActThisTurn())
        {
            FloatingTipGenerator.Instance?.ShowTipAtObject(transform, $"无法行动");
            EndTurn();
        }
        //展示攻击逻辑
        yield return TurnStateManager.Instance.ChangeState(TurnState.InCharacterTurn, this);
        OnCharacterEnterTurn?.Invoke(this);
        yield return new WaitUntil(() => endTurn);
        //等待死亡动画结束
        yield return WaitForDeathEvents();
        //结束玩家回合的内容
        ExitMoveDOT();
        yield return new WaitForSeconds(moveAnimDuration + 0.2f);
    }
    public void EndTurn()
    {
        endTurn = true;
    }
    public float GetAttractCount()//获取受击权重
    {
        float c = 1;
        foreach (var state in States)
        {
            c *= state.GetAttractMultiplier(this);
        }
        return c;
    }
    #region 混沌值相关

    public override void Die()
    {
        base.Die();
    }

    /// <summary>
    /// 将混沌状态层数与混沌值同步。
    /// 使用 AddState 的 ifChangeStackByExtraStacks=false 模式，确保层数被覆盖为当前混沌值。
    /// </summary>
    private void SyncChaosState()
    {
        if (chaosValue <= 0)
        {
            // 混沌值为0时移除状态
            State existing = GetState(StateType.Chaos);
            if (existing != null)
            {
                RemoveState(existing);
            }
            return;
        }

        // 用 ifChangeStackByExtraStacks=false 覆盖层数为 chaosValue
        AddState(StateType.Chaos, this, 99, chaosValue, false);
    }

    public bool TryAddChaos(int amount)
    {
        if (amount <= 0 || chaosValue >= MaxChaosValue || dead)
        {
            return false;
        }

        int before = chaosValue;
        SetChaos(Mathf.Clamp(chaosValue + amount, 0, MaxChaosValue));
        if (chaosValue == before)
        {
            return false;
        }

        FloatingTipGenerator.Instance?.ShowTipAtObject(transform, $"混沌+{chaosValue - before} ({chaosValue}/{MaxChaosValue})");
        if (chaosValue >= MaxChaosValue)
        {
            if (TemporaryBattleModifierRuntimeManager.TryHandleChaosMaxReached(this))
            {
                return true;
            }

            FloatingTipGenerator.Instance?.ShowTipAtObject(transform, "混沌达到上限，下回合将无法行动");
        }
        return true;
    }

    public void SetChaos(int value)
    {
        chaosValue = Mathf.Clamp(value, 0, MaxChaosValue);
        SyncChaosState();
    }

    public int ReduceChaos(int amount)
    {
        if (amount <= 0 || dead)
        {
            return 0;
        }

        int before = chaosValue;
        SetChaos(chaosValue - amount);
        int reducedValue = before - chaosValue;
        if (reducedValue <= 0)
        {
            return 0;
        }

        FloatingTipGenerator.Instance?.ShowTipAtObject(transform, $"混沌-{reducedValue} ({chaosValue}/{MaxChaosValue})");
        TemporaryBattleModifierRuntimeManager.NotifyChaosReduced(this, reducedValue);
        return reducedValue;
    }
    #region  换人cd相关

    public void TriggerSwapCooldown()
    {
        bool wasOnCooldown = IsSwapOnCooldown;
        switchCooldownRemaining = Mathf.Max(0f, switchCooldownMax);
        NotifySwapCooldownAvailabilityChangedIfNeeded(wasOnCooldown);
    }

    public void SetSwitchCooldownMax(float value, bool clampCurrent = true)
    {
        bool wasOnCooldown = IsSwapOnCooldown;
        switchCooldownMax = Mathf.Max(0f, value);
        if (clampCurrent)
        {
            switchCooldownRemaining = Mathf.Min(switchCooldownRemaining, switchCooldownMax);
        }

        NotifySwapCooldownAvailabilityChangedIfNeeded(wasOnCooldown);
    }

    public float ReduceSwitchCooldown(float amount)
    {
        if (amount <= 0f || switchCooldownRemaining <= 0f)
        {
            return 0f;
        }

        bool wasOnCooldown = IsSwapOnCooldown;
        float before = switchCooldownRemaining;
        switchCooldownRemaining = Mathf.Max(0f, switchCooldownRemaining - amount);
        NotifySwapCooldownAvailabilityChangedIfNeeded(wasOnCooldown);
        return before - switchCooldownRemaining;
    }

    private void NotifySwapCooldownAvailabilityChangedIfNeeded(bool wasOnCooldown)
    {
        if (wasOnCooldown == IsSwapOnCooldown)
        {
            return;
        }

        OnSwapCooldownAvailabilityChanged?.Invoke(this);
    }

    #endregion
    private void EnterMoveDOT()
    {
        m_originalPosition = transform.position;
        animator.SetTrigger("Move");
        transform.DOMove(targetPos, moveAnimDuration).SetEase(moveAnimEase).OnComplete(() =>
        {
            animator.SetTrigger("Idle");
        });
    }
    private void ExitMoveDOT()
    {
        if (LevelCharacterSpawner.TryGetSpawnPosition(standPosition, out Vector3 spawnPosition))
        {
            transform.DOMove(spawnPosition, moveAnimDuration).SetEase(moveAnimEase);
        }
        m_originalPosition = Vector3.zero;
    }
    #endregion
    #region 选友相关

    private void OnMouseEnter()
    {
        if (CharacterManager.Instance.IsSelectingFieldCharacter)
        {
            PlayMouseHoverEffect();
            SkillDescription.Instance.ChangeDescription(null);
        }
        if (SkillManager.Instance.IsSelectingCharacters)
        {
            if (dead)
                return;
            PlayMouseHoverEffect();
        }
    }
    private void OnMouseExit()
    {
        if (CharacterManager.Instance.IsSelectingFieldCharacter)
        {
            StopMouseHoverEffect();
            SkillDescription.Instance.ChangeDescription(null);
        }
        if (SkillManager.Instance.IsSelectingCharacters)
        {
            if (dead)
                return;
            StopMouseHoverEffect();
        }
    }
    private void OnMouseDown()
    {
        Debug.Log($"Clicked on character: {name}");
        if (CharacterManager.Instance.IsSelectingFieldCharacter)
        {
            CharacterManager.Instance?.OnFieldCharacterClicked(this);
            SkillDescription.Instance.ChangeDescription(null);
            StopMouseHoverEffect();
        }
        if (SkillManager.Instance.IsSelectingCharacters)
        {
            if (dead)
                return;
            SkillManager.Instance?.OnCharacterClicked(this);
            StopMouseHoverEffect(); 
        }
    }

    public void SetSelectedVisual(bool selected)
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
    #region 切换动画
    public void PlayATKAnimation()
    {
        animator.SetTrigger("ATK");
    }
    public void EndATKAnimation()
    {
        animator.SetTrigger("Idle");
    }
    public IEnumerator PlayExitAnimation()
    {
        //简单的退场动画：向右移动并淡出
        Vector3 targetPosition = transform.position + new Vector3(20f, 0, 0);
        float duration = 0.5f;
        Sequence exitSequence = DOTween.Sequence();
        exitSequence.Append(transform.DOMove(targetPosition, duration).SetEase(Ease.InBack));
        exitSequence.Join(spriteRenderer.DOFade(0, duration));
        yield return exitSequence.WaitForCompletion();
    }
    public IEnumerator PlayEnterAnimation()
    {
        //简单的入场动画：从右侧飞入并淡入
        Vector3 startPosition = transform.position + new Vector3(2f, 0, 0);
        transform.position = startPosition;
        Vector3 targetPosition = transform.position - new Vector3(2f, 0, 0);
        float duration = 0.5f;
        Sequence enterSequence = DOTween.Sequence();
        enterSequence.Append(transform.DOMove(targetPosition, duration).SetEase(Ease.OutBack));
        enterSequence.Join(spriteRenderer.DOFade(1, duration));
        foreach (var cg in slidersCanvasGroups)
        {
            enterSequence.Join(cg.DOFade(1, duration));
        }
        yield return enterSequence.WaitForCompletion();
    }
    #endregion
    #region 技能相关
    private void TickSkillCooldowns()
    {
        if (m_skillInstances == null)
        {
            return;
        }

        for (int i = 0; i < m_skillInstances.Count; i++)
        {
            m_skillInstances[i]?.TickCooldown(this);
        }
    }
    #endregion
    public void InitializeSkill()
    {
        CleanupSkillInstances();
        m_skillInstances.Clear();
        m_skillInstanceMap.Clear();
        foreach (var skillType in skills)
        {
            var skill = CreateSkillInstance(skillType);
            if (skill == null)
            {
                continue;
            }

            m_skillInstances.Add(skill);
            m_skillInstanceMap[skillType] = skill;
        }
        m_enterSkillInstance = CreateSkillInstance(enterSkill);
        additionalSkill = CreateSkillInstance(additionalSkillType);
    }

    public CharacterSkillBase GetSkillInstance(CharacterSkillType skillType)
    {
        if (m_skillInstanceMap == null || m_skillInstanceMap.Count == 0)
        {
            InitializeSkill();
        }

        m_skillInstanceMap.TryGetValue(skillType, out CharacterSkillBase skill);
        return skill;
    }

    public CharacterSkillBase GetEnterSkillInstance()
    {
        if (m_enterSkillInstance == null)
        {
            InitializeSkill();
        }

        return m_enterSkillInstance;
    }

    private CharacterSkillBase CreateSkillInstance(CharacterSkillType skillType)
    {
        CharacterSkillBase template = SkillDictionaryManager.GetSkill(skillType);
        if (template == null)
        {
            return null;
        }

        CharacterSkillBase instance = Instantiate(template);
        instance.name = template.name;
        return instance;
    }

    private void CleanupSkillInstances()
    {
        DestroySkillInstance(m_enterSkillInstance);
        DestroySkillInstance(additionalSkill);
        additionalSkill = null;
        m_enterSkillInstance = null;

        if (m_skillInstances == null)
        {
            return;
        }

        for (int i = 0; i < m_skillInstances.Count; i++)
        {
            DestroySkillInstance(m_skillInstances[i]);
        }
    }

    private void DestroySkillInstance(CharacterSkillBase skillInstance)
    {
        if (skillInstance == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(skillInstance);
        }
        else
        {
            DestroyImmediate(skillInstance);
        }
    }

    protected override void OnDestroy()
    {
        CleanupSkillInstances();
        base.OnDestroy();
    }

    public override float ConsumeTurnEndActionValue()
    {
        return TemporaryBattleModifierRuntimeManager.GetCharacterTurnEndActionValue(base.ConsumeTurnEndActionValue(), this);
    }

    public void LoadDataFromCSV()
    {
        if (string.IsNullOrEmpty(characterID))
        {
            Debug.Log("CharacterID is null or empty for " + gameObject.name);
            return;
        }

        if (!LevelDataContainer.TryGetCharacterLevelData(characterID, level, out CharacterLevelData levelData))
        {
            Debug.LogError($"未找到角色数据: {characterID} 等级: {level}");
            return;
        }

        maxHP = Mathf.RoundToInt(levelData.maxHP * TemporaryBattleModifierRuntimeManager.GetPlayerMaxHealthMultiplier(this));
        currentHP = maxHP;
        attack = levelData.attack;
        defense = Mathf.RoundToInt(levelData.defense * TemporaryBattleModifierRuntimeManager.GetPlayerDefenseMultiplier(this));
        critRate = levelData.critRate;
        critDamage = levelData.critDamage;
        //耦合度有点高了
        speed = Mathf.Max(1, Mathf.RoundToInt(levelData.speed / TemporaryBattleModifierRuntimeManager.GetPlayerSpeedMultiplier(this)));
        K = levelData.K;
    }
}
