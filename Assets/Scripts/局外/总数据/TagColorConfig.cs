using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 标签颜色配置表 - 存储标签名与背景色、文字色的对应关系
/// 放置于 Resources/配置可编程物体/技能/关键词配置/ 下，通过 Resources.Load 自动获取
/// </summary>
[CreateAssetMenu(fileName = "TagColorConfig", menuName = "配置/标签颜色配置")]
public class TagColorConfig : ScriptableObject
{
    private static TagColorConfig s_instance;

    public static TagColorConfig Instance
    {
        get
        {
            if (s_instance == null)
            {
                s_instance = Resources.Load<TagColorConfig>("配置可编程物体/技能/关键词配置/TagColorConfig");
            }
            return s_instance;
        }
    }

    [Header("标签颜色映射")]
    [Tooltip("每个标签的名字、背景色和文字色")]
    public List<TagColorEntry> tagColors = new List<TagColorEntry>();

    /// <summary>
    /// 根据标签名获取颜色配置
    /// </summary>
    /// <param name="tagName">标签名</param>
    /// <param name="backgroundColor">输出的背景色</param>
    /// <param name="textColor">输出的文字色</param>
    /// <returns>是否找到对应配置</returns>
    public bool TryGetColors(string tagName, out Color backgroundColor, out Color textColor)
    {
        backgroundColor = Color.white;
        textColor = Color.black;

        if (string.IsNullOrEmpty(tagName) || tagColors == null)
        {
            return false;
        }

        for (int i = 0; i < tagColors.Count; i++)
        {
            if (tagColors[i].tagName == tagName)
            {
                backgroundColor = tagColors[i].backgroundColor;
                textColor = tagColors[i].textColor;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 对标签实例应用颜色配置（设置背景 Image 和 TMP_Text 的颜色）
    /// </summary>
    /// <param name="tagInstance">标签 GameObject 实例</param>
    /// <param name="tagName">标签名</param>
    public void ApplyColorsToTag(GameObject tagInstance, string tagName)
    {
        if (tagInstance == null)
        {
            return;
        }

        if (!TryGetColors(tagName, out Color backgroundColor, out Color textColor))
        {
            return;
        }

        UnityEngine.UI.Image bgImage = tagInstance.GetComponent<UnityEngine.UI.Image>();
        if (bgImage != null)
        {
            bgImage.color = backgroundColor;
        }

        TMPro.TMP_Text tagText = tagInstance.GetComponentInChildren<TMPro.TMP_Text>();
        if (tagText != null)
        {
            tagText.color = textColor;
        }
    }
}

/// <summary>
/// 单个标签的颜色配置条目
/// </summary>
[Serializable]
public class TagColorEntry
{
    [Tooltip("标签名字（需与技能数据中的标签名完全一致）")]
    public string tagName;

    [Tooltip("标签背景色")]
    public Color backgroundColor = Color.white;

    [Tooltip("标签文字色")]
    public Color textColor = Color.black;
}
