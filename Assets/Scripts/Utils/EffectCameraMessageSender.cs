using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class EffectCameraMessageSender : MonoBehaviour
{
    public Camera mainCamera;  // 拖拽主摄像机，或自动获取
    
    private GameObject lastHitObject;  // 上一帧击中的物体
    
    void Reset()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }
    
    void Update()
    {
        if (mainCamera == null) return;
        
        // 从主摄像机发射射线
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        GameObject currentHitObject = null;
        
        if (Physics.Raycast(ray, out hit))
        {
            currentHitObject = hit.collider.gameObject;
        }
        
        // 处理 Enter / Exit / Over
        if (currentHitObject != lastHitObject)
        {
            // 离开上一个物体
            if (lastHitObject != null)
            {
                lastHitObject.SendMessage("OnMouseExit", SendMessageOptions.DontRequireReceiver);
            }
            
            // 进入新物体
            if (currentHitObject != null)
            {
                currentHitObject.SendMessage("OnMouseEnter", SendMessageOptions.DontRequireReceiver);
            }
        }
        else if (currentHitObject != null)
        {
            // 停留在同一物体上
            currentHitObject.SendMessage("OnMouseOver", SendMessageOptions.DontRequireReceiver);
        }
        if(currentHitObject != null&&Input.GetMouseButtonDown(0))
        {
            currentHitObject.SendMessage("OnMouseDown", SendMessageOptions.DontRequireReceiver);
        }
        
        lastHitObject = currentHitObject;
    }
}
