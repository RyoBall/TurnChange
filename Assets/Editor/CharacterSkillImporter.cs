using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CharacterSkillImporter
{
    [MenuItem("Tools/Import Character Skills")]
    public static void ImportCharacterSkills()
    {
        if (Config.Instance == null)
        {
            Debug.LogError("[CharacterSkillImporter] 未找到 AppConfig，无法导入技能数据");
            return;
        }

        string csvPath = Config.Instance.CharacterSkillCSVPath;
        string outputFolder = Config.Instance.CharacterSkillAssetOutputPath;
        if (string.IsNullOrWhiteSpace(csvPath))
        {
            Debug.LogError("[CharacterSkillImporter] CharacterSkillCSVPath 未配置");
            return;
        }

        if (string.IsNullOrWhiteSpace(outputFolder))
        {
            Debug.LogError("[CharacterSkillImporter] CharacterSkillAssetOutputPath 未配置");
            return;
        }

        List<Dictionary<string, string>> csvData = CSVReader.ReadCSV(csvPath);
        EnsureFolderExists(outputFolder);

        int importedCount = 0;
        for (int i = 0; i < csvData.Count; i++)
        {
            Dictionary<string, string> row = csvData[i];
            string skillName = GetString(row, "Name");
            if (string.IsNullOrWhiteSpace(skillName))
            {
                continue;
            }

            if (!TryGetEnum(row, "Type", out CharacterSkillType skillType))
            {
                Debug.LogWarning($"[CharacterSkillImporter] 技能 {skillName} 的 Type 无效，已跳过");
                continue;
            }

            string assetPath = $"{outputFolder}/{SanitizeAssetName(skillName)}.asset";
            CharacterSkillBase skillAsset = AssetDatabase.LoadAssetAtPath<CharacterSkillBase>(assetPath);
            if (skillAsset == null)
            {
                skillAsset = ScriptableObject.CreateInstance<CharacterSkillBase>();
                AssetDatabase.CreateAsset(skillAsset, assetPath);
            }

            skillAsset.name = skillName;
            skillAsset.skillName = skillName;
            skillAsset.shortDescription = GetString(row, "Description_Simple", skillAsset.shortDescription);
            skillAsset.description = GetString(row, "Description", skillAsset.description);
            skillAsset.skillType = skillType;
            skillAsset.skillCoef = GetFloat(row, "SkillCoef", 1f);
            skillAsset.skillBase = Mathf.RoundToInt(GetFloat(row, "SkillBase", 0f));
            skillAsset.requiresEnemyTarget = GetBool(row, "IfRequireEnemy");
            skillAsset.enemyTargetCount = Mathf.Max(1, GetInt(row, "RequireEnemyNum", 1));
            skillAsset.requiresAllyTarget = GetBool(row, "IfRequireCharacter");
            skillAsset.allyTargetCount = Mathf.Max(1, GetInt(row, "RequireCharacterNum", 1));
            skillAsset.endTurnAfterUse = GetBool(row, "EndTurnAfterUse", skillAsset.endTurnAfterUse);
            skillAsset.cooldownTurns = Mathf.Max(0, GetInt(row, "CoolDown", skillAsset.cooldownTurns));
            skillAsset.extraData1 = GetFloat(row, "Extra_Data_1", 0f);
            skillAsset.extraData2 = GetFloat(row, "Extra_Data_2", 0f);
            skillAsset.extraData3 = GetFloat(row, "Extra_Data_3", 0f);
            skillAsset.extraData4 = GetFloat(row, "Extra_Data_4", 0f);

            EditorUtility.SetDirty(skillAsset);
            importedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CharacterSkillImporter] 技能导入完成，共处理 {importedCount} 个技能资产");
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

    private static bool GetBool(Dictionary<string, string> row, string key, bool defaultValue = false)
    {
        string value = GetString(row, key);
        if (string.Equals(value, "是", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(value, "否", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return defaultValue;
    }

    private static bool TryGetEnum<TEnum>(Dictionary<string, string> row, string key, out TEnum result) where TEnum : struct
    {
        string value = GetString(row, key);
        return Enum.TryParse(value, true, out result);
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
