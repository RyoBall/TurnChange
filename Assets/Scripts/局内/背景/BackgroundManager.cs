using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class BackgroundManager : MonoBehaviour
{
    public static BackgroundManager Instance { get; private set; }
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    public Material backgroundMaterial;
    public Color defaultColor = Color.white;
    public Color darkColor;
    public float duration;
    public Ease easeType = Ease.InOutQuad;

    /// <summary>当前变暗请求的优先级，0 表示未变暗</summary>
    private int m_darkPriority = 0;

    /// <summary>
    /// 切换背景明暗。
    /// </summary>
    /// <param name="enter">true 变暗，false 恢复</param>
    /// <param name="priority">优先级，变暗时取较大值保留，恢复时低于当前优先级则忽略。默认 1。</param>
    public Tween ChangeBackground(bool enter, int priority = 1)
    {
        if (enter)
        {
            if (priority > m_darkPriority)
            {
                m_darkPriority = priority;
            }
            return backgroundMaterial.DOColor(darkColor, duration).SetEase(easeType);
        }
        else
        {
            if (priority < m_darkPriority)
            {
                return null;
            }
            m_darkPriority = 0;
            return backgroundMaterial.DOColor(defaultColor, duration).SetEase(easeType);
        }
    }
}
