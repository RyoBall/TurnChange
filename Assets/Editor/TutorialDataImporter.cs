using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class TutorialDataImporter
{
    [MenuItem("Tools/Import Tutorial Data")]
    public static void ImportTutorialData()
    {
        if (Config.Instance == null)
        {
            Debug.LogError("[TutorialDataImporter] 未找到 AppConfig，无法导入教程数据");
            return;
        }

        string csvPath = Config.Instance.TutorialDataCSVPath;
        string outputFolder = Config.Instance.TutorialAssetOutputPath;
        if (string.IsNullOrWhiteSpace(csvPath))
        {
            Debug.LogError("[TutorialDataImporter] TutorialDataCSVPath 未配置");
            return;
        }

        if (string.IsNullOrWhiteSpace(outputFolder))
        {
            Debug.LogError("[TutorialDataImporter] TutorialAssetOutputPath 未配置");
            return;
        }

        List<Dictionary<string, string>> csvData = CSVReader.ReadCSV(csvPath);
        EnsureFolderExists(outputFolder);

        int importedCount = 0;
        for (int i = 0; i < csvData.Count; i++)
        {
            Dictionary<string, string> row = csvData[i];
            string tutorialName = GetString(row, "教程名");
            if (string.IsNullOrWhiteSpace(tutorialName))
            {
                continue;
            }

            if (!TryGetEnum(row, "教程名", out TutorialType tutorialType))
            {
                Debug.LogWarning($"[TutorialDataImporter] 教程 {tutorialName} 的类型无法解析，已跳过");
                continue;
            }

            string assetPath = $"{outputFolder}/{SanitizeAssetName(tutorialName)}.asset";
            TutorialData tutorialAsset = AssetDatabase.LoadAssetAtPath<TutorialData>(assetPath);
            if (tutorialAsset == null)
            {
                tutorialAsset = ScriptableObject.CreateInstance<TutorialData>();
                AssetDatabase.CreateAsset(tutorialAsset, assetPath);
            }

            tutorialAsset.name = tutorialName;

            // 收集所有非空的教程文本
            List<string> textList = new List<string>();
            for (int col = 1; col <= 10; col++)
            {
                string key = $"教程文本{(col == 1 ? "一" : col == 2 ? "二" : col == 3 ? "三" : col == 4 ? "四" : col == 5 ? "五" : col == 6 ? "六" : col == 7 ? "七" : col == 8 ? "八" : col == 9 ? "九" : "十")}";
                string text = GetString(row, key);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    // 将 \n 替换为真正的换行符
                    text = text.Replace("\\n", "\n");
                    textList.Add(text);
                }
            }

            // 通过反射设置 m_textList
            SerializedObject serializedObject = new SerializedObject(tutorialAsset);
            SerializedProperty textListProp = serializedObject.FindProperty("m_textList");
            if (textListProp != null)
            {
                textListProp.ClearArray();
                for (int j = 0; j < textList.Count; j++)
                {
                    textListProp.InsertArrayElementAtIndex(j);
                    SerializedProperty elementProp = textListProp.GetArrayElementAtIndex(j);
                    elementProp.stringValue = textList[j];
                }
            }

            // 设置 m_type
            SerializedProperty typeProp = serializedObject.FindProperty("m_type");
            if (typeProp != null)
            {
                typeProp.intValue = (int)tutorialType;
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(tutorialAsset);
            importedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[TutorialDataImporter] 教程数据导入完成，共处理 {importedCount} 个教程资产");
    }

    private static string GetString(Dictionary<string, string> row, string key, string defaultValue = "")
    {
        if (row == null || !row.TryGetValue(key, out string value))
        {
            return defaultValue;
        }

        return value?.Trim() ?? defaultValue;
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
            string parentPath = currentPath;
            currentPath = $"{currentPath}/{segments[i]}";
            if (!AssetDatabase.IsValidFolder(currentPath))
            {
                AssetDatabase.CreateFolder(parentPath, segments[i]);
            }
        }
    }
}
