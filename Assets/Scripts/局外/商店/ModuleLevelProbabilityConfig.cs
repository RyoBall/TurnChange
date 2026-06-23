using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单个队伍等级对应的各大小序体刷新概率权重
/// </summary>
[Serializable]
public class ModuleLevelProbabilityEntry
{
    [Tooltip("队伍等级（1~99）")]
    [Range(1, 99)]
    public int teamLevel = 1;

    [Tooltip("小序体刷新权重")]
    [Range(0f, 100f)]
    public float smallWeight = 40f;

    [Tooltip("中序体刷新权重")]
    [Range(0f, 100f)]
    public float normalWeight = 40f;

    [Tooltip("大序体刷新权重")]
    [Range(0f, 100f)]
    public float largeWeight = 20f;
}

/// <summary>
/// 可编程物体：配置每个队伍等级对应的不同大小序体的刷新概率。
/// 在 ShopModuleManager 中引用此资产，每个商品槽刷新时根据队伍等级读取对应权重独立掷骰。
/// </summary>
[CreateAssetMenu(fileName = "ModuleLevelProbabilityConfig", menuName = "背包/序体等级概率配置")]
public class ModuleLevelProbabilityConfig : ScriptableObject
{
    [Tooltip("各队伍等级对应的序体大小概率权重列表。未配置的等级将使用最接近的已配置等级。")]
    [SerializeField] private List<ModuleLevelProbabilityEntry> levelEntries = new List<ModuleLevelProbabilityEntry>();

    /// <summary>
    /// 根据队伍等级获取各大小序体的刷新权重
    /// </summary>
    /// <param name="teamLevel">当前队伍等级</param>
    /// <returns>包含 Small / Normal / Large 权重的元组</returns>
    public (float small, float normal, float large) GetWeightsForLevel(int teamLevel)
    {
        ModuleLevelProbabilityEntry matchedEntry = FindBestMatch(teamLevel);
        if (matchedEntry == null)
        {
            // 没有配置任何条目时返回默认均等权重
            Debug.LogWarning($"[ModuleLevelProbabilityConfig] 未配置任何等级条目，使用默认均等权重。", this);
            return (1f, 1f, 1f);
        }

        return (matchedEntry.smallWeight, matchedEntry.normalWeight, matchedEntry.largeWeight);
    }

    /// <summary>
    /// 根据权重随机选取一个序体大小等级
    /// </summary>
    /// <param name="teamLevel">当前队伍等级</param>
    /// <returns>随机选中的 GridModuleLevel</returns>
    public GridModuleLevel RollModuleLevel(int teamLevel)
    {
        (float small, float normal, float large) = GetWeightsForLevel(teamLevel);
        return RollByWeights(small, normal, large);
    }

    /// <summary>
    /// 查找与给定等级最匹配的配置条目。
    /// 优先精确匹配，否则找 <= teamLevel 的最大等级，再否则找 > teamLevel 的最小等级。
    /// </summary>
    private ModuleLevelProbabilityEntry FindBestMatch(int teamLevel)
    {
        if (levelEntries == null || levelEntries.Count == 0)
        {
            return null;
        }

        ModuleLevelProbabilityEntry bestLower = null;
        ModuleLevelProbabilityEntry bestHigher = null;

        for (int i = 0; i < levelEntries.Count; i++)
        {
            ModuleLevelProbabilityEntry entry = levelEntries[i];
            if (entry == null)
            {
                continue;
            }

            if (entry.teamLevel == teamLevel)
            {
                return entry;
            }

            if (entry.teamLevel < teamLevel)
            {
                if (bestLower == null || entry.teamLevel > bestLower.teamLevel)
                {
                    bestLower = entry;
                }
            }
            else
            {
                if (bestHigher == null || entry.teamLevel < bestHigher.teamLevel)
                {
                    bestHigher = entry;
                }
            }
        }

        // 优先返回最接近的较低等级，否则返回最接近的较高等级
        return bestLower ?? bestHigher;
    }

    /// <summary>
    /// 根据三个权重值按概率随机选取一个 GridModuleLevel
    /// </summary>
    private static GridModuleLevel RollByWeights(float smallWeight, float normalWeight, float largeWeight)
    {
        float totalWeight = Mathf.Max(0f, smallWeight) + Mathf.Max(0f, normalWeight) + Mathf.Max(0f, largeWeight);

        if (totalWeight <= 0f)
        {
            // 所有权重都为 0，返回默认 Small
            return GridModuleLevel.Small;
        }

        float roll = UnityEngine.Random.Range(0f, totalWeight);

        float small = Mathf.Max(0f, smallWeight);
        if (roll < small)
        {
            return GridModuleLevel.Small;
        }

        float normal = Mathf.Max(0f, normalWeight);
        if (roll < small + normal)
        {
            return GridModuleLevel.Normal;
        }

        return GridModuleLevel.Large;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 在 Inspector 中校验配置数据
    /// </summary>
    private void OnValidate()
    {
        if (levelEntries == null)
        {
            return;
        }

        for (int i = 0; i < levelEntries.Count; i++)
        {
            ModuleLevelProbabilityEntry entry = levelEntries[i];
            if (entry == null)
            {
                continue;
            }

            entry.teamLevel = Mathf.Clamp(entry.teamLevel, 1, 99);
            entry.smallWeight = Mathf.Max(0f, entry.smallWeight);
            entry.normalWeight = Mathf.Max(0f, entry.normalWeight);
            entry.largeWeight = Mathf.Max(0f, entry.largeWeight);
        }
    }
#endif
}
