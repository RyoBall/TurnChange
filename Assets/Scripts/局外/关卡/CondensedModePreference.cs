using UnityEngine;

/// <summary>
/// 浓缩模式运行时偏好：启动时从 PlayerPrefs 加载，并在设置界面切换时持久化。
/// </summary>
public static class CondensedModePreference
{
    private const string PrefKey = "condensed_mode_enabled";
    private const int PrefNotSet = -1;

    private static bool s_hasLoaded;
    private static bool s_isEnabled;

    public static bool IsEnabled
    {
        get
        {
            EnsureLoaded();
            return s_isEnabled;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureLoaded();
    }

    public static void SetEnabled(bool enabled)
    {
        EnsureLoaded();
        if (s_isEnabled == enabled)
        {
            return;
        }

        s_isEnabled = enabled;
        PlayerPrefs.SetInt(PrefKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        ApplyToActiveConfig(notifyChange: true);
    }

    public static void ApplyToConfigWithoutNotify(CondensedLevelConfig config)
    {
        if (config == null)
        {
            return;
        }

        config.ApplyRuntimeCondensedMode(s_isEnabled);
    }

    private static void EnsureLoaded()
    {
        if (s_hasLoaded)
        {
            return;
        }

        int stored = PlayerPrefs.GetInt(PrefKey, PrefNotSet);
        if (stored == PrefNotSet)
        {
            CondensedLevelConfig defaultConfig = CondensedLevelConfig.LoadDefaultAsset();
            s_isEnabled = defaultConfig != null && defaultConfig.IsCondensedModeEnabled;
        }
        else
        {
            s_isEnabled = stored != 0;
        }

        s_hasLoaded = true;
    }

    private static void ApplyToActiveConfig(bool notifyChange)
    {
        if (Datas.Instance != null)
        {
            CondensedLevelConfig config = Datas.Instance.GetCondensedLevelConfig() as CondensedLevelConfig;
            if (config == null)
            {
                return;
            }

            if (notifyChange)
            {
                config.SetCondensedModeEnabled(s_isEnabled);
            }
            else
            {
                ApplyToConfigWithoutNotify(config);
            }

            return;
        }

        ApplyToConfigWithoutNotify(CondensedLevelConfig.LoadDefaultAsset());
    }
}
