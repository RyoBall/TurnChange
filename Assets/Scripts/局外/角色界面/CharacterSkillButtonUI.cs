using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CharacterSkillButtonUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text skillNameText;

    private SkillBase m_skill;
    private Action<SkillBase> m_onClick;

    public RectTransform RectTransform => transform as RectTransform;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (skillNameText == null)
        {
            skillNameText = GetComponentInChildren<TMP_Text>(true);
        }

        if (iconImage == null)
        {
            iconImage = GetComponentInChildren<Image>(true);
        }
    }

    public void Bind(SkillBase skill, Action<SkillBase> onClick)
    {
        m_skill = skill;
        m_onClick = onClick;

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

        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }
    }

    private void HandleClick()
    {
        m_onClick?.Invoke(m_skill);
    }
}