using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 页面类型枚举
/// </summary>
public enum PanelType
{
    角色页面,
    背包页面,
    商店页面,
    关卡页面,
}

public class ChangePanelButton : MonoBehaviour
{
    /// <summary>页面切换时的静态事件，参数为打开的页面类型</summary>
    public static event Action<PanelType> PanelSwitched;

    [SerializeField] private RectTransform panel;
    [SerializeField] private PanelType m_panelType;

    public void OnButtonClick()
    {
        // 切换面板的显示状态
        StartCoroutine(SwitchPanelCoroutine());
    }

    IEnumerator SwitchPanelCoroutine()
    {
        // 在这里可以添加切换动画或过渡效果
        yield return ScreenTransition.Instance.EnterTransition(); // 等待转场完成
        panel.gameObject.SetActive(true);
        PanelSwitched?.Invoke(m_panelType);
        yield return ScreenTransition.Instance.ExitTransition(); // 等待转场完成
    }
}
