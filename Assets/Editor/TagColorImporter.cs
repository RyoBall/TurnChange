using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 标签颜色配置导入器
/// 从 CSV 读取标签名、背景色、文字色，生成/更新 TagColorConfig ScriptableObject 资产
/// </summary>
public static class TagColorImporter
{
    [MenuItem("Tools/Import Tag Color Config")]
    public static void ImportTagColorConfig()
    {
        if (Config.Instance == null)
        {
            Debug.LogError("[TagColorImporter] 未找到 AppConfig，无法导入标签颜色配置");
            return;
        }

        string csvPath = Config.Instance.TagColorCSVPath;
        string outputFolder = Config.Instance.TagColorAssetOutputPath;
        if (string.IsNullOrWhiteSpace(csvPath))
        {
            Debug.LogError("[TagColorImporter] TagColorCSVPath 未配置");
            return;
        }

        if (string.IsNullOrWhiteSpace(outputFolder))
        {
            Debug.LogError("[TagColorImporter] TagColorAssetOutputPath 未配置");
            return;
        }

        List<Dictionary<string, string>> csvData = CSVReader.ReadCSV(csvPath);
        EnsureFolderExists(outputFolder);

        string assetPath = $"{outputFolder}/TagColorConfig.asset";
        TagColorConfig config = AssetDatabase.LoadAssetAtPath<TagColorConfig>(assetPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<TagColorConfig>();
            AssetDatabase.CreateAsset(config, assetPath);
        }

        // 清空旧数据
        config.tagColors.Clear();

        int importedCount = 0;
        for (int i = 0; i < csvData.Count; i++)
        {
            Dictionary<string, string> row = csvData[i];
            string tagName = GetString(row, "标签名");
            if (string.IsNullOrWhiteSpace(tagName))
            {
                continue;
            }

            Color backgroundColor = ParseColor(GetString(row, "背景颜色"));
            Color textColor = ParseColor(GetString(row, "文字颜色"));

            config.tagColors.Add(new TagColorEntry
            {
                tagName = tagName,
                backgroundColor = backgroundColor,
                textColor = textColor
            });
            importedCount++;
        }

        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[TagColorImporter] 标签颜色配置导入完成，共导入 {importedCount} 条标签颜色");
    }

    /// <summary>
    /// 解析 "R G B A" 格式的颜色字符串（每个分量 0-1 范围）
    /// </summary>
    private static Color ParseColor(string colorString)
    {
        if (string.IsNullOrWhiteSpace(colorString))
        {
            return Color.white;
        }

        string[] parts = colorString.Trim().Split(' ');
        if (parts.Length < 3)
        {
            Debug.LogWarning($"[TagColorImporter] 无法解析颜色字符串: '{colorString}'，使用白色");
            return Color.white;
        }

        float r = TryParseFloat(parts[0], 1f);
        float g = TryParseFloat(parts[1], 1f);
        float b = TryParseFloat(parts[2], 1f);
        float a = parts.Length >= 4 ? TryParseFloat(parts[3], 1f) : 1f;

        return new Color(r, g, b, a);
    }

    private static float TryParseFloat(string value, float defaultValue)
    {
        if (float.TryParse(value, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float result))
        {
            return result;
        }

        return defaultValue;
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
