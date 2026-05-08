using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using System.IO;
using System;
public class EnemyDataImporter
{
   [MenuItem("Tools/")]
    public static void ImportEnemyDatas()
    {
        Debug.Log("正在导入敌人数据...");
        if(LevelDataContainer.EnemyLevelData==null)
            LevelDataContainer.EnemyLevelData = new Dictionary<string, Dictionary<int, EnemyLevelData>>();
        List<Dictionary<string, string>> csvData = CSVReader.ReadCSV(Config.Instance.EnemyDataCSVPath);
        string enemyName=null;
        foreach (var row in csvData)
        {
            if(GetInt(row,"Level")==-1)
            {
                enemyName = row["Level"];
                Debug.Log($"正在导入敌人: {enemyName}");
                continue;
            }
            int level= GetInt(row, "Level");
            EnemyLevelData levelData = new EnemyLevelData
            (
                GetInt(row, "MaxHP"),
                GetInt(row, "Attack"),
                GetInt(row, "Defense"), 
                GetInt(row, "Speed"),
                GetFloat(row,"K")
            );
            if (!LevelDataContainer.EnemyLevelData.ContainsKey(enemyName))
                LevelDataContainer.EnemyLevelData[enemyName] = new Dictionary<int, EnemyLevelData>();
            LevelDataContainer.EnemyLevelData[enemyName][level] = levelData;
        }
        Debug.Log("敌人数据导入完成！");
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
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RuntimeInitialize()
    {
        ImportEnemyDatas();
    }
}
