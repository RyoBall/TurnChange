using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 模块单元格工厂 — 统一创建模块形状的单元格 Image，并可选应用 ModuleGradient 渐变 shader。
/// 所有需要渲染模块形状的位置（背包预览、商店预览、网格放置面板、光标跟随预览）都应通过此工厂创建单元格。
/// </summary>
public static class ModuleCellFactory
{
    /// <summary>
    /// 创建一个模块单元格 GameObject（带 RectTransform + Image），挂载到指定父节点下。
    /// 如果 config 中配置了 cellPrefab，则 Instantiate 该 prefab；否则动态创建。
    /// 如果 config 中配置了 gradientShader，则为该单元格创建独立的 Material 实例并应用渐变参数。
    /// 返回创建出的 Material（如果使用了渐变 shader），调用方需要在合适时机 Destroy 该 Material。
    /// </summary>
    public static GameObject CreateCell(
        RectTransform parent,
        string cellName,
        Vector2 cellSize,
        Vector2 anchoredPosition,
        Color moduleColor,
        float cellAlpha,
        ModuleCellConfig config,
        Color gradientColorB,
        Vector2 cellOffset,
        Vector2 boundsMin,
        Vector2 boundsMax,
        out Material createdMaterial)
    {
        createdMaterial = null;

        GameObject cellObject;
        if (config != null && config.CellPrefab != null)
        {
            cellObject = Object.Instantiate(config.CellPrefab, parent);
            cellObject.name = cellName;
        }
        else
        {
            cellObject = new GameObject(cellName, typeof(RectTransform), typeof(Image));
            cellObject.transform.SetParent(parent, false);
        }

        RectTransform cellRect = cellObject.GetComponent<RectTransform>();
        if (cellRect == null)
        {
            cellRect = cellObject.AddComponent<RectTransform>();
        }

        cellRect.anchorMin = new Vector2(0.5f, 0.5f);
        cellRect.anchorMax = new Vector2(0.5f, 0.5f);
        cellRect.pivot = new Vector2(0.5f, 0.5f);
        cellRect.sizeDelta = cellSize;
        cellRect.anchoredPosition = anchoredPosition;

        Image cellImage = cellObject.GetComponent<Image>();
        if (cellImage == null)
        {
            cellImage = cellObject.AddComponent<Image>();
        }

        cellImage.raycastTarget = false;

        Shader gradientShader = config != null ? config.GradientShader : null;
        bool useGradient = gradientShader != null;

        if (useGradient)
        {
            // 白色顶点色 — 渐变颜色完全由 shader 控制
            cellImage.color = new Color(1f, 1f, 1f, cellAlpha);

            Material mat = new Material(gradientShader);
            Color colorB = gradientColorB;
            colorB.a = moduleColor.a * cellAlpha;
            Color colorA = moduleColor;
            colorA.a *= cellAlpha;
            mat.SetColor("_ColorA", colorA);
            mat.SetColor("_ColorB", colorB);
            mat.SetFloat("_GradientAngle", config.GradientAngle);
            mat.SetVector("_CellOffset", new Vector4(cellOffset.x, cellOffset.y, 0f, 0f));
            mat.SetVector("_CellSize", new Vector4(cellSize.x, cellSize.y, 0f, 0f));
            mat.SetVector("_BoundsMin", new Vector4(boundsMin.x, boundsMin.y, 0f, 0f));
            mat.SetVector("_BoundsMax", new Vector4(boundsMax.x, boundsMax.y, 0f, 0f));
            // 确保 _MainTex 有默认纹理，防止 Image.sprite 为 null 时 shader 采样到空纹理导致透明
            mat.SetTexture("_MainTex", Texture2D.whiteTexture);
            // 禁用 RectMask2D 裁剪（cell 的可见范围由父 RectTransform 保证，无需 shader 级裁剪；
            // 且自定义 shader 中 _ClipRect 的坐标系可能与 RectMask2D 不兼容导致 cell 被错误裁掉）
            mat.SetVector("_ClipRect", new Vector4(-float.MaxValue, -float.MaxValue, float.MaxValue, float.MaxValue));
            cellImage.material = mat;
            createdMaterial = mat;
        }
        else
        {
            Color cellColor = moduleColor;
            cellColor.a *= cellAlpha;
            cellImage.color = cellColor;
        }

        return cellObject;
    }

    /// <summary>
    /// 计算模块归一化形状在 shapeRoot 局部空间中的包围盒（像素坐标）。
    /// </summary>
    public static void ComputeShapeBounds(
        IReadOnlyList<Vector2Int> normalizedCells,
        Vector2 moduleCenter,
        Vector2 cellStep,      // 单元格步长（含间距），用于计算 anchoredPosition
        Vector2 cellDrawSize,  // 单元格实际绘制大小
        out Vector2 boundsMin,
        out Vector2 boundsMax)
    {
        float halfW = cellDrawSize.x * 0.5f;
        float halfH = cellDrawSize.y * 0.5f;
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        for (int i = 0; i < normalizedCells.Count; i++)
        {
            Vector2Int cell = normalizedCells[i];
            float cx = (cell.x - moduleCenter.x) * cellStep.x;
            float cy = -(cell.y - moduleCenter.y) * cellStep.y;
            minX = Mathf.Min(minX, cx - halfW);
            minY = Mathf.Min(minY, cy - halfH);
            maxX = Mathf.Max(maxX, cx + halfW);
            maxY = Mathf.Max(maxY, cy + halfH);
        }

        boundsMin = new Vector2(minX, minY);
        boundsMax = new Vector2(maxX, maxY);
    }

    /// <summary>
    /// 计算单个 cell 在 shapeRoot 局部空间中的 anchoredPosition 和 shader 用的 cellOffset。
    /// anchoredPosition 使用 cellStep（含间距），cellOffset 使用 cellDrawSize（实际绘制大小）。
    /// </summary>
    public static void ComputeCellPosition(
        Vector2Int cell,
        Vector2 moduleCenter,
        Vector2 cellStep,
        out Vector2 anchoredPosition,
        out Vector2 cellOffset)
    {
        float ax = (cell.x - moduleCenter.x) * cellStep.x;
        float ay = -(cell.y - moduleCenter.y) * cellStep.y;
        anchoredPosition = new Vector2(ax, ay);

        // cellOffset 用于 shader 中的像素级位置计算，使用 cellStep 保持一致
        cellOffset = new Vector2(ax, ay);
    }
}
