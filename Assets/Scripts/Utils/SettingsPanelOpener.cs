using UnityEngine;

/// <summary>
/// 挂到现有 Button 上，Inspector onClick 调用 OnButtonClick 即可打开设置 Panel。
/// </summary>
[DisallowMultipleComponent]
public class SettingsPanelOpener : MonoBehaviour
{
    [SerializeField] private SettingsPanelView m_settingsPanel;

    /// <summary>Button.onClick 绑定此方法</summary>
    public void OnButtonClick()
    {
        SettingsPanelView panel = m_settingsPanel != null ? m_settingsPanel : SettingsPanelView.Instance;
        if (panel == null)
        {
            Debug.LogWarning("[SettingsPanelOpener] 未找到 SettingsPanelView，请把 Prefab 拖入场景并赋值。", this);
            return;
        }

        panel.Open();
    }
}
