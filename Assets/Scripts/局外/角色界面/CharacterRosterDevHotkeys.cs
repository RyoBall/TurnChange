using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 角色解锁开发者快捷键：V 解锁全部可用角色。
/// 进入 Main 场景时自动创建。
/// </summary>
[DisallowMultipleComponent]
public class CharacterRosterDevHotkeys : MonoBehaviour
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

        if (Object.FindAnyObjectByType<CharacterRosterDevHotkeys>() != null)
        {
            return;
        }

        var host = new GameObject(nameof(CharacterRosterDevHotkeys));
        host.AddComponent<CharacterRosterDevHotkeys>();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.V))
        {
            return;
        }

        UnlockAllCharacters();
    }

    private static void UnlockAllCharacters()
    {
        if (Datas.Instance == null)
        {
            Debug.LogWarning("[CharacterRosterDevHotkeys] Datas.Instance 为空，无法解锁角色。");
            return;
        }

        int beforeCount = Datas.Instance.GetUnlockedCharacterRosters().Count;
        Datas.Instance.DevUnlockAllCharacters();
        int afterCount = Datas.Instance.GetUnlockedCharacterRosters().Count;
        int addedCount = afterCount - beforeCount;

        if (addedCount > 0)
        {
            Debug.Log($"[Dev] 已解锁 {addedCount} 名角色，当前共 {afterCount} 名（按 V）。");
            return;
        }

        Debug.Log($"[Dev] 全部角色已解锁，当前共 {afterCount} 名（按 V）。");
    }
}
