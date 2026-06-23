using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 制作人名单面板：屏幕渐暗后浮现名单，支持关闭按钮。
/// </summary>
[DisallowMultipleComponent]
public class CreditsPanelView : MonoBehaviour
{
    public static CreditsPanelView Instance { get; private set; }

    private const string DefaultCreditsText =
        "策划：容克Kaiser,雨达神\n程序：张良\n美术：Akane,猫猫\n特效/音效：Seabed";
    private const string DefaultThanksText = "感谢游玩我们的游戏！";

    [Header("面板")]
    [SerializeField] private GameObject m_panelRoot;
    [SerializeField] private Image m_dimOverlay;
    [SerializeField] private CanvasGroup m_contentCanvasGroup;
    [SerializeField] private TMP_Text m_creditsText;
    [SerializeField] private TMP_Text m_thanksText;
    [SerializeField] private Button m_closeButton;

    [Header("可选：背景渐暗（开始场景）")]
    [SerializeField] private RectTransform m_sceneBackgroundTransform;
    [SerializeField] private Image m_sceneBackgroundImage;
    [SerializeField] private Transform m_startSceneUiRoot;

    [Header("动画")]
    [SerializeField] private float m_dimDuration = 1.2f;
    [SerializeField] private float m_backgroundScaleMultiplier = 1.35f;
    [SerializeField] private float m_contentFadeDuration = 0.6f;
    [SerializeField] private Ease m_dimEase = Ease.InQuad;
    [SerializeField] private Ease m_exitEase = Ease.OutQuad;
    [SerializeField] private Ease m_contentEase = Ease.OutQuad;

    private bool m_isShowing;
    private Action m_onClosed;
    private Tween m_activeTween;
    private Vector3 m_initialBackgroundScale = Vector3.one;
    private Color m_initialBackgroundColor = Color.white;
    private Coroutine m_showCoroutine;

