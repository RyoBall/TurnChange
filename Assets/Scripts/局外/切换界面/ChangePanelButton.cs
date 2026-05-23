using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class ChangePanelButton : MonoBehaviour
{
    [SerializeField] private RectTransform panel;
    public void OnButtonClick()
    {
        // 切换面板的显示状态
        panel.gameObject.SetActive(true);
    }
}
