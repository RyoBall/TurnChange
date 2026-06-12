using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class GridModuleImporter
{
    private static readonly Dictionary<string, GridModuleType> s_moduleTypeByName = new Dictionary<string, GridModuleType>(StringComparer.Ordinal)
    {
        { "有备而来", GridModuleType.BattleCommandBonus },
        { "先发制人", GridModuleType.OpeningAdvance },
        { "额外指挥", GridModuleType.ExtraCommand },
        { "增伤切入", GridModuleType.SwapDamageBoost },
        { "迅速切入", GridModuleType.SwapSpeedBoost },
        { "呼吸回血", GridModuleType.SwapSelfHeal },
        { "治愈增强", GridModuleType.HealingBoost },
        { "混沌疗化", GridModuleType.HealChaosCleanse },
        { "Dot强化", GridModuleType.DotBoost },
        { "直伤强化", GridModuleType.DirectDamageBoost },
        { "紧急回避", GridModuleType.EmergencyEvade },
        { "起死回生", GridModuleType.FatalGuard },
        { "生命强化", GridModuleType.MaxHealthBoost },
        { "防御强化", GridModuleType.DefenseBoost },
        { "暴伤强化", GridModuleType.CritDamageBoost },
        { "暴率强化", GridModuleType.CritRateBoost },
        { "沉重毒素", GridModuleType.HeavyPoison },
        { "重型炮台", GridModuleType.HeavyTurret },
        { "赌徒步伐", GridModuleType.GamblerStride },
        { "燃血逆转", GridModuleType.BloodReverse },
        { "域场共鸣", GridModuleType.DomainResonance },
        { "蓄势逆击·共鸣", GridModuleType.ChargeCounterResonance },
        { "物法双修", GridModuleType.HybridDamage },
        { "辅切瞬发", GridModuleType.SupportSwapAdvance },
        { "混沌豁免", GridModuleType.ChaosImmunity },
        { "切人蓄爆", GridModuleType.SwapChargeBurst },
        { "紧急切入", GridModuleType.EmergencySwapIn },
        { "暴噬蔓延", GridModuleType.CritDotSpread },
    };

    [MenuItem("Tools/Import Grid Modules")]
    public static void ImportGridModules()
    {
        if (Config.Instance == null)
        {
            Debug.LogError("[GridModuleImporter] 未找到 AppConfig，无法导入模块数据");
            return;
        }

        string csvPath = Config.Instance.GridModuleCSVPath;
        string outputFolder = Config.Instance.GridModuleAssetOutputPath;
        if (string.IsNullOrWhiteSpace(csvPath))
        {
            Debug.LogError("[GridModuleImporter] GridModuleCSVPath 未配置");
            return;
        }

        if (string.IsNullOrWhiteSpace(outputFolder))
        {
            Debug.LogError("[GridModuleImporter] GridModuleAssetOutputPath 未配置");
            return;
        }

        List<Dictionary<string, string>> csvData = CSVReader.ReadCSV(csvPath);
        EnsureFolderExists(outputFolder);

        int importedCount = 0;
        for (int i = 0; i < csvData.Count; i++)
        {
            Dictionary<string, string> row = csvData[i];
            string moduleName = GetString(row, "序体名称", "Name");
            if (string.IsNullOrWhiteSpace(moduleName))
            {
                continue;
            }

            if (!TryGetModuleType(moduleName, row, out GridModuleType moduleType))
            {
                Debug.LogWarning($"[GridModuleImporter] 模块 {moduleName} 的类型无效，已跳过");
                continue;
            }

            string levelKey = GetString(row, "序体级别", "Level");
            GridModuleLevel moduleLevel;
            switch(levelKey)
            {
                case "小":
                    moduleLevel = GridModuleLevel.Small;
                    break;
                case "中":
                    moduleLevel = GridModuleLevel.Normal;
                    break;
                case "大":
                    moduleLevel = GridModuleLevel.Large;
                    break;
                default:
                    Debug.LogWarning($"[GridModuleImporter] 模块 {moduleName} 的级别无效，已跳过");
                    continue;
            }

            string assetPath = $"{outputFolder}/{SanitizeAssetName(moduleName)}.asset";
            GridModuleDefinition moduleAsset = AssetDatabase.LoadAssetAtPath<GridModuleDefinition>(assetPath);
            if (moduleAsset == null)
            {
                moduleAsset = ScriptableObject.CreateInstance<GridModuleDefinition>();
                AssetDatabase.CreateAsset(moduleAsset, assetPath);
            }

            moduleAsset.name = moduleName;
            moduleAsset.moduleName = moduleName;
            moduleAsset.description = GetString(row, "详细词条描述", "Description", moduleAsset.description);
            moduleAsset.moduleType = moduleType;
            moduleAsset.level = moduleLevel;

            EditorUtility.SetDirty(moduleAsset);
            importedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[GridModuleImporter] 模块导入完成，共处理 {importedCount} 个模块资产");
    }

    private static string GetString(Dictionary<string, string> row, params string[] keys)
    {
        return GetString(row, keys, string.Empty);
    }

    private static string GetString(Dictionary<string, string> row, string[] keys, string defaultValue)
    {
        if (row == null || keys == null)
        {
            return defaultValue;
        }

        for (int i = 0; i < keys.Length; i++)
        {
            string key = keys[i];
            if (string.IsNullOrWhiteSpace(key) || !row.TryGetValue(key, out string value))
            {
                continue;
            }

            string trimmedValue = value != null ? value.Trim() : string.Empty;
            if (!string.IsNullOrWhiteSpace(trimmedValue))
            {
                return trimmedValue;
            }
        }

        return defaultValue;
    }

    private static bool TryGetModuleType(string moduleName, Dictionary<string, string> row, out GridModuleType result)
    {
        if (s_moduleTypeByName.TryGetValue(moduleName, out result))
        {
            return true;
        }

        result = default;
        return false;
    }

    private static string SanitizeAssetName(string assetName)
    {
        string sanitized = assetName;
        char[] invalidChars = Path.GetInvalidFileNameChars();
        for (int i = 0; i < invalidChars.Length; i++)
        {
            sanitized = sanitized.Replace(invalidChars[i], '_');
        }

        return sanitized;
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