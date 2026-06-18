using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ModulePlacementController : MonoBehaviour, IModulePlacementController//背包
{
    public static ModulePlacementController Instance { get; private set; }
    [Header("基础引用")]
    [SerializeField] private RectTransform moduleRoot;
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

    private readonly List<Vector2Int> m_shapeBuffer = new List<Vector2Int>();
    private readonly List<Image> m_cursorCells = new List<Image>();
    private readonly List<Material> m_cursorMaterials = new List<Material>();
    private readonly List<PlacedModuleData> m_placedDataBuffer = new List<PlacedModuleData>();
    private readonly List<IGridModule> m_runtimeOwnedModules = new List<IGridModule>();

    private GridModuleDefinition m_selectedModule;
    private RectTransform m_cursorRoot;
    private Vector2Int? m_hoveredBoardCell;
    private Vector2 m_currentPreviewStep = Vector2.one;
    private Vector2 m_currentPreviewDrawSize = Vector2.one;
    private int m_selectionChangedFrame = -1;
    private int m_rotationCount;
    private bool m_runtimeModulesPrepared;

    public int ModuleCount => GetOwnedModules().Count;
#region 生命周期
    private void Awake()
    {
        if(Instance!=null&&Instance!=this)
        {
            Destroy(this.gameObject);
            return;
        }
        else if(Instance==null)
        {
            Instance = this;
        }
        
        if (inventoryView != null)
        {
            inventoryView.ModulePressed += HandleModulePressed;
            inventoryView.ModuleHovered += HandleInventoryModuleHovered;
            inventoryView.ModuleHoverExited += HandleModuleHoverExited;
        }

        if (placementBoard != null)
        {
            placementBoard.BuildBoard();
            placementBoard.ModuleHovered += HandleBoardModuleHovered;
            placementBoard.ModuleHoverExited += HandleModuleHoverExited;
        }

        SubscribeToDataSource();
        EnsureCursorRoot();
    }

    private void Start()
    {
        RestorePlacedModulesFromData();
        RefreshViews();
        SetSelection(null);
    }
    
    private void OnDestroy()
    {
        if (inventoryView != null)
        {
            inventoryView.ModulePressed -= HandleModulePressed;
            inventoryView.ModuleHovered -= HandleInventoryModuleHovered;
            inventoryView.ModuleHoverExited -= HandleModuleHoverExited;
        }

        if (placementBoard != null)
        {
            placementBoard.ModuleHovered -= HandleBoardModuleHovered;
            placementBoard.ModuleHoverExited -= HandleModuleHoverExited;
        }

        UnsubscribeFromDataSource();

        for (int i = 0; i < m_cursorMaterials.Count; i++)
        {
            if (m_cursorMaterials[i] != null)
                Destroy(m_cursorMaterials[i]);
        }
        m_cursorMaterials.Clear();
    }

    private void Update()
    {
        if(!moduleRoot.gameObject.activeInHierarchy)
        {
            return;
        }
        UpdateHoveredBoardCell();
        if (m_selectedModule != null)
        {
            UpdateCursorPreviewPosition();
        }

        if (Input.GetMouseButtonDown(0))
        {
            HandleLeftMousePressed();
        }

        if (Input.GetMouseButtonUp(0))
        {
            HandleLeftMouseReleased();
        }

        if (m_selectedModule != null && Input.GetKeyDown(KeyCode.Escape))
        {
            SetSelection(null);
        }

        if (m_selectedModule != null && Input.GetMouseButtonDown(1))
        {
            RotateSelectedModule();
        }
    }
#endregion
    private void HandleModulePressed(IGridModule module)
    {
        if (module == null || module.IsLoaded)
        {
            return;
        }

        SetSelection(module as GridModuleDefinition);
    }

    private void HandleInventoryModuleHovered(IGridModule module)
    {
        GridModuleDefinition moduleDef = module as GridModuleDefinition;
        if (moduleDef == null || selectionText == null)
        {
            return;
        }

        selectionText.text = moduleDef.moduleName + "\n" + moduleDef.description;
    }

    private void HandleBoardModuleHovered(IGridModule module)
    {
        GridModuleDefinition moduleDef = module as GridModuleDefinition;
        if (moduleDef == null || selectionText == null)
        {
            return;
        }

        selectionText.text = moduleDef.moduleName + "（已装载）\n" + moduleDef.description;
    }

    private void HandleModuleHoverExited()
    {
        if (selectionText == null)
        {
            return;
        }

        // 恢复默认提示文本
        selectionText.text = m_selectedModule == null
            ? "按住左键拿起背包中的模块，松开左键时自动尝试放置"
            : "已拿起：" + m_selectedModule.moduleName + "，松开左键时自动尝试放置";
    }

    private void TryPickupModuleAtCell(Vector2Int cell)
    {
        if (placementBoard == null)
        {
            return;
        }

        Datas datas = Datas.Instance;
        if (datas == null)
        {
            Debug.LogWarning("[ModulePlacementController] Datas.Instance 为空，无法修改模块状态。", this);
            return;
        }

        if (placementBoard.TryPickupModuleAt(cell, out IGridModule pickedModule))
        {
            pickedModule.RemoveFromBoard();

            if (!TryGetRuntimeModuleIndex(pickedModule as GridModuleDefinition, out int moduleIndex) || !TryRemovePlacedModuleData(datas, moduleIndex))
            {
                Debug.LogWarning("[ModulePlacementController] 从 Datas 取回模块失败，已按数据源重建网格。", this);
                RestorePlacedModulesFromData();
                RefreshViews();
                return;
            }

            GridModuleDefinition pickedModuleDef = GetOwnedModule(moduleIndex);
            SetSelection(pickedModuleDef);
            if (selectionText != null)
            {
                selectionText.text = "已从网格取回：" + (pickedModule is GridModuleDefinition def ? def.moduleName : "");
            }
        }
    }

    private void TryPlaceSelectedModuleOrReturnToInventory()
    {
        if (m_selectedModule == null)
        {
            return;
        }

        if (m_hoveredBoardCell.HasValue && TryPlaceSelectedModule(m_hoveredBoardCell.Value))
        {
            return;
        }

        if (selectionText != null)
        {
            selectionText.text = "模块已返回背包";
        }

        SetSelection(null);
    }

    private bool TryPlaceSelectedModule(Vector2Int cell)
    {
        if (placementBoard == null || m_selectedModule == null)
        {
            return false;
        }

        Datas datas = Datas.Instance;
        if (datas == null)
        {
            Debug.LogWarning("[ModulePlacementController] Datas.Instance 为空，无法修改模块状态。", this);
            return false;
        }

        if (!placementBoard.TryPlace(m_selectedModule, cell))
        {
            return false;
        }

        if (!TryGetRuntimeModuleIndex(m_selectedModule, out int selectedModuleIndex) || !TryStorePlacedModuleData(datas, selectedModuleIndex, cell))
        {
            placementBoard.TryPickupModuleAt(cell, out _);
            Debug.LogWarning("[ModulePlacementController] 向 Datas 写入放置结果失败，已回滚本次放置。", this);
            return false;
        }

        SetSelection(null);

        if (selectionText != null)
        {
            selectionText.text = "模块放置成功";
        }

        return true;
    }

    private void SetSelection(GridModuleDefinition module)
    {
        m_selectedModule = module;
        m_rotationCount = 0;
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
            ? "按住左键拿起背包中的模块，松开左键时自动尝试放置"
            : "已拿起：" + m_selectedModule.moduleName + "，松开左键时自动尝试放置";
    }

    private void RotateSelectedModule()
    {
        if (m_selectedModule == null)
        {
            return;
        }

        // 确定旋转锚点：优先使用鼠标悬停的棋盘单元格，否则使用模块中心
        Vector2Int anchorNormalizedCell;
        if (m_hoveredBoardCell.HasValue)
        {
            // 将棋盘单元格坐标转换为模块归一化坐标
            // m_hoveredBoardCell 是锚点单元格（即模块左上角在棋盘上的位置）
            // 需要找出鼠标悬停的那个棋盘格对应模块的哪个归一化单元格
            anchorNormalizedCell = FindNormalizedCellUnderMouse();
        }
        else
        {
            // 鼠标不在棋盘上时，以模块几何中心旋转
            Vector2 center = m_selectedModule.GetNormalizedCenter();
            anchorNormalizedCell = new Vector2Int(Mathf.RoundToInt(center.x), Mathf.RoundToInt(center.y));
        }

        m_selectedModule.RotateClockwise(anchorNormalizedCell);
        m_rotationCount = (m_rotationCount + 1) % 4;
        RefreshCursorPreview();

        if (selectionText != null)
        {
            selectionText.text = "已旋转：" + m_selectedModule.moduleName;
        }
    }

    /// <summary>
    /// 找到鼠标当前悬停的棋盘单元格对应的模块归一化单元格
    /// 预览吸附时模块的 (0,0) 单元格对齐到 m_hoveredBoardCell，
    /// 因此鼠标悬停的归一化单元格就是 (0,0)
    /// </summary>
    private Vector2Int FindNormalizedCellUnderMouse()
    {
        return Vector2Int.zero;
    }

    private void UpdateHoveredBoardCell()//更新鼠标悬停的网格单元格
    {
        Vector2Int? previousHoveredCell = m_hoveredBoardCell;
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

        // 检测网格上悬停的模块变化
        if (m_hoveredBoardCell.HasValue)
        {
            if (placementBoard.TryGetModuleAtCell(m_hoveredBoardCell.Value, out IGridModule hoveredModule))
            {
                if (!previousHoveredCell.HasValue || !placementBoard.TryGetModuleAtCell(previousHoveredCell.Value, out IGridModule prevModule) || prevModule != hoveredModule)
                {
                    placementBoard.NotifyModuleHovered(hoveredModule);
                }
                return;
            }
        }

        // 鼠标离开了已放置模块
        if (previousHoveredCell.HasValue && placementBoard.TryGetModuleAtCell(previousHoveredCell.Value, out _))
        {
            placementBoard.NotifyModuleHoverExited();
        }
    }

    private void RefreshViews()//刷新所有相关界面显示
    {
        if (inventoryView != null)
        {
            inventoryView.Rebuild(GetOwnedModules(), m_selectedModule);
        }
    }

    public void AddModuleToInventory(IGridModule module, bool autoSelect = false)
    {
        if (module == null || Datas.Instance == null)
        {
            return;
        }

        GridModuleDefinition moduleDef = module as GridModuleDefinition;
        if (moduleDef == null)
        {
            return;
        }

        int addedModuleIndex = Datas.Instance.GetOwnedModuleDefinitions().Count;
        GridModuleDefinition storedModule = Datas.Instance.AddOwnedModule(moduleDef);
        if (storedModule == null)
        {
            return;
        }

        GridModuleDefinition runtimeModule = GetOwnedModule(addedModuleIndex);

        if (autoSelect)
        {
            SetSelection(runtimeModule);
        }

        if (selectionText != null)
        {
            selectionText.text = "已加入背包：" + runtimeModule.moduleName;
        }
    }

    private void RestorePlacedModulesFromData()
    {
        if (placementBoard == null)
        {
            return;
        }

        placementBoard.ClearBoard();
        if (Datas.Instance == null)
        {
            return;
        }

        IReadOnlyList<IGridModule> ownedModules = GetOwnedModules();
        CopyPlacedModuleData(m_placedDataBuffer);

        for (int i = 0; i < m_placedDataBuffer.Count; i++)
        {
            PlacedModuleData placedModule = m_placedDataBuffer[i];
            if (placedModule == null)
            {
                continue;
            }

            int moduleIndex = placedModule.ModuleIndex;
            if (moduleIndex < 0 || moduleIndex >= ownedModules.Count)
            {
                continue;
            }

            GridModuleDefinition module = ownedModules[moduleIndex] as GridModuleDefinition;
            if (module == null)
            {
                continue;
            }

            // 恢复旋转状态
            ApplyRotationToModule(module, placedModule.RotationCount);

            if (placementBoard.TryPlace(module, placedModule.AnchorCell))
            {
                module.ApplyToBoard();
            }
        }
    }

    /// <summary>
    /// 对模块应用指定次数的顺时针旋转（以 (0,0) 为锚点）
    /// </summary>
    private void ApplyRotationToModule(GridModuleDefinition module, int rotationCount)
    {
        int rotations = rotationCount % 4;
        for (int i = 0; i < rotations; i++)
        {
            module.RotateClockwise(Vector2Int.zero);
        }
    }

    private void SubscribeToDataSource()
    {
        if (Datas.Instance == null)
        {
            return;
        }

        Datas.Instance.ModuleStateChanged -= HandleModuleStateChanged;
        Datas.Instance.ModuleStateChanged += HandleModuleStateChanged;
        Datas.Instance.BackpackWidthChanged -= HandleBackpackWidthChanged;
        Datas.Instance.BackpackWidthChanged += HandleBackpackWidthChanged;
    }

    private void UnsubscribeFromDataSource()
    {
        if (Datas.Instance == null)
        {
            return;
        }

        Datas.Instance.ModuleStateChanged -= HandleModuleStateChanged;
        Datas.Instance.BackpackWidthChanged -= HandleBackpackWidthChanged;
    }

    private void HandleModuleStateChanged()
    {
        int selectedModuleIndex = m_selectedModule != null ? m_runtimeOwnedModules.IndexOf(m_selectedModule) : -1;
        m_runtimeModulesPrepared = false;
        RestorePlacedModulesFromData();

        if (selectedModuleIndex >= 0)
        {
            GridModuleDefinition refreshedModule = GetOwnedModule(selectedModuleIndex);
            m_selectedModule = refreshedModule != null && !refreshedModule.IsLoaded ? refreshedModule : null;
            m_rotationCount = 0;
        }
        else if (m_selectedModule != null)
        {
            m_selectedModule = null;
            m_rotationCount = 0;
        }

        RefreshViews();
        RefreshCursorPreview();
    }

    private void HandleBackpackWidthChanged()
    {
        if (placementBoard != null)
        {
            placementBoard.BuildBoard();
        }

        RestorePlacedModulesFromData();
        RefreshCursorPreview();
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

        for (int i = 0; i < m_cursorMaterials.Count; i++)
        {
            if (m_cursorMaterials[i] != null)
                Destroy(m_cursorMaterials[i]);
        }
        m_cursorMaterials.Clear();

        if (m_selectedModule == null)
        {
            m_cursorRoot.gameObject.SetActive(false);
            return;
        }

        m_selectedModule.GetNormalizedCells(m_shapeBuffer);
        ApplyCursorPreviewLayout(false);

        // 计算包围盒用于渐变 shader
        Vector2 moduleCenter = m_selectedModule.GetNormalizedCenter();
        ModuleCellFactory.ComputeShapeBounds(
            m_shapeBuffer, moduleCenter, m_currentPreviewStep, m_currentPreviewDrawSize,
            out Vector2 boundsMin, out Vector2 boundsMax);
        Color moduleColor = m_selectedModule.color;

        for (int i = 0; i < m_shapeBuffer.Count; i++)
        {
            Vector2Int cell = m_shapeBuffer[i];
            ModuleCellFactory.ComputeCellPosition(cell, moduleCenter, m_currentPreviewStep,
                out Vector2 anchoredPos, out Vector2 cellOffset);

            Material createdMaterial;
            GameObject cellObject = ModuleCellFactory.CreateCell(
                m_cursorRoot,
                "PreviewCell",
                m_currentPreviewDrawSize,
                anchoredPos,
                moduleColor,
                0.65f,
                ModuleCellConfig.Instance,
                m_selectedModule.gradientColorB,
                cellOffset,
                boundsMin,
                boundsMax,
                out createdMaterial);

            if (createdMaterial != null)
            {
                m_cursorMaterials.Add(createdMaterial);
            }

            Image image = cellObject.GetComponent<Image>();
            m_cursorCells.Add(image);
        }

        ApplyCursorPreviewLayout(false);
        m_cursorRoot.gameObject.SetActive(true);
        UpdateCursorPreviewPosition();
    }

    private void UpdateCursorPreviewPosition()//更新光标预览位置
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

        if (m_hoveredBoardCell.HasValue)//如果鼠标悬停在网格上，渲染网格
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

    private void ApplyCursorPreviewLayout(bool alignToBoard)//应用光标预览布局
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
        //设置每个预览单元格的位置和大小
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

        bool useGradient = ModuleCellConfig.Instance != null && ModuleCellConfig.Instance.GradientShader != null;

        for (int i = 0; i < m_cursorCells.Count; i++)
        {
            if (m_cursorCells[i] != null)
            {
                if (useGradient && m_cursorCells[i].material != null)
                {
                    // 渐变模式下更新 material 的 _ColorA，保持 _ColorB 不变
                    Color colorA = targetColor;
                    colorA.a = targetColor.a;
                    m_cursorCells[i].material.SetColor("_ColorA", colorA);
                }
                else
                {
                    m_cursorCells[i].color = targetColor;
                }
            }
        }
    }

    private void HandleLeftMousePressed()
    {
        if (m_selectionChangedFrame == Time.frameCount)
        {
            return;
        }

        if (m_selectedModule != null)
        {
            return;
        }

        if (m_hoveredBoardCell.HasValue)
        {
            TryPickupModuleAtCell(m_hoveredBoardCell.Value);
        }
    }

    private void HandleLeftMouseReleased()
    {
        if (m_selectedModule == null)
        {
            return;
        }

        TryPlaceSelectedModuleOrReturnToInventory();
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

    public IReadOnlyList<IGridModule> GetOwnedModules()
    {
        EnsureRuntimeModulesPrepared();
        return m_runtimeOwnedModules;
    }

    private GridModuleDefinition GetOwnedModule(int moduleIndex)
    {
        EnsureRuntimeModulesPrepared();
        return moduleIndex >= 0 && moduleIndex < m_runtimeOwnedModules.Count ? m_runtimeOwnedModules[moduleIndex] as GridModuleDefinition : null;
    }

    private bool TryGetRuntimeModuleIndex(IGridModule module, out int moduleIndex)
    {
        EnsureRuntimeModulesPrepared();
        moduleIndex = module != null ? m_runtimeOwnedModules.IndexOf(module) : -1;
        return moduleIndex >= 0;
    }

    public bool TryGetOwnedModuleIndex(IGridModule module, out int moduleIndex)
    {
        return TryGetRuntimeModuleIndex(module, out moduleIndex);
    }

    private void EnsureRuntimeModulesPrepared()
    {
        if (m_runtimeModulesPrepared)
        {
            return;
        }

        m_runtimeModulesPrepared = true;
        m_runtimeOwnedModules.Clear();

        Datas datas = Datas.Instance;
        if (datas == null)
        {
            return;
        }

        IReadOnlyList<GridModuleDefinition> ownedModuleDefinitions = datas.GetOwnedModuleDefinitions();
        for (int i = 0; i < ownedModuleDefinitions.Count; i++)
        {
            GridModuleDefinition sourceModule = ownedModuleDefinitions[i];
            m_runtimeOwnedModules.Add(sourceModule != null ? sourceModule.Clone() : null);
        }

        CopyPlacedModuleData(m_placedDataBuffer);
        for (int i = 0; i < m_placedDataBuffer.Count; i++)
        {
            PlacedModuleData placedModule = m_placedDataBuffer[i];
            if (placedModule == null)
            {
                continue;
            }

            GridModuleDefinition runtimeModule = GetOwnedModuleUnsafe(placedModule.ModuleIndex);
            if (runtimeModule != null && !runtimeModule.IsLoaded)
            {
                runtimeModule.ApplyToBoard();
            }
        }
    }

    private GridModuleDefinition GetOwnedModuleUnsafe(int moduleIndex)
    {
        return moduleIndex >= 0 && moduleIndex < m_runtimeOwnedModules.Count ? m_runtimeOwnedModules[moduleIndex] as GridModuleDefinition : null;
    }

    private void CopyPlacedModuleData(List<PlacedModuleData> results)
    {
        if (results == null)
        {
            return;
        }

        results.Clear();

        Datas datas = Datas.Instance;
        if (datas == null)
        {
            return;
        }

        IReadOnlyList<PlacedModuleData> placedModuleEntries = datas.GetPlacedModuleEntries();
        for (int i = 0; i < placedModuleEntries.Count; i++)
        {
            PlacedModuleData placedModule = placedModuleEntries[i];
            if (placedModule != null)
            {
                results.Add(new PlacedModuleData(placedModule.ModuleIndex, placedModule.AnchorCell, placedModule.RotationCount));
            }
        }
    }

    private bool TryStorePlacedModuleData(Datas datas, int moduleIndex, Vector2Int anchorCell)
    {
        if (datas == null)
        {
            return false;
        }

        IReadOnlyList<GridModuleDefinition> ownedModuleDefinitions = datas.GetOwnedModuleDefinitions();
        if (moduleIndex < 0 || moduleIndex >= ownedModuleDefinitions.Count)
        {
            return false;
        }

        RemovePlacedEntriesForModule(datas, moduleIndex);
        datas.AddPlacedModuleEntry(new PlacedModuleData(moduleIndex, anchorCell, m_rotationCount));
        datas.NotifyModuleStateChanged();
        return true;
    }

    private bool TryRemovePlacedModuleData(Datas datas, int moduleIndex)
    {
        if (datas == null)
        {
            return false;
        }

        IReadOnlyList<GridModuleDefinition> ownedModuleDefinitions = datas.GetOwnedModuleDefinitions();
        if (moduleIndex < 0 || moduleIndex >= ownedModuleDefinitions.Count)
        {
            return false;
        }

        bool removedAnyEntry = RemovePlacedEntriesForModule(datas, moduleIndex);
        if (!removedAnyEntry)
        {
            return false;
        }
        datas.NotifyModuleStateChanged();
        return true;
    }

    private bool RemovePlacedEntriesForModule(Datas datas, int moduleIndex)
    {
        bool removedAnyEntry = false;
        IReadOnlyList<PlacedModuleData> placedModuleEntries = datas.GetPlacedModuleEntries();
        for (int i = placedModuleEntries.Count - 1; i >= 0; i--)
        {
            PlacedModuleData placedModule = placedModuleEntries[i];
            if (placedModule == null || placedModule.ModuleIndex != moduleIndex)
            {
                continue;
            }

            if (datas.RemovePlacedModuleEntryAt(i))
            {
                removedAnyEntry = true;
            }
        }

        return removedAnyEntry;
    }
}