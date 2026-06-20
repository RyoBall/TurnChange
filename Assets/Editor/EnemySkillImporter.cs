using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class EnemySkillImporter
{
    [MenuItem("Tools/Import Enemy Skills")]
    public static void ImportEnemySkills()
    {
        if (Config.Instance == null)
        {
            Debug.LogError("[EnemySkillImporter] 未找到 AppConfig，无法导入敌人技能数据");
            return;
        }

        string csvPath = Config.Instance.EnemySkillCSVPath;
        string outputFolder = Config.Instance.EnemySkillAssetOutputPath;
        if (string.IsNullOrWhiteSpace(csvPath))
        {
            Debug.LogError("[EnemySkillImporter] EnemySkillCSVPath 未配置");
            return;
        }

        if (string.IsNullOrWhiteSpace(outputFolder))
        {
            Debug.LogError("[EnemySkillImporter] EnemySkillAssetOutputPath 未配置");
            return;
        }

        List<Dictionary<string, string>> csvData = CSVReader.ReadCSV(csvPath);
        EnsureFolderExists(outputFolder);

        int importedCount = 0;
        for (int i = 0; i < csvData.Count; i++)
        {
            Dictionary<string, string> row = csvData[i];
            string skillName = GetString(row, "敌人/技能名称");
            if (string.IsNullOrWhiteSpace(skillName))
            {
                continue;
            }

            if (!TryGetEnum(row, "Type", out EnemySkillType skillType))
            {
                Debug.LogWarning($"[EnemySkillImporter] 技能 {skillName} 的 Type 无效，已跳过");
                continue;
            }

            string assetName = SanitizeAssetName(skillName);
            string assetPath = $"{outputFolder}/{assetName}.asset";
            EnemySkillBase skillAsset = AssetDatabase.LoadAssetAtPath<EnemySkillBase>(assetPath);
            if (skillAsset == null)
            {
                skillAsset = ScriptableObject.CreateInstance<EnemySkillBase>();
                AssetDatabase.CreateAsset(skillAsset, assetPath);
            }

            skillAsset.name = assetName;
            skillAsset.skillName = skillName;
            skillAsset.enemySkillType = skillType;
            skillAsset.skillCoef = GetFloat(row, "SkillCoef", 1f);
            skillAsset.skillBase = GetInt(row, "SkillBase", 0);
            skillAsset.cooldownTurns = Mathf.Max(0, GetInt(row, "CooldownTurns", 0));
            skillAsset.extraData1 = GetFloat(row, "ExtraData1", 0f);
            skillAsset.extraData2 = GetFloat(row, "ExtraData2", 0f);
            skillAsset.extraData3 = GetFloat(row, "ExtraData3", 0f);
            skillAsset.extraData4 = GetFloat(row, "ExtraData4", 0f);

            // 设置目标类型
            if (TryGetEnum(row, "TargetType", out targetType parsedTargetType))
            {
                SetTargetType(skillAsset, parsedTargetType);
            }

            EditorUtility.SetDirty(skillAsset);
            importedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[EnemySkillImporter] 敌人技能导入完成，共处理 {importedCount} 个技能资产");
    }

    private static string GetString(Dictionary<string, string> row, string key, string defaultValue = "")
    {
        if (row == null || !row.TryGetValue(key, out string value))
        {
            return defaultValue;
        }

        return value?.Trim() ?? defaultValue;
    }

    private static int GetInt(Dictionary<string, string> row, string key, int defaultValue = 0)
    {
        string value = GetString(row, key);
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : defaultValue;
    }

    private static float GetFloat(Dictionary<string, string> row, string key, float defaultValue = 0f)
    {
        string value = GetString(row, key);
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result) ? result : defaultValue;
    }

    private static bool TryGetEnum<TEnum>(Dictionary<string, string> row, string key, out TEnum result) where TEnum : struct
    {
        string value = GetString(row, key);
        return Enum.TryParse(value, true, out result);
    }

    private static void SetTargetType(EnemySkillBase skill, targetType type)
    {
        // targetType 是 [SerializeField] private，通过反射设置
        var field = typeof(EnemySkillBase).GetField("targetType",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(skill, type);
        }
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
