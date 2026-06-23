using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 战斗关卡数据导入器 — 从 BattleLevelData.csv 读取数据并生成 LevelSelectionData 资产
/// </summary>
public static class BattleLevelDataImporter
{
    /// <summary>
    /// CSV 中敌人代号 → EnemyRosterData.enemyID 的映射表（E=普通敌人，B=Boss/特殊敌人）
    /// </summary>
    private static readonly Dictionary<string, string> EnemyCodeToID = new Dictionary<string, string>
    {
        { "E1", "Shield" },   // 护盾手
        { "E2", "Single" },   // 单体攻击手
        { "E3", "Debuff" },   // 负面手
        { "E4", "Bomb" },     // 群体自爆手
        { "E5", "Dot" },      // 持续伤害施加手
        { "B1", "剑客" },
        { "B2", "混沌龙" },
        { "B3", "Dot龙" },
        { "B4", "直伤龙" },
        { "B5", "Chess" },    // 初始棋子
        { "B6", "Queen" },    // 皇后
    };

    /// <summary>
    /// 教程敌人代号 → 资产路径（与 E1/E2 等同 enemyID，需按路径区分）
    /// </summary>
    private static readonly Dictionary<string, string> TutorialEnemyCodeToAssetPath = new Dictionary<string, string>
    {
        { "T1", "Assets/Resources/配置可编程物体/参战者/敌人/教程/教程_护盾手(仅技能一).asset" },
        { "T2", "Assets/Resources/配置可编程物体/参战者/敌人/教程/教程_单体攻击手(定制).asset" },
    };

    // 缓存：enemyID → EnemyRosterData，避免重复查找
    private static Dictionary<string, EnemyRosterData> s_enemyCache;

