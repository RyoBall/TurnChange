using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 为 UI 按钮提供鼠标移入/移出动画：移入时弹性放大，移出时缓缓恢复原始大小。
/// 挂到按钮根节点或任何 UI 元素上；可在 Inspector 中配置参数与目标 Transform。
/// </summary>
[AddComponentMenu("UI/UIButton Hover Effect")]
public class UIButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Scale")]
    [Tooltip("缩放倍数（相对于初始局部缩放）")]
    [SerializeField] private float hoverScale = 1.12f;
    [Tooltip("鼠标移入的总时长（秒）")]
    [SerializeField] private float enterDuration = 0.35f;
    [Tooltip("鼠标移出的总时长（秒）")]
    [SerializeField] private float exitDuration = 0.45f;

    [Header("弹性参数")]
    [Tooltip("弹性强度，越大震荡衰减越快")]
    [SerializeField, Range(0f, 5f)] private float elasticity = 1.0f;
    [Tooltip("震荡次数（移入时）")]
    [SerializeField, Range(0, 8)] private int vibrato = 3;

    [Header("Target")]
    [Tooltip("如果指定，将作用于该 Transform，否则作用于挂载组件的 Transform")]
    [SerializeField] private Transform targetTransformOverride;

    [Header("Behavior")]
    [Tooltip("使用不受时间缩放影响的时间（建议用于 UI）")]
    [SerializeField] private bool useUnscaledTime = true;
    [Tooltip("在组件禁用时是否重置为初始缩放")]
    [SerializeField] private bool resetOnDisable = true;
    private Coroutine runningCoroutine;
    private Transform target;
    private Vector3 originalLocalScale;

    private void Awake()
    {
        target = targetTransformOverride != null ? targetTransformOverride : transform;
        originalLocalScale = target.localScale;
    }

    private void OnEnable()
    {
        // 以防在编辑器或运行时被修改
        if (target == null) target = targetTransformOverride != null ? targetTransformOverride : transform;
        originalLocalScale = target.localScale;
    }

    private void OnDisable()
    {
        if (resetOnDisable && target != null)
            target.localScale = originalLocalScale;
        if (runningCoroutine != null)
            StopCoroutine(runningCoroutine);
        runningCoroutine = null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StartEnter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StartExit();
    }

    /// <summary>
    /// 外部可调用以触发移入动画
    /// </summary>
    public void StartEnter()
    {
        if (target == null) target = transform;
        if (runningCoroutine != null) StopCoroutine(runningCoroutine);
        GameAudioEvents.Raise(GameAudioEventType.ButtonHoverEnter, this, this);
        runningCoroutine = StartCoroutine(EnterRoutine());
    }

    /// <summary>
    /// 外部可调用以触发移出动画
    /// </summary>
    public void StartExit()
    {
        if (target == null) target = transform;
        if (runningCoroutine != null) StopCoroutine(runningCoroutine);
        GameAudioEvents.Raise(GameAudioEventType.ButtonHoverExit, this, this);
        runningCoroutine = StartCoroutine(ExitRoutine());
    }

    private IEnumerator EnterRoutine()
    {
        float elapsed = 0f;
        Vector3 from = target.localScale;
        Vector3 to = Vector3.Scale(originalLocalScale, new Vector3(hoverScale, hoverScale, hoverScale));
        // 先用一个带回弹的 ease 将缩放推到目标附近，然后再用衰减正弦震荡让其回落到目标值
        while (elapsed < enterDuration)
        {
            float t = Mathf.Clamp01(elapsed / enterDuration);
            float baseEase = EaseOutBack(t);
            Vector3 baseScale = Vector3.LerpUnclamped(from, to, baseEase);

            // 震荡（随时间衰减）
            float decay = Mathf.Exp(-t * elasticity * 5f);
            float oscillation = 0f;
            if (vibrato > 0)
                oscillation = Mathf.Sin(t * Mathf.PI * 2f * vibrato) * decay * 0.12f * elasticity;

            Vector3 finalScale = baseScale + originalLocalScale * oscillation;
            target.localScale = finalScale;

            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
        target.localScale = to;
        runningCoroutine = null;
    }

    private IEnumerator ExitRoutine()
    {
        float elapsed = 0f;
        Vector3 from = target.localScale;
        Vector3 to = originalLocalScale;

        while (elapsed < exitDuration)
        {
            float t = Mathf.Clamp01(elapsed / exitDuration);
            // 缓和回到原始大小
            float ease = Mathf.SmoothStep(0f, 1f, t);
            target.localScale = Vector3.LerpUnclamped(from, to, ease);

            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
        target.localScale = to;
        runningCoroutine = null;
    }

    // Back easing（带一点回弹的缓出），用于进场的基准曲线
    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
