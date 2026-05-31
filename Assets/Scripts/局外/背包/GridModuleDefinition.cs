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
}

public enum GridModuleLevel
{
    Small,
    Normal,
    Large
}

[CreateAssetMenu(fileName = "GridModule", menuName = "背包/新模块")]
public class GridModuleDefinition : ScriptableObject
{
    [Header("模块配置")]
    public GridModuleType moduleType;
    public string moduleName = "新模块";
    [TextArea(2, 5)] public string description;
    public GridModuleLevel level = GridModuleLevel.Small;
    public Color color = new Color(0.28f, 0.78f, 1f, 0.9f);
    public TemporaryBattleModifierData modifierData = new TemporaryBattleModifierData();
    public float baseExtraData1;
    public float baseExtraData2;
    public float baseExtraData3;
    public float baseExtraData4;
    [SerializeField]int privePerCell = 5;
    public List<Vector2Int> cells = new List<Vector2Int>
    {
        Vector2Int.zero
    };

    [NonSerialized] private IGridModuleBehavior m_behavior;
    [NonSerialized] private bool m_isLoaded;

    public bool IsLoaded => m_isLoaded;

    private IGridModuleBehavior Behavior
    {
        get
        {
            if (m_behavior == null)
            {
                m_behavior = GridModuleBehaviorFactory.Create(moduleType);
                m_behavior.Initialize(this);
            }

            return m_behavior;
        }
    }

    private void OnEnable()
    {
        m_behavior = null;
    }

    private void OnValidate()
    {
        m_behavior = null;
    }
    public int GetPricePerCell()
    {
        return Mathf.Max(0, privePerCell);
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
        clone.modifierData = modifierData != null ? modifierData.Clone() : null;
        clone.baseExtraData1 = baseExtraData1;
        clone.baseExtraData2 = baseExtraData2;
        clone.baseExtraData3 = baseExtraData3;
        clone.baseExtraData4 = baseExtraData4;
        clone.privePerCell = privePerCell;
        clone.cells = new List<Vector2Int>(cells.Count);
        clone.m_behavior = null;
        clone.m_isLoaded = false;

        for (int i = 0; i < cells.Count; i++)
        {
            clone.cells.Add(cells[i]);
        }

        return clone;
    }

    public void ApplyToBoard()
    {
        if (m_isLoaded)
        {
            return;
        }

        m_isLoaded = true;
        TemporaryBattleModifierRuntimeManager.SyncModuleModifier(this);
        Behavior.OnApplyToBoard();
    }

    public void RemoveFromBoard()
    {
        if (!m_isLoaded)
        {
            return;
        }

        TemporaryBattleModifierRuntimeManager.RemoveModuleModifier(this);
        Behavior.OnRemoveFromBoard();
        m_isLoaded = false;
    }

    public float GetDotDamageMultiplier()
    {
        return Behavior.GetDotDamageMultiplier();
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

public interface IGridModuleBehavior
{
    void Initialize(GridModuleDefinition module);
    void OnApplyToBoard();
    void OnRemoveFromBoard();
    float GetDotDamageMultiplier();
}

public abstract class GridModuleBehaviorBase : IGridModuleBehavior
{
    protected GridModuleDefinition module;

    public virtual void Initialize(GridModuleDefinition module)
    {
        this.module = module;
    }

    public virtual void OnApplyToBoard() { }
    public virtual void OnRemoveFromBoard() { }
    public virtual float GetDotDamageMultiplier() { return 1f; }
}

public static class GridModuleBehaviorFactory
{
    public static IGridModuleBehavior Create(GridModuleType moduleType)
    {
        switch (moduleType)
        {
            case GridModuleType.LegacyDotDamage:
                return new DotDamageGridModuleBehavior();
            default:
                return new DefaultGridModuleBehavior();
        }
    }
}

public class DefaultGridModuleBehavior : GridModuleBehaviorBase
{
}

public class DotDamageGridModuleBehavior : GridModuleBehaviorBase
{
    public override float GetDotDamageMultiplier()
    {
        return Mathf.Max(0f, 1f + module.baseExtraData1);
    }
}