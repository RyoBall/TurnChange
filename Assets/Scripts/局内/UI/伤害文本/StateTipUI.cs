using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 状态漂浮提示 UI 组件 — 挂载到状态提示预制体上
/// 包含一个 Image（状态图标）和一个 TMP_Text（状态名称）
/// </summary>
public class StateTipUI : MonoBehaviour
{
    [Header("UI 组件")]
    [SerializeField] private Image m_stateIcon;
    [SerializeField] private TMP_Text m_stateNameText;

    /// <summary>设置状态图标和名称</summary>
    public void SetState(Sprite icon, string stateName)
    {
        if (m_stateIcon != null)
        {
            m_stateIcon.sprite = icon;
            m_stateIcon.enabled = icon != null;
        }

        if (m_stateNameText != null)
        {
            m_stateNameText.text = stateName ?? string.Empty;
        }
    }

    /// <summary>仅设置图标</summary>
    public void SetIcon(Sprite icon)
    {
        if (m_stateIcon != null)
        {
            m_stateIcon.sprite = icon;
            m_stateIcon.enabled = icon != null;
        }
    }

    /// <summary>仅设置名称</summary>
    public void SetStateName(string stateName)
    {
        if (m_stateNameText != null)
        {
            m_stateNameText.text = stateName ?? string.Empty;
        }
    }

    /// <summary>获取 Image 组件引用</summary>
    public Image StateIcon => m_stateIcon;

    /// <summary>获取 TMP_Text 组件引用</summary>
    public TMP_Text StateNameText => m_stateNameText;

    /// <summary>运行时绑定 UI 组件引用（用于动态创建时）</summary>
    public void BindComponents(Image icon, TMP_Text nameText)
    {
        m_stateIcon = icon;
        m_stateNameText = nameText;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (m_stateIcon == null)
        {
            m_stateIcon = GetComponentInChildren<Image>();
        }
        if (m_stateNameText == null)
        {
            m_stateNameText = GetComponentInChildren<TMP_Text>();
        }
    }
#endif
}
