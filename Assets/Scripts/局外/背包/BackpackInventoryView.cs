using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class BackpackInventoryView : MonoBehaviour, IBackpackInventoryView//背包列表
{
    private const float PaginationBarHeight = 40f;

    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private GameObject moduleItemPrefab;
    [SerializeField] private Vector2 backpackSize = new Vector2(480f, 360f);
    [SerializeField] private int modulesPerRow = 3;
    [SerializeField] private int modulesPerColumn = 3;
    [SerializeField] private Vector2 itemSize = new Vector2(150f, 112f);
    [SerializeField] private Vector2 shapePreviewMaxCellSize = new Vector2(22f, 22f);
    [SerializeField] private Vector2 shapePreviewMinCellSize = new Vector2(12f, 12f);
    [SerializeField] private int previewScaleMaxDimension = 5;
    [SerializeField] private Button previousPageButton;
    [SerializeField] private Button nextPageButton;
    [SerializeField] private TMP_Text pageIndicatorText;

    private readonly List<BackpackModuleItemUI> m_items = new List<BackpackModuleItemUI>();
    private readonly List<IGridModule> m_sortedModulesBuffer = new List<IGridModule>();

    private RectTransform m_paginationRoot;
    private int m_currentPageIndex;
    private IGridModule m_selectedModule;
    private bool m_layoutPrepared;

    public event Action<IGridModule> ModulePressed;
    public event Action<IGridModule> ModuleHovered;
    public event Action ModuleHoverExited;

    private int ModulesPerPage => Mathf.Max(1, modulesPerRow * modulesPerColumn);

    private void Awake()
    {
        EnsureLayoutInfrastructure();
        EnsureLayout();
    }

    private void OnDestroy()
    {
        UnbindPaginationButtons();
    }

    private void OnRectTransformDimensionsChange()
    {
        EnsureLayout();
    }

    public void Rebuild(IReadOnlyList<IGridModule> modules, IGridModule selectedModule)
    {
        EnsureLayoutInfrastructure();
        EnsureLayout();

        m_selectedModule = selectedModule;
        BuildSortedModuleList(modules, m_sortedModulesBuffer);

        int totalPages = GetTotalPageCount(m_sortedModulesBuffer.Count);
        m_currentPageIndex = Mathf.Clamp(m_currentPageIndex, 0, Mathf.Max(0, totalPages - 1));

        RebuildCurrentPageItems();
        RefreshPaginationControls(totalPages);
    }

    private void HandleModulePressed(IGridModule module)
    {
        ModulePressed?.Invoke(module);
    }

    private void HandleModuleHovered(IGridModule module)
    {
        ModuleHovered?.Invoke(module);
    }

    private void HandleModuleHoverExited()
    {
        ModuleHoverExited?.Invoke();
    }

    private void HandlePreviousPageClicked()
    {
        if (m_currentPageIndex <= 0)
        {
            return;
        }

        m_currentPageIndex--;
        RebuildCurrentPageItems();
        RefreshPaginationControls(GetTotalPageCount(m_sortedModulesBuffer.Count));
    }

    private void HandleNextPageClicked()
    {
        int totalPages = GetTotalPageCount(m_sortedModulesBuffer.Count);
        if (m_currentPageIndex >= totalPages - 1)
        {
            return;
        }

        m_currentPageIndex++;
        RebuildCurrentPageItems();
        RefreshPaginationControls(totalPages);
    }

    private void RebuildCurrentPageItems()
    {
        ClearPageItems();

        if (moduleItemPrefab == null)
        {
            Debug.LogError("BackpackInventoryView is missing moduleItemPrefab.", this);
            return;
        }

        int startIndex = m_currentPageIndex * ModulesPerPage;
        int endIndex = Mathf.Min(startIndex + ModulesPerPage, m_sortedModulesBuffer.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            IGridModule module = m_sortedModulesBuffer[i];
            if (module == null)
            {
                continue;
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
            item.Bind(module, module == m_selectedModule, isLoaded, GetPreviewCellSize(module), HandleModulePressed, HandleModuleHovered, HandleModuleHoverExited);
            m_items.Add(item);
        }
    }

    private void ClearPageItems()
    {
        for (int i = 0; i < m_items.Count; i++)
        {
            if (m_items[i] != null)
            {
                Destroy(m_items[i].gameObject);
            }
        }

        m_items.Clear();
    }

    private void RefreshPaginationControls(int totalPages)
    {
        if (previousPageButton != null)
        {
            previousPageButton.interactable = m_currentPageIndex > 0;
        }

        if (nextPageButton != null)
        {
            nextPageButton.interactable = totalPages > 0 && m_currentPageIndex < totalPages - 1;
        }

        if (pageIndicatorText != null)
        {
            pageIndicatorText.text = totalPages <= 0
                ? "0 / 0"
                : (m_currentPageIndex + 1) + " / " + totalPages;
        }
    }

    private int GetTotalPageCount(int moduleCount)
    {
        if (moduleCount <= 0)
        {
            return 0;
        }

        return Mathf.CeilToInt(moduleCount / (float)ModulesPerPage);
    }

    private static void BuildSortedModuleList(IReadOnlyList<IGridModule> modules, List<IGridModule> results)
    {
        results.Clear();

        if (modules == null)
        {
            return;
        }

        for (int i = 0; i < modules.Count; i++)
        {
            if (modules[i] != null)
            {
                results.Add(modules[i]);
            }
        }

        results.Sort(CompareModulesForDisplay);
    }

    private static int CompareModulesForDisplay(IGridModule left, IGridModule right)
    {
        bool leftLoaded = left != null && left.IsLoaded;
        bool rightLoaded = right != null && right.IsLoaded;
        if (leftLoaded != rightLoaded)
        {
            return leftLoaded ? -1 : 1;
        }

        int defaultOrderCompare = CompareModuleDefaultOrder(left as GridModuleDefinition, right as GridModuleDefinition);
        if (defaultOrderCompare != 0)
        {
            return defaultOrderCompare;
        }

        GridModuleDefinition leftDef = left as GridModuleDefinition;
        GridModuleDefinition rightDef = right as GridModuleDefinition;
        return string.Compare(leftDef != null ? leftDef.moduleName : string.Empty, rightDef != null ? rightDef.moduleName : string.Empty, StringComparison.Ordinal);
    }

    private static int CompareModuleDefaultOrder(GridModuleDefinition left, GridModuleDefinition right)
    {
        if (left == null && right == null)
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        int typeCompare = left.moduleType.CompareTo(right.moduleType);
        if (typeCompare != 0)
        {
            return typeCompare;
        }

        int levelCompare = left.level.CompareTo(right.level);
        if (levelCompare != 0)
        {
            return levelCompare;
        }

        return string.Compare(left.moduleName, right.moduleName, StringComparison.Ordinal);
    }

    private void EnsureLayoutInfrastructure()
    {
        if (m_layoutPrepared)
        {
            return;
        }

        m_layoutPrepared = true;
        MigrateContentRootIfNeeded();
        EnsurePaginationControls();
    }

    private void MigrateContentRootIfNeeded()
    {
        RectTransform selfRect = transform as RectTransform;
        if (contentRoot == null)
        {
            contentRoot = selfRect;
        }

        if (contentRoot != selfRect)
        {
            ApplyGridContainerLayout(contentRoot);
            return;
        }

        Transform existingGrid = transform.Find("GridContainer");
        if (existingGrid != null)
        {
            contentRoot = existingGrid as RectTransform;
            ApplyGridContainerLayout(contentRoot);
            return;
        }

        GameObject gridObject = new GameObject("GridContainer", typeof(RectTransform));
        gridObject.transform.SetParent(transform, false);
        contentRoot = gridObject.GetComponent<RectTransform>();
        ApplyGridContainerLayout(contentRoot);

        GridLayoutGroup parentGrid = GetComponent<GridLayoutGroup>();
        if (parentGrid != null)
        {
            Destroy(parentGrid);
        }

        RectMask2D parentMask = GetComponent<RectMask2D>();
        if (parentMask != null)
        {
            Destroy(parentMask);
        }
    }

    private void ApplyGridContainerLayout(RectTransform gridRoot)
    {
        if (gridRoot == null)
        {
            return;
        }

        gridRoot.anchorMin = Vector2.zero;
        gridRoot.anchorMax = Vector2.one;
        gridRoot.pivot = new Vector2(0.5f, 0.5f);
        gridRoot.offsetMin = new Vector2(0f, PaginationBarHeight);
        gridRoot.offsetMax = Vector2.zero;
    }

    private void EnsurePaginationControls()
    {
        RectTransform selfRect = transform as RectTransform;

        if (m_paginationRoot == null)
        {
            Transform existingPagination = transform.Find("PaginationBar");
            m_paginationRoot = existingPagination as RectTransform;
        }

        if (m_paginationRoot == null)
        {
            GameObject paginationObject = new GameObject("PaginationBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            paginationObject.transform.SetParent(transform, false);
            m_paginationRoot = paginationObject.GetComponent<RectTransform>();

            m_paginationRoot.anchorMin = new Vector2(0f, 0f);
            m_paginationRoot.anchorMax = new Vector2(1f, 0f);
            m_paginationRoot.pivot = new Vector2(0.5f, 0f);
            m_paginationRoot.anchoredPosition = Vector2.zero;
            m_paginationRoot.sizeDelta = new Vector2(0f, PaginationBarHeight);

            HorizontalLayoutGroup layoutGroup = paginationObject.GetComponent<HorizontalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.spacing = 12f;
            layoutGroup.padding = new RectOffset(8, 8, 4, 4);
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = true;
        }

        if (previousPageButton == null)
        {
            previousPageButton = CreatePaginationButton(m_paginationRoot, "PreviousPageButton", "◀");
        }

        if (pageIndicatorText == null)
        {
            pageIndicatorText = CreatePageIndicator(m_paginationRoot);
        }

        if (nextPageButton == null)
        {
            nextPageButton = CreatePaginationButton(m_paginationRoot, "NextPageButton", "▶");
        }

        UnbindPaginationButtons();
        previousPageButton.onClick.AddListener(HandlePreviousPageClicked);
        nextPageButton.onClick.AddListener(HandleNextPageClicked);
    }

    private void UnbindPaginationButtons()
    {
        if (previousPageButton != null)
        {
            previousPageButton.onClick.RemoveListener(HandlePreviousPageClicked);
        }

        if (nextPageButton != null)
        {
            nextPageButton.onClick.RemoveListener(HandleNextPageClicked);
        }
    }

    private static Button CreatePaginationButton(Transform parent, string objectName, string label)
    {
        GameObject buttonObject = DefaultControls.CreateButton(new DefaultControls.Resources());
        buttonObject.name = objectName;
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(44f, 32f);

        Text legacyLabel = buttonObject.GetComponentInChildren<Text>();
        if (legacyLabel != null)
        {
            legacyLabel.text = label;
            legacyLabel.fontSize = 20;
            legacyLabel.alignment = TextAnchor.MiddleCenter;
        }

        return buttonObject.GetComponent<Button>();
    }

    private static TMP_Text CreatePageIndicator(Transform parent)
    {
        GameObject textObject = new GameObject("PageIndicator", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(72f, 32f);

        TMP_Text pageText = textObject.GetComponent<TextMeshProUGUI>();
        pageText.fontSize = 18f;
        pageText.alignment = TextAlignmentOptions.Center;
        pageText.color = Color.white;
        pageText.text = "1 / 1";
        return pageText;
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

        RectTransform selfRect = transform as RectTransform;
        if (selfRect != null)
        {
            selfRect.sizeDelta = backpackSize;
        }

        if (contentRoot == null)
        {
            contentRoot = selfRect;
        }

        GridLayoutGroup gridLayout = contentRoot.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = contentRoot.gameObject.AddComponent<GridLayoutGroup>();
        }

        Vector2 gridAreaSize = GetGridAreaSize();
        Vector2 computedSpacing = GetComputedSpacing(gridAreaSize);
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

    private Vector2 GetGridAreaSize()
    {
        return new Vector2(backpackSize.x, Mathf.Max(1f, backpackSize.y - PaginationBarHeight));
    }

    private Vector2 GetComputedSpacing(Vector2 gridAreaSize)
    {
        float horizontalSpacing = modulesPerRow <= 1
            ? 0f
            : (gridAreaSize.x - modulesPerRow * itemSize.x) / (modulesPerRow - 1);
        float verticalSpacing = modulesPerColumn <= 1
            ? 0f
            : (gridAreaSize.y - modulesPerColumn * itemSize.y) / (modulesPerColumn - 1);

        return new Vector2(horizontalSpacing, verticalSpacing);
    }

    private Vector2 GetPreviewCellSize(IGridModule module)
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
