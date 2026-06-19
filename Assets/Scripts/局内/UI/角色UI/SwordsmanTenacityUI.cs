using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 剑客韧性点 UI — 在 SwordsmanEnemy 上方显示紧密排列的韧性点指示器。
/// 通过不同 Sprite 区分「有韧性」和「已消耗」状态。
/// 挂载在 SwordsmanEnemy 的 GameObject 或子 Canvas 上。
/// </summary>
public class SwordsmanTenacityUI : MonoBehaviour
{
    [Header("目标绑定")]
    [SerializeField] private SwordsmanEnemy m_targetSwordsman;

    [Header("外观配置")]
    [SerializeField] private Sprite m_fullSprite;      // 有韧性点时的 Sprite
    [SerializeField] private Sprite m_emptySprite;     // 已消耗时的 Sprite
    [SerializeField] private float m_dotSpacing = 4f;  // 点之间的间距（像素）
    [SerializeField] private float m_dotSize = 12f;    // 每个点的大小（像素）
    [SerializeField] private Color m_fullColor = Color.white;
    [SerializeField] private Color m_emptyColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);

    [Header("布局")]
    [SerializeField] private RectTransform m_container; // Image 的父容器，若为空则自动创建

    private Image[] m_dotImages;
    private int m_cachedMaxTenacity;
    private int m_cachedCurrentTenacity;

    private void Awake()
    {
        InitializeContainer();
    }

    private void Start()
    {
        if (m_targetSwordsman == null)
        {
            m_targetSwordsman = GetComponentInParent<SwordsmanEnemy>();
        }

        if (m_targetSwordsman != null)
        {
            m_targetSwordsman.TenacityChanged += OnTenacityChanged;
            RebuildDots();
        }
    }

    private void OnDestroy()
    {
        if (m_targetSwordsman != null)
        {
            m_targetSwordsman.TenacityChanged -= OnTenacityChanged;
        }
    }

    private void OnEnable()
    {
        RefreshDisplay();
    }

    // ============ 公开方法 ============

    /// <summary>设置目标剑客并初始化显示</summary>
    public void SetTarget(SwordsmanEnemy swordsman)
    {
        if (m_targetSwordsman != null)
        {
            m_targetSwordsman.TenacityChanged -= OnTenacityChanged;
        }

        m_targetSwordsman = swordsman;

        if (m_targetSwordsman != null)
        {
            m_targetSwordsman.TenacityChanged += OnTenacityChanged;
        }

        RebuildDots();
    }

    // ============ 内部逻辑 ============

    private void InitializeContainer()
    {
        if (m_container == null)
        {
            GameObject go = new GameObject("TenacityContainer", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            m_container = go.GetComponent<RectTransform>();
            m_container.anchorMin = new Vector2(0.5f, 0.5f);
            m_container.anchorMax = new Vector2(0.5f, 0.5f);
            m_container.pivot = new Vector2(0.5f, 0.5f);
            m_container.anchoredPosition = Vector2.zero;
        }
    }

    private void OnTenacityChanged()
    {
        RefreshDisplay();
    }

    /// <summary>根据当前韧性点上限重建所有 Image 点</summary>
    private void RebuildDots()
    {
        ClearDots();

        if (m_targetSwordsman == null) return;

        int maxTenacity = m_targetSwordsman.MaxTenacity;
        if (maxTenacity <= 0) return;

        m_dotImages = new Image[maxTenacity];

        // 计算总宽度：maxTenacity 个点 + (maxTenacity-1) 个间距
        float totalWidth = maxTenacity * m_dotSize + (maxTenacity - 1) * m_dotSpacing;
        float startX = -totalWidth / 2f + m_dotSize / 2f;

        for (int i = 0; i < maxTenacity; i++)
        {
            GameObject dotGo = new GameObject($"TenacityDot_{i}", typeof(RectTransform), typeof(Image));
            dotGo.transform.SetParent(m_container, false);

            RectTransform rt = dotGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(m_dotSize, m_dotSize);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(startX + i * (m_dotSize + m_dotSpacing), 0f);

            Image img = dotGo.GetComponent<Image>();
            img.raycastTarget = false;
            m_dotImages[i] = img;
        }

        m_cachedMaxTenacity = maxTenacity;
        RefreshDisplay();
    }

    private void ClearDots()
    {
        if (m_dotImages != null)
        {
            for (int i = 0; i < m_dotImages.Length; i++)
            {
                if (m_dotImages[i] != null)
                {
                    Destroy(m_dotImages[i].gameObject);
                }
            }
            m_dotImages = null;
        }
    }

    /// <summary>刷新每个点的 Sprite 和颜色</summary>
    private void RefreshDisplay()
    {
        if (m_targetSwordsman == null) return;

        int maxTenacity = m_targetSwordsman.MaxTenacity;
        int currentTenacity = m_targetSwordsman.CurrentTenacity;

        // 上限变化时重建
        if (maxTenacity != m_cachedMaxTenacity)
        {
            RebuildDots();
            return;
        }

        if (m_dotImages == null) return;

        for (int i = 0; i < m_dotImages.Length; i++)
        {
            if (m_dotImages[i] == null) continue;

            bool isFull = i < currentTenacity;
            m_dotImages[i].sprite = isFull ? m_fullSprite : m_emptySprite;
            m_dotImages[i].color = isFull ? m_fullColor : m_emptyColor;
        }

        m_cachedCurrentTenacity = currentTenacity;
    }
}
