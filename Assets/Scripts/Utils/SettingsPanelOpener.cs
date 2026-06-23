using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 挂到现有 Button 上，Awake 时自动绑定 onClick；也可在 Inspector 手动调用 OnButtonClick。
/// </summary>
[DisallowMultipleComponent]
public class SettingsPanelOpener : MonoBehaviour
{
    [SerializeField] private SettingsPanelView m_settingsPanel;
    [SerializeField] private Button m_button;

    private void Awake()
    {
        BindButton();
    }

    private void OnEnable()
    {
        BindButton();
    }

    private void OnDisable()
    {
        if (m_button != null)
        {
            m_button.onClick.RemoveListener(OnButtonClick);
        }
    }

    /// <summary>Button.onClick 绑定此方法</summary>
    public void OnButtonClick()
    {
        SettingsPanelView panel = SettingsPanelView.Instance != null
            ? SettingsPanelView.Instance
            : m_settingsPanel;
        if (panel == null)
        {
            Debug.LogWarning("[SettingsPanelOpener] 未找到 SettingsPanelView，请把 SettingsPanel Prefab 放入场景。", this);
            return;
        }

        panel.Open();
    }

    private void BindButton()
    {
        if (m_button == null)
        {
            m_button = GetComponent<Button>();
        }

        if (m_button == null)
        {
            return;
        }

        m_button.onClick.RemoveListener(OnButtonClick);
        m_button.onClick.AddListener(OnButtonClick);
    }
}
