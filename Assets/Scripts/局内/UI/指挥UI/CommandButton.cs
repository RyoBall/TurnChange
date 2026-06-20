using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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
    public static event System.Action OnChangeButtonClicked;
    public CommandButtonState buttonState = CommandButtonState.Character;
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
    private bool m_isPointerOver;

    public bool HasSkill => m_skill != null;
    public bool IsChangeSkillButton =>
        m_skill is CommandSkillBase commandSkill && commandSkill.commandSkillType == CommandSkillType.Change;

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

        if (CharacterManager.Instance != null)
        {
            CharacterManager.Instance.OnReserveSwapAvailabilityChanged += HandleReserveSwapAvailabilityChanged;
        }
    }

    private void OnDisable()
    {
        SkillExecuteManager.OnSkillExecuted -= HandleSkillExecuted;
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnOrderChanged -= HandleTurnOrderChanged;
        }

        if (CharacterManager.Instance != null)
        {
            CharacterManager.Instance.OnReserveSwapAvailabilityChanged -= HandleReserveSwapAvailabilityChanged;
        }

        // 安全复位：防止鼠标离开事件丢失导致按钮保持放大
        ResetScaleImmediate();
        m_isPointerOver = false;
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
        if(m_skill is CommandSkillBase commandSkill && commandSkill.commandSkillType == CommandSkillType.Change)
        {
            OnChangeButtonClicked?.Invoke();
        }
        SkillExecuteManager.ExecuteSkill(m_owner, m_skill, m_skill is CommandSkillBase);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        m_isPointerOver = true;
        SkillDescription.Instance?.ChangeDescription(m_skill);
        PlaySelectAnimation();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        m_isPointerOver = false;
        SkillDescription.Instance?.HideDescription();
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

        // 技能变更后，如果鼠标不在按钮上，确保缩放复位
        if (!m_isPointerOver)
        {
            ResetScaleImmediate();
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
                    bool hasChangerTurn = TurnManager.Instance != null && TurnManager.Instance.HasChangerTurn();
                    bool canStartSwapFlow = CharacterManager.Instance != null && CharacterManager.Instance.CanStartSwapFlow();
                    button.interactable = !hasChangerTurn && canStartSwapFlow;
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

    private void HandleReserveSwapAvailabilityChanged()
    {
        RefreshInformation();
    }

    public void PlaySelectAnimation()
    {
        if (!HasSkill)
            return;

        KillScaleTween();
        var target = m_defaultScale * selectedScale;
        var t = m_rectTransform != null ? m_rectTransform : (RectTransform)transform;
        m_scaleTween = t.DOScale(target, selectAnimDuration).SetEase(Ease.OutQuad);
    }

    public void PlayDeselectAnimation(bool immediate = false)
    {
        KillScaleTween();

        var t = m_rectTransform != null ? m_rectTransform : (RectTransform)transform;
        if (immediate)
        {
            t.localScale = m_defaultScale;
        }
        else
        {
            m_scaleTween = t.DOScale(m_defaultScale, selectAnimDuration).SetEase(Ease.OutQuad);
        }
    }

    /// <summary>
    /// 立即复位缩放，不做动画。用于 OnDisable 等需要同步复位的场景。
    /// </summary>
    private void ResetScaleImmediate()
    {
        KillScaleTween();
        var t = m_rectTransform != null ? m_rectTransform : (RectTransform)transform;
        if (t != null)
        {
            t.localScale = m_defaultScale;
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
