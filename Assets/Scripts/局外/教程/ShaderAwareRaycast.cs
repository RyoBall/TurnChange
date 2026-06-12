using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShaderAwareRaycast : MonoBehaviour, ICanvasRaycastFilter
{
    public Material guideMaterial;  // 你的 RectGuideMask 材质
    public RectTransform highlightRect;
    
    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        // 获取点击位置的屏幕UV
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            GetComponent<RectTransform>(), screenPoint, eventCamera, out localPoint);
        
        Rect rect = GetComponent<RectTransform>().rect;
        Vector2 uv = new Vector2(
            (localPoint.x - rect.x) / rect.width,
            (localPoint.y - rect.y) / rect.height
        );
        
        // 从材质的当前参数判断是否在高亮区域内
        float minX = guideMaterial.GetFloat("_RectMinX");
        float maxX = guideMaterial.GetFloat("_RectMaxX");
        float minY = guideMaterial.GetFloat("_RectMinY");
        float maxY = guideMaterial.GetFloat("_RectMaxY");
        
        bool insideHighlight = (uv.x >= minX && uv.x <= maxX && 
                                uv.y >= minY && uv.y <= maxY);
        
        // 高亮区域内：穿透遮罩（返回false）
        // 遮罩区域：阻挡射线（返回true）
        return !insideHighlight;
    }
}