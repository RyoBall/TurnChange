using UnityEngine;

/// <summary>
/// Debug 模式单例，用于控制是否启用调试功能
/// </summary>
public class DebugMode : MonoBehaviour
{
    public static DebugMode Instance { get; private set; }

    [SerializeField] private bool m_isDebugMode;

    public bool IsDebugMode => m_isDebugMode;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 在运行时切换 Debug 模式
    /// </summary>
    public void ToggleDebugMode()
    {
        m_isDebugMode = !m_isDebugMode;
        Debug.Log($"[DebugMode] Debug 模式已{(m_isDebugMode ? "开启" : "关闭")}");
    }

    /// <summary>
    /// 设置 Debug 模式的开关状态
    /// </summary>
    public void SetDebugMode(bool enabled)
    {
        m_isDebugMode = enabled;
        Debug.Log($"[DebugMode] Debug 模式已{(m_isDebugMode ? "开启" : "关闭")}");
    }
}
