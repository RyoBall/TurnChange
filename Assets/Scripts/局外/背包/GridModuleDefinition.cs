using System;
using System.Collections.Generic;
using UnityEngine;

public enum GridModuleType
{
    None,
    LegacyDotDamage,
    BattleCommandBonus,
    OpeningAdvance,
    ExtraCommand,
    SwapDamageBoost,
    SwapSpeedBoost,
    SwapSelfHeal,
    HealingBoost,
    HealChaosCleanse,
    DotBoost,
    DirectDamageBoost,
    EmergencyEvade,
    FatalGuard,
    MaxHealthBoost,
    DefenseBoost,
    CritDamageBoost,
    CritRateBoost,
    HeavyPoison,
    HeavyTurret,
    GamblerStride,
    BloodReverse,
    DomainResonance,
    ChargeCounterResonance,
    HybridDamage,
    SupportSwapAdvance,
    ChaosImmunity,
    SwapChargeBurst,
    EmergencySwapIn,
    CritDotSpread,
    FocusFire,
}

public enum GridModuleLevel
{
    Small,
    Normal,
    Large
}

[CreateAssetMenu(fileName = "GridModule", menuName = "背包/新模块")]
public class GridModuleDefinition : ScriptableObject, IGridModule
{
    [Header("模块配置")]
    public GridModuleType moduleType;
    public string moduleName = "新模块";
    [TextArea(2, 5)] public string description;
    public GridModuleLevel level = GridModuleLevel.Small;
    public Color color = new Color(0.28f, 0.78f, 1f, 0.9f);
    public Color gradientColorB = new Color(0.1f, 0.3f, 0.85f, 0.9f);
    public TemporaryBattleModifierData modifierData = new TemporaryBattleModifierData();
    public List<Vector2Int> cells = new List<Vector2Int>
    {
        Vector2Int.zero
    };
    [NonSerialized] private bool m_isLoaded;

    public bool IsLoaded => m_isLoaded;
    public Color ModuleColor => color;
    public Color GradientColorB => gradientColorB;
    public int GetPricePerCell()
    {
        return GetDefaultPricePerCell(level);
    }

    public static int GetDefaultPricePerCell(GridModuleLevel moduleLevel)
    {
        switch (moduleLevel)
        {
            case GridModuleLevel.Small:
                return 4;
            case GridModuleLevel.Normal:
                return 5;
            case GridModuleLevel.Large:
                return 7;
            default:
                return 4;
        }
    }

    public GridModuleDefinition Clone()
    {
        GridModuleDefinition clone = Instantiate(this);
        clone.hideFlags = HideFlags.HideAndDontSave;
        clone.moduleName = moduleName;
        clone.moduleType = moduleType;
        clone.description = description;
        clone.level = level;
        clone.color = color;
        clone.gradientColorB = gradientColorB;
        clone.modifierData = modifierData != null ? modifierData.Clone() : null;
        clone.cells = new List<Vector2Int>(cells.Count);
        clone.m_isLoaded = false;

        for (int i = 0; i < cells.Count; i++)
        {
            clone.cells.Add(cells[i]);
        }

        return clone;
    }

    IGridModule IGridModule.Clone()
    {
        return Clone();
    }

    public void ApplyToBoard()
    {
        if (m_isLoaded)
        {
            return;
        }

        m_isLoaded = true;
        TemporaryBattleModifierRuntimeManager.SyncModuleModifier(this);
    }

    public void RemoveFromBoard()
    {
        if (!m_isLoaded)
        {
            return;
        }

        TemporaryBattleModifierRuntimeManager.RemoveModuleModifier(this);
        m_isLoaded = false;
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

    public void RotateClockwise(Vector2Int anchorNormalizedCell)
    {
        if (cells == null || cells.Count == 0)
        {
            return;
        }

        // 先归一化当前 cells 以便正确计算偏移
        NormalizeCellsInPlace();

        // 以 anchorNormalizedCell 为锚点顺时针旋转 90°:
        // (x, y) 绕 (ax, ay) 旋转 → (ax + (y - ay), ay - (x - ax))
        int ax = anchorNormalizedCell.x;
        int ay = anchorNormalizedCell.y;

        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int cell = cells[i];
            cells[i] = new Vector2Int(ax + (cell.y - ay), ay - (cell.x - ax));
        }

        // 重新归一化，使最小坐标为 (0, 0)
        NormalizeCellsInPlace();
    }

    private void NormalizeCellsInPlace()
    {
        if (cells == null || cells.Count == 0)
        {
            return;
        }

        int minX = int.MaxValue;
        int minY = int.MaxValue;

        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int cell = cells[i];
            if (cell.x < minX) minX = cell.x;
            if (cell.y < minY) minY = cell.y;
        }

        for (int i = 0; i < cells.Count; i++)
        {
            cells[i] = new Vector2Int(cells[i].x - minX, cells[i].y - minY);
        }
    }
}