using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using MoreMountains.Feedbacks;
using System;

public class Character : UnitCombatant

{
    public Transform spriteTransform;
    public string characterID;
    [Header("动画覆盖")]
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterAnimationOverrideDatabase animationOverrideDatabase;
    public List<CharacterSkillType> skills = new List<CharacterSkillType>();
    public CharacterSkillType enterSkill;//入场技能，回合开始时自动触发
    public CharacterSkillBase additionalSkill;
    private List<CharacterSkillBase> m_skillInstances = new List<CharacterSkillBase>();
    private Dictionary<CharacterSkillType, CharacterSkillBase> m_skillInstanceMap = new Dictionary<CharacterSkillType, CharacterSkillBase>();
    private CharacterSkillBase m_enterSkillInstance;
    private AnimatorOverrideController m_animatorOverrideController;

    [Header("混沌值")]
    [SerializeField, Range(0, MaxChaosValue)] private int chaosValue = 0;
    [SerializeField] private bool pendingChaosRecover;
    private const int MaxChaosValue = 5;
    private const int ChaosRecoverValue = 2;
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

        RuntimeAnimatorController runtimeController = animator.runtimeAnimatorController;
        if (runtimeController == null)
        {
            return;
        }

        m_animatorOverrideController = new AnimatorOverrideController(runtimeController);
        List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(m_animatorOverrideController.overridesCount);
        m_animatorOverrideController.GetOverrides(overrides);

        Dictionary<AnimationClip, AnimationClip> overrideLookup = new Dictionary<AnimationClip, AnimationClip>();
        for (int i = 0; i < clipOverrides.Count; i++)
        {
            AnimationClipOverrideEntry entry = clipOverrides[i];
            if (entry == null || entry.OriginalClip == null || entry.overrideClip == null)
            {
                continue;
            }

            overrideLookup[entry.OriginalClip] = entry.overrideClip;
        }

        if (overrideLookup.Count == 0)
        {
            return;
        }

        for (int i = 0; i < overrides.Count; i++)
        {
            KeyValuePair<AnimationClip, AnimationClip> currentOverride = overrides[i];
            if (!overrideLookup.TryGetValue(currentOverride.Key, out AnimationClip overrideClip))
            {
                continue;
            }

            overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(currentOverride.Key, overrideClip);
        }

