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

        if (carousel != null)
        {
            carousel.RegenerateItems(levels.Count);
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

            bool shouldEnable = appliedCount < levels.Count;
            item.gameObject.SetActive(shouldEnable || !disableExtraItems);

            if (!shouldEnable)
            {
                continue;
            }

            item.SetLevelData(levels[appliedCount]);
            appliedCount++;
        }

        carousel?.RefreshItems();
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