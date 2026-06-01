using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class CharacterSkillButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text skillNameText;

    private SkillBase m_skill;

    public RectTransform RectTransform => transform as RectTransform;

    private void Awake()
    {
        if (skillNameText == null)
        {
            skillNameText = GetComponentInChildren<TMP_Text>(true);
        }

        if (iconImage == null)
        {
            iconImage = GetComponentInChildren<Image>(true);
        }
    }

    public void Bind(SkillBase skill)
    {
        m_skill = skill;

        if (skillNameText != null)
        {
            skillNameText.text = skill != null && !string.IsNullOrWhiteSpace(skill.skillName)
                ? skill.skillName
                : "未配置技能";
        }

        if (iconImage != null)
        {
            //暂且默认
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // call panel to show description for this skill
        CharacterPanelView panel = FindObjectOfType<CharacterPanelView>();
        panel?.ShowSkillDescription(m_skill);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // call panel to hide description
        CharacterPanelView panel = FindObjectOfType<CharacterPanelView>();
        panel?.HideSkillDescription();
    }
}