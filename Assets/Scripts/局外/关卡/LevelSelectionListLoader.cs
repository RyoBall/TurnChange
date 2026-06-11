using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class LevelSelectionListLoader : MonoBehaviour
{
    public static LevelSelectionListLoader instance;
    [Header("楼层关卡配置")]
    [SerializeField] private List<LevelSelectionFloorData> levelFloors = new List<LevelSelectionFloorData>();

    [Header("UI引用")]
    [SerializeField] private RectTransform itemsRoot;
    [SerializeField] private HorizontalSnapCarousel carousel;

    [Header("生成选项")]
    [SerializeField] private bool populateOnStart = true;
    [SerializeField] private bool disableExtraItems = true;

    private Datas m_dataSource;

    public IReadOnlyList<LevelSelectionFloorData> LevelFloors => levelFloors;

    private void Awake()
    {
        instance=this;
        SubscribeToDataSource();
    }

    private void Start()
    {
        if (!populateOnStart)
        {
            return;
        }

        ApplyLevels();
    }

    private void OnDestroy()
    {
        UnsubscribeFromDataSource();
    }

    public void ApplyLevels()
    {
        ResolveReferences();

        List<LevelSelectionData> sourceLevels = GetSourceLevels();//获取关卡列表

        if (carousel != null)
        {
            carousel.RegenerateItems(sourceLevels.Count);   
            itemsRoot = carousel.ItemsRoot;
        }

        if (itemsRoot == null)
        {
            Debug.LogWarning("[LevelSelectionListLoader] 缺少 itemsRoot，无法应用关卡列表。", this);
            return;
        }

        int appliedCount = 0;
        for (int i = 0; i < itemsRoot.childCount; i++)
        {
            LevelSelectionItemUI item = itemsRoot.GetChild(i).GetComponent<LevelSelectionItemUI>();
            if (item == null)
            {
                continue;
            }

            bool shouldEnable = appliedCount < sourceLevels.Count;
            item.gameObject.SetActive(shouldEnable || !disableExtraItems);

            if (!shouldEnable)
            {
                continue;
            }

            LevelSelectionData levelData = sourceLevels[appliedCount];
            bool isUnlocked = levelData != null && GetPreviousLevel(sourceLevels, appliedCount) == null;
            if (Datas.Instance != null && levelData != null)
            {
                isUnlocked = Datas.Instance.IsLevelUnlocked(levelData.levelId);
                levelData.isUnlocked = isUnlocked;
            }

            bool isCompleted = Datas.Instance != null && Datas.Instance.IsLevelCompleted(levelData != null ? levelData.levelId : string.Empty);
            item.SetLevelData(levelData);
            item.SetUnlockedState(isUnlocked);
            item.SetCompletedState(isCompleted);
            appliedCount++;
        }

        carousel?.RefreshItems();
    }

    private List<LevelSelectionData> GetSourceLevels()
    {
        if (Datas.Instance != null)
        {
            // Debug 模式下返回所有楼层的关卡
            if (DebugMode.Instance != null && DebugMode.Instance.IsDebugMode)
            {
                return GetAllFloorLevels();
            }

            return new List<LevelSelectionData>(Datas.Instance.GetCurrentFloorLevels());
        }

        if (levelFloors == null || levelFloors.Count == 0)
        {
            return new List<LevelSelectionData>();
        }

        LevelSelectionFloorData floorData = levelFloors[0];
        return floorData != null ? new List<LevelSelectionData>(floorData.GetLevels()) : new List<LevelSelectionData>();
    }

    /// <summary>
    /// 获取所有楼层中所有关卡的扁平列表（用于 Debug 模式）
    /// </summary>
    private List<LevelSelectionData> GetAllFloorLevels()
    {
        var allLevels = new List<LevelSelectionData>();
        IReadOnlyList<LevelSelectionFloorData> floors = Datas.Instance.GetLevelFloors();

        if (floors == null)
        {
            return allLevels;
        }

        foreach (LevelSelectionFloorData floor in floors)
        {
            if (floor == null)
            {
                continue;
            }

            IReadOnlyList<LevelSelectionData> floorLevels = floor.GetLevels();
            if (floorLevels == null)
            {
                continue;
            }

            foreach (LevelSelectionData level in floorLevels)
            {
                if (level != null)
                {
                    // Debug 模式强制解锁所有关卡
                    level.isUnlocked = true;
                    allLevels.Add(level);
                }
            }
        }

        return allLevels;
    }

    private void ResolveReferences()
    {
        if (carousel == null)
        {
            carousel = GetComponent<HorizontalSnapCarousel>();
        }

        if (itemsRoot == null)
        {
            itemsRoot = carousel != null ? carousel.ItemsRoot : transform as RectTransform;
        }
    }

    private void SubscribeToDataSource()
    {
        Datas dataSource = Datas.Instance;
        if (m_dataSource == dataSource)
        {
            return;
        }

        UnsubscribeFromDataSource();
        m_dataSource = dataSource;
        if (m_dataSource == null)
        {
            return;
        }

        m_dataSource.LevelCompleted -= RebuildLevelListAfterCompletion;
        m_dataSource.LevelCompleted += RebuildLevelListAfterCompletion;
    }

    private void UnsubscribeFromDataSource()
    {
        if (m_dataSource == null)
        {
            return;
        }

        m_dataSource.LevelCompleted -= RebuildLevelListAfterCompletion;
        m_dataSource = null;
    }

    private void RebuildLevelListAfterCompletion(string levelId)
    {
        if (string.IsNullOrWhiteSpace(levelId))
        {
            return;
        }

        ApplyLevels();
    }

    private static LevelSelectionData GetPreviousLevel(IReadOnlyList<LevelSelectionData> levels, int currentIndex)
    {
        if (levels == null)
        {
            return null;
        }

        for (int i = currentIndex - 1; i >= 0; i--)
        {
            if (levels[i] != null)
            {
                return levels[i];
            }
        }

        return null;
    }
}