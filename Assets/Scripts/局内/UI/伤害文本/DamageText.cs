using UnityEngine;
using TMPro;
using DG.Tweening;
using MoreMountains.Feedbacks;
using UnityEngine.UI;

/// <summary>
/// 伤害文本
/// </summary>
public class DamageText : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("文字颜色")]
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private Color dotDamageColor = Color.yellow;
    [SerializeField] private Color healColor = Color.green;
    [SerializeField] private Color critDamageColor = new Color(1f, 0.5f, 0f); // 暴击颜色（橙色）

    [Header("暴击设置")]
    [SerializeField] private float critFontSizeMultiplier = 1.5f;

    private RectTransform rectTransform;
    private RectTransform parentRectTransform;
    private Canvas rootCanvas;
    private Camera uiCamera;
    private Vector3 originalScale;
    private float originalFontSize;
    private Sequence currentSequence;
    [SerializeField] private Image backGroundImage;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        originalScale = rectTransform.localScale;
        if (damageText != null)
        {
            originalFontSize = damageText.fontSize;
        }
    }

    public void Initialize(Canvas canvas, RectTransform parentRect)
    {
        rootCanvas = canvas;
        parentRectTransform = parentRect;
        uiCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? rootCanvas.worldCamera
            : null;
    }

    public void ShowDamage(int damage, Vector3 worldPosition, bool isDotDamage = false, bool isCriticalHit = false)
    {
        if (isCriticalHit)
        {
            damageText.text = damage.ToString() + "!";
            damageText.color = critDamageColor;
            damageText.fontSize = originalFontSize * critFontSizeMultiplier;
        }
        else
        {
            damageText.text = damage.ToString();
            damageText.color = isDotDamage ? dotDamageColor : damageColor;
            damageText.fontSize = originalFontSize;
        }
        Vector3 offset = GetRandomOffset();
        if (!TrySetCanvasPosition(worldPosition + offset))
        {
            ReturnToPool();
            return;
        }

        PlayAnimation(true, isCriticalHit);
    }
    public void ShowHeal(int healAmount, Vector3 worldPosition)
    {
        damageText.text = healAmount.ToString();
        damageText.color = healColor;
        damageText.fontSize = originalFontSize;

        Vector3 offset = GetRandomOffset();
        if (!TrySetCanvasPosition(worldPosition + offset))
        {
            ReturnToPool();
            return;
        }

        PlayAnimation(false);
    }
    public void ShowCustomText(string customMessage, Vector3 position, Color color)
    {
        backGroundImage.GetComponent<Image>().enabled = true;
        damageText.text = customMessage;
        damageText.color = color;
        damageText.fontSize = originalFontSize;

        Vector3 offset = GetRandomOffset();
        if (!TrySetCanvasPosition(position + offset))
        {
            ReturnToPool();
            return;
        }

        PlayAnimation(false);
    }
    #region DOTween动画
    [Header("动画参数")]
    [SerializeField] private float scaleDuration = 0.3f;
    [SerializeField] private float maxScaleMultiplier = 1.3f;
    [SerializeField] private float fadeOutDuration = 1.2f;
    [SerializeField] private float fadeOutDelay = 0.3f;
    [SerializeField] private Vector2 randomOffsetRange = new Vector2(40f, 30f);

    /// <summary>
    /// 获取基于 randomOffsetRange 的双向随机偏移（世界坐标）
    /// </summary>
    private Vector3 GetRandomOffset()
    {
        float x = Random.Range(-randomOffsetRange.x, randomOffsetRange.x);
        float y = Random.Range(-randomOffsetRange.y, randomOffsetRange.y);
        return new Vector3(x, y, 0f);
    }

    private void PlayAnimation(bool isDamage, bool isCriticalHit = false)
    {
        currentSequence?.Kill();

        rectTransform.localScale = originalScale;
        canvasGroup.alpha = 1f;

        // 弹性放大：先放大到 maxScale，再回弹到原始大小
        rectTransform.localScale = Vector3.zero;
        float effectiveMaxScale = isCriticalHit ? maxScaleMultiplier * 1.2f : maxScaleMultiplier;
        Vector3 maxScale = originalScale * effectiveMaxScale;
        currentSequence = DOTween.Sequence();
        currentSequence.Append(rectTransform.DOScale(maxScale, scaleDuration * 0.6f).SetEase(Ease.OutBack));
        currentSequence.Append(rectTransform.DOScale(originalScale, scaleDuration * 0.4f).SetEase(Ease.OutBack));
        // 停留片刻后缓缓淡出
        currentSequence.AppendInterval(fadeOutDelay);
        currentSequence.Append(canvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.OutSine));
        currentSequence.OnComplete(ReturnToPool);
    }
    #endregion
    private void ReturnToPool()
    {
        // 恢复字体大小为原始大小（暴击时可能被修改）
        if (damageText != null)
        {
            damageText.fontSize = originalFontSize;
        }
        // 返回对象池
        DamageTextPool.Instance?.ReturnToPool(this);
    }

    private bool TrySetCanvasPosition(Vector3 worldPosition)
    {
        if (parentRectTransform == null)
        {
            parentRectTransform = rectTransform.parent as RectTransform;
        }

        if (parentRectTransform == null)
        {
            Debug.LogWarning("DamageText 缺少父级 RectTransform，无法转换伤害跳字坐标。", this);
            return false;
        }

        Camera worldCamera = Camera.main;
        if (worldCamera == null)
        {
            Debug.LogWarning("DamageText 未找到主摄像机，无法转换伤害跳字坐标。", this);
            return false;
        }

        Vector3 screenPosition = worldCamera.WorldToScreenPoint(worldPosition);
        if (screenPosition.z < 0f)
        {
            return false;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRectTransform,
                screenPosition,
                uiCamera,
                out Vector2 localPoint))
        {
            return false;
        }

        rectTransform.anchoredPosition = localPoint;
        return true;
    }
}