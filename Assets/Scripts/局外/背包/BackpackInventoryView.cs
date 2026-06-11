using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class BackpackInventoryView : MonoBehaviour//背包列表
{
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private GameObject moduleItemPrefab;
    [SerializeField] private Vector2 backpackSize = new Vector2(480f, 360f);
    [SerializeField] private int modulesPerRow = 3;
    [SerializeField] private int modulesPerColumn = 3;
    [SerializeField] private Vector2 itemSize = new Vector2(150f, 112f);
    [SerializeField] private Vector2 shapePreviewMaxCellSize = new Vector2(22f, 22f);
    [SerializeField] private Vector2 shapePreviewMinCellSize = new Vector2(12f, 12f);
    [SerializeField] private int previewScaleMaxDimension = 5;

    private readonly List<BackpackModuleItemUI> m_items = new List<BackpackModuleItemUI>();

    public event Action<GridModuleDefinition> ModulePressed;
    public event Action<GridModuleDefinition> ModuleHovered;
    public event Action ModuleHoverExited;

    private void Awake()
    {
        EnsureLayout();
    }

    private void OnRectTransformDimensionsChange()
    {
        EnsureLayout();
    }

    public void Rebuild(IReadOnlyList<GridModuleDefinition> modules, GridModuleDefinition selectedModule)
    {
        EnsureLayout();

        for (int i = 0; i < m_items.Count; i++)
        {
            if (m_items[i] != null)
            {
                Destroy(m_items[i].gameObject);
            }
        }

        m_items.Clear();

        if (modules == null)
        {
            return;
        }

        for (int i = 0; i < modules.Count; i++)
        {
            GridModuleDefinition module = modules[i];
            if (module == null)
            {
                continue;
            }

            if (moduleItemPrefab == null)
            {
                Debug.LogError("BackpackInventoryView is missing moduleItemPrefab.", this);
                return;
            }

            GameObject itemObject = Instantiate(moduleItemPrefab, contentRoot);
            itemObject.name = "ModuleItem_" + i;
            BackpackModuleItemUI item = itemObject.GetComponent<BackpackModuleItemUI>();

            LayoutElement layoutElement = item.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = item.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.preferredWidth = itemSize.x;
            layoutElement.preferredHeight = itemSize.y;
            layoutElement.minWidth = itemSize.x;
            layoutElement.minHeight = itemSize.y;

            bool isLoaded = module.IsLoaded;
            item.Bind(module, module == selectedModule, isLoaded, GetPreviewCellSize(module), HandleModulePressed, HandleModuleHovered, HandleModuleHoverExited);
            m_items.Add(item);
        }
    }

    private void HandleModulePressed(GridModuleDefinition module)
    {
        ModulePressed?.Invoke(module);
    }

    private void HandleModuleHovered(GridModuleDefinition module)
    {
        ModuleHovered?.Invoke(module);
    }

    private void HandleModuleHoverExited()
    {
        ModuleHoverExited?.Invoke();
    }

    private void EnsureLayout()
    {
        backpackSize.x = Mathf.Max(1f, backpackSize.x);
        backpackSize.y = Mathf.Max(1f, backpackSize.y);
        modulesPerRow = Mathf.Max(1, modulesPerRow);
        modulesPerColumn = Mathf.Max(1, modulesPerColumn);
        itemSize.x = Mathf.Max(1f, itemSize.x);
        itemSize.y = Mathf.Max(1f, itemSize.y);
        shapePreviewMaxCellSize.x = Mathf.Max(1f, shapePreviewMaxCellSize.x);
        shapePreviewMaxCellSize.y = Mathf.Max(1f, shapePreviewMaxCellSize.y);
        shapePreviewMinCellSize.x = Mathf.Max(1f, shapePreviewMinCellSize.x);
        shapePreviewMinCellSize.y = Mathf.Max(1f, shapePreviewMinCellSize.y);
        previewScaleMaxDimension = Mathf.Max(1, previewScaleMaxDimension);

        if (contentRoot == null)
        {
            contentRoot = transform as RectTransform;
        }

        contentRoot.sizeDelta = backpackSize;

        GridLayoutGroup gridLayout = contentRoot.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = contentRoot.gameObject.AddComponent<GridLayoutGroup>();
        }

        Vector2 computedSpacing = GetComputedSpacing();
        gridLayout.cellSize = itemSize;
        gridLayout.spacing = computedSpacing;
        gridLayout.padding = new RectOffset(0, 0, 0, 0);
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.childAlignment = TextAnchor.UpperLeft;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = modulesPerRow;

        ContentSizeFitter fitter = contentRoot.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            DestroyImmediate(fitter);
        }

        RectMask2D rectMask = contentRoot.GetComponent<RectMask2D>();
        if (rectMask == null)
        {
            rectMask = contentRoot.gameObject.AddComponent<RectMask2D>();
        }
    }

    private Vector2 GetComputedSpacing()
    {
        float horizontalSpacing = modulesPerRow <= 1
            ? 0f
            : (backpackSize.x - modulesPerRow * itemSize.x) / (modulesPerRow - 1);
        float verticalSpacing = modulesPerColumn <= 1
            ? 0f
            : (backpackSize.y - modulesPerColumn * itemSize.y) / (modulesPerColumn - 1);

        return new Vector2(horizontalSpacing, verticalSpacing);
    }

    private Vector2 GetPreviewCellSize(GridModuleDefinition module)
    {
        if (module == null)
        {
            return shapePreviewMaxCellSize;
        }

        int maxDimension = Mathf.Max(1, module.GetMaxDimension());
        float t = previewScaleMaxDimension <= 1 ? 1f : Mathf.InverseLerp(1f, previewScaleMaxDimension, maxDimension);
        return Vector2.Lerp(shapePreviewMaxCellSize, shapePreviewMinCellSize, t);
    }
}