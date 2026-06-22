using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 退出游戏按钮：挂到开始界面等场景的 Button 上，Awake 时自动绑定 onClick。
/// </summary>
[DisallowMultipleComponent]
public class QuitGameButton : MonoBehaviour
{
    [SerializeField] private Button m_button;

    private bool m_isQuitting;

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
            m_button.onClick.RemoveListener(OnQuitClicked);
        }
    }

    /// <summary>Button.onClick 绑定此方法</summary>
    public void OnQuitClicked()
    {
        if (m_isQuitting)
        {
            return;
        }

        m_isQuitting = true;
        if (m_button != null)
        {
            m_button.interactable = false;
        }

        QuitApplication();
    }

    private static void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
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

        m_button.onClick.RemoveListener(OnQuitClicked);
        m_button.onClick.AddListener(OnQuitClicked);
    }
}
