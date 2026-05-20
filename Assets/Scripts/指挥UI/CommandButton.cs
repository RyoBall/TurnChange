using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum CommandButtonState
{
    Character,
    Command
}
public class CommandButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public CommandButtonState buttonState = CommandButtonState.Character;
    public Image skillIcon;
    public TMP_Text skillNameText;
    public TMP_Text skillDescriptionText;
    public float selectedScale = 1.08f;
    public float selectAnimDuration = 0.12f;

    [Header("Feedback")]
    [SerializeField] private MMF_Player pointerEnterFeedback;
    [SerializeField] private MMF_Player pointerExitFeedback;

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

    private void OnEnable()
    {
        SkillExecuteManager.OnSkillExecuted += HandleSkillExecuted;
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnOrderChanged += HandleTurnOrderChanged;
        }
    }

    private void OnDisable()
    {
        SkillExecuteManager.OnSkillExecuted -= HandleSkillExecuted;
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnOrderChanged -= HandleTurnOrderChanged;
        }
    }

    void Start()
    {
        StartCoroutine(ReadyTurnInitialization());
    }
    IEnumerator ReadyTurnInitialization()
    {
        while (!TurnManager.Instance.IsTurnInitialized)
        {
            GetComponent<Button>().interactable = false;
            yield return null;
        }
        RefreshInformation();
    }

    public void OnButtonClicked()
    {
        if (m_skill == null)
        {
            Debug.LogWarning($"[CommandButton] {name} 未绑定技能或角色");
            return;
        }

        SkillExecuteManager.ExecuteSkill(m_owner, m_skill);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerEnterFeedback?.PlayFeedbacks();
        SkillDescription.Instance?.ChangeDescription(m_skill);
        PlaySelectAnimation();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerExitFeedback?.PlayFeedbacks();
        SkillDescription.Instance?.ChangeDescription(null);
        PlayDeselectAnimation();
    }

    public void BindSkill(Character owner, SkillBase skill)//注入当前角色和技能
    {
        if (skill == null)
            return;
        m_owner = owner;
        m_skill = skill;

        RefreshInformation();

        if (skill == null)
        {
            PlayDeselectAnimation(true);
        }
    }

    void RefreshInformation()
    {
        if (skillDescriptionText != null)
        {
            skillDescriptionText.text = m_skill != null
                ? (!string.IsNullOrWhiteSpace(m_skill.shortDescription) ? m_skill.shortDescription : m_skill.description)
                : string.Empty;
        }

        if (skillIcon != null)
        {
            skillIcon.sprite = m_skill != null ? m_skill.icon : null;
            skillIcon.enabled = m_skill != null && m_skill.icon != null;
        }
        var button = GetComponent<Button>();
        var charaSkill = m_skill as CharacterSkillBase;
        if (button != null)
        {
            if (charaSkill != null)
            {
                int cooldown = charaSkill.GetRemainingCooldown(m_owner);
                button.interactable = cooldown <= 0;
                if (skillNameText != null)
                {
                    skillNameText.text = cooldown > 0 ? $"{charaSkill.skillName} (冷却中:{cooldown})" : charaSkill.skillName;
                }
            }
            else
            {
                var commandSkill = m_skill as CommandSkillBase;
                if (commandSkill != null && commandSkill.commandSkillType == CommandSkillType.Change)
                {
                    button.interactable = TurnManager.Instance == null || !TurnManager.Instance.HasChangerTurn();
                }
                else
                {
                    button.interactable = true;
                }

                if (skillNameText != null&& m_skill != null)
                {
                    skillNameText.text = m_skill.skillName;
                }
            }
        }
    }

    private void HandleSkillExecuted(UnitCombatant owner, SkillBase skill)
    {
        if (m_skill == null || skill != m_skill)
        {
            return;
        }

        if (m_owner != owner)
        {
            return;
        }

        RefreshInformation();
    }

    private void HandleTurnOrderChanged()
    {
        RefreshInformation();
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
            m_scaleTween = m_rectTransform.DOScale(target, selectAnimDuration).SetEase(Ease.OutQuad);
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
