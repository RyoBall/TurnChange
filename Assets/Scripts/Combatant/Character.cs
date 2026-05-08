using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Character : UnitCombatant
{
    public string characterID;
    public List<SkillBase> skills = new List<SkillBase>();
    public SkillBase enterSkill;//入场技能，回合开始时自动触发
    public SkillBase exitSkill;//退场技能，回合结束时自动触发

    bool endTurn = false;
    [Header("选中效果")]
    public float selectedScale = 1.1f;
    public float selectAnimDuration = 0.12f;
    private Vector3 m_defaultScale;
    private Tween m_scaleTween;
    [Header("动画精灵")]
    [SerializeField] private List<SpriteRenderer> spriteRenderer;
    private void Start()
    {
        LoadDataFromCSV();
        m_defaultScale = transform.localScale;
        foreach (var sr in spriteRenderer)
        {
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, participateInTurnLoopAtStart?1f:0f);
        }
    }
    
    public override IEnumerator PerformTurn()
    {
        endTurn=false;
        //结算状态
        ProcessStatesOnTurnStart();
        //展示攻击逻辑
        yield return CommandButtonManager.Instance.FadeInButtons(this);
        yield return new WaitUntil(() => endTurn);
        //结束玩家回合的内容
        yield return CommandButtonManager.Instance.FadeOutButtons();
    }
    public void EndTurn()
    {
        endTurn = true;
    }
    #region 选友相关

    private void OnMouseDown()
    {
        CharacterManager.Instance?.OnFieldCharacterClicked(this);
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
        yield return enterSequence.WaitForCompletion();
    }
    #endregion
    #region 技能相关
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
