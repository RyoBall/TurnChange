using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class ChangePanelButton : MonoBehaviour
{
    [SerializeField] private RectTransform panel;
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
        yield return ScreenTransition.Instance.ExitTransition(); // 等待转场完成
    }
}
