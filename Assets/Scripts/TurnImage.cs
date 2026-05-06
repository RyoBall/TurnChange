using DG.Tweening;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class TurnImage : MonoBehaviour
{
    public Combatant combatant;
    public TMP_Text actionValueText;
    public TMP_Text nameText;

    public float CurrentLayoutScale { get; private set; } = 1f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void Initialize(Vector2 size, float initialScale)
    {
        SetTopLeftAnchor();

        if (rectTransform != null)
        {
            rectTransform.sizeDelta = size;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
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

    // 将锚点与轴心统一到左上，便于垂直列表从父节点左上角向下排布
    public void SetTopLeftAnchor()
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 0.5f);
    }

    // 改变尺寸时保持左上角视觉位置不变，避免元素因 pivot 不同出现位移
    public void SetSizeAndKeepTopLeft(Vector2 size)
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
    }

    public void SetAnchoredPosition(Vector2 position)
    {
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

    // 使用尺寸变化驱动布局缩放，而不是直接缩放 transform，避免列表间距在动画中失真。
    public void SetLayoutScale(Vector2 baseSize, float scale)
    {
        CurrentLayoutScale = scale;
        SetSizeAndKeepTopLeft(baseSize * Mathf.Max(0f, scale));
    }

    public Tween MoveTo(Vector2 position, float duration)
    {
        if (rectTransform == null)
        {
            return DOTween.Sequence();
        }

        return rectTransform.DOAnchorPos(position, duration).SetTarget(gameObject);
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
}
