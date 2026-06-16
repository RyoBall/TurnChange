using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 教程关 EnemyRosterData 生成器：为教程关创建定制的敌人配置和预制体
/// </summary>
public static class TutorialEnemyRosterDataGenerator
{
    private const string OutputFolder = "Assets/Resources/配置可编程物体/参战者/敌人/教程";
    private const string PrefabOutputFolder = "Assets/Resources/Prefabs/教程敌人";

    [MenuItem("Tools/Generate Tutorial Enemy Roster Data")]
    public static void GenerateTutorialEnemyRosterData()
    {
        EnsureFolderExists(OutputFolder);
        EnsureFolderExists(PrefabOutputFolder);

        // 查找默认敌人预制体
        GameObject defaultEnemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/角色预制体/Enemy.prefab");
        if (defaultEnemyPrefab == null)
        {
            // 尝试其他路径
            string[] enemyPrefabGuids = AssetDatabase.FindAssets("Enemy t:Prefab");
            foreach (string guid in enemyPrefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("Enemy") && !path.Contains("Tutorial"))
                {
                    defaultEnemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    break;
                }
            }
        }

        if (defaultEnemyPrefab == null)
        {
            Debug.LogError("[TutorialEnemyRosterDataGenerator] 未找到默认敌人预制体，请手动创建教程敌人预制体");
        }

        // 关卡一：护盾手只使用技能一（使用默认 Enemy 预制体即可）
        EnemyRosterData shieldOnly1 = CreateEnemyRosterData(
            "教程_护盾手(仅技能一)",
            "Shield",
            new List<EnemySkillType> { EnemySkillType.ShieldSupport_1 }
        );

        // 关卡二 W1：定制单体攻击手（TutorialSingleEnemy）
        GameObject tutorialSinglePrefab = CreateTutorialEnemyPrefab(defaultEnemyPrefab, "TutorialSingleEnemy");
        EnemyRosterData tutorialSingle = CreateEnemyRosterData(
            "教程_单体攻击手(定制)",
            "TutorialSingle",
            new List<EnemySkillType> { EnemySkillType.SingleAttack },
            tutorialSinglePrefab
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[TutorialEnemyRosterDataGenerator] 教程敌人配置生成完成");
    }

    private static GameObject CreateTutorialEnemyPrefab(GameObject sourcePrefab, string enemyClassName)
    {
        if (sourcePrefab == null)
        {
            Debug.LogWarning($"[TutorialEnemyRosterDataGenerator] 源预制体为空，无法创建 {enemyClassName} 预制体");
            return null;
        }

        string prefabPath = $"{PrefabOutputFolder}/{enemyClassName}.prefab";

        // 检查是否已存在
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing != null)
        {
            Debug.Log($"[TutorialEnemyRosterDataGenerator] 预制体已存在: {prefabPath}");
            return existing;
        }

        // 实例化源预制体
        GameObject instance = Object.Instantiate(sourcePrefab);
        instance.name = enemyClassName;

        // 移除原有的 Enemy 组件
        Enemy existingEnemy = instance.GetComponent<Enemy>();
        if (existingEnemy != null)
        {
            Object.DestroyImmediate(existingEnemy);
        }

        // 添加 TutorialSingleEnemy 组件
        System.Type enemyType = System.Type.GetType(enemyClassName);
        if (enemyType == null)
        {
            // 尝试在 Assembly-CSharp 中查找
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                enemyType = assembly.GetType(enemyClassName);
                if (enemyType != null) break;
            }
        }

        if (enemyType != null && typeof(Enemy).IsAssignableFrom(enemyType))
        {
            instance.AddComponent(enemyType);
        }
        else
        {
            Debug.LogError($"[TutorialEnemyRosterDataGenerator] 未找到类型 {enemyClassName} 或它不是 Enemy 的子类");
            Object.DestroyImmediate(instance);
            return null;
        }

        // 保存为预制体
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        Object.DestroyImmediate(instance);

        Debug.Log($"[TutorialEnemyRosterDataGenerator] 已创建预制体: {prefabPath}");
        return prefab;
    }

    private static EnemyRosterData CreateEnemyRosterData(string assetName, string enemyID, List<EnemySkillType> skills, GameObject prefabOverride = null)
    {
        string assetPath = $"{OutputFolder}/{assetName}.asset";

        EnemyRosterData existing = AssetDatabase.LoadAssetAtPath<EnemyRosterData>(assetPath);
        if (existing != null)
        {
            existing.enemyID = enemyID;
            existing.enemyName = assetName;
            existing.skills = new List<EnemySkillType>(skills);
            existing.prefabOverride = prefabOverride;
            EditorUtility.SetDirty(existing);
            Debug.Log($"[TutorialEnemyRosterDataGenerator] 已更新: {assetPath}");
            return existing;
        }

        EnemyRosterData data = ScriptableObject.CreateInstance<EnemyRosterData>();
        data.enemyID = enemyID;
        data.enemyName = assetName;
        data.skills = new List<EnemySkillType>(skills);
        data.prefabOverride = prefabOverride;

        AssetDatabase.CreateAsset(data, assetPath);
        Debug.Log($"[TutorialEnemyRosterDataGenerator] 已创建: {assetPath}");
        return data;
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
