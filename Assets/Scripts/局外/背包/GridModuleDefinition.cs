using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GridModuleDefinition
{
    public string moduleName = "新模块";
    public Color color = new Color(0.28f, 0.78f, 1f, 0.9f);
    public List<Vector2Int> cells = new List<Vector2Int>
    {
        Vector2Int.zero
    };

    public GridModuleDefinition Clone()
    {
        GridModuleDefinition clone = new GridModuleDefinition();
        clone.moduleName = moduleName;
        clone.color = color;
        clone.cells = new List<Vector2Int>(cells.Count);

        for (int i = 0; i < cells.Count; i++)
        {
            clone.cells.Add(cells[i]);
        }

        return clone;
    }

    public void GetNormalizedCells(List<Vector2Int> results)
    {
        if (results == null)
        {
            return;
        }

        results.Clear();

        if (cells == null || cells.Count == 0)
        {
            results.Add(Vector2Int.zero);
            return;
        }

        int minX = int.MaxValue;
        int minY = int.MaxValue;

        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int cell = cells[i];
            if (cell.x < minX)
            {
                minX = cell.x;
            }

            if (cell.y < minY)
            {
                minY = cell.y;
            }
        }

        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int cell = cells[i];
            Vector2Int normalized = new Vector2Int(cell.x - minX, cell.y - minY);

            if (!results.Contains(normalized))
            {
                results.Add(normalized);
            }
        }

        if (results.Count == 0)
        {
            results.Add(Vector2Int.zero);
        }
    }

    public Vector2Int GetSize()
    {
        List<Vector2Int> normalizedCells = new List<Vector2Int>();
        GetNormalizedCells(normalizedCells);

        int maxX = 0;
        int maxY = 0;

        for (int i = 0; i < normalizedCells.Count; i++)
        {
            Vector2Int cell = normalizedCells[i];
            if (cell.x > maxX)
            {
                maxX = cell.x;
            }

            if (cell.y > maxY)
            {
                maxY = cell.y;
            }
        }

        return new Vector2Int(maxX + 1, maxY + 1);
    }

    public Vector2 GetNormalizedCenter()
    {
        List<Vector2Int> normalizedCells = new List<Vector2Int>();
        GetNormalizedCells(normalizedCells);

        int maxX = 0;
        int maxY = 0;

        for (int i = 0; i < normalizedCells.Count; i++)
        {
            Vector2Int cell = normalizedCells[i];
            if (cell.x > maxX)
            {
                maxX = cell.x;
            }

            if (cell.y > maxY)
            {
                maxY = cell.y;
            }
        }

        return new Vector2(maxX * 0.5f, maxY * 0.5f);
    }

    public int GetMaxDimension()
    {
        Vector2Int size = GetSize();
        return Mathf.Max(size.x, size.y);
    }
}