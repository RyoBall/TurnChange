using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CommandButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image skillIcon;
    public TMP_Text skillNameText;
    public TMP_Text skillDescriptionText;
    public float selectedScale = 1.08f;
    public float selectAnimDuration = 0.12f;

    private Character m_owner;
    private SkillBase m_skill;
    private RectTransform m_rectTransform;
    private Vector3 m_defaultScale;
    private Tween m_scaleTween;

    public bool HasSkill => m_skill != null;

    private void Awake()
    {
        m_rectTransform = GetComponent<RectTransform>();
        m_defaultScale = m_rectTransform != null ? m_rectTransform.localScale : transform.localScale;
    }

    public void OnButtonClicked()
    {
        if (m_owner == null || m_skill == null)
        {
            Debug.LogWarning($"[CommandButton] {name} 未绑定技能或角色");
            return;
        }

        SkillExecuteManager.ExecuteSkill(m_owner, m_skill);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!HasSkill)
            return;

        CommandButtonManager.Instance?.OnButtonPointerEnter(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CommandButtonManager.Instance?.OnButtonPointerExit(this);
    }

    public void BindSkill(Character owner, SkillBase skill)//注入当前角色和技能
    {
        m_owner = owner;
        m_skill = skill;

        InitializeInformation(skill);

        var button = GetComponent<Button>();
        if (button != null)
        {
            button.interactable = skill != null;
        }

        if (skill == null)
        {
            PlayDeselectAnimation(true);
        }
    }

    public void InitializeInformation(SkillBase skill)//初始化按钮显示信息
    {
        if (skillIcon != null)
        {
            skillIcon.sprite = skill != null ? skill.icon : null;
            skillIcon.enabled = skill != null && skill.icon != null;
        }

        if (skillNameText != null)
        {
            skillNameText.text = skill != null ? skill.skillName : string.Empty;
        }

        if (skillDescriptionText != null)
        {
            skillDescriptionText.text = skill != null ? skill.description : string.Empty;
        }
    }

    public void PlaySelectAnimation()
    {
        if (!HasSkill)
            return;

        KillScaleTween();
        var target = m_defaultScale * selectedScale;

        if (m_rectTransform != null)
        {
            m_scaleTween = m_rectTransform.DOScale(target, selectAnimDuration).SetEase(Ease.OutQuad);
        }
        else
        {
            m_scaleTween = transform.DOScale(target, selectAnimDuration).SetEase(Ease.OutQuad);
        }
    }

    public void PlayDeselectAnimation(bool immediate = false)
    {
        KillScaleTween();

        if (m_rectTransform != null)
        {
            if (immediate)
            {
                m_rectTransform.localScale = m_defaultScale;
            }
            else
            {
                m_scaleTween = m_rectTransform.DOScale(m_defaultScale, selectAnimDuration).SetEase(Ease.OutQuad);
            }
        }
        else
        {
            if (immediate)
            {
                transform.localScale = m_defaultScale;
            }
            else
            {
                m_scaleTween = transform.DOScale(m_defaultScale, selectAnimDuration).SetEase(Ease.OutQuad);
            }
        }
    }

    private void KillScaleTween()
    {
        if (m_scaleTween == null)
            return;

        m_scaleTween.Kill();
        m_scaleTween = null;
    }
}