    [MenuItem("Tools/Import Battle Level Data")]
    public static void ImportBattleLevelData()
    {
        if (Config.Instance == null)
        {
            Debug.LogError("[BattleLevelDataImporter] 未找到 AppConfig，无法导入关卡数据");
            return;
        }

        string csvPath = Config.Instance.BattleLevelDataCSVPath;
        string outputFolder = Config.Instance.LevelSelectionDataOutputPath;

        if (string.IsNullOrWhiteSpace(csvPath))
        {
            Debug.LogError("[BattleLevelDataImporter] BattleLevelDataCSVPath 未配置");
            return;
        }

        if (string.IsNullOrWhiteSpace(outputFolder))
        {
            Debug.LogError("[BattleLevelDataImporter] LevelSelectionDataOutputPath 未配置");
            return;
        }

        if (!File.Exists(csvPath))
        {
            Debug.LogError($"[BattleLevelDataImporter] CSV 文件不存在: {csvPath}");
            return;
        }

        // 加载敌人缓存
        BuildEnemyCache();

        List<Dictionary<string, string>> csvData = CSVReader.ReadCSV(csvPath);
        EnsureFolderExists(outputFolder);

        int importedCount = 0;
        for (int i = 0; i < csvData.Count; i++)
        {
            Dictionary<string, string> row = csvData[i];
            string levelId = GetString(row, "关卡编号");
            if (string.IsNullOrWhiteSpace(levelId))
            {
                Debug.LogWarning($"[BattleLevelDataImporter] 第 {i + 2} 行的关卡编号为空，已跳过");
                continue;
            }

            string levelName = GetString(row, "关卡名称");
            if (string.IsNullOrWhiteSpace(levelName))
            {
                Debug.LogWarning($"[BattleLevelDataImporter] 关卡 {levelId} 的名称为空，已跳过");
                continue;
            }

            // 生成资产路径：用 "关卡编号-关卡名称" 作为文件名
            string assetName = SanitizeAssetName($"{levelId}-{levelName}");
            string assetPath = $"{outputFolder}/{assetName}.asset";

            LevelSelectionData levelAsset = AssetDatabase.LoadAssetAtPath<LevelSelectionData>(assetPath);
            if (levelAsset == null)
            {
                levelAsset = ScriptableObject.CreateInstance<LevelSelectionData>();
                AssetDatabase.CreateAsset(levelAsset, assetPath);
            }

            // 基础字段
            levelAsset.levelId = levelId;
            levelAsset.levelName = levelName;
            levelAsset.name = $"{levelId}-{levelName}";
            levelAsset.isUnlocked = false; // 默认未解锁，由游戏逻辑控制
            string winCondition = GetString(row, "胜利条件");
            if (IsCreditsLevel(winCondition))
            {
                levelAsset.buttonType = LevelSelectionButtonType.CreditsLevel;
                levelAsset.eventData = null;
                levelAsset.enemyWaves.Clear();
            }
            else
            {
                levelAsset.buttonType = LevelSelectionButtonType.BattleLevel;
                levelAsset.eventData = null; // 战斗关无事件数据
            }
            levelAsset.playerLevel = Mathf.Max(1, GetInt(row, "玩家等级", 1));

            // 奖励
            levelAsset.rewardExperience = GetInt(row, "获得经验", 0);
            levelAsset.rewardGold = GetInt(row, "获得金币", 0);

            // 解析敌人波次
            string enemyConfig = GetString(row, "敌人阵容配置");
            int enemyLevel = GetInt(row, "敌人等级", 1);
            List<LevelEnemyWaveData> waves = ParseEnemyConfig(enemyConfig, enemyLevel);

            // 更新 enemyWaves：仅在解析出有效敌人时才修改，避免因无法识别的特殊文字清空已有数据
            if (waves != null && waves.Count > 0)
            {
                levelAsset.enemyWaves.Clear();
                levelAsset.enemyWaves.AddRange(waves);
            }
            else if (waves == null)
            {
                Debug.LogWarning($"[BattleLevelDataImporter] 关卡 {levelId} 的敌人阵容配置为空或无法解析，已保留原有敌人数据");
            }
            else
            {
                Debug.LogWarning($"[BattleLevelDataImporter] 关卡 {levelId} 的敌人阵容配置中所有敌人均无法识别，已保留原有敌人数据");
            }

            EditorUtility.SetDirty(levelAsset);
            importedCount++;
            Debug.Log($"[BattleLevelDataImporter] 已处理关卡: {levelId}-{levelName}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BattleLevelDataImporter] 关卡数据导入完成，共处理 {importedCount} 个关卡");
    }

    /// <summary>
    /// 加载所有 EnemyRosterData 资产到缓存，以 enemyID 为键
    /// </summary>
    private static void BuildEnemyCache()
    {
        s_enemyCache = new Dictionary<string, EnemyRosterData>();
        EnemyRosterData[] allEnemies = Resources.LoadAll<EnemyRosterData>("");

        foreach (EnemyRosterData enemy in allEnemies)
        {
            if (enemy == null || string.IsNullOrWhiteSpace(enemy.enemyID))
            {
                continue;
            }

            if (!s_enemyCache.ContainsKey(enemy.enemyID))
            {
                s_enemyCache.Add(enemy.enemyID, enemy);
            }
        }

        if (s_enemyCache.Count == 0)
        {
            Debug.LogWarning("[BattleLevelDataImporter] 未找到任何 EnemyRosterData 资产，请先导入敌人数据");
        }
    }

    /// <summary>
    /// 解析敌人阵容配置字符串
    /// 格式: W1:E2*1+E5*1;W2:E1*1+E5*1
    /// 支持独立等级: E2:-1*1 表示 E2 等级 -1
    /// 教程敌人: T1=教程盾手, T2=教程单体
    /// Boss敌人: B1=剑客, B2=混沌龙, B3=Dot龙, B4=直伤龙, B5=初始棋子, B6=皇后
    /// W(Wave) = 波次, *后面的数字 = 数量
    /// </summary>
    private static List<LevelEnemyWaveData> ParseEnemyConfig(string config, int defaultLevel)
    {
        if (string.IsNullOrWhiteSpace(config))
        {
            Debug.LogWarning("[BattleLevelDataImporter] 敌人阵容配置为空");
            return null;
        }

        var result = new List<LevelEnemyWaveData>();

        // 按分号分割波次
        string[] waveParts = config.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string wavePart in waveParts)
        {
            string trimmed = wavePart.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            // 匹配波次格式: W1:E2*1+E5*1 或 W1:特殊_xxx*1
            Match waveMatch = Regex.Match(trimmed, @"^W(\d+):(.+)$");
            if (!waveMatch.Success)
            {
                Debug.LogWarning($"[BattleLevelDataImporter] 无法解析波次配置: {trimmed}");
                continue;
            }

            string enemiesPart = waveMatch.Groups[2].Value.Trim();
            if (string.IsNullOrWhiteSpace(enemiesPart))
            {
                continue;
            }

            var waveData = new LevelEnemyWaveData();

            // 按加号分割同一波次中的不同敌人
            string[] enemyEntries = enemiesPart.Split(new char[] { '+' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string entry in enemyEntries)
            {
                string entryTrimmed = entry.Trim();
                if (string.IsNullOrWhiteSpace(entryTrimmed))
                {
                    continue;
                }

                int count = 1;
                int enemyLevel = defaultLevel;
                EnemyRosterData enemyData = null;

                // 尝试匹配代号格式（支持独立等级）: E2:-1*1、T1*1、B3*1
                Match standardMatch = Regex.Match(entryTrimmed, @"^([ETB]\d+)(?::(-?\d+))?(?:\*(\d+))?$");
                if (standardMatch.Success)
                {
                    string enemyCode = standardMatch.Groups[1].Value;
                    if (standardMatch.Groups[2].Success)
                    {
                        int.TryParse(standardMatch.Groups[2].Value, out enemyLevel);
                    }
                    if (standardMatch.Groups[3].Success)
                    {
                        int.TryParse(standardMatch.Groups[3].Value, out count);
                    }

                    enemyData = ResolveEnemyData(enemyCode);
                }
                else
                {
                    // 尝试匹配特殊敌人格式: 特殊_名称:-1*1 或 特殊_名称*1
                    Match specialMatch = Regex.Match(entryTrimmed, @"^特殊_(.+?)(?::(-?\d+))?(?:\*(\d+))?$");
                    if (specialMatch.Success)
                    {
                        string specialName = specialMatch.Groups[1].Value.Trim();
                        if (specialMatch.Groups[2].Success)
                        {
                            int.TryParse(specialMatch.Groups[2].Value, out enemyLevel);
                        }
                        if (specialMatch.Groups[3].Success)
                        {
                            int.TryParse(specialMatch.Groups[3].Value, out count);
                        }

                        enemyData = ResolveSpecialEnemyData(specialName);
                    }
                    else
                    {
                        Debug.LogWarning($"[BattleLevelDataImporter] 无法解析敌人条目: {entryTrimmed}");
                        continue;
                    }
                }

                if (enemyData == null)
                {
                    Debug.LogWarning($"[BattleLevelDataImporter] 未找到敌人条目 {entryTrimmed} 对应的 EnemyRosterData");
                    continue;
                }

                // 按数量添加
                for (int j = 0; j < count; j++)
                {
                    waveData.enemies.Add(new LevelEnemyEntry
                    {
                        enemyData = enemyData,
                        level = enemyLevel
                    });
                }
            }

            if (waveData.enemies.Count > 0)
            {
                result.Add(waveData);
            }
        }

        return result;
    }

    /// <summary>
    /// 通过 CSV 中的敌人代号查找 EnemyRosterData
    /// </summary>
    private static EnemyRosterData ResolveEnemyData(string enemyCode)
    {
        if (s_enemyCache == null)
        {
            BuildEnemyCache();
        }

        if (TutorialEnemyCodeToAssetPath.TryGetValue(enemyCode, out string assetPath))
        {
            EnemyRosterData tutorialEnemy = AssetDatabase.LoadAssetAtPath<EnemyRosterData>(assetPath);
            if (tutorialEnemy != null)
            {
                return tutorialEnemy;
            }

            Debug.LogWarning($"[BattleLevelDataImporter] 教程敌人代号 {enemyCode} 的资产未找到: {assetPath}");
            return null;
        }

        // 先查代号映射表
        if (EnemyCodeToID.TryGetValue(enemyCode, out string enemyID))
        {
            if (s_enemyCache.TryGetValue(enemyID, out EnemyRosterData data))
            {
                return data;
            }

            Debug.LogWarning($"[BattleLevelDataImporter] 代号 {enemyCode} → enemyID '{enemyID}' 在缓存中未找到");
        }
        else
        {
            Debug.LogWarning($"[BattleLevelDataImporter] 未知的敌人代号: {enemyCode}，请在 EnemyCodeToID 映射表中添加");
        }

        return null;
    }

    /// <summary>
    /// 通过特殊敌人名称查找 EnemyRosterData（模糊匹配 enemyName 或 enemyID）
    /// </summary>
    private static EnemyRosterData ResolveSpecialEnemyData(string specialName)
    {
        if (s_enemyCache == null)
        {
            BuildEnemyCache();
        }

        // 遍历缓存，匹配 enemyName 或 enemyID
        foreach (EnemyRosterData enemy in s_enemyCache.Values)
        {
            if (enemy == null)
            {
                continue;
            }

            if ((!string.IsNullOrWhiteSpace(enemy.enemyName) && enemy.enemyName.Contains(specialName)) ||
                (!string.IsNullOrWhiteSpace(enemy.enemyID) && enemy.enemyID.Contains(specialName)))
            {
                return enemy;
            }
        }

        Debug.LogWarning($"[BattleLevelDataImporter] 未找到匹配特殊名称 '{specialName}' 的 EnemyRosterData");
        return null;
    }

    // ====== 辅助方法 ======

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

    private static bool IsCreditsLevel(string winCondition)
    {
        if (string.IsNullOrWhiteSpace(winCondition))
        {
            return false;
        }

        return winCondition.Contains("制作人")
            || winCondition.Contains("Credits", StringComparison.OrdinalIgnoreCase);
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
