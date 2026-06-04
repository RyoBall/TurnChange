using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 教程高亮显示控制器，管理高亮区域的展示与隐藏
/// 使用时需将本脚本挂载到全屏遮罩 Image 所在的 GameObject 上，
/// 该 Image 使用 RectGuideMask Shader 材质
/// </summary>
public class GuideDisplayController : MonoBehaviour
{
    [Header("高亮配置列表")]
    [SerializeField] private List<GuideHighlightConfig> m_highlightConfigs = new List<GuideHighlightConfig>();

    [Header("遮罩材质")]
    [SerializeField] private Material m_guideMaterial;

    // Shader 属性ID缓存
    private int m_rectMinXId;
    private int m_rectMaxXId;
    private int m_rectMinYId;
    private int m_rectMaxYId;

    // 当前激活的高亮类型（用于防重复设置）
    private GuideHighlightType? m_currentHighlight;

    private void Awake()
    {
        // 缓存 Shader 属性ID
        m_rectMinXId = Shader.PropertyToID("_RectMinX");
        m_rectMaxXId = Shader.PropertyToID("_RectMaxX");
        m_rectMinYId = Shader.PropertyToID("_RectMinY");
        m_rectMaxYId = Shader.PropertyToID("_RectMaxY");

        if (m_guideMaterial == null)
        {
            Debug.LogError("GuideDisplayController: 遮罩材质未赋值！");
        }
    }

    /// <summary>
    /// 显示指定类型的高亮区域
    /// </summary>
    /// <param name="type">高亮区域种类</param>
    public void ShowHighlight(GuideHighlightType type)
    {
        if (m_guideMaterial == null)
            return;

        // 如果已经是同一个类型，不重复设置
        if (m_currentHighlight.HasValue && m_currentHighlight.Value == type)
            return;

        GuideHighlightConfig config = m_highlightConfigs.Find(c => c.HighlightType == type);
        if (config == null)
        {
            Debug.LogWarning($"GuideDisplayController: 未找到类型 {type} 的高亮配置");
            return;
        }

        m_guideMaterial.SetFloat(m_rectMinXId, config.RectMinX);
        m_guideMaterial.SetFloat(m_rectMaxXId, config.RectMaxX);
        m_guideMaterial.SetFloat(m_rectMinYId, config.RectMinY);
        m_guideMaterial.SetFloat(m_rectMaxYId, config.RectMaxY);

        m_currentHighlight = type;

        // 确保遮罩 GameObject 可见
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 取消高亮效果，将矩形区域设为全屏（四个参数均设为 0 和 1 以覆盖整个屏幕）
    /// </summary>
    public void HideHighlight()
    {
        if (m_guideMaterial == null)
            return;

        // 将矩形设为全屏范围，遮罩覆盖全部区域 → 相当于取消高亮
        m_guideMaterial.SetFloat(m_rectMinXId, 0f);
        m_guideMaterial.SetFloat(m_rectMaxXId, 1f);
        m_guideMaterial.SetFloat(m_rectMinYId, 0f);
        m_guideMaterial.SetFloat(m_rectMaxYId, 1f);

        m_currentHighlight = null;

        // 隐藏遮罩 GameObject
        gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    /// <summary>
    /// 在编辑器中验证配置列表是否有重复类型
    /// </summary>
    private void OnValidate()
    {
        for (int i = 0; i < m_highlightConfigs.Count; i++)
        {
            for (int j = i + 1; j < m_highlightConfigs.Count; j++)
            {
                if (m_highlightConfigs[i].HighlightType == m_highlightConfigs[j].HighlightType)
                {
                    Debug.LogWarning($"GuideDisplayController: 高亮配置列表中存在重复的类型 {m_highlightConfigs[i].HighlightType}");
                }
            }
        }
    }
#endif
}
