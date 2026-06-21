using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class TurnImage : MonoBehaviour
{
    public Combatant combatant;
    public TMP_Text actionValueText;
    public TMP_Text nameText;

    public float CurrentLayoutScale { get; private set; } = 1f;

    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image highlightImage;
    [SerializeField] private Sprite highlightSprite;
    private CanvasGroup canvasGroup;
    private Tween moveTween;
    private Tween m_highlightFillTween;
    [SerializeField] private Sprite enemySprite;
    [SerializeField] private Sprite playerSprite;

    private const string HighlightSpriteResourcePath = "Art/UIs/HighLight";

    private void Awake()
    {
        if (rectTransform == null)
        {
            rectTransform = transform as RectTransform;
        }
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        EnsureHighlightImageReady();
    }

    public void Initialize(Vector2 size, float initialScale)
    {
        SetTopRightAnchor();

        if (rectTransform != null)
        {
            rectTransform.sizeDelta = size;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        // 如果不是 UnitCombatant 或者 TurnImageSprite 为空，则头像和背景都置空
        if (combatant is not UnitCombatant unitCombatant || unitCombatant.TurnImageSprite == null)
        {
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }
            if (backgroundImage != null)
            {
                backgroundImage.sprite = playerSprite; // 使用默认玩家背景图，敌人没有头像时也使用玩家背景图
            }
        }
        else
        {
            if (iconImage != null)
            {
                iconImage.sprite = unitCombatant.TurnImageSprite;
                iconImage.enabled = true;
            }
            // 根据战斗者类型设置背景图，敌人和玩家使用不同的默认背景
            if (combatant is Enemy)
            {
                backgroundImage.sprite = enemySprite;
            }
            else
            {
                backgroundImage.sprite = playerSprite;
            }
        }
        CurrentLayoutScale = initialScale;
        transform.localScale = Vector3.one;
        ResetHoverHighlightImmediate();
    }

    private void Update()
    {
        if (combatant != null && actionValueText != null)
        {
            actionValueText.text = combatant.currentActionValue.ToString("F0");
            nameText.text = combatant.combatantName;
        }
    }

    public void SetSize(Vector2 size)
    {
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = size;
        }
    }

    // 将锚点与轴心统一到右上，便于垂直列表从父节点右上角向下排布
    public void SetTopRightAnchor()
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = new Vector2(1f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(1f, 1f);
    }

    public void SetAnchoredPosition(Vector2 position)
    {
        rectTransform = transform as RectTransform;
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = position;
        }
    }

    public Vector2 GetAnchoredPosition()
    {
        if (rectTransform == null)
        {
            return Vector2.zero;
        }

        return rectTransform.anchoredPosition;
    }

    public void SetAlpha(float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }
    }

    public void SetScale(float scale)
    {
        transform.localScale = Vector3.one * scale;
    }

    // 直接缩放Scale
    public void SetLayoutScale(Vector2 baseSize, float scale)
    {
        CurrentLayoutScale = scale;
        rectTransform.localScale = Vector3.one * scale;
    }

    public Tween MoveTo(Vector2 position, float duration)
    {
        if (rectTransform == null)
        {
            return DOTween.Sequence();
        }

        if (moveTween != null)
        {
            moveTween.Kill();
        }

        moveTween = rectTransform.DOAnchorPos(position, duration).SetTarget(gameObject);
        return moveTween;
    }

    public Tween FadeIn(float duration)
    {
        if (canvasGroup == null)
        {
            return DOTween.Sequence();
        }

        return canvasGroup.DOFade(1f, duration).SetTarget(gameObject);
    }

    public Tween FadeOut(float duration)
    {
        if (canvasGroup == null)
        {
            return DOTween.Sequence();
        }

        return canvasGroup.DOFade(0f, duration).SetTarget(gameObject);
    }

    public Tween ScaleTo(float scale, float duration)
    {
        return transform.DOScale(Vector3.one * scale, duration).SetTarget(gameObject);
    }

    /// <summary>
    /// 选敌悬停：高亮 FillAmount 0 → 1。
    /// </summary>
    public void PlayHoverHighlightIn(float duration)
    {
        EnsureHighlightImageReady();
        if (highlightImage == null)
        {
            return;
        }

        KillHighlightFillTween();
        highlightImage.enabled = true;
        m_highlightFillTween = highlightImage
            .DOFillAmount(1f, duration)
            .SetEase(Ease.OutQuad)
            .SetTarget(gameObject);
    }

    /// <summary>
    /// 选敌悬停结束：高亮 FillAmount 1 → 0。
    /// </summary>
    public void PlayHoverHighlightOut(float duration)
    {
        if (highlightImage == null)
        {
            return;
        }

        KillHighlightFillTween();
        m_highlightFillTween = highlightImage
            .DOFillAmount(0f, duration)
            .SetEase(Ease.OutQuad)
            .SetTarget(gameObject);
    }

    /// <summary>
    /// 立即清除高亮（选敌结束等场景）。
    /// </summary>
    public void ResetHoverHighlightImmediate()
    {
        KillHighlightFillTween();
        if (highlightImage == null)
        {
            return;
        }

        highlightImage.fillAmount = 0f;
    }

    private void EnsureHighlightImageReady()
    {
        if (highlightImage == null)
        {
            Transform highlightTransform = transform.Find("Highlight");
            if (highlightTransform != null)
            {
                highlightImage = highlightTransform.GetComponent<Image>();
            }
        }

        if (highlightImage == null)
        {
            highlightImage = CreateHighlightImageChild();
        }

        if (highlightImage == null)
        {
            return;
        }

        Sprite sprite = highlightSprite != null ? highlightSprite : LoadHighlightSprite();
        if (sprite != null)
        {
            highlightImage.sprite = sprite;
        }

        highlightImage.type = Image.Type.Filled;
        highlightImage.fillMethod = Image.FillMethod.Horizontal;
        highlightImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        highlightImage.raycastTarget = false;
        highlightImage.fillAmount = 0f;
    }

    private Image CreateHighlightImageChild()
    {
        var highlightObject = new GameObject("Highlight", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        highlightObject.transform.SetParent(transform, false);
        highlightObject.transform.SetAsLastSibling();

        var highlightRect = highlightObject.GetComponent<RectTransform>();
        highlightRect.anchorMin = Vector2.zero;
        highlightRect.anchorMax = Vector2.one;
        highlightRect.offsetMin = Vector2.zero;
        highlightRect.offsetMax = Vector2.zero;

        return highlightObject.GetComponent<Image>();
    }

    private static Sprite LoadHighlightSprite()
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(HighlightSpriteResourcePath);
        if (sprites == null || sprites.Length == 0)
        {
            return Resources.Load<Sprite>(HighlightSpriteResourcePath);
        }

        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null)
            {
                return sprites[i];
            }
        }

        return null;
    }

    private void KillHighlightFillTween()
    {
        if (m_highlightFillTween != null && m_highlightFillTween.IsActive())
        {
            m_highlightFillTween.Kill();
        }

        m_highlightFillTween = null;
    }

    private void OnDestroy()
    {
        KillHighlightFillTween();
    }

    #region NotUse
    /* 改变尺寸时保持右上角视觉位置不变，避免元素因 pivot 不同出现位移
public void SetSizeAndKeepTopRight(Vector2 size)
{
    if (rectTransform == null)
    {
        return;
    }

    Vector2 oldSize = rectTransform.sizeDelta;
    Vector2 oldAnchored = rectTransform.anchoredPosition;
    Vector2 pivot = rectTransform.pivot;

    rectTransform.sizeDelta = size;

    Vector2 delta = size - oldSize;
    Vector2 correction = new Vector2(pivot.x * delta.x, -(1f - pivot.y) * delta.y);
    rectTransform.anchoredPosition = oldAnchored + correction;
}*/
    #endregion
}