        m_animatorOverrideController.ApplyOverrides(overrides);
        animator.runtimeAnimatorController = m_animatorOverrideController;
    }

    public override IEnumerator PerformTurn()
    {
        endTurn = false;
        TickSkillCooldowns();
        HandleChaosTurnStart();
        //结算状态
        yield return ProcessStatesOnTurnStart();
        //如果死亡就结束回合
        if(dead)
        {
            yield break;
        }
        EnterMoveDOT();
        yield return new WaitForSeconds(moveAnimDuration);

        if (chaosValue >= MaxChaosValue)
        {
            pendingChaosRecover = true;
            FloatingTipGenerator.Instance?.ShowTipAtObject(transform, $"{name}混沌过载，无法行动");
            EndTurn();
            yield break;
        }

        if (!CanActThisTurn())
        {
            FloatingTipGenerator.Instance?.ShowTipAtObject(transform, $"{name}受到震慑，无法行动");
            EndTurn();
            yield break;
        }

        if (IsActionEffectHalved)
        {
            FloatingTipGenerator.Instance?.ShowTipAtObject(transform, $"{name}混沌偏高，本回合行动效果减半");
        }
        //展示攻击逻辑
        yield return TurnStateManager.Instance.ChangeState(TurnState.InCharacterTurn, this);
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

    public bool IsActionEffectHalved => HasState(StateType.ChaosHalf);

    public float GetActionEffectMultiplier()
    {
        return HasState(StateType.ChaosHalf) ? 0.5f : 1f;
    }
    public override void Die()
    {
        base.Die();
    }
    public bool TryAddChaos(int amount)
    {
        if (amount <= 0 || chaosValue >= MaxChaosValue || dead)
        {
            return false;
        }

        int before = chaosValue;
        chaosValue = Mathf.Clamp(chaosValue + amount, 0, MaxChaosValue);
        if (chaosValue == before)
        {
            return false;
        }

        FloatingTipGenerator.Instance?.ShowTipAtObject(transform, $"混沌+{chaosValue - before} ({chaosValue}/{MaxChaosValue})");
        if (chaosValue >= MaxChaosValue)
        {
            FloatingTipGenerator.Instance?.ShowTipAtObject(transform, "混沌达到上限，下回合将无法行动");
        }

        UpdateChaosStates();
        return true;
    }

    public void SetChaos(int value)
    {
        chaosValue = Mathf.Clamp(value, 0, MaxChaosValue);
        UpdateChaosStates();
    }

    public int ReduceChaos(int amount)
    {
        if (amount <= 0 || dead)
        {
            return 0;
        }

        int before = chaosValue;
        chaosValue = Mathf.Clamp(chaosValue - amount, 0, MaxChaosValue);
        int reducedValue = before - chaosValue;
        if (reducedValue <= 0)
        {
            return 0;
        }

        FloatingTipGenerator.Instance?.ShowTipAtObject(transform, $"混沌-{reducedValue} ({chaosValue}/{MaxChaosValue})");
        UpdateChaosStates();
        return reducedValue;
    }
    #region  换人cd相关

    public void TriggerSwapCooldown()
    {
        switchCooldownRemaining = Mathf.Max(0f, switchCooldownMax);
    }

    public void SetSwitchCooldownMax(float value, bool clampCurrent = true)
    {
        switchCooldownMax = Mathf.Max(0f, value);
        if (clampCurrent)
        {
            switchCooldownRemaining = Mathf.Min(switchCooldownRemaining, switchCooldownMax);
        }
    }

    public float ReduceSwitchCooldown(float amount)
    {
        if (amount <= 0f || switchCooldownRemaining <= 0f)
        {
            return 0f;
        }

        float before = switchCooldownRemaining;
        switchCooldownRemaining = Mathf.Max(0f, switchCooldownRemaining - amount);
        return before - switchCooldownRemaining;
    }

    #endregion
    private void EnterMoveDOT()
    {
        m_originalPosition = transform.position;
        transform.DOMove(targetPos, moveAnimDuration).SetEase(moveAnimEase);
    }
    private void ExitMoveDOT()
    {
        transform.DOMove(m_originalPosition, moveAnimDuration).SetEase(moveAnimEase);
        m_originalPosition = Vector3.zero;
    }
    /// <summary>
    /// 根据当前混沌值自动添加/移除混沌半效和眩晕状态
    /// </summary>
    private void UpdateChaosStates()
    {
        // 先移除自身的混沌相关状态
        if (chaosValue < 3)
        {
            RemoveState(GetState(StateType.ChaosHalf));
            RemoveState(GetState(StateType.ChaosStun));
        }
        if (chaosValue >= 3 && chaosValue <= 4)
        {
            RemoveState(GetState(StateType.ChaosStun));
            AddState(StateType.ChaosHalf, this, 99);
        }
        else if (chaosValue >= MaxChaosValue)
        {
            AddState(StateType.ChaosHalf, this, 99);
            AddState(StateType.ChaosStun, this, 99);
        }
    }
    private void HandleChaosTurnStart()
    {
        if (!pendingChaosRecover)
        {
            // 每回合刷新混沌相关状态
            UpdateChaosStates();
            return;
        }

        pendingChaosRecover = false;
        SetChaos(ChaosRecoverValue);
        FloatingTipGenerator.Instance?.ShowTipAtObject(transform, $"{name}混沌回落至{ChaosRecoverValue}");
        UpdateChaosStates();
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

        maxHP = levelData.maxHP;
        currentHP = maxHP;
        attack = levelData.attack;
        defense = levelData.defense;
        critRate = levelData.critRate;
        critDamage = levelData.critDamage;
        K = levelData.K;
    }
}
