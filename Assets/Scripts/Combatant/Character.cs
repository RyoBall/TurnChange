using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using MoreMountains.Feedbacks;

public class Character : UnitCombatant

{
    public string characterID;
    public List<CharacterSkillType> skills = new List<CharacterSkillType>();
    public CharacterSkillType enterSkill;//入场技能，回合开始时自动触发
    public CharacterSkillType exitSkill;//退场技能，回合结束时自动触发

    [Header("混沌值")]
    [SerializeField, Range(0, MaxChaosValue)] private int chaosValue = 0;
    [SerializeField] private bool pendingChaosRecover;
    private const int MaxChaosValue = 5;
    private const int ChaosRecoverValue = 2;
    public int ChaosValue=> chaosValue;
    public int MaxChaosValueConst => MaxChaosValue;
    bool endTurn = false;
    [Header("选中效果")]
    public float selectedScale = 1.1f;
    public float selectAnimDuration = 0.12f;
    private Vector3 m_defaultScale;
    private Tween m_scaleTween;
    [Header("动画精灵")]
    [SerializeField] private List<SpriteRenderer> spriteRenderer;
    [SerializeField] private List<CanvasGroup> slidersCanvasGroups;
    private void Start()
    {
        LoadDataFromCSV();
        m_defaultScale = transform.localScale;
        foreach (var sr in spriteRenderer)
        {
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, participateInTurnLoopAtStart ? 1f : 0f);
        }
    }

    public override IEnumerator PerformTurn()
    {
        endTurn = false;
        TickSkillCooldowns();
        HandleChaosTurnStart();
        //结算状态
        ProcessStatesOnTurnStart();

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
        enterFeedback?.PlayFeedbacks();
        //展示攻击逻辑
        yield return TurnStateManager.Instance.ChangeState(TurnState.InCharacterTurn, this);
        yield return new WaitUntil(() => endTurn);
        //结束玩家回合的内容
        yield return TurnStateManager.Instance.ChangeState(TurnState.OutCharacterTurn, this);
        yield return new WaitForSeconds(0.2f);
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


    public bool TryAddChaos(int amount)
    {
        if (amount <= 0 || chaosValue >= MaxChaosValue)
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
            enterFeedback?.PlayFeedbacks();
            SkillDescription.Instance.ChangeDescription(SkillDictionaryManager.GetSkill(exitSkill));
        }
    }
    private void OnMouseExit()
    {
        if (CharacterManager.Instance.IsSelectingFieldCharacter)
            SkillDescription.Instance.ChangeDescription(null);
    }
    private void OnMouseDown()
    {
        if (CharacterManager.Instance.IsSelectingFieldCharacter)
        {
            CharacterManager.Instance?.OnFieldCharacterClicked(this);
            SkillDescription.Instance.ChangeDescription(null);
        }
        SkillManager.Instance?.OnCharacterClicked(this);
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
    public IEnumerator PlayExitAnimation()
    {
        //简单的退场动画：向右移动并淡出
        Vector3 targetPosition = transform.position + new Vector3(2f, 0, 0);
        float duration = 0.5f;
        Sequence exitSequence = DOTween.Sequence();
        exitSequence.Append(transform.DOMove(targetPosition, duration).SetEase(Ease.InBack));
        foreach (var sr in spriteRenderer)
        {
            exitSequence.Join(sr.DOFade(0, duration));
        }
        foreach (var cg in slidersCanvasGroups)
        {
            exitSequence.Join(cg.DOFade(0, duration));
        }
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
        foreach (var sr in spriteRenderer)
        {
            enterSequence.Join(sr.DOFade(1, duration));
        }
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
        if (skills == null)
        {
            return;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            SkillDictionaryManager.GetSkill(skills[i])?.TickCooldown(this);
        }
    }

    #endregion
    #region   //读取数据
    public void LoadDataFromCSV()
    {
        if (string.IsNullOrEmpty(characterID))
        {
            Debug.Log("CharacterID is null or empty for " + gameObject.name);
            return;
        }
        var levelDataDict = LevelDataContainer.CharacterLevelData[characterID];
        var levelData = levelDataDict[level];
        maxHP = levelData.maxHP;
        currentHP = maxHP;
        attack = levelData.attack;
        defense = levelData.defense;
    }
    #endregion
}
