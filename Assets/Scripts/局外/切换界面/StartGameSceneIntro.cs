using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 开始场景进入主界面前的背景过场：背景放大并渐暗至全黑。
/// </summary>
[DisallowMultipleComponent]
public class StartGameSceneIntro : MonoBehaviour
{
    [Header("背景目标")]
    [SerializeField] private RectTransform m_backgroundTransform;
    [SerializeField] private Image m_backgroundImage;

    [Header("按钮隐藏")]
    [SerializeField] private Transform m_uiRoot;
    [SerializeField] private CanvasGroup m_startButtonCanvasGroup;

    [Header("动画参数")]
    [SerializeField] private float m_duration = 1.2f;
    [SerializeField] private float m_targetScaleMultiplier = 1.35f;
    [SerializeField] private Ease m_ease = Ease.InQuad;

    private Vector3 m_initialScale = Vector3.one;
    private Color m_initialColor = Color.white;
    private Tween m_activeTween;

    private void Awake()
    {
        ResolveReferences();
        CacheInitialState();
    }

    private void OnDestroy()
    {
        KillActiveTween();
    }

    private void ResolveReferences()
    {
        if (m_uiRoot == null)
        {
            m_uiRoot = transform.parent;
        }

        if (m_startButtonCanvasGroup == null)
        {
            m_startButtonCanvasGroup = GetComponent<CanvasGroup>();
        }
    }

    private void CacheInitialState()
    {
        if (m_backgroundTransform != null)
        {
            m_initialScale = m_backgroundTransform.localScale;
        }

        if (m_backgroundImage != null)
        {
            m_initialColor = m_backgroundImage.color;
        }
    }

    /// <summary>
    /// 隐藏开场 UI（背景过场开始前调用）。
    /// </summary>
    public void HideUiForIntro()
    {
        ResolveReferences();
        HideButtonsAtIntroStart();
    }

    /// <summary>
    /// 播放背景放大并渐暗至黑色的过场动画。
    /// </summary>
    public IEnumerator PlayIntroCoroutine()
    {
        HideUiForIntro();

        if (m_backgroundTransform == null || m_backgroundImage == null)
        {
            Debug.LogWarning("[StartGameSceneIntro] 未配置背景引用，跳过预过场动画。", this);
            yield break;
        }

        KillActiveTween();

        Vector3 targetScale = m_initialScale * m_targetScaleMultiplier;
        Sequence sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject);

        sequence.Join(m_backgroundTransform.DOScale(targetScale, m_duration).SetEase(m_ease));
        sequence.Join(m_backgroundImage.DOColor(Color.black, m_duration).SetEase(m_ease));

        m_activeTween = sequence;
        yield return sequence.WaitForCompletion();
        m_activeTween = null;
    }

    private void HideButtonsAtIntroStart()
    {
        Transform uiRoot = m_uiRoot != null ? m_uiRoot : transform.parent;
        if (uiRoot == null)
        {
            HideStartButtonVisual();
            return;
        }

        foreach (Transform child in uiRoot)
        {
            if (IsBackgroundLayer(child))
            {
                continue;
            }

            if (child.gameObject == gameObject)
            {
                HideStartButtonVisual();
            }
            else
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    private bool IsBackgroundLayer(Transform child)
    {
        if (m_backgroundTransform != null && child == m_backgroundTransform)
        {
            return true;
        }

        return child.name == "BackgroundLayer";
    }

    private void HideStartButtonVisual()
    {
        if (m_startButtonCanvasGroup != null)
        {
            m_startButtonCanvasGroup.alpha = 0f;
            m_startButtonCanvasGroup.interactable = false;
            m_startButtonCanvasGroup.blocksRaycasts = false;
        }

        foreach (Graphic graphic in GetComponents<Graphic>())
        {
            graphic.enabled = false;
        }

        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.enabled = false;
        }

        UIButtonHoverEffect hoverEffect = GetComponent<UIButtonHoverEffect>();
        if (hoverEffect != null)
        {
            hoverEffect.enabled = false;
        }

        HoverImageSwapper hoverSwapper = GetComponent<HoverImageSwapper>();
        if (hoverSwapper != null)
        {
            hoverSwapper.enabled = false;
        }
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
