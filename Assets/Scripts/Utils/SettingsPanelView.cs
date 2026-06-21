using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 设置 Panel：模态遮罩 + 居中面板。通过 CanvasGroup alpha 渐变控制显隐。
/// GameObject 始终保持激活，Awake 中注册单例。
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
[RequireComponent(typeof(CanvasGroup))]
public class SettingsPanelView : MonoBehaviour, ISettingsPanelView
{
    private const string PanelFontResourcePath = "font/哥特";
    private const float FadeDuration = 0.25f;

    public static SettingsPanelView Instance { get; private set; }

    [SerializeField] private GameObject m_panelRoot;
    [SerializeField] private Slider m_volumeSlider;
    [SerializeField] private TMP_Text m_volumeValueText;

    /// <summary>打开面板前的时间流速缓存，关闭时恢复</summary>
    private float m_cachedTimeScale = 1.5f;

    private CanvasGroup m_canvasGroup;
    private Tweener m_fadeTween;

    public bool IsOpen => m_canvasGroup != null && m_canvasGroup.alpha > 0.5f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializeComponents();
    }

    private void Start()
    {
        ApplyPanelTypography();
    }

    private void OnDestroy()
    {
        m_fadeTween?.Kill();
        m_fadeTween = null;

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void InitializeComponents()
    {
        m_canvasGroup = GetComponent<CanvasGroup>();
        m_canvasGroup.alpha = 0f;
        m_canvasGroup.interactable = false;
        m_canvasGroup.blocksRaycasts = false;

        EnsureModalCanvas();

        if (m_panelRoot == null)
        {
            Debug.LogError("[SettingsPanelView] 未配置 m_panelRoot，请在 Prefab 中指定 PanelRoot 子节点。", this);
        }
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

    /// <summary>打开设置 Panel — 绑定到外部 Button.onClick</summary>
    public void Open()
    {
        if (m_panelRoot == null)
        {
            Debug.LogError("[SettingsPanelView] 无法打开：m_panelRoot 未配置。", this);
            return;
        }

        // 缓存当前时间流速并暂停
        ITimeScaleController timeScale = TimeScaleController.Instance;
        if (timeScale != null)
        {
            m_cachedTimeScale = timeScale.CurrentTimeScale;
            timeScale.Pause();
        }

        SyncVolumeControls();
        transform.SetAsLastSibling();
        FadeIn();
    }

    /// <summary>关闭设置 Panel（返回游戏）— 绑定到 Panel 内 Button.onClick</summary>
    public void Close()
    {
        FadeOut();

        // 恢复时间流速
        ITimeScaleController timeScale = TimeScaleController.Instance;
        if (timeScale != null)
        {
            timeScale.SetTimeScale(m_cachedTimeScale);
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

    private void FadeIn()
    {
        m_fadeTween?.Kill();

        m_canvasGroup.interactable = true;
        m_canvasGroup.blocksRaycasts = true;
        m_fadeTween = m_canvasGroup.DOFade(1f, FadeDuration).SetUpdate(true);
    }

    private void FadeOut()
    {
        m_fadeTween?.Kill();

        m_canvasGroup.interactable = false;
        m_canvasGroup.blocksRaycasts = false;
        m_fadeTween = m_canvasGroup.DOFade(0f, FadeDuration).SetUpdate(true);
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
