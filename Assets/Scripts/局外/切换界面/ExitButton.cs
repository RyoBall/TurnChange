using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitButton : MonoBehaviour
{
    [SerializeField] private RectTransform panel;
    public void OnButtonClick()
    {
        // 切换面板的显示状态
        panel.gameObject.SetActive(false);
    }
}
