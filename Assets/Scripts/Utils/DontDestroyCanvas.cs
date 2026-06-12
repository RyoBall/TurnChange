using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DontDestroyCanvas : MonoBehaviour
{
    void Awake()
    {
        if(ScreenTransition.Instance != null&&ScreenTransition.Instance.OverlayCanvas.gameObject != this.gameObject)
        {
            Debug.LogWarning("[DontDestroyCanvas] 场景中已存在 ScreenTransition 的 OverlayCanvas，当前 GameObject 将被销毁以避免重复。", this);
            Destroy(gameObject);
        }
    }
}
