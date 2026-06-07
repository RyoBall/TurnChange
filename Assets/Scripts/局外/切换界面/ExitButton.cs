using System;
using System.Collections;
using UnityEngine;

public class ExitButton : MonoBehaviour
{
    /// <summary>面板关闭时的静态事件，参数为关闭的面板类型</summary>
    public static event Action<PanelType> PanelClosed;

    [SerializeField] private RectTransform panel;
    [SerializeField] private PanelType m_panelType;

    public void OnButtonClick()
    {
        // 切换面板的显示状态
        StartCoroutine(SwitchPanelCoroutine());
    }
    private IEnumerator SwitchPanelCoroutine()
    {
        // 在这里可以添加切换动画或过渡效果
        yield return ScreenTransition.Instance.Transition(() => {panel.gameObject.SetActive(false);PanelClosed?.Invoke(m_panelType);});    }
}
