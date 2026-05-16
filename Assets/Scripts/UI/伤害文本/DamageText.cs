using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections;

/// <summary>
/// �˺����ֶ���
/// </summary>
public class DamageText : MonoBehaviour
{
    [Header("���")]
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("��������")]

    private RectTransform rectTransform;
    private RectTransform parentRectTransform;
    private Canvas rootCanvas;
    private Camera uiCamera;
    private Vector3 originalScale;
    private Sequence currentSequence;

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

    public void ShowDamage(int damage, Vector3 worldPosition, bool isDotDamage = false,string additionalText="")
    {
        // ������������
        damageText.text = damage.ToString();
        if(isDotDamage)
        {
            damageText.color = Color.yellow;
            damageText.text=$"{additionalText}:{damageText.text} ";
        }
        else
        {
            damageText.color = Color.white;
        }
        if (!TrySetCanvasPosition(worldPosition))
        {
            ReturnToPool();
            return;
        }

        // ���Ŷ���
        PlayAnimation(isDotDamage);
    }

    [Header("跳跃动画参数")]
    [SerializeField] private float jumpVelocity = 8f;    // ��ʼ�����ٶ�
    [SerializeField] private float gravity = 20f;        // �������ٶ�
    [SerializeField] private float horizontalRange = 40f; // ˮƽ�����Χ
    [SerializeField] private float scaleDuration = 0.2f;   // ����ʱ��
    [SerializeField] private float fadeDuration = 0.5f;    // ����ʱ��

    private Coroutine physicsCoroutine;

    private void PlayAnimation(bool isDotDamage)
    { 
        // ֹͣ��ǰ����������ģ��
        currentSequence?.Kill();
        if (physicsCoroutine != null)
            StopCoroutine(physicsCoroutine);

        // ����״̬
        rectTransform.localScale = originalScale;
        canvasGroup.alpha = 1f;

        // �볡���Ŷ���
        rectTransform.localScale = Vector3.zero;
        currentSequence = DOTween.Sequence();
        currentSequence.Append(rectTransform.DOScale(originalScale, scaleDuration).SetEase(Ease.OutElastic));
        physicsCoroutine = StartCoroutine(PhysicsFallCoroutine(isDotDamage));
    }

    private IEnumerator PhysicsFallCoroutine(bool isDotDamage)
    {
        // ��ʼλ��
        Vector2 anchoredPosition = rectTransform.anchoredPosition;
        Vector3 velocity = new Vector3(
            Random.Range(-horizontalRange, horizontalRange), 
            jumpVelocity,                
            0
        );

        float elapsedTime = 0f;
        float maxDuration = fadeDuration;  

        while (elapsedTime < maxDuration)
        {
            velocity.y -= gravity * Time.deltaTime;
            velocity.y = velocity.y>0?velocity.y:0;
            velocity.x = velocity.y>0?velocity.x:0;

            anchoredPosition += (Vector2)(velocity * Time.deltaTime);
            rectTransform.anchoredPosition = anchoredPosition;

            float fadeProgress = elapsedTime / maxDuration;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeProgress);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        ReturnToPool();
    }

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