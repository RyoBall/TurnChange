using UnityEngine;

/// <summary>
/// 启动时将分辨率强制设为 1920×1080，并在运行期间持续校正。
/// </summary>
[DisallowMultipleComponent]
public class ScreenResolutionLocker : MonoBehaviour
{
    public const int TargetWidth = 1920;
    public const int TargetHeight = 1080;

    private static bool s_bootstrapped;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (s_bootstrapped)
        {
            return;
        }

        ApplyResolution();

        var lockerObject = new GameObject(nameof(ScreenResolutionLocker));
        lockerObject.AddComponent<ScreenResolutionLocker>();
        DontDestroyOnLoad(lockerObject);

        s_bootstrapped = true;
    }

#if !UNITY_EDITOR
    private void Update()
    {
        if (Screen.width == TargetWidth && Screen.height == TargetHeight)
        {
            return;
        }

        ApplyResolution();
    }
#endif

    private static void ApplyResolution()
    {
        Screen.SetResolution(TargetWidth, TargetHeight, Screen.fullScreenMode);
    }
}
