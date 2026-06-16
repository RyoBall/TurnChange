using UnityEngine;

/// <summary>
/// 让 RectTransform 同时叠加两种位移效果：
/// 1. 鼠标跟随 —— 根据鼠标在屏幕上的位置做微偏移移动；
/// 2. 正弦晃动 —— 随时间左右平移呈正弦波摆动。
/// 挂载到带有 RectTransform 的 UI 元素（通常是 Image）上即可。
/// </summary>
[AddComponentMenu("UI/UIMouse Follow Effect")]
public class UIMouseFollowEffect : MonoBehaviour
{
    [Header("鼠标跟随")]
    [Tooltip("水平方向最大偏移量（像素）")]
    [SerializeField] private float m_HorizontalAmplitude = 10f;
    [Tooltip("垂直方向最大偏移量（像素）")]
    [SerializeField] private float m_VerticalAmplitude = 10f;
    [Tooltip("跟随平滑度，值越小越平滑（0.01=很慢, 1=瞬间）")]
    [SerializeField, Range(0.01f, 1f)] private float m_SmoothSpeed = 0.1f;

    [Header("正弦晃动")]
    [Tooltip("是否启用以时间为驱动的正弦晃动")]
    [SerializeField] private bool m_EnableSineWobble = true;
    [Tooltip("水平晃动幅度（像素）")]
    [SerializeField] private float m_SineHorizontalAmplitude = 5f;
    [Tooltip("垂直晃动幅度（像素）")]
    [SerializeField] private float m_SineVerticalAmplitude = 3f;
    [Tooltip("晃动频率（Hz，每秒完整周期数）")]
    [SerializeField, Range(0.1f, 10f)] private float m_SineFrequency = 1f;
    [Tooltip("水平与垂直晃动的相位差（度），0 表示同步，90 表示正交（画圆）")]
    [SerializeField, Range(0f, 360f)] private float m_SinePhaseOffset = 0f;

    [Header("Target")]
    [Tooltip("如果不指定，则使用自身的 RectTransform")]
    [SerializeField] private RectTransform m_TargetRectTransform;

    private RectTransform m_RectTransform;
    private Vector2 m_CenterPosition;
    private Vector2 m_CurrentMouseOffset;

    private void Awake()
    {
        InitializeComponents();
    }

    private void OnEnable()
    {
        InitializeComponents();
    }

    private void Update()
    {
        ApplyCombinedOffset();
    }

    private void InitializeComponents()
    {
        m_RectTransform = m_TargetRectTransform != null ? m_TargetRectTransform : GetComponent<RectTransform>();
        if (m_RectTransform != null)
        {
            m_CenterPosition = m_RectTransform.anchoredPosition;
        }
    }

    private void ApplyCombinedOffset()
    {
        if (m_RectTransform == null) return;

        Vector2 mouseOffset = CalculateMouseOffset();
        Vector2 sineOffset = CalculateSineOffset();

        m_CurrentMouseOffset = Vector2.Lerp(m_CurrentMouseOffset, mouseOffset, m_SmoothSpeed);
        Vector2 totalOffset = m_CurrentMouseOffset + sineOffset;

        m_RectTransform.anchoredPosition = m_CenterPosition + totalOffset;
    }

    private Vector2 CalculateMouseOffset()
    {
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        Vector2 mousePosition = Input.mousePosition;

        float normalizedX = (mousePosition.x / screenSize.x) * 2f - 1f;
        float normalizedY = (mousePosition.y / screenSize.y) * 2f - 1f;

        float offsetX = normalizedX * m_HorizontalAmplitude;
        float offsetY = normalizedY * m_VerticalAmplitude;

        return new Vector2(offsetX, offsetY);
    }

    private Vector2 CalculateSineOffset()
    {
        if (!m_EnableSineWobble) return Vector2.zero;

        float time = Time.time;
        float phaseRad = m_SinePhaseOffset * Mathf.Deg2Rad;

        float offsetX = Mathf.Sin(time * m_SineFrequency * 2f * Mathf.PI) * m_SineHorizontalAmplitude;
        float offsetY = Mathf.Sin(time * m_SineFrequency * 2f * Mathf.PI + phaseRad) * m_SineVerticalAmplitude;

        return new Vector2(offsetX, offsetY);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying && m_RectTransform != null)
        {
            m_CenterPosition = m_RectTransform.anchoredPosition;
        }
    }
#endif
}
