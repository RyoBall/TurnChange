using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using System.IO;
using System;
public class CharacterDataImporter
{

    [MenuItem("Tools/ImportCharacterDatas")]
    public static void ImportCharacterDatas()
    {
        Debug.Log("正在导入角色数据...");
        if (LevelDataContainer.CharacterLevelData == null)
            LevelDataContainer.CharacterLevelData = new Dictionary<string, Dictionary<int, CharacterLevelData>>();
        List<Dictionary<string, string>> csvData = CSVReader.ReadCSV(Config.Instance.CharacterDataCSVPath);
        string characterName = null;
        int ID=0;
        foreach (var row in csvData)
        {
            if (GetInt(row, "Level") == -1)
            {
                characterName = row["Level"];
                ID++;
                Debug.Log($"正在导入角色: {characterName},ID: {ID}");
                continue;
            }
            int level = GetInt(row, "Level");
            CharacterLevelData levelData = new CharacterLevelData
            (
                GetInt(row, "MaxHP"),
                GetInt(row, "Attack"),
                GetInt(row, "Defense"),
                GetPercent(row, "CriticalRate"),
                GetPercent(row, "CriticalEffect"),
                GetInt(row, "Speed"),
                GetInt(row, "K")
            );
            if (!LevelDataContainer.CharacterLevelData.ContainsKey(ID.ToString()))
                LevelDataContainer.CharacterLevelData[ID.ToString()] = new Dictionary<int, CharacterLevelData>();
            LevelDataContainer.CharacterLevelData[ID.ToString()][level] = levelData;
        }
        Debug.Log($"角色数据导入完成！");
    }

    static int GetInt(Dictionary<string, string> dict, string key, int defaultValue = -1)
    {
        if (!dict.ContainsKey(key)) return defaultValue;
        if (int.TryParse(dict[key], out int result)) 
        {
            return result;
        }
        return defaultValue;
    }
    static float GetFloat(Dictionary<string, string> dict, string key, float defaultValue = -1)
    {
        if (!dict.ContainsKey(key)) return defaultValue;
        if (float.TryParse(dict[key], out float result)) return result;
        return defaultValue;
    }
    static float GetPercent(Dictionary<string, string> dict, string key, float defaultValue = -1)
    {
        if (!dict.ContainsKey(key)) return defaultValue;    
        string value = dict[key].Replace("%", "");
        if (float.TryParse(value, out float result)) 
        {
            return result / 100f;
        }
        return defaultValue;
    }
#if UNITY_EDITOR
    [InitializeOnLoadMethod]  // 编辑器启动或脚本重编译时自动执行
    static void EditorInitialize()
    {
        ImportCharacterDatas();
        EditorApplication.projectChanged -= ImportCharacterDatas; // 避免重复订阅
        EditorApplication.projectChanged += ImportCharacterDatas; // 项目发生变化时重新导入数据
    }
#endif
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RuntimeInitialize()
    {
        ImportCharacterDatas();
    }
}