using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 背包开发者快捷键：N 获得随机序体与 1000 金币；B 获得所有种类序体各一个。
/// 进入 Main 场景时自动创建。
/// </summary>
[DisallowMultipleComponent]
public class BackpackDevHotkeys : MonoBehaviour
{
    private const string MainSceneName = "Main";
    private const string ModuleResourcePath = "配置可编程物体/模块";
    private const int DevGoldReward = 1000;

    private static readonly List<GridModuleDefinition> s_modulePoolBuffer = new List<GridModuleDefinition>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryCreateForScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreateForScene(scene);
    }

    private static void TryCreateForScene(Scene scene)
    {
        if (scene.name != MainSceneName)
        {
            return;
        }

        if (Object.FindAnyObjectByType<BackpackDevHotkeys>() != null)
        {
            return;
        }

        var host = new GameObject(nameof(BackpackDevHotkeys));
        host.AddComponent<BackpackDevHotkeys>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            GrantRandomModuleAndGold();
            return;
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            GrantAllModuleTypes();
        }
    }

    private static void GrantRandomModuleAndGold()
    {
        Datas datas = Datas.Instance;
        if (datas == null)
        {
            Debug.LogWarning("[BackpackDevHotkeys] Datas.Instance 为空，无法发放开发者奖励。");
            return;
        }

        GridModuleDefinition randomModule = PickRandomModuleDefinition();
        if (randomModule == null)
        {
            Debug.LogWarning("[BackpackDevHotkeys] 未找到可用序体资源，无法发放随机序体。");
            return;
        }

        datas.AddGold(DevGoldReward);
        GridModuleDefinition addedModule = datas.AddOwnedModule(randomModule);
        if (addedModule == null)
        {
            Debug.LogWarning("[BackpackDevHotkeys] 随机序体加入背包失败。");
            return;
        }

        Debug.Log($"[Dev] 已获得 {DevGoldReward} 金币与序体「{addedModule.moduleName}」（按 N）。");
    }

    private static void GrantAllModuleTypes()
    {
        Datas datas = Datas.Instance;
        if (datas == null)
        {
            Debug.LogWarning("[BackpackDevHotkeys] Datas.Instance 为空，无法发放全种类序体。");
            return;
        }

        GridModuleDefinition[] allModules = Resources.LoadAll<GridModuleDefinition>(ModuleResourcePath);
        int addedCount = 0;

        for (int i = 0; i < allModules.Length; i++)
        {
            GridModuleDefinition module = allModules[i];
            if (module == null || module.moduleType == GridModuleType.None)
            {
                continue;
            }

            if (datas.AddOwnedModule(module) != null)
            {
                addedCount++;
            }
        }

        if (addedCount == 0)
        {
            Debug.LogWarning("[BackpackDevHotkeys] 未找到可用序体资源，无法发放全种类序体。");
            return;
        }

        Debug.Log($"[Dev] 已获得全部 {addedCount} 种序体各 1 个（按 B）。");
    }

    private static GridModuleDefinition PickRandomModuleDefinition()
    {
        s_modulePoolBuffer.Clear();

        GridModuleDefinition[] allModules = Resources.LoadAll<GridModuleDefinition>(ModuleResourcePath);
        for (int i = 0; i < allModules.Length; i++)
        {
            GridModuleDefinition module = allModules[i];
            if (module == null || module.moduleType == GridModuleType.None)
            {
                continue;
            }

            s_modulePoolBuffer.Add(module);
        }

        if (s_modulePoolBuffer.Count == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, s_modulePoolBuffer.Count);
        return s_modulePoolBuffer[randomIndex];
    }
}
