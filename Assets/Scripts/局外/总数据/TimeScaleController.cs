using UnityEngine;
using System;
/// <summary>
/// 全局时间流速控制器接口。
/// 其他模块只允许通过此接口访问时间流速功能，禁止直接引用实现类。
/// </summary>
public interface ITimeScaleController
{
    /// <summary>当前目标时间流速（只读）</summary>
    float CurrentTimeScale { get; }

    /// <summary>时间流速发生变化时触发</summary>
    event Action<float> TimeScaleChanged;

    /// <summary>设置时间流速，自动 clamp 到 [0, 100]</summary>
    void SetTimeScale(float timeScale);

    /// <summary>恢复为默认 1.5 倍速</summary>
    void ResetToDefault();

    /// <summary>暂停时间（timeScale = 0）</summary>
    void Pause();

    /// <summary>压栈暂停：首次调用时缓存当前流速，可嵌套配对 PopPause</summary>
    void PushPause();

    /// <summary>弹栈恢复：仅当栈空时恢复到 PushPause 前的流速</summary>
    void PopPause();
}

/// <summary>
/// 全局时间流速控制器。
/// 默认 1.5 倍速，实现 ITimeScaleController 接口。
/// 其他模块只能通过 ITimeScaleController 接口访问，禁止直接引用此类。
/// 挂载在 Datas 所在的 DontDestroyOnLoad GameObject 上。
/// </summary>
[DefaultExecutionOrder(100)]
public class TimeScaleController : MonoBehaviour, ITimeScaleController
{
    private const float DefaultTimeScale = 1.5f;

    /// <summary>全局访问点，返回接口类型。其他模块只允许通过此属性访问。</summary>
    public static ITimeScaleController Instance { get; private set; }

    private float m_currentTimeScale = DefaultTimeScale;
    private float m_savedTimeScaleBeforePause = DefaultTimeScale;
    private int m_pauseDepth;

    float ITimeScaleController.CurrentTimeScale => m_currentTimeScale;

    event System.Action<float> ITimeScaleController.TimeScaleChanged
    {
        add { m_timeScaleChanged += value; }
        remove { m_timeScaleChanged -= value; }
    }

    private event System.Action<float> m_timeScaleChanged;

    private void Awake()
    {
        if (!TryClaimSingleton())
        {
            return;
        }
    }

    private void Update()
    {
        SyncTimeScaleToEngine();
    }

    private void OnDestroy()
    {
        ClearSingleton();
    }

    private bool TryClaimSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return false;
        }

        Instance = this;
        return true;
    }

    private void ClearSingleton()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void ITimeScaleController.SetTimeScale(float timeScale)
    {
        float clamped = Mathf.Clamp(timeScale, 0f, 100f);

        if (Mathf.Approximately(m_currentTimeScale, clamped))
        {
            return;
        }

        m_currentTimeScale = clamped;
        m_timeScaleChanged?.Invoke(m_currentTimeScale);
        SyncTimeScaleToEngine();
    }

    void ITimeScaleController.ResetToDefault()
    {
        ((ITimeScaleController)this).SetTimeScale(DefaultTimeScale);
    }

    void ITimeScaleController.Pause()
    {
        ((ITimeScaleController)this).SetTimeScale(0f);
    }

    void ITimeScaleController.PushPause()
    {
        if (m_pauseDepth == 0)
        {
            m_savedTimeScaleBeforePause = m_currentTimeScale > 0f ? m_currentTimeScale : DefaultTimeScale;
        }

        m_pauseDepth++;
        ((ITimeScaleController)this).SetTimeScale(0f);
    }

    void ITimeScaleController.PopPause()
    {
        if (m_pauseDepth <= 0)
        {
            return;
        }

        m_pauseDepth--;
        if (m_pauseDepth == 0)
        {
            ((ITimeScaleController)this).SetTimeScale(m_savedTimeScaleBeforePause);
        }
    }

    private void SyncTimeScaleToEngine()
    {
        Time.timeScale = m_currentTimeScale;
    }
}
