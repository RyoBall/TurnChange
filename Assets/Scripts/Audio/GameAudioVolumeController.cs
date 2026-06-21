using UnityEngine;

/// <summary>
/// 全局主音量控制：启动时从 PlayerPrefs 加载，并通过 AudioListener.volume 统一生效。
/// </summary>
public interface IGameAudioVolumeController
{
    float MasterVolume { get; }
    void SetMasterVolume(float volume);
}

public static class GameAudioVolumeController
{
    private const string PrefKey = "master_volume";

    private static float s_masterVolume = 1f;

    public static float MasterVolume => s_masterVolume;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        s_masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefKey, 1f));
        Apply();
    }

    public static void SetMasterVolume(float volume)
    {
        s_masterVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(PrefKey, s_masterVolume);
        PlayerPrefs.Save();
        Apply();
    }

    private static void Apply()
    {
        AudioListener.volume = s_masterVolume;
    }
}