    private bool m_initialized;
    private readonly List<CanvasGroup> m_startUiCanvasGroups = new List<CanvasGroup>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureInitialized();
        HideImmediate();
    }

    private void OnDestroy()
    {
        KillActiveTween();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 播放渐暗动画并显示制作人名单。
    /// </summary>
    public void Show(Action onClosed = null, RectTransform backgroundTransform = null, Image backgroundImage = null, Transform startSceneUiRoot = null)
    {
        if (m_isShowing)
        {
            return;
        }

        EnsureInitialized();
        m_onClosed = onClosed;
        if (backgroundTransform != null)
        {
            m_sceneBackgroundTransform = backgroundTransform;
        }

        if (backgroundImage != null)
        {
            m_sceneBackgroundImage = backgroundImage;
        }

        if (startSceneUiRoot != null)
        {
            m_startSceneUiRoot = startSceneUiRoot;
        }

        CacheBackgroundState();
        CacheStartUiCanvasGroups();
        if (!isActiveAndEnabled)
        {
            StartCoroutine(ShowWhenReady());
            return;
        }

        m_showCoroutine = StartCoroutine(ShowCoroutine());
    }

    private IEnumerator ShowWhenReady()
    {
        EnsureInitialized();
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        yield return null;
        m_showCoroutine = StartCoroutine(ShowCoroutine());
    }

    /// <summary>
    /// 关闭制作人名单面板。
    /// </summary>
    public void Close()
    {
        if (!m_isShowing)
        {
            return;
        }

        if (m_showCoroutine != null)
        {
            StopCoroutine(m_showCoroutine);
            m_showCoroutine = null;
        }

        EnsureInitialized();
        StartCoroutine(CloseCoroutine());
    }

    private IEnumerator ShowCoroutine()
    {
        m_isShowing = true;
        ResetVisualState();
        SetDimOverlayRaycast(true);
        KillActiveTween();

        Sequence sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject);

        if (m_dimOverlay != null)
        {
            Color targetColor = m_dimOverlay.color;
            targetColor.a = 0.92f;
            sequence.Join(m_dimOverlay.DOColor(targetColor, m_dimDuration).SetEase(m_dimEase));
        }

        if (m_sceneBackgroundTransform != null && m_sceneBackgroundImage != null)
        {
            Vector3 targetScale = m_initialBackgroundScale * m_backgroundScaleMultiplier;
            sequence.Join(m_sceneBackgroundTransform.DOScale(targetScale, m_dimDuration).SetEase(m_dimEase));
            sequence.Join(m_sceneBackgroundImage.DOColor(Color.black, m_dimDuration).SetEase(m_dimEase));
        }

        JoinStartUiFade(sequence, 0f, m_dimDuration, m_dimEase);

        m_activeTween = sequence;
        yield return sequence.WaitForCompletion();
        m_activeTween = null;

        if (m_contentCanvasGroup != null)
        {
            m_contentCanvasGroup.gameObject.SetActive(true);
            m_contentCanvasGroup.alpha = 0f;
            m_contentCanvasGroup.transform.localScale = Vector3.one * 0.92f;

            Sequence contentSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject);
            contentSequence.Join(m_contentCanvasGroup.DOFade(1f, m_contentFadeDuration).SetEase(m_contentEase));
            contentSequence.Join(m_contentCanvasGroup.transform.DOScale(1f, m_contentFadeDuration).SetEase(m_contentEase));

            m_activeTween = contentSequence;
            yield return contentSequence.WaitForCompletion();
            m_activeTween = null;
        }

        m_showCoroutine = null;
    }

    private IEnumerator CloseCoroutine()
    {
        KillActiveTween();

        if (m_contentCanvasGroup != null)
        {
            Sequence contentSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject);
            contentSequence.Join(m_contentCanvasGroup.DOFade(0f, m_contentFadeDuration * 0.6f).SetEase(Ease.InQuad));
            contentSequence.Join(m_contentCanvasGroup.transform.DOScale(0.92f, m_contentFadeDuration * 0.6f).SetEase(Ease.InQuad));
            m_activeTween = contentSequence;
            yield return contentSequence.WaitForCompletion();
            m_activeTween = null;
            m_contentCanvasGroup.gameObject.SetActive(false);
        }

        Sequence exitSequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject);

        if (m_dimOverlay != null)
        {
            Color clearColor = m_dimOverlay.color;
            clearColor.a = 0f;
            exitSequence.Join(m_dimOverlay.DOColor(clearColor, m_dimDuration).SetEase(m_exitEase));
        }

        if (m_sceneBackgroundTransform != null && m_sceneBackgroundImage != null)
        {
            exitSequence.Join(m_sceneBackgroundTransform.DOScale(m_initialBackgroundScale, m_dimDuration).SetEase(m_exitEase));
            exitSequence.Join(m_sceneBackgroundImage.DOColor(m_initialBackgroundColor, m_dimDuration).SetEase(m_exitEase));
        }

        JoinStartUiFade(exitSequence, 1f, m_dimDuration, m_exitEase);

        m_activeTween = exitSequence;
        yield return exitSequence.WaitForCompletion();
        m_activeTween = null;

        FinalizeStartUiAfterFade(1f);
        HideImmediate();

        m_isShowing = false;
        Action callback = m_onClosed;
        m_onClosed = null;
        callback?.Invoke();
    }

    private void EnsureInitialized()
    {
        if (m_initialized)
        {
            return;
        }

        ResolveReferences();
        BindCloseButton();
        ApplyPanelTexts();
        m_initialized = true;
    }

    private void ResolveReferences()
    {
        if (m_panelRoot == null)
        {
            m_panelRoot = gameObject;
        }

        if (m_dimOverlay == null)
        {
            Transform overlay = transform.Find("DimOverlay");
            if (overlay != null)
            {
                m_dimOverlay = overlay.GetComponent<Image>();
            }
        }

        if (m_contentCanvasGroup == null)
        {
            Transform content = transform.Find("ContentPanel");
            if (content != null)
            {
                m_contentCanvasGroup = content.GetComponent<CanvasGroup>();
            }
        }

        if (m_creditsText == null)
        {
            Transform credits = transform.Find("ContentPanel/CreditsText");
            if (credits != null)
            {
                m_creditsText = credits.GetComponent<TMP_Text>();
            }
        }

        if (m_thanksText == null)
        {
            Transform thanks = transform.Find("ContentPanel/CreditsText/ThanksText");
            if (thanks != null)
            {
                m_thanksText = thanks.GetComponent<TMP_Text>();
            }
        }

        if (m_closeButton == null)
        {
            Transform close = transform.Find("ContentPanel/CloseButton");
            if (close != null)
            {
                m_closeButton = close.GetComponent<Button>();
            }
        }
    }

    private void BindCloseButton()
    {
        if (m_closeButton == null)
        {
            return;
        }

        m_closeButton.onClick.RemoveListener(Close);
        m_closeButton.onClick.AddListener(Close);
    }

    private void ApplyPanelTexts()
    {
        if (m_creditsText != null)
        {
            m_creditsText.text = DefaultCreditsText;
        }

        if (m_thanksText != null)
        {
            m_thanksText.text = DefaultThanksText;
        }
    }

    private void CacheStartUiCanvasGroups()
    {
        m_startUiCanvasGroups.Clear();
        if (m_startSceneUiRoot == null)
        {
            return;
        }

        foreach (Transform child in m_startSceneUiRoot)
        {
            if (IsStartUiBackgroundLayer(child))
            {
                continue;
            }

            CanvasGroup canvasGroup = child.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = child.gameObject.AddComponent<CanvasGroup>();
            }

            m_startUiCanvasGroups.Add(canvasGroup);
        }
    }

    private bool IsStartUiBackgroundLayer(Transform child)
    {
        if (m_sceneBackgroundTransform != null && child == m_sceneBackgroundTransform)
        {
            return true;
        }

        return child.name == "BackgroundLayer";
    }

    private void JoinStartUiFade(Sequence sequence, float targetAlpha, float duration, Ease ease)
    {
        if (m_startUiCanvasGroups.Count == 0)
        {
            return;
        }

        if (targetAlpha <= 0f)
        {
            SetStartUiInteractable(false);
        }

        for (int i = 0; i < m_startUiCanvasGroups.Count; i++)
        {
            CanvasGroup canvasGroup = m_startUiCanvasGroups[i];
            if (canvasGroup == null)
            {
                continue;
            }

            sequence.Join(canvasGroup.DOFade(targetAlpha, duration).SetEase(ease));
        }
    }

    private void SetStartUiInteractable(bool interactable)
    {
        for (int i = 0; i < m_startUiCanvasGroups.Count; i++)
        {
            CanvasGroup canvasGroup = m_startUiCanvasGroups[i];
            if (canvasGroup == null)
            {
                continue;
            }

            canvasGroup.interactable = interactable;
            canvasGroup.blocksRaycasts = interactable;
        }
    }

    private void FinalizeStartUiAfterFade(float targetAlpha)
    {
        for (int i = 0; i < m_startUiCanvasGroups.Count; i++)
        {
            CanvasGroup canvasGroup = m_startUiCanvasGroups[i];
            if (canvasGroup == null)
            {
                continue;
            }

            canvasGroup.alpha = targetAlpha;
        }

        if (targetAlpha > 0f)
        {
            SetStartUiInteractable(true);
        }
    }

    private void RestoreStartUiImmediate()
    {
        FinalizeStartUiAfterFade(1f);
    }

    private void CacheBackgroundState()
    {
        if (m_sceneBackgroundTransform != null)
        {
            m_initialBackgroundScale = m_sceneBackgroundTransform.localScale;
        }

        if (m_sceneBackgroundImage != null)
        {
            m_initialBackgroundColor = m_sceneBackgroundImage.color;
        }
    }

    private void SetDimOverlayRaycast(bool enabled)
    {
        if (m_dimOverlay != null)
        {
            m_dimOverlay.raycastTarget = enabled;
        }
    }

    private void ResetVisualState()
    {
        if (m_dimOverlay != null)
        {
            Color color = m_dimOverlay.color;
            color.a = 0f;
            m_dimOverlay.color = color;
        }

        if (m_contentCanvasGroup != null)
        {
            m_contentCanvasGroup.alpha = 0f;
            m_contentCanvasGroup.gameObject.SetActive(false);
        }
    }

    private void HideImmediate()
    {
        m_isShowing = false;
        ResetVisualState();
        SetDimOverlayRaycast(false);
        RestoreStartUiImmediate();
    }

    private void KillActiveTween()
    {
        if (m_activeTween != null && m_activeTween.IsActive())
        {
            m_activeTween.Kill();
        }

        m_activeTween = null;
    }
}
