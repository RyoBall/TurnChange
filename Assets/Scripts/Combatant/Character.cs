using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Character : Combatant
{
    public List<SkillBase> skills = new List<SkillBase>();

    bool endTurn = false;
    [Header("选中效果")]
    public float selectedScale = 1.1f;
    public float selectAnimDuration = 0.12f;
    private Vector3 m_defaultScale;
    private Tween m_scaleTween;
    [Header("属性")]
    public int maxHP;
    public int currentHP;
    public float attack;
    private void Start()
    {
        m_defaultScale = transform.localScale;
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
}
