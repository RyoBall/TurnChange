using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 设置 Panel：模态遮罩 + 居中面板。Prefab 挂到 Canvas 下，外部 Button.onClick 调用 Open()。
/// </summary>
public interface ISettingsPanelView
{
    bool IsOpen { get; }
    void Open();
    void Close();
    void QuitGame();
    void OnVolumeChanged(float value);
}

[DisallowMultipleComponent]
public class SettingsPanelView : MonoBehaviour, ISettingsPanelView
{
    private const string PanelFontResourcePath = "font/哥特";

    public static SettingsPanelView Instance { get; private set; }

    [SerializeField] private GameObject m_panelRoot;
    [SerializeField] private Slider m_volumeSlider;
    [SerializeField] private TMP_Text m_volumeValueText;

    public bool IsOpen => m_panelRoot != null && m_panelRoot.activeSelf;

    private void Awake()
    {
        EnsureModalCanvas();

        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[SettingsPanelView] 场景中存在多个 SettingsPanel，仅保留第一个 Instance。", this);
        }
        else
        {
            Instance = this;
        }

        if (m_panelRoot == null)
        {
            Debug.LogError("[SettingsPanelView] 未配置 m_panelRoot，请在 Prefab 中指定 PanelRoot 子节点。", this);
            return;
        }

        m_panelRoot.SetActive(false);
    }

    private void Start()
    {
        ApplyPanelTypography();
    }

    private void ApplyPanelTypography()
    {
        if (m_panelRoot == null)
        {
            return;
        }

        TMP_FontAsset panelFont = Resources.Load<TMP_FontAsset>(PanelFontResourcePath);
        if (panelFont == null)
        {
            Debug.LogWarning($"[SettingsPanelView] 未找到字体 Resources/{PanelFontResourcePath}，中文可能显示异常。", this);
            return;
        }

        TMP_Text[] texts = m_panelRoot.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            texts[i].font = panelFont;
            texts[i].color = Color.black;
        }
    }

    private void EnsureModalCanvas()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>打开设置 Panel — 绑定到外部 Button.onClick</summary>
    public void Open()
    {
        if (m_panelRoot == null)
        {
            Debug.LogError("[SettingsPanelView] 无法打开：m_panelRoot 未配置。", this);
            return;
        }

        SyncVolumeControls();
        transform.SetAsLastSibling();
        m_panelRoot.SetActive(true);
    }

    /// <summary>关闭设置 Panel（返回游戏）— 绑定到 Panel 内 Button.onClick</summary>
    public void Close()
    {
        if (m_panelRoot != null)
        {
            m_panelRoot.SetActive(false);
        }
    }

    /// <summary>退出游戏 — 绑定到 Panel 内 Button.onClick</summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>音量 Slider 回调 — 绑定 Slider.onValueChanged</summary>
    public void OnVolumeChanged(float value)
    {
        GameAudioVolumeController.SetMasterVolume(value);
        UpdateVolumeLabel(value);
    }

    private void SyncVolumeControls()
    {
        float volume = GameAudioVolumeController.MasterVolume;

        if (m_volumeSlider != null)
        {
            m_volumeSlider.SetValueWithoutNotify(volume);
        }

        UpdateVolumeLabel(volume);
    }

    private void UpdateVolumeLabel(float volume)
    {
        if (m_volumeValueText != null)
        {
            m_volumeValueText.text = $"{Mathf.RoundToInt(volume * 100f)}%";
        }
    }
}
