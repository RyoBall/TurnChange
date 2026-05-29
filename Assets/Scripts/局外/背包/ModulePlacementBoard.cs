using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class ModulePlacementBoard : MonoBehaviour, IPointerClickHandler
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
    [SerializeField] private int width = 5;
    [SerializeField] private int height = 5;
    [SerializeField] private float spacing = 4f;
    [SerializeField] private Color emptyCellColor = new Color(0.15f, 0.18f, 0.22f, 0.95f);

    private readonly List<Vector2Int> m_shapeBuffer = new List<Vector2Int>();

    private RectTransform m_cellsRoot;
    private Image[,] m_cells;
    private bool[,] m_occupied;
    private PlacedModuleEntry[,] m_placedEntries;

    public event Action<Vector2Int> CellClicked;
    private void OnValidate()
    {
        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);
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

    public bool CanPlace(GridModuleDefinition module, Vector2Int anchorCell)
    {
        if (module == null || m_occupied == null)
        {
            return false;
        }

        module.GetNormalizedCells(m_shapeBuffer);
        for (int i = 0; i < m_shapeBuffer.Count; i++)
        {
            Vector2Int boardCell = anchorCell + m_shapeBuffer[i];
            if (boardCell.x < 0 || boardCell.x >= width || boardCell.y < 0 || boardCell.y >= height)
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

    public bool TryPlace(GridModuleDefinition module, Vector2Int anchorCell)
    {
        if (!CanPlace(module, anchorCell))
        {
            return false;
        }

        PlacedModuleEntry entry = new PlacedModuleEntry
        {
            module = module,
            anchorCell = anchorCell
        };

        module.GetNormalizedCells(m_shapeBuffer);
        for (int i = 0; i < m_shapeBuffer.Count; i++)
        {
            Vector2Int boardCell = anchorCell + m_shapeBuffer[i];
            m_occupied[boardCell.x, boardCell.y] = true;
            m_placedEntries[boardCell.x, boardCell.y] = entry;
            entry.occupiedCells.Add(boardCell);
            m_cells[boardCell.x, boardCell.y].color = module.color;
        }

        return true;
    }

    public bool TryPickupModuleAt(Vector2Int cell, out GridModuleDefinition module)
    {
        module = null;

        if (m_placedEntries == null)
        {
            return false;
        }

        if (cell.x < 0 || cell.x >= width || cell.y < 0 || cell.y >= height)
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

        if (cell.x < 0 || cell.x >= width || cell.y < 0 || cell.y >= height)
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

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                m_occupied[x, y] = false;
                m_placedEntries[x, y] = null;
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

        HashSet<PlacedModuleEntry> visitedEntries = new HashSet<PlacedModuleEntry>();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
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
        for (int i = m_cellsRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(m_cellsRoot.GetChild(i).gameObject);
        }

        GridLayoutGroup gridLayout = m_cellsRoot.GetComponent<GridLayoutGroup>();
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = width;
        gridLayout.spacing = new Vector2(spacing, spacing);
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.childAlignment = TextAnchor.UpperLeft;

        float cellSize = CalculateCellSize();
        gridLayout.cellSize = new Vector2(cellSize, cellSize);

        m_cells = new Image[width, height];
        m_occupied = new bool[width, height];
        m_placedEntries = new PlacedModuleEntry[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
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

        int x = Mathf.FloorToInt(localX / stride);
        int y = Mathf.FloorToInt(localY / stride);

        if (x < 0 || x >= width || y < 0 || y >= height)
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
        float widthSize = (rect.width - spacing * (width - 1)) / width;
        float heightSize = (rect.height - spacing * (height - 1)) / height;
        return Mathf.Max(1f, Mathf.Min(widthSize, heightSize));
    }
}