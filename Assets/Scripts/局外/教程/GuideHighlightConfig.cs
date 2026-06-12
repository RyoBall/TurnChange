using UnityEngine;

/// <summary>
/// 高亮区域的可编程配置对象，包含矩形参数和对应的种类标识
/// </summary>
[System.Serializable,CreateAssetMenu(fileName = "GuideHighlightConfig", menuName = "教程/Guide Highlight Config")]
public class GuideHighlightConfig:ScriptableObject
{
    [SerializeField] private GuideHighlightType m_highlightType;
    [SerializeField] private float m_rectMinX;
    [SerializeField] private float m_rectMaxX;
    [SerializeField] private float m_rectMinY;
    [SerializeField] private float m_rectMaxY;

    public GuideHighlightType HighlightType => m_highlightType;
    public float RectMinX => m_rectMinX;
    public float RectMaxX => m_rectMaxX;
    public float RectMinY => m_rectMinY;
    public float RectMaxY => m_rectMaxY;

    public GuideHighlightConfig(GuideHighlightType type, float minX, float maxX, float minY, float maxY)
    {
        m_highlightType = type;
        m_rectMinX = minX;
        m_rectMaxX = maxX;
        m_rectMinY = minY;
        m_rectMaxY = maxY;
    }
}

/// <summary>
/// 高亮区域的种类枚举
/// </summary>
public enum GuideHighlightType
{
    角色栏,
    技能栏,
    切换角色按钮,
    开始战斗按钮,
    商店物品,
    背包,
    关卡选择,
    关卡信息,
    角色头像选择,
    角色状态栏,
    行动序列,
    追惩技能,
    敌人词条,
    商店按钮,
    序体按钮,
    序体商品,
    刷新按钮,
    扩容按钮,
    切人按键,
    指挥点,
    全黑,
    厄运播撒技能
}
