using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Boss血条管理器 — 在屏幕上方管理多个Boss血条的生成、排列、等比缩放
/// 当ChessQueenEnemy、SwordsmanEnemy、DragonBossEnemy出现时自动创建血条
/// </summary>
public class BossHealthBarManager : MonoBehaviour
{
    public static BossHealthBarManager Instance { get; private set; }

    [Header("预制体")]
    [SerializeField] private BossHealthBar bossHealthBarPrefab;

    [Header("布局")]
    [SerializeField] private RectTransform barsContainer;
    [SerializeField] private float barWidth = 400f;
    [SerializeField] private float barHeight = 40f;
    [SerializeField] private float barSpacing = 16f;
    [SerializeField] private float topMargin = 20f;

    private readonly List<BossHealthBar> m_activeBars = new List<BossHealthBar>();
    private readonly Dictionary<UnitCombatant, BossHealthBar> m_barByTarget = new Dictionary<UnitCombatant, BossHealthBar>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>注册一个Boss单位，为其创建血条</summary>
    public void RegisterBoss(UnitCombatant boss)
    {
        if (boss == null || m_barByTarget.ContainsKey(boss))
        {
            return;
        }

        if (bossHealthBarPrefab == null)
        {
            Debug.LogWarning("[BossHealthBarManager] bossHealthBarPrefab 未设置");
            return;
        }

        BossHealthBar bar = Instantiate(bossHealthBarPrefab, barsContainer);
        bar.Bind(boss);
        bar.SetVisible(true);

        m_activeBars.Add(bar);
        m_barByTarget[boss] = bar;

        RefreshLayout();
    }

    /// <summary>注销一个Boss单位（死亡时调用），移除其血条</summary>
    public void UnregisterBoss(UnitCombatant boss)
    {
        if (boss == null || !m_barByTarget.TryGetValue(boss, out BossHealthBar bar))
        {
            return;
        }

        m_barByTarget.Remove(boss);
        m_activeBars.Remove(bar);

        if (bar != null)
        {
            bar.Unbind();
            Destroy(bar.gameObject);
        }

        RefreshLayout();
    }

    /// <summary>刷新所有血条的排列和宽度</summary>
    private void RefreshLayout()
    {
        int count = m_activeBars.Count;
        if (count == 0)
        {
            return;
        }

        // 等比缩放：所有血条平分可用宽度
        float individualWidth = barWidth / count;

        for (int i = 0; i < count; i++)
        {
            BossHealthBar bar = m_activeBars[i];
            if (bar == null)
            {
                continue;
            }

            RectTransform rect = bar.RectTransform;
            if (rect == null)
            {
                continue;
            }

            // 强制锚点为顶部居中单点，确保 SetSizeWithCurrentAnchors 生效
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);

            // 设置宽高（锚点为单点模式下生效）
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, individualWidth);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, barHeight);

            // 水平排列（居中）
            float xOffset = (i - (count - 1) * 0.5f) * (individualWidth + barSpacing);
            rect.anchoredPosition = new Vector2(xOffset, -topMargin);
        }
    }
}
