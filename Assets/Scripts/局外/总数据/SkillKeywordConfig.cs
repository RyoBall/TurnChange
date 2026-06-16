using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 技能关键词配置表 - 存储所有关键词及其注释说明
/// 通过 Editor 菜单 Tools/打开技能关键词配置 进行编辑
/// </summary>
[CreateAssetMenu(fileName = "SkillKeywordConfig", menuName = "配置/技能关键词配置")]
public class SkillKeywordConfig : ScriptableObject
{
    private static SkillKeywordConfig s_instance;

    public static SkillKeywordConfig Instance
    {
        get
        {
            if (s_instance == null)
            {
                s_instance = Resources.Load<SkillKeywordConfig>("配置可编程物体/技能/关键词配置/SkillKeywordConfig");
            }
            return s_instance;
        }
    }


    [Header("关键词列表")]
    [Tooltip("所有可用的关键词")]
    public List<string> keywords = new List<string>();

    [Header("关键词注释")]
    [Tooltip("与关键词一一对应的注释说明")]
    public List<string> keywordDescriptions = new List<string>();

    /// <summary>
    /// 根据关键词获取对应的注释描述
    /// </summary>
    /// <param name="keyword">要查询的关键词</param>
    /// <returns>关键词的注释描述，若未找到则返回空字符串</returns>
    public string GetDescription(string keyword)
    {
        if (string.IsNullOrEmpty(keyword))
        {
            return string.Empty;
        }

        int index = keywords.FindIndex(k => k == keyword);
        if (index >= 0 && index < keywordDescriptions.Count)
        {
            return keywordDescriptions[index];
        }

        return string.Empty;
    }

    /// <summary>
    /// 检查是否包含某个关键词
    /// </summary>
    public bool ContainsKeyword(string keyword)
    {
        return keywords.Contains(keyword);
    }

    /// <summary>
    /// 用富文本标签包裹单个关键词（黄色+加粗+下划线）
    /// </summary>
    public static string WrapKeyword(string keyword)
    {
        if (string.IsNullOrEmpty(keyword))
        {
            return keyword;
        }

        return $"<b><u><color=yellow>{keyword}</color></u></b>";
    }

    /// <summary>
    /// 扫描文本中的关键词并用富文本标签包裹（黄色+加粗+下划线）
    /// 如果关键词已被包裹则跳过，避免重复嵌套
    /// </summary>
    public string ApplyKeywordRichText(string text)
    {
        if (string.IsNullOrEmpty(text) || keywords == null || keywords.Count == 0)
        {
            return text;
        }

        string result = text;
        for (int i = 0; i < keywords.Count; i++)
        {
            string keyword = keywords[i];
            if (string.IsNullOrEmpty(keyword) || keyword.Length == 0)
            {
                continue;
            }

            string wrapped = WrapKeyword(keyword);

            // 如果已包含包裹后的版本（Editor 导入时已处理），跳过
            if (result.Contains(wrapped))
            {
                continue;
            }

            result = result.Replace(keyword, wrapped);
        }

        return result;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 校验关键词与注释列表长度是否一致（Editor 下使用）
    /// </summary>
    public void ValidateLists()
    {
        while (keywordDescriptions.Count < keywords.Count)
        {
            keywordDescriptions.Add(string.Empty);
        }

        while (keywordDescriptions.Count > keywords.Count)
        {
            keywordDescriptions.RemoveAt(keywordDescriptions.Count - 1);
        }
    }
#endif
}

