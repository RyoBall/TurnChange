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
    void OnCondensedModeChanged(bool isOn);
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
    [SerializeField] private Toggle m_condensedModeToggle;

    private bool m_hasPushedTimeScalePause;

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
        EnsureComponentsInitialized();
    }

    private void Start()
    {
        EnsureComponentsInitialized();
        ApplyPanelTypography();
    }

    private void OnDestroy()
    {
        ReleaseTimeScalePause();
        m_fadeTween?.Kill();
        m_fadeTween = null;

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void EnsureComponentsInitialized()
    {
        if (m_canvasGroup == null)
        {
            m_canvasGroup = GetComponent<CanvasGroup>();
            if (m_canvasGroup == null)
            {
                m_canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            m_canvasGroup.alpha = 0f;
            m_canvasGroup.interactable = false;
            m_canvasGroup.blocksRaycasts = false;
        }

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

        Canvas rootCanvas = transform.parent != null
            ? transform.parent.GetComponentInParent<Canvas>()
            : null;

        if (rootCanvas != null
            && rootCanvas.renderMode == RenderMode.ScreenSpaceCamera
            && rootCanvas.worldCamera != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = rootCanvas.worldCamera;
            canvas.planeDistance = rootCanvas.planeDistance;
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
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
        EnsureComponentsInitialized();

        if (m_canvasGroup == null)
        {
            Debug.LogError("[SettingsPanelView] 无法打开：CanvasGroup 初始化失败。", this);
            return;
        }

        if (m_panelRoot == null)
        {
            Debug.LogError("[SettingsPanelView] 无法打开：m_panelRoot 未配置。", this);
            return;
        }

        if (IsOpen)
        {
            return;
        }

        PushTimeScalePause();

        SyncVolumeControls();
        SyncCondensedModeToggle();
        transform.SetAsLastSibling();
        FadeIn();
    }

    /// <summary>关闭设置 Panel（返回游戏）— 绑定到 Panel 内 Button.onClick</summary>
    public void Close()
    {
        EnsureComponentsInitialized();

        if (m_canvasGroup == null)
        {
            return;
        }

        ReleaseTimeScalePause();
        FadeOut();
    }

    /// <summary>退出游戏 — 绑定到 Panel 内 Button.onClick</summary>
    public void QuitGame()
    {
        ReleaseTimeScalePause();
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

    /// <summary>浓缩模式 Toggle 回调 — 绑定 Toggle.onValueChanged</summary>
    public void OnCondensedModeChanged(bool isOn)
    {
        CondensedModePreference.SetEnabled(isOn);
    }

    private void PushTimeScalePause()
    {
        if (m_hasPushedTimeScalePause)
        {
            return;
        }

        ITimeScaleController timeScale = TimeScaleController.Instance;
        if (timeScale == null)
        {
            return;
        }

        timeScale.PushPause();
        m_hasPushedTimeScalePause = true;
    }

    private void ReleaseTimeScalePause()
    {
        if (!m_hasPushedTimeScalePause)
        {
            return;
        }

        ITimeScaleController timeScale = TimeScaleController.Instance;
        timeScale?.PopPause();
        m_hasPushedTimeScalePause = false;
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

    private void SyncCondensedModeToggle()
    {
        if (m_condensedModeToggle == null)
        {
            return;
        }

        m_condensedModeToggle.SetIsOnWithoutNotify(CondensedModePreference.IsEnabled);
    }

    private void UpdateVolumeLabel(float volume)
    {
        if (m_volumeValueText != null)
        {
            m_volumeValueText.text = $"{Mathf.RoundToInt(volume * 100f)}%";
        }
    }
}
