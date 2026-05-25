using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
public class EnemyDataImporter
{
    [MenuItem("Tools/ImportEnemyDatas")]
    public static void ImportEnemyDatas()
    {
        Debug.Log("正在导入敌人数据...");
        if (Config.Instance == null)
        {
            Debug.LogError("[EnemyDataImporter] 未找到 AppConfig，无法导入敌人数据");
            return;
        }

        var importedData = new Dictionary<string, Dictionary<int, EnemyLevelData>>();
        List<Dictionary<string, string>> csvData = CSVReader.ReadCSV(Config.Instance.EnemyDataCSVPath);
        string enemyName = null;
        foreach (var row in csvData)
        {
            if (GetInt(row, "Level") == -1)
            {
                enemyName = row["Level"];
                Debug.Log($"正在导入敌人: {enemyName}");
                continue;
            }
            int level = GetInt(row, "Level");
            EnemyLevelData levelData = new EnemyLevelData
            (
                GetInt(row, "MaxHP"),
                GetInt(row, "Attack"),
                GetInt(row, "Defense"), 
                GetInt(row, "Speed"),
                GetFloat(row, "K")
            );
            if (string.IsNullOrWhiteSpace(enemyName))
            {
                continue;
            }

            if (!importedData.ContainsKey(enemyName))
                importedData[enemyName] = new Dictionary<int, EnemyLevelData>();
            importedData[enemyName][level] = levelData;
        }

        CharacterLevelDataContainer dataContainer = CharacterLevelDataContainer.GetOrCreateAsset();
        dataContainer.ImportEnemyData(importedData);
        EditorUtility.SetDirty(dataContainer);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"敌人数据导入完成！已更新资产: {CharacterLevelDataContainer.AssetPath}");
    }

    static int GetInt(Dictionary<string, string> dict, string key, int defaultValue = -1)
    {
        if (!dict.ContainsKey(key)) return defaultValue;
        if (int.TryParse(dict[key], out int result)) return result;
        return defaultValue;
    }
    static float GetFloat(Dictionary<string, string> dict, string key, float defaultValue = 0)
    {
        if (!dict.ContainsKey(key)) return defaultValue;
        if (float.TryParse(dict[key], out float result)) return result;
        return defaultValue;
    }

    #if UNITY_EDITOR
    [InitializeOnLoadMethod]  // 编辑器启动或脚本重编译时自动执行
    static void EditorInitialize()
    {
        ImportEnemyDatas();
        EditorApplication.projectChanged -= ImportEnemyDatas; // 避免重复订阅
        EditorApplication.projectChanged += ImportEnemyDatas; // 项目发生变化时重新导入数据
    }
    #endif
}
