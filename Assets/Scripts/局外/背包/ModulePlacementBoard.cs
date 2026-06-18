using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class ModulePlacementBoard : MonoBehaviour, IPointerClickHandler, IModulePlacementBoard
{
    public struct PlacedModuleState
    {
        public GridModuleDefinition module;
        public Vector2Int anchorCell;
    }

    private class PlacedModuleEntry
    {
        public GridModuleDefinition module;
        public Vector2Int anchorCell;
        public readonly List<Vector2Int> occupiedCells = new List<Vector2Int>();
    }

    [SerializeField] private RectTransform boardRoot;
    [SerializeField] private float spacing = 4f;
    [SerializeField] private Color emptyCellColor = new Color(0.15f, 0.18f, 0.22f, 0.95f);

    private readonly List<Vector2Int> m_shapeBuffer = new List<Vector2Int>();

    private RectTransform m_cellsRoot;
    private Image[,] m_cells;
    private Material[,] m_cellMaterials;
    private bool[,] m_occupied;
    private PlacedModuleEntry[,] m_placedEntries;

    public event Action<Vector2Int> CellClicked;
    public event Action<IGridModule> ModuleHovered;
    public event Action ModuleHoverExited;
    private void OnValidate()
    {
        spacing = Mathf.Max(0f, spacing);
    }

    public void BuildBoard()
    {
        if (boardRoot == null)
        {
            boardRoot = transform as RectTransform;
        }
        EnsureBoardRaycastTarget();
        EnsureCellsRoot();
        BuildCells();
    }

    public bool CanPlace(IGridModule module, Vector2Int anchorCell)
    {
        if (module == null || m_occupied == null)
        {
            return false;
        }

        GridModuleDefinition moduleDef = module as GridModuleDefinition;
        if (moduleDef == null)
        {
            return false;
        }

        int boardSize = GetBoardSize();
        moduleDef.GetNormalizedCells(m_shapeBuffer);
        for (int i = 0; i < m_shapeBuffer.Count; i++)
        {
            Vector2Int boardCell = anchorCell + m_shapeBuffer[i];
            if (boardCell.x < 0 || boardCell.x >= boardSize || boardCell.y < 0 || boardCell.y >= boardSize)
            {
                return false;
            }

            if (m_occupied[boardCell.x, boardCell.y])
            {
                return false;
            }
        }

        return true;
    }

    public bool TryPlace(IGridModule module, Vector2Int anchorCell)
    {
        if (!CanPlace(module, anchorCell))
        {
            return false;
        }

        GridModuleDefinition moduleDef = module as GridModuleDefinition;
        if (moduleDef == null)
        {
            return false;
        }

        PlacedModuleEntry entry = new PlacedModuleEntry
        {
            module = moduleDef,
            anchorCell = anchorCell
        };

        module.GetNormalizedCells(m_shapeBuffer);

        // 计算模块形状包围盒（用于渐变 shader）
        float cellSize = CalculateCellSize();
        float cellStride = cellSize + spacing;
        Vector2 cellStep = new Vector2(cellStride, cellStride);
        Vector2 cellDrawSize = new Vector2(Mathf.Max(1f, cellSize - 2f), Mathf.Max(1f, cellSize - 2f));
        Vector2 moduleCenter = module.GetNormalizedCenter();
        ModuleCellFactory.ComputeShapeBounds(
            m_shapeBuffer, moduleCenter, cellStep, cellDrawSize,
            out Vector2 boundsMin, out Vector2 boundsMax);

        bool useGradient = ModuleCellConfig.Instance != null && ModuleCellConfig.Instance.GradientShader != null;

        for (int i = 0; i < m_shapeBuffer.Count; i++)
        {
            Vector2Int boardCell = anchorCell + m_shapeBuffer[i];
            m_occupied[boardCell.x, boardCell.y] = true;
            m_placedEntries[boardCell.x, boardCell.y] = entry;
            entry.occupiedCells.Add(boardCell);

            Image cellImage = m_cells[boardCell.x, boardCell.y];

            if (useGradient)
            {
                Vector2Int normalizedCell = m_shapeBuffer[i];
                ModuleCellFactory.ComputeCellPosition(normalizedCell, moduleCenter, cellStep,
                    out Vector2 anchoredPos, out Vector2 cellOffset);

                // 清理旧 material
                if (m_cellMaterials[boardCell.x, boardCell.y] != null)
                {
                    Destroy(m_cellMaterials[boardCell.x, boardCell.y]);
                }

                Shader gradientShader = ModuleCellConfig.Instance.GradientShader;
                Material mat = new Material(gradientShader);
                Color colorA = moduleDef.color;
                Color colorB = moduleDef.gradientColorB;
                colorB.a = colorA.a;
                mat.SetColor("_ColorA", colorA);
                mat.SetColor("_ColorB", colorB);
                mat.SetFloat("_GradientAngle", ModuleCellConfig.Instance.GradientAngle);
                mat.SetVector("_CellOffset", new Vector4(cellOffset.x, cellOffset.y, 0f, 0f));
                mat.SetVector("_CellSize", new Vector4(cellDrawSize.x, cellDrawSize.y, 0f, 0f));
                mat.SetVector("_BoundsMin", new Vector4(boundsMin.x, boundsMin.y, 0f, 0f));
                mat.SetVector("_BoundsMax", new Vector4(boundsMax.x, boundsMax.y, 0f, 0f));
                mat.SetTexture("_MainTex", Texture2D.whiteTexture);
                mat.SetVector("_ClipRect", new Vector4(-float.MaxValue, -float.MaxValue, float.MaxValue, float.MaxValue));
                cellImage.material = mat;
                cellImage.color = new Color(1f, 1f, 1f, 1f);
                m_cellMaterials[boardCell.x, boardCell.y] = mat;
            }
            else
            {
                cellImage.color = moduleDef.color;
            }
        }

        return true;
    }

    public bool TryPickupModuleAt(Vector2Int cell, out IGridModule module)
    {
        module = null;

        if (m_placedEntries == null)
        {
            return false;
        }

        int boardSize = GetBoardSize();
        if (cell.x < 0 || cell.x >= boardSize || cell.y < 0 || cell.y >= boardSize)
        {
            return false;
        }

        PlacedModuleEntry entry = m_placedEntries[cell.x, cell.y];
        if (entry == null || entry.module == null)
        {
            return false;
        }

        for (int i = 0; i < entry.occupiedCells.Count; i++)
        {
            Vector2Int occupiedCell = entry.occupiedCells[i];
            m_occupied[occupiedCell.x, occupiedCell.y] = false;
            m_placedEntries[occupiedCell.x, occupiedCell.y] = null;

            // 清理渐变 material
            if (m_cellMaterials != null && m_cellMaterials[occupiedCell.x, occupiedCell.y] != null)
            {
                Destroy(m_cellMaterials[occupiedCell.x, occupiedCell.y]);
                m_cellMaterials[occupiedCell.x, occupiedCell.y] = null;
            }

            m_cells[occupiedCell.x, occupiedCell.y].material = null;
            m_cells[occupiedCell.x, occupiedCell.y].color = emptyCellColor;
        }

        module = entry.module;
        return true;
    }

    public bool TryGetCellFromScreenPoint(Vector2 screenPoint, Camera eventCamera, out Vector2Int cell)
    {
        return TryGetCellFromScreenPointInternal(screenPoint, eventCamera, out cell);
    }

    public bool TryGetCellCenterInRect(RectTransform targetRect, Vector2Int cell, out Vector2 localPoint)
    {
        localPoint = Vector2.zero;

        if (targetRect == null || boardRoot == null)
        {
            return false;
        }

        int boardSize = GetBoardSize();
        if (cell.x < 0 || cell.x >= boardSize || cell.y < 0 || cell.y >= boardSize)
        {
            return false;
        }

        Vector2 boardLocalPoint = GetCellCenterLocalPoint(cell);
        Vector3 worldPoint = boardRoot.TransformPoint(boardLocalPoint);
        localPoint = targetRect.InverseTransformPoint(worldPoint);
        return true;
    }

    public float GetCellSize()
    {
        return CalculateCellSize();
    }

    public float GetCellStride()
    {
        return CalculateCellSize() + spacing;
    }

    public void ClearBoard()
    {
        if (m_occupied == null || m_cells == null || m_placedEntries == null)
        {
            return;
        }

        int boardSize = GetBoardSize();
        for (int y = 0; y < boardSize; y++)
        {
            for (int x = 0; x < boardSize; x++)
            {
                m_occupied[x, y] = false;
                m_placedEntries[x, y] = null;

                // 清理渐变 material
                if (m_cellMaterials != null && m_cellMaterials[x, y] != null)
                {
                    Destroy(m_cellMaterials[x, y]);
                    m_cellMaterials[x, y] = null;
                }

                m_cells[x, y].material = null;
                m_cells[x, y].color = emptyCellColor;
            }
        }
    }

    public void GetPlacedModules(List<PlacedModuleState> results)
    {
        if (results == null)
        {
            return;
        }

        results.Clear();
        if (m_placedEntries == null)
        {
            return;
        }

        int boardSize = GetBoardSize();
        HashSet<PlacedModuleEntry> visitedEntries = new HashSet<PlacedModuleEntry>();
        for (int y = 0; y < boardSize; y++)
        {
            for (int x = 0; x < boardSize; x++)
            {
                PlacedModuleEntry entry = m_placedEntries[x, y];
                if (entry == null || !visitedEntries.Add(entry))
                {
                    continue;
                }

                results.Add(new PlacedModuleState
                {
                    module = entry.module,
                    anchorCell = entry.anchorCell
                });
            }
        }
    }

    public bool TryGetModuleAtCell(Vector2Int cell, out IGridModule module)
    {
        module = null;

        if (m_placedEntries == null)
        {
            return false;
        }

        int boardSize = GetBoardSize();
        if (cell.x < 0 || cell.x >= boardSize || cell.y < 0 || cell.y >= boardSize)
        {
            return false;
        }

        PlacedModuleEntry entry = m_placedEntries[cell.x, cell.y];
        if (entry == null || entry.module == null)
        {
            return false;
        }

        module = entry.module;
        return true;
    }

    public void NotifyModuleHovered(IGridModule module)
    {
        ModuleHovered?.Invoke(module);
    }

    public void NotifyModuleHoverExited()
    {
        ModuleHoverExited?.Invoke();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Vector2Int cell;
        if (TryGetCellFromPointer(eventData, out cell))
        {
            CellClicked?.Invoke(cell);
        }
    }
#region 预防性函数
    private void EnsureBoardRaycastTarget()
    {
        Image image = boardRoot.GetComponent<Image>();
        if (image == null)
        {
            image = boardRoot.gameObject.AddComponent<Image>();
        }

        image.color = new Color(0f, 0f, 0f, 0.05f);
        image.raycastTarget = true;
    }

    private void EnsureCellsRoot()
    {
        Transform existing = boardRoot.Find("Cells");
        if (existing != null)
        {
            m_cellsRoot = existing as RectTransform;
        }

        if (m_cellsRoot == null)
        {
            GameObject rootObject = new GameObject("Cells", typeof(RectTransform), typeof(GridLayoutGroup));
            rootObject.transform.SetParent(boardRoot, false);
            m_cellsRoot = rootObject.GetComponent<RectTransform>();
        }

        m_cellsRoot.anchorMin = Vector2.zero;
        m_cellsRoot.anchorMax = Vector2.one;
        m_cellsRoot.offsetMin = Vector2.zero;
        m_cellsRoot.offsetMax = Vector2.zero;
    }
#endregion
    private void BuildCells()
    {
        int boardSize = GetBoardSize();

        for (int i = m_cellsRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(m_cellsRoot.GetChild(i).gameObject);
        }

        GridLayoutGroup gridLayout = m_cellsRoot.GetComponent<GridLayoutGroup>();
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = boardSize;
        gridLayout.spacing = new Vector2(spacing, spacing);
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.childAlignment = TextAnchor.UpperLeft;

        float cellSize = CalculateCellSize();
        gridLayout.cellSize = new Vector2(cellSize, cellSize);

        m_cells = new Image[boardSize, boardSize];
        m_cellMaterials = new Material[boardSize, boardSize];
        m_occupied = new bool[boardSize, boardSize];
        m_placedEntries = new PlacedModuleEntry[boardSize, boardSize];

        for (int y = 0; y < boardSize; y++)
        {
            for (int x = 0; x < boardSize; x++)
            {
                GameObject cellObject = new GameObject("Cell_" + x + "_" + y, typeof(RectTransform), typeof(Image), typeof(Outline));
                cellObject.transform.SetParent(m_cellsRoot, false);

                Image image = cellObject.GetComponent<Image>();
                image.color = emptyCellColor;
                image.raycastTarget = false;

                Outline outline = cellObject.GetComponent<Outline>();
                outline.effectDistance = new Vector2(1f, -1f);
                outline.effectColor = new Color(1f, 1f, 1f, 0.12f);

                m_cells[x, y] = image;
                m_occupied[x, y] = false;
                m_placedEntries[x, y] = null;
            }
        }
    }

    private bool TryGetCellFromPointer(PointerEventData eventData, out Vector2Int cell)
    {
        return TryGetCellFromScreenPointInternal(eventData.position, eventData.pressEventCamera, out cell);
    }

    private bool TryGetCellFromScreenPointInternal(Vector2 screenPoint, Camera eventCamera, out Vector2Int cell)
    {
        cell = Vector2Int.zero;

        if (boardRoot == null)
        {
            return false;
        }

        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(boardRoot, screenPoint, eventCamera, out localPoint))
        {
            return false;
        }

        float cellSize = CalculateCellSize();
        float stride = cellSize + spacing;
        Rect rect = boardRoot.rect;
        float localX = localPoint.x - rect.xMin;
        float localY = rect.yMax - localPoint.y;

        int boardSize = GetBoardSize();
        int x = Mathf.FloorToInt(localX / stride);
        int y = Mathf.FloorToInt(localY / stride);

        if (x < 0 || x >= boardSize || y < 0 || y >= boardSize)
        {
            return false;
        }

        float offsetX = localX - x * stride;
        float offsetY = localY - y * stride;
        if (offsetX > cellSize || offsetY > cellSize)
        {
            return false;
        }

        cell = new Vector2Int(x, y);
        return true;
    }

    private Vector2 GetCellCenterLocalPoint(Vector2Int cell)
    {
        float cellSize = CalculateCellSize();
        float stride = cellSize + spacing;
        Rect rect = boardRoot.rect;
        float x = rect.xMin + cell.x * stride + cellSize * 0.5f;
        float y = rect.yMax - cell.y * stride - cellSize * 0.5f;
        return new Vector2(x, y);
    }

    private float CalculateCellSize()
    {
        Rect rect = boardRoot.rect;
        int boardSize = GetBoardSize();
        float widthSize = (rect.width - spacing * (boardSize - 1)) / boardSize;
        float heightSize = (rect.height - spacing * (boardSize - 1)) / boardSize;
        return Mathf.Max(1f, Mathf.Min(widthSize, heightSize));
    }

    private int GetBoardSize()
    {
        Datas datas = Datas.Instance;
        if (datas == null)
        {
            return 1;
        }

        return Mathf.Max(1, datas.GetBackpackWidth());
    }

    private void OnDestroy()
    {
        if (m_cellMaterials != null)
        {
            int boardSize = GetBoardSize();
            for (int y = 0; y < boardSize; y++)
            {
                for (int x = 0; x < boardSize; x++)
                {
                    if (m_cellMaterials[x, y] != null)
                    {
                        Destroy(m_cellMaterials[x, y]);
                        m_cellMaterials[x, y] = null;
                    }
                }
            }
        }
    }
}