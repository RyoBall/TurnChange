using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitLevelPanelButton : MonoBehaviour
{
    public void OnButtonClick()
    {
        // 切换面板的显示状态
        PreparationPanelView.Instance.Close();
    }
}
