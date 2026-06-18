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
    private CanvasGroup canvasGroup;
    private Tween moveTween;
    [SerializeField] private Sprite enemySprite;
    [SerializeField] private Sprite playerSprite;
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
        // 优先使用 UnitCombatant 配置的专属 Sprite，否则回退到默认的敌我 Sprite
        if (iconImage != null)
        {
            if (combatant is UnitCombatant unitCombatant && unitCombatant.TurnImageSprite != null)
            {
                iconImage.sprite = unitCombatant.TurnImageSprite;
            }
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
        CurrentLayoutScale = initialScale;
        transform.localScale = Vector3.one;
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
