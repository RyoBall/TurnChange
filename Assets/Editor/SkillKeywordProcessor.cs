using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 技能关键词批量处理工具
/// - 读取所有 CharacterSkillBase 资产的描述文本
/// - 根据 SkillKeywordConfig 中的关键词列表进行匹配
/// - 将匹配到的关键词自动填入技能的 tags 列表
/// </summary>
public static class SkillKeywordProcessor
{
    private static string KeywordConfigPath
    {
        get
        {
            if (Config.Instance == null)
            {
                return "Assets/Resources/配置可编程物体/SkillKeywordConfig.asset";
            }
            return $"{Config.Instance.KeyWordConfigAssetOutputPath}/SkillKeywordConfig.asset";
        }
    }

    [MenuItem("Tools/技能关键词/从描述自动匹配关键词到技能")]
    public static void MatchKeywordsFromDescriptions()
    {
        // 1. 加载关键词配置
        SkillKeywordConfig keywordConfig = AssetDatabase.LoadAssetAtPath<SkillKeywordConfig>(KeywordConfigPath);
        if (keywordConfig == null)
        {
            Debug.LogError($"[SkillKeywordProcessor] 未找到关键词配置文件，请先在 {KeywordConfigPath} 创建 SkillKeywordConfig 资产");
            return;
        }

        keywordConfig.ValidateLists();

        List<string> allKeywords = keywordConfig.keywords
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim())
            .Distinct()
            .ToList();

        if (allKeywords.Count == 0)
        {
            Debug.LogWarning("[SkillKeywordProcessor] 关键词列表为空，请先在 SkillKeywordConfig 中添加关键词");
            return;
        }

        Debug.Log($"[SkillKeywordProcessor] 已加载 {allKeywords.Count} 个关键词：{string.Join(", ", allKeywords)}");

        // 2. 从 Config 配置的路径加载所有 CharacterSkillBase 资产
        string skillFolder = Config.Instance.CharacterSkillAssetOutputPath;
        if (string.IsNullOrWhiteSpace(skillFolder) || !AssetDatabase.IsValidFolder(skillFolder))
        {
            Debug.LogError($"[SkillKeywordProcessor] 技能资产路径无效: {skillFolder}");
            return;
        }

        string[] skillAssetPaths = AssetDatabase.FindAssets("t:CharacterSkillBase", new[] { skillFolder });
        if (skillAssetPaths.Length == 0)
        {
            Debug.LogWarning($"[SkillKeywordProcessor] 在 {skillFolder} 下未找到任何 CharacterSkillBase 资产");
            return;
        }

        int totalProcessed = 0;
        int totalKeywordsAdded = 0;

        for (int i = 0; i < skillAssetPaths.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(skillAssetPaths[i]);
            CharacterSkillBase skill = AssetDatabase.LoadAssetAtPath<CharacterSkillBase>(assetPath);
            if (skill == null)
            {
                continue;
            }

            // 显示进度条
            float progress = (float)i / skillAssetPaths.Length;
            if (EditorUtility.DisplayCancelableProgressBar(
                "技能关键词匹配",
                $"正在处理: {skill.skillName} ({i + 1}/{skillAssetPaths.Length})",
                progress))
            {
                EditorUtility.ClearProgressBar();
                Debug.Log("[SkillKeywordProcessor] 用户取消了操作");
                return;
            }

            // 3. 在技能描述中检索关键词
            string searchText = $"{skill.description} {skill.shortDescription}";
            if (string.IsNullOrWhiteSpace(searchText))
            {
                continue;
            }

            List<string> matchedKeywords = FindMatchingKeywords(searchText, allKeywords);
            if (matchedKeywords.Count == 0)
            {
                continue;
            }

            // 4. 将匹配到的关键词加入技能的 tags 列表（去重）
            int addedCount = 0;
            foreach (string keyword in matchedKeywords)
            {
                if (!skill.words.Contains(keyword))
                {
                    skill.words.Add(keyword);
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                EditorUtility.SetDirty(skill);
                totalKeywordsAdded += addedCount;
                Debug.Log($"[SkillKeywordProcessor] 技能 [{skill.skillName}] 添加了 {addedCount} 个关键词: {string.Join(", ", matchedKeywords)}");
            }

            totalProcessed++;
        }

        EditorUtility.ClearProgressBar();

        // 5. 保存所有修改
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SkillKeywordProcessor] 处理完成！共处理 {totalProcessed} 个技能，添加了 {totalKeywordsAdded} 个关键词");
    }

    /// <summary>
    /// 在文本中检索匹配的关键词
    /// </summary>
    private static List<string> FindMatchingKeywords(string text, List<string> keywords)
    {
        List<string> matched = new List<string>();

        foreach (string keyword in keywords)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                continue;
            }

            // 使用 Contains 进行子串匹配（不区分大小写，但保留原始关键词的大小写）
            if (text.Contains(keyword))
            {
                matched.Add(keyword);
            }
        }

        return matched;
    }

    /// <summary>
    /// 创建或定位关键词配置文件
    /// </summary>
    [MenuItem("Tools/技能关键词/打开技能关键词配置")]
    public static void OpenKeywordConfig()
    {
        SkillKeywordConfig config = AssetDatabase.LoadAssetAtPath<SkillKeywordConfig>(KeywordConfigPath);
        if (config == null)
        {
            // 确保目录存在
            string folder = Path.GetDirectoryName(KeywordConfigPath);
            if (!AssetDatabase.IsValidFolder(folder))
            {
                string parent = Path.GetDirectoryName(folder);
                string newFolder = Path.GetFileName(folder);
                AssetDatabase.CreateFolder(parent, newFolder);
            }

            config = ScriptableObject.CreateInstance<SkillKeywordConfig>();
            AssetDatabase.CreateAsset(config, KeywordConfigPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SkillKeywordProcessor] 已在 {KeywordConfigPath} 创建关键词配置文件");
        }

        // 在 Inspector 中选中该资产
        Selection.activeObject = config;
        EditorGUIUtility.PingObject(config);
    }

    /// <summary>
    /// 清空所有技能的 tags 列表
    /// </summary>
    [MenuItem("Tools/技能关键词/清空所有技能的关键词")]
    public static void ClearAllSkillKeywords()
    {
        if (!EditorUtility.DisplayDialog(
            "确认清空",
            "确定要清空所有 CharacterSkillBase 的 tags 列表吗？此操作不可撤销。",
            "确定清空",
            "取消"))
        {
            return;
        }

        string skillFolder = Config.Instance.CharacterSkillAssetOutputPath;
        if (string.IsNullOrWhiteSpace(skillFolder) || !AssetDatabase.IsValidFolder(skillFolder))
        {
            Debug.LogError($"[SkillKeywordProcessor] 技能资产路径无效: {skillFolder}");
            return;
        }

        string[] skillAssetPaths = AssetDatabase.FindAssets("t:CharacterSkillBase", new[] { skillFolder });
        int clearedCount = 0;

        for (int i = 0; i < skillAssetPaths.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(skillAssetPaths[i]);
            CharacterSkillBase skill = AssetDatabase.LoadAssetAtPath<CharacterSkillBase>(assetPath);
            if (skill == null || skill.words.Count == 0)
            {
                continue;
            }

            float progress = (float)i / skillAssetPaths.Length;
            EditorUtility.DisplayProgressBar("清空关键词", $"正在处理: {skill.skillName}", progress);

            skill.words.Clear();
            EditorUtility.SetDirty(skill);
            clearedCount++;
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SkillKeywordProcessor] 已清空 {clearedCount} 个技能的关键词");
    }
}
