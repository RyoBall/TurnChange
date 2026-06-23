using UnityEditor;
using UnityEngine;

/// <summary>
/// 安全修复序体资产：应用配色、移除已废弃的 privePerCell 字段。
/// 通过 AssetDatabase 读写，避免直接编辑 YAML 导致解析失败或 cells 丢失。
/// </summary>
public static class GridModuleAssetRepair
{
    [MenuItem("Tools/Repair Grid Module Assets")]
    public static void RepairAllModules()
    {
        if (Config.Instance == null)
        {
            Debug.LogError("[GridModuleAssetRepair] 未找到 AppConfig。");
            return;
        }

        string outputFolder = Config.Instance.GridModuleAssetOutputPath;
        if (string.IsNullOrWhiteSpace(outputFolder))
        {
            Debug.LogError("[GridModuleAssetRepair] GridModuleAssetOutputPath 未配置。");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:GridModuleDefinition", new[] { outputFolder });
        int repairedCount = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            GridModuleDefinition moduleAsset = AssetDatabase.LoadAssetAtPath<GridModuleDefinition>(assetPath);
            if (moduleAsset == null)
            {
                continue;
            }

            int cellCountBefore = moduleAsset.cells != null ? moduleAsset.cells.Count : 0;
            GridModuleColorPalette.ApplyColors(moduleAsset);
            EditorUtility.SetDirty(moduleAsset);
            repairedCount++;

            int cellCountAfter = moduleAsset.cells != null ? moduleAsset.cells.Count : 0;
            if (cellCountAfter < cellCountBefore)
            {
                Debug.LogWarning($"[GridModuleAssetRepair] {moduleAsset.moduleName} cells 数量异常：{cellCountBefore} -> {cellCountAfter}，请从版本库恢复该资产。");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[GridModuleAssetRepair] 已修复 {repairedCount} 个序体资产（配色 + 保留 cells）。");
    }
}
