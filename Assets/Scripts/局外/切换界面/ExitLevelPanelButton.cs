using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitLevelPanelButton : MonoBehaviour
{
    public void OnButtonClick()
    {
        // 切换面板的显示状态
        StartCoroutine(SwitchPanelCoroutine());
    }
    private IEnumerator SwitchPanelCoroutine()
    {
        // 在这里可以添加切换动画或过渡效果
        yield return ScreenTransition.Instance.Transition(() => PreparationPanelView.Instance.Close());
    }
}
