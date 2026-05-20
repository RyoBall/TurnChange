using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class StateImporter
{
    [MenuItem("Tools/Import States")]
    public static void ImportStates()
    {
        if (Config.Instance == null)
        {
            Debug.LogError("[StateImporter] 未找到 AppConfig，无法导入状态数据");
            return;
        }

        string csvPath = Config.Instance.StateDataCSVPath;
        string outputFolder = Config.Instance.StateAssetOutputPath;
        if (string.IsNullOrWhiteSpace(csvPath))
        {
            Debug.LogError("[StateImporter] StateDataCSVPath 未配置");
            return;
        }

        if (string.IsNullOrWhiteSpace(outputFolder))
        {
            Debug.LogError("[StateImporter] StateAssetOutputPath 未配置");
            return;
        }

        List<Dictionary<string, string>> csvData = CSVReader.ReadCSV(csvPath);
        EnsureFolderExists(outputFolder);

        int importedCount = 0;
        for (int i = 0; i < csvData.Count; i++)
        {
            Dictionary<string, string> row = csvData[i];
            string stateName = GetString(row, "Name");
            if (string.IsNullOrWhiteSpace(stateName))
            {
                continue;
            }

            if (!TryGetEnum(row, "EnumName", out StateType stateType))
            {
                Debug.LogWarning($"[StateImporter] 状态 {stateName} 的 EnumName 无效，已跳过");
                continue;
            }

            string assetPath = $"{outputFolder}/{SanitizeAssetName(stateName)}.asset";
            State stateAsset = AssetDatabase.LoadAssetAtPath<State>(assetPath);
            if (stateAsset == null)
            {
                stateAsset = ScriptableObject.CreateInstance<State>();
                AssetDatabase.CreateAsset(stateAsset, assetPath);
            }

            stateAsset.name = stateName;
            stateAsset.stateType = stateType;
            stateAsset.description = GetString(row, "Description", stateAsset.description);
            stateAsset.baseMultiplier = GetFloat(row, "BaseMultiplier", 0f);
            stateAsset.baseExtraData1 = GetFloat(row, "Extra_Data_1", 0f);
            stateAsset.baseExtraData2 = GetFloat(row, "Extra_Data_2", 0f);
            stateAsset.baseExtraData3 = GetFloat(row, "Extra_Data_3", 0f);
            stateAsset.isDebuff = GetBool(row, "IsDebuff");
            stateAsset.isDot = GetBool(row, "IsDot");

            SerializedObject serializedObject = new SerializedObject(stateAsset);
            serializedObject.FindProperty("maxStacks").intValue = Mathf.Max(1, GetInt(row, "MaxStacks", 1));
            serializedObject.FindProperty("defaultTurns").intValue = Mathf.Max(1, GetInt(row, "DefaultTurns", 1));
            serializedObject.FindProperty("defaultActionValue").intValue = Mathf.Max(1, GetInt(row, "DefaultActionValue", 100));

            if (TryGetEnum(row, "DurationType", out StateDurationType durationType))
            {
                serializedObject.FindProperty("durationType").enumValueIndex = (int)durationType;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(stateAsset);
            importedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[StateImporter] 状态导入完成，共处理 {importedCount} 个状态资产");
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
