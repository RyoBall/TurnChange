using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 浓缩关配置：一号列表为常规关卡，二号列表为浓缩关关卡。
/// 开启浓缩关时用二号列表覆盖 Datas 的关卡楼层；关闭时用一号列表覆盖。
/// </summary>
public interface ICondensedLevelConfig
{
    bool IsCondensedModeEnabled { get; }
    bool UseLevelConfiguredPlayerLevel { get; }
    bool ShouldUseLevelConfiguredPlayerLevel { get; }
    IReadOnlyList<LevelSelectionFloorData> GetActiveLevelFloors();
    void SetCondensedModeEnabled(bool enabled);
    void SetUseLevelConfiguredPlayerLevel(bool enabled);
}

[CreateAssetMenu(fileName = "CondensedLevelConfig", menuName = "关卡数据/浓缩关配置")]
public class CondensedLevelConfig : ScriptableObject, ICondensedLevelConfig
{
    public const string DefaultResourcePath = "配置可编程物体/关卡数据/CondensedLevelConfig";

    [Header("浓缩关开关")]
    [SerializeField] private bool m_isCondensedModeEnabled;

    [Header("关卡等级")]
    [Tooltip("进入战斗时使用关卡配置的 playerLevel，而非战队等级。Debug 模式开启时也会自动启用（见 Datas.ShouldUseLevelConfiguredPlayerLevel）。")]
    [SerializeField] private bool m_useLevelConfiguredPlayerLevel;

    [Header("一号列表（常规关卡）")]
    [SerializeField] private List<LevelSelectionFloorData> m_primaryLevelFloors = new List<LevelSelectionFloorData>();

    [Header("二号列表（浓缩关）")]
    [SerializeField] private List<LevelSelectionFloorData> m_condensedLevelFloors = new List<LevelSelectionFloorData>();

    public bool IsCondensedModeEnabled => m_isCondensedModeEnabled;
    public bool UseLevelConfiguredPlayerLevel => m_useLevelConfiguredPlayerLevel;
    public bool ShouldUseLevelConfiguredPlayerLevel => m_useLevelConfiguredPlayerLevel || m_isCondensedModeEnabled;

    public IReadOnlyList<LevelSelectionFloorData> PrimaryLevelFloors => m_primaryLevelFloors;
    public IReadOnlyList<LevelSelectionFloorData> CondensedLevelFloors => m_condensedLevelFloors;

    public IReadOnlyList<LevelSelectionFloorData> GetActiveLevelFloors()
    {
        return m_isCondensedModeEnabled ? m_condensedLevelFloors : m_primaryLevelFloors;
    }

    public void SetCondensedModeEnabled(bool enabled)
    {
        if (m_isCondensedModeEnabled == enabled)
        {
            return;
        }

        m_isCondensedModeEnabled = enabled;
        if (enabled)
        {
            m_useLevelConfiguredPlayerLevel = true;
        }

        NotifyLevelFloorsChanged();
    }

    public void SetUseLevelConfiguredPlayerLevel(bool enabled)
    {
        if (m_useLevelConfiguredPlayerLevel == enabled)
        {
            return;
        }

        m_useLevelConfiguredPlayerLevel = enabled;
    }

    public static CondensedLevelConfig LoadDefaultAsset()
    {
        return Resources.Load<CondensedLevelConfig>(DefaultResourcePath);
    }

    private void NotifyLevelFloorsChanged()
    {
        if (Datas.Instance != null)
        {
            Datas.Instance.RefreshLevelFloorsFromConfig();
        }
    }
}
