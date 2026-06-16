using UnityEditor;
using UnityEngine;

/// <summary>
/// 确保 TagColorConfig.asset 存在于 Resources 目录下
/// </summary>
public static class TagColorConfigInitializer
{
    private const string AssetPath = "Assets/Resources/配置可编程物体/技能/关键词配置/TagColorConfig.asset";

    [MenuItem("Tools/创建标签颜色配置")]
    public static void CreateTagColorConfig()
    {
        TagColorConfig existing = AssetDatabase.LoadAssetAtPath<TagColorConfig>(AssetPath);
        if (existing != null)
        {
            Debug.Log($"[TagColorConfigInitializer] 配置已存在: {AssetPath}");
            Selection.activeObject = existing;
            return;
        }

        EnsureDirectoryExists();

        TagColorConfig config = ScriptableObject.CreateInstance<TagColorConfig>();
        AssetDatabase.CreateAsset(config, AssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[TagColorConfigInitializer] 已创建标签颜色配置: {AssetPath}");
        Selection.activeObject = config;
    }

    private static void EnsureDirectoryExists()
    {
        string directory = System.IO.Path.GetDirectoryName(AssetPath);
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }
    }
}
