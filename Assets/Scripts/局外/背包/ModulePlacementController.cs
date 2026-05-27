using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ModulePlacementController : MonoBehaviour
{
    [Header("基础引用")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private BackpackInventoryView inventoryView;
    [SerializeField] private ModulePlacementBoard placementBoard;
    [SerializeField] private TMP_Text selectionText;


    [Header("鼠标预览")]
    [SerializeField] private Vector2 cursorPreviewCellSize;
    [SerializeField] private int cursorPreviewScaleMaxDimension = 5;
    [SerializeField] private Vector2 cursorOffset = new Vector2(20f, -20f);
    [SerializeField] private Color validPlacementColor = new Color(0.35f, 0.95f, 0.45f, 0.72f);
    [SerializeField] private Color invalidPlacementColor = new Color(1f, 0.28f, 0.28f, 0.72f);

    private readonly List<GridModuleDefinition> m_runtimeModules = new List<GridModuleDefinition>();
    private readonly HashSet<GridModuleDefinition> m_loadedModules = new HashSet<GridModuleDefinition>();
    private readonly List<Vector2Int> m_shapeBuffer = new List<Vector2Int>();
    private readonly List<Image> m_cursorCells = new List<Image>();
    private readonly List<PlacedModuleData> m_savedPlacementBuffer = new List<PlacedModuleData>();
    private readonly List<ModulePlacementBoard.PlacedModuleState> m_placedModuleBuffer = new List<ModulePlacementBoard.PlacedModuleState>();

    private GridModuleDefinition m_selectedModule;
    private RectTransform m_cursorRoot;
    private Vector2Int? m_hoveredBoardCell;
    private Vector2 m_currentPreviewStep = Vector2.one;
    private Vector2 m_currentPreviewDrawSize = Vector2.one;
    private int m_selectionChangedFrame = -1;

    public int ModuleCount => m_runtimeModules.Count;

    private void Awake()
    {
        if (targetCanvas == null)
        {
            targetCanvas = GetComponentInParent<Canvas>();
        }

        if (inventoryView == null)
        {
            inventoryView = GetComponentInChildren<BackpackInventoryView>();
        }

        if (placementBoard == null)
        {
            placementBoard = GetComponentInChildren<ModulePlacementBoard>();
        }

        if (inventoryView != null)
        {
            inventoryView.ModuleClicked += HandleModuleClicked;
        }

        if (placementBoard != null)
        {
            placementBoard.InitializeBoard();
        }
        EnsureCursorRoot();
    }

    private void Start()
    {
        BuildRuntimeInventory();
        RestorePlacedModulesFromData();
        RefreshViews();
        SetSelection(null);
    }

    private void OnDestroy()
    {
        if (inventoryView != null)
        {
            inventoryView.ModuleClicked -= HandleModuleClicked;
        }

        if (placementBoard != null)
        {
        }
    }

    private void Update()
    {
        UpdateHoveredBoardCell();

        if (m_selectedModule != null)
        {
            UpdateCursorPreviewPosition();
        }

        if (Input.GetMouseButtonDown(0))
        {
            HandleLeftMouseClick();
        }

        if (m_selectedModule != null && (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)))
        {
            SetSelection(null);
        }
    }

    private void HandleModuleClicked(GridModuleDefinition module)
    {
        if (module == null || m_loadedModules.Contains(module))
        {
            return;
        }

        if (module == m_selectedModule)
        {
            SetSelection(null);
            return;
        }

        SetSelection(module);
    }

    private void HandleBoardCellClicked(Vector2Int cell)
    {
        if (placementBoard == null)
        {
            return;
        }

        if (m_selectedModule == null)
        {
            if (placementBoard.TryPickupModuleAt(cell, out GridModuleDefinition pickedModule))
            {
                m_loadedModules.Remove(pickedModule);
                SyncModuleStateToData();

                SetSelection(pickedModule);
                if (selectionText != null)
                {
                    selectionText.text = "已从网格取回：" + pickedModule.moduleName;
                }
            }

            return;
        }

        if (!placementBoard.TryPlace(m_selectedModule, cell))
        {
            if (selectionText != null)
            {
                selectionText.text = "该位置无法放置当前模块";
            }

            return;
        }

        m_loadedModules.Add(m_selectedModule);
        SyncModuleStateToData();
        SetSelection(null);
        RefreshViews();

        if (selectionText != null)
        {
            selectionText.text = "模块放置成功";
        }
    }

    private void SetSelection(GridModuleDefinition module)
    {
        m_selectedModule = module;
        if (m_selectedModule == null)
        {
            m_hoveredBoardCell = null;
        }

        m_selectionChangedFrame = Time.frameCount;
        RefreshViews();
        RefreshCursorPreview();

        if (selectionText == null)
        {
            return;
        }

        selectionText.text = m_selectedModule == null
            ? "点击背包中的模块后，再点击右侧 5x5 网格进行放置"
            : "已选中：" + m_selectedModule.moduleName + "，点击网格尝试放置";
    }

    private void UpdateHoveredBoardCell()
    {
        m_hoveredBoardCell = null;

        if (placementBoard == null || targetCanvas == null)
        {
            return;
        }

        Camera uiCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetCanvas.worldCamera;
        if (placementBoard.TryGetCellFromScreenPoint(Input.mousePosition, uiCamera, out Vector2Int hoveredCell))
        {
            m_hoveredBoardCell = hoveredCell;
        }
    }

    private void RefreshViews()//刷新所有相关界面显示
    {
        if (inventoryView != null)
        {
            inventoryView.Rebuild(m_runtimeModules, m_selectedModule, m_loadedModules);
        }
    }

    private void BuildRuntimeInventory()
    {
        m_runtimeModules.Clear();
        m_loadedModules.Clear();

        if (Datas.Instance != null && Datas.Instance.HasModuleState())
        {
            List<GridModuleDefinition> savedModules = Datas.Instance.CreateOwnedModuleDefinitions();
            for (int i = 0; i < savedModules.Count; i++)
            {
                if (savedModules[i] != null)
                {
                    m_runtimeModules.Add(savedModules[i]);
                }
            }

            return;
        }

        SyncModuleStateToData();
    }

    public void AddModuleToInventory(GridModuleDefinition module, bool autoSelect = false)
    {
        if (module == null)
        {
            return;
        }

        GridModuleDefinition runtimeModule = module.Clone();
        m_runtimeModules.Add(runtimeModule);
        SyncModuleStateToData();
        RefreshViews();

        if (selectionText != null)
        {
            selectionText.text = "已加入背包：" + runtimeModule.moduleName;
        }
    }

    private void RestorePlacedModulesFromData()
    {
        if (placementBoard == null || Datas.Instance == null || !Datas.Instance.HasModuleState())
        {
            return;
        }

        placementBoard.ClearBoard();
        m_loadedModules.Clear();
        Datas.Instance.GetPlacedModuleData(m_savedPlacementBuffer);

        for (int i = 0; i < m_savedPlacementBuffer.Count; i++)
        {
            PlacedModuleData placedModule = m_savedPlacementBuffer[i];
            if (placedModule == null)
            {
                continue;
            }

            int moduleIndex = placedModule.ModuleIndex;
            if (moduleIndex < 0 || moduleIndex >= m_runtimeModules.Count)
            {
                continue;
            }

            GridModuleDefinition module = m_runtimeModules[moduleIndex];
            if (module == null)
            {
                continue;
            }

            if (placementBoard.TryPlace(module, placedModule.AnchorCell))
            {
                m_loadedModules.Add(module);
            }
        }

        SyncModuleStateToData();
    }

    private void SyncModuleStateToData()
    {
        if (Datas.Instance == null)
        {
            return;
        }

        m_savedPlacementBuffer.Clear();
        if (placementBoard != null)
        {
            placementBoard.GetPlacedModules(m_placedModuleBuffer);
            for (int i = 0; i < m_placedModuleBuffer.Count; i++)
            {
                ModulePlacementBoard.PlacedModuleState placedModule = m_placedModuleBuffer[i];
                int moduleIndex = m_runtimeModules.IndexOf(placedModule.module);
                if (moduleIndex >= 0)
                {
                    m_savedPlacementBuffer.Add(new PlacedModuleData(moduleIndex, placedModule.anchorCell));
                }
            }
        }

        Datas.Instance.SaveModuleState(m_runtimeModules, m_savedPlacementBuffer);
    }

    private void EnsureCursorRoot()
    {
        if (targetCanvas == null)
        {
            return;
        }

        Transform existing = targetCanvas.transform.Find("ModuleCursorPreview");
        if (existing != null)
        {
            m_cursorRoot = existing as RectTransform;
        }

        if (m_cursorRoot == null)
        {
            GameObject previewObject = new GameObject("ModuleCursorPreview", typeof(RectTransform), typeof(CanvasGroup));
            previewObject.transform.SetParent(targetCanvas.transform, false);
            m_cursorRoot = previewObject.GetComponent<RectTransform>();
        }

        m_cursorRoot.anchorMin = new Vector2(0.5f, 0.5f);
        m_cursorRoot.anchorMax = new Vector2(0.5f, 0.5f);
        m_cursorRoot.pivot = new Vector2(0.5f, 0.5f);
        m_cursorRoot.gameObject.SetActive(false);

        CanvasGroup canvasGroup = m_cursorRoot.GetComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        canvasGroup.alpha = 1f;
    }

    private void RefreshCursorPreview()
    {
        if (m_cursorRoot == null)
        {
            return;
        }

        for (int i = 0; i < m_cursorCells.Count; i++)
        {
            if (m_cursorCells[i] != null)   
            {
                Destroy(m_cursorCells[i].gameObject);
            }
        }

        m_cursorCells.Clear();

        if (m_selectedModule == null)
        {
            m_cursorRoot.gameObject.SetActive(false);
            return;
        }

        m_selectedModule.GetNormalizedCells(m_shapeBuffer);
        ApplyCursorPreviewLayout(false);

        for (int i = 0; i < m_shapeBuffer.Count; i++)
        {
            GameObject cellObject = new GameObject("PreviewCell", typeof(RectTransform), typeof(Image));
            cellObject.transform.SetParent(m_cursorRoot, false);

            Image image = cellObject.GetComponent<Image>();
            image.color = GetDefaultPreviewColor();
            image.raycastTarget = false;
            m_cursorCells.Add(image);
        }

        ApplyCursorPreviewLayout(false);
        m_cursorRoot.gameObject.SetActive(true);
        UpdateCursorPreviewPosition();
    }

    private void UpdateCursorPreviewPosition()
    {
        if (targetCanvas == null || m_cursorRoot == null || m_selectedModule == null)
        {
            return;
        }

        RectTransform canvasRect = targetCanvas.transform as RectTransform;
        Camera uiCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetCanvas.worldCamera;
        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, Input.mousePosition, uiCamera, out localPoint))
        {
            return;
        }

        bool canPlace = false;
        bool isHoveringBoard = false;

        if (m_hoveredBoardCell.HasValue)
        {
            isHoveringBoard = true;
            Vector2Int hoveredCell = m_hoveredBoardCell.Value;
            canPlace = placementBoard != null && placementBoard.CanPlace(m_selectedModule, hoveredCell);
            ApplyCursorPreviewLayout(true);

            if (placementBoard != null && placementBoard.TryGetCellCenterInRect(canvasRect, hoveredCell, out Vector2 snappedPoint))
            {
                Vector2 moduleCenter = m_selectedModule.GetNormalizedCenter();
                localPoint = snappedPoint + new Vector2(moduleCenter.x * m_currentPreviewStep.x, -moduleCenter.y * m_currentPreviewStep.y);
            }
        }
        else
        {
            ApplyCursorPreviewLayout(false);
        }

        m_cursorRoot.anchoredPosition = localPoint + (isHoveringBoard ? Vector2.zero : cursorOffset);
        UpdateCursorPreviewColor(canPlace, m_hoveredBoardCell.HasValue);
    }

    private void ApplyCursorPreviewLayout(bool alignToBoard)
    {
        if (m_cursorRoot == null || m_selectedModule == null)
        {
            return;
        }

        m_selectedModule.GetNormalizedCells(m_shapeBuffer);
        Vector2Int size = m_selectedModule.GetSize();
        Vector2 moduleCenter = m_selectedModule.GetNormalizedCenter();

        if (alignToBoard && placementBoard != null)
        {
            float boardCellSize = Mathf.Max(1f, placementBoard.GetCellSize());
            float boardStride = Mathf.Max(boardCellSize, placementBoard.GetCellStride());
            m_currentPreviewDrawSize = new Vector2(Mathf.Max(1f, boardCellSize - 2f), Mathf.Max(1f, boardCellSize - 2f));
            m_currentPreviewStep = new Vector2(boardStride, boardStride);
        }
        else
        {
            Vector2 previewCellSize = GetFloatingPreviewCellSize();
            m_currentPreviewDrawSize = new Vector2(Mathf.Max(1f, previewCellSize.x - 2f), Mathf.Max(1f, previewCellSize.y - 2f));
            m_currentPreviewStep = previewCellSize;
        }

        float rootWidth = Mathf.Max(m_currentPreviewDrawSize.x, (size.x - 1) * m_currentPreviewStep.x + m_currentPreviewDrawSize.x);
        float rootHeight = Mathf.Max(m_currentPreviewDrawSize.y, (size.y - 1) * m_currentPreviewStep.y + m_currentPreviewDrawSize.y);
        m_cursorRoot.sizeDelta = new Vector2(rootWidth, rootHeight);

        for (int i = 0; i < m_cursorCells.Count && i < m_shapeBuffer.Count; i++)
        {
            RectTransform cellRect = m_cursorCells[i].rectTransform;
            Vector2Int cell = m_shapeBuffer[i];
            cellRect.anchorMin = new Vector2(0.5f, 0.5f);
            cellRect.anchorMax = new Vector2(0.5f, 0.5f);
            cellRect.pivot = new Vector2(0.5f, 0.5f);
            cellRect.sizeDelta = m_currentPreviewDrawSize;
            cellRect.anchoredPosition = new Vector2(
                (cell.x - moduleCenter.x) * m_currentPreviewStep.x,
                -(cell.y - moduleCenter.y) * m_currentPreviewStep.y);
        }
    }

    private Color GetDefaultPreviewColor()
    {
        Color previewColor = m_selectedModule != null ? m_selectedModule.color : Color.white;
        previewColor.a = 0.65f;
        return previewColor;
    }

    private void UpdateCursorPreviewColor(bool canPlace, bool isHoveringBoard)
    {
        Color targetColor = isHoveringBoard
            ? (canPlace ? validPlacementColor : invalidPlacementColor)
            : GetDefaultPreviewColor();

        for (int i = 0; i < m_cursorCells.Count; i++)
        {
            if (m_cursorCells[i] != null)
            {
                m_cursorCells[i].color = targetColor;
            }
        }
    }

    private void HandleLeftMouseClick()
    {
        if (m_selectionChangedFrame == Time.frameCount)
        {
            return;
        }

        if (m_hoveredBoardCell.HasValue)
        {
            HandleBoardCellClicked(m_hoveredBoardCell.Value);
            return;
        }

        if (m_selectedModule == null)
        {
            return;
        }

        if (selectionText != null)
        {
            selectionText.text = "模块已返回背包";
        }

        SetSelection(null);
    }

    private Vector2 GetFloatingPreviewCellSize()
    {
        float baseWidth = cursorPreviewCellSize.x > 1f
            ? cursorPreviewCellSize.x
            : (placementBoard != null ? placementBoard.GetCellSize() * 0.9f : 28f);
        float baseHeight = cursorPreviewCellSize.y > 1f
            ? cursorPreviewCellSize.y
            : (placementBoard != null ? placementBoard.GetCellSize() * 0.9f : 28f);

        int maxDimension = m_selectedModule == null ? 1 : Mathf.Max(1, m_selectedModule.GetMaxDimension());
        float t = cursorPreviewScaleMaxDimension <= 1 ? 1f : Mathf.InverseLerp(1f, cursorPreviewScaleMaxDimension, maxDimension);
        float scale = Mathf.Lerp(1.15f, 0.7f, t);
        return new Vector2(Mathf.Max(1f, baseWidth * scale), Mathf.Max(1f, baseHeight * scale));
    }
}