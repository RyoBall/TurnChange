using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 关卡选择界面开发者快捷键：M 解锁当前楼层全部关卡。
/// 进入 Main 场景时自动创建。
/// </summary>
[DisallowMultipleComponent]
public class LevelSelectionDevHotkeys : MonoBehaviour
{
    private const string MainSceneName = "Main";

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

        if (Object.FindAnyObjectByType<LevelSelectionDevHotkeys>() != null)
        {
            return;
        }

        var host = new GameObject(nameof(LevelSelectionDevHotkeys));
        host.AddComponent<LevelSelectionDevHotkeys>();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.M))
        {
            return;
        }

        UnlockCurrentFloorLevels();
    }

    private static void UnlockCurrentFloorLevels()
    {
        if (Datas.Instance == null)
        {
            Debug.LogWarning("[LevelSelectionDevHotkeys] Datas.Instance 为空，无法解锁关卡。");
            return;
        }

        int floorIndex = Datas.Instance.GetCurrentFloorIndex();
        Datas.Instance.DevUnlockCurrentFloorLevels();
        Debug.Log($"[Dev] 已解锁第 {floorIndex + 1} 层全部关卡（按 M）。");
    }
}
