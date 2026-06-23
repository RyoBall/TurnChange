using UnityEditor;
using UnityEngine;

/// <summary>
/// 批量为序体资产应用名称意象配色。
/// </summary>
public static class GridModuleColorApplier
{
    [MenuItem("Tools/Apply Grid Module Colors")]
    public static void ApplyColorsToAllModules()
    {
        if (Config.Instance == null)
        {
            Debug.LogError("[GridModuleColorApplier] 未找到 AppConfig，无法定位序体资产目录。");
            return;
        }

        string outputFolder = Config.Instance.GridModuleAssetOutputPath;
        if (string.IsNullOrWhiteSpace(outputFolder))
        {
            Debug.LogError("[GridModuleColorApplier] GridModuleAssetOutputPath 未配置。");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:GridModuleDefinition", new[] { outputFolder });
        int appliedCount = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            GridModuleDefinition moduleAsset = AssetDatabase.LoadAssetAtPath<GridModuleDefinition>(assetPath);
            if (moduleAsset == null)
            {
                continue;
            }

            GridModuleColorPalette.ApplyColors(moduleAsset);
            EditorUtility.SetDirty(moduleAsset);
            appliedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[GridModuleColorApplier] 已为 {appliedCount} 个序体应用配色。");
    }
}
