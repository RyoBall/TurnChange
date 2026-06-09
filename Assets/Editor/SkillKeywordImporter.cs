using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 关键词配置导入器
/// 从 CSV 读取关键词和关键词描述，生成/更新 SkillKeywordConfig ScriptableObject 资产
/// </summary>
public static class SkillKeywordImporter
{
    [MenuItem("Tools/Import KeyWord Config")]
    public static void ImportKeyWordConfig()
    {
        if (Config.Instance == null)
        {
            Debug.LogError("[SkillKeywordImporter] 未找到 AppConfig，无法导入关键词配置");
            return;
        }

        string csvPath = Config.Instance.KeyWordConfigCSVPath;
        string outputFolder = Config.Instance.KeyWordConfigAssetOutputPath;
        if (string.IsNullOrWhiteSpace(csvPath))
        {
            Debug.LogError("[SkillKeywordImporter] KeyWordConfigCSVPath 未配置");
            return;
        }

        if (string.IsNullOrWhiteSpace(outputFolder))
        {
            Debug.LogError("[SkillKeywordImporter] KeyWordConfigAssetOutputPath 未配置");
            return;
        }

        List<Dictionary<string, string>> csvData = CSVReader.ReadCSV(csvPath);
        EnsureFolderExists(outputFolder);

        string assetPath = $"{outputFolder}/SkillKeywordConfig.asset";
        SkillKeywordConfig config = AssetDatabase.LoadAssetAtPath<SkillKeywordConfig>(assetPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<SkillKeywordConfig>();
            AssetDatabase.CreateAsset(config, assetPath);
        }

        // 清空旧数据
        config.keywords.Clear();
        config.keywordDescriptions.Clear();

        int importedCount = 0;
        for (int i = 0; i < csvData.Count; i++)
        {
            Dictionary<string, string> row = csvData[i];
            string keyword = GetString(row, "Keyword");
            if (string.IsNullOrWhiteSpace(keyword))
            {
                continue;
            }

            string description = GetString(row, "KeywordDescription");

            config.keywords.Add(keyword);
            config.keywordDescriptions.Add(description);
            importedCount++;
        }

        config.ValidateLists();
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SkillKeywordImporter] 关键词配置导入完成，共导入 {importedCount} 条关键词");
    }

    private static string GetString(Dictionary<string, string> row, string key, string defaultValue = "")
    {
        if (row == null || !row.TryGetValue(key, out string value))
        {
            return defaultValue;
        }

        return value?.Trim() ?? defaultValue;
    }

    private static void EnsureFolderExists(string assetFolderPath)
    {
        string normalizedPath = assetFolderPath.Replace("\\", "/").TrimEnd('/');
        if (AssetDatabase.IsValidFolder(normalizedPath))
        {
            return;
        }

        string[] segments = normalizedPath.Split('/');
        if (segments.Length <= 1)
        {
            return;
        }

        string currentPath = segments[0];
        for (int i = 1; i < segments.Length; i++)
        {
            string nextPath = $"{currentPath}/{segments[i]}";
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, segments[i]);
            }

            currentPath = nextPath;
        }
    }
}
