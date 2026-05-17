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

    [Header("参数引用")]

    private RectTransform rectTransform;
    private RectTransform parentRectTransform;
    private Canvas rootCanvas;
    private Camera uiCamera;
    private Vector3 originalScale;
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
    }

    public void Initialize(Canvas canvas, RectTransform parentRect)
    {
        rootCanvas = canvas;
        parentRectTransform = parentRect;
        uiCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? rootCanvas.worldCamera
            : null;
    }

    public void ShowDamage(int damage, Vector3 worldPosition, bool isDotDamage = false, string additionalText = "")
    {
        // ������������
        damageText.text = damage.ToString();
        if (isDotDamage)
        {
            damageText.color = Color.yellow;
            damageText.text = $"{additionalText}:{damageText.text} ";
        }
        else
        {
            damageText.color = Color.green;
        }
        Vector3 offset = new Vector3(Random.Range(0, positionOffset.x), Random.Range(0, positionOffset.y), Random.Range(0, positionOffset.z));
        if (!TrySetCanvasPosition(worldPosition + offset))
        {
            ReturnToPool();
            return;
        }

        PlayAnimation(true);
    }
    public void ShowCustomText(string customMessage, Vector3 position, Color color)
    {
        backGroundImage.GetComponent<Image>().enabled = true;
         // 设置文本内容和颜色
        damageText.text = customMessage;
        damageText.color = color;

        Vector3 offset = new Vector3(Random.Range(0, positionOffset.x), Random.Range(0, positionOffset.y), Random.Range(0, positionOffset.z));
        if (!TrySetCanvasPosition(position + offset/2))
        {
            ReturnToPool();
            return;
        }

        PlayAnimation(false);
    }
    #region DOTween动画
    [Header("漂浮动画参数")]
    [SerializeField] private float floatDistance = 60f;
    [SerializeField] private float scaleDuration = 0.2f;   // ����ʱ��
    [SerializeField] private float floatDuration = 0.6f;
    [SerializeField] private Vector3 positionOffset;

    private void PlayAnimation(bool isDamage)
    {
        currentSequence?.Kill();

        rectTransform.localScale = originalScale;
        canvasGroup.alpha = 1f;
        Vector2 startPos = rectTransform.anchoredPosition;

        float finalFloatDistance = isDamage ? floatDistance : floatDistance * 0.5f;

        rectTransform.localScale = Vector3.zero;
        currentSequence = DOTween.Sequence();
        currentSequence.Append(rectTransform.DOScale(originalScale, scaleDuration).SetEase(Ease.OutElastic));
        currentSequence.Join(
            rectTransform.DOAnchorPosY(startPos.y + finalFloatDistance, floatDuration)
                .SetEase(Ease.OutSine)
        );
        currentSequence.Join(
            canvasGroup.DOFade(0f, floatDuration)
                .SetEase(Ease.Linear)
        );
        currentSequence.OnComplete(ReturnToPool);
    }
    #endregion
    private void ReturnToPool()
    {
        // ���ص������
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