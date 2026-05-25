using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class LevelSelectionListLoader : MonoBehaviour
{
    [Header("关卡列表")]
    [SerializeField] private List<LevelSelectionData> levels = new List<LevelSelectionData>();

    [Header("UI引用")]
    [SerializeField] private RectTransform itemsRoot;
    [SerializeField] private HorizontalSnapCarousel carousel;

    [Header("生成选项")]
    [SerializeField] private bool populateOnStart = true;
    [SerializeField] private bool disableExtraItems = true;

    public IReadOnlyList<LevelSelectionData> Levels => levels;

    private void Start()
    {
        if (!populateOnStart)
        {
            return;
        }

        ApplyLevels();
    }

    public void ApplyLevels()
    {
        ResolveReferences();

        List<LevelSelectionData> sourceLevels = GetSourceLevels();

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
            bool isCompleted = Datas.Instance != null && Datas.Instance.IsLevelCompleted(levelData != null ? levelData.levelId : string.Empty);
            item.SetLevelData(levelData);
            item.SetCompletedState(isCompleted);
            appliedCount++;
        }

        carousel?.RefreshItems();
    }

    private List<LevelSelectionData> GetSourceLevels()
    {
        if (Datas.Instance != null)
        {
            Datas.Instance.SetAllLevels(levels);
            return new List<LevelSelectionData>(Datas.Instance.GetAllLevels());
        }

        return levels != null ? new List<LevelSelectionData>(levels) : new List<LevelSelectionData>();
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
}