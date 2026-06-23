using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 制作人名单按钮：点击后播放渐暗动画并显示名单面板。
/// </summary>
[DisallowMultipleComponent]
public class CreditsPanelButton : MonoBehaviour
{
    [SerializeField] private CreditsPanelView m_creditsPanel;
    [SerializeField] private Button m_button;
    [SerializeField] private RectTransform m_backgroundTransform;
    [SerializeField] private Image m_backgroundImage;
    [SerializeField] private Transform m_startSceneUiRoot;

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
        CreditsPanelView panel = CreditsPanelView.Instance != null
            ? CreditsPanelView.Instance
            : m_creditsPanel;
        if (panel == null)
        {
            Debug.LogWarning("[CreditsPanelButton] 未找到 CreditsPanelView，请将 CreditsPanel 预制体放入场景。", this);
            return;
        }

        panel.Show(null, m_backgroundTransform, m_backgroundImage, ResolveStartSceneUiRoot());
    }

    private Transform ResolveStartSceneUiRoot()
    {
        if (m_startSceneUiRoot != null)
        {
            return m_startSceneUiRoot;
        }

        Transform parent = transform.parent;
        return parent != null && parent.name == "Main" ? parent : null;
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
