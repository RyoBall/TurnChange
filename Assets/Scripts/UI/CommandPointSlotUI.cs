using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CommandPointSlotUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private int maxValue = 5;
    [SerializeField] private int currentValue = 0;

    [Header("Visual")]
    [SerializeField] private Image slotPrefab;
    [SerializeField] private RectTransform slotContainer;
    [SerializeField] private Vector2 slotSize = new Vector2(24f, 24f);

    private readonly List<Image> slotImages = new List<Image>();

    public int MaxValue => maxValue;
    public int CurrentValue => currentValue;

    private void Awake()
    {
        Initialize();
        RefreshFromCommander();
    }

    private void Update()
    {
        RefreshFromCommander();
    }

    private void RefreshFromCommander()
    {
        Commander commander = Commander.GetInstance();
        SetMaxAndValue(commander.MaxCommandPoints, commander.CommandPoints);
    }

    private void SetMaxAndValue(int newMaxValue, int newValue)
    {
        int clampedMaxValue = Mathf.Max(0, newMaxValue);
        int clampedValue = Mathf.Clamp(newValue, 0, clampedMaxValue);
        bool maxChanged = maxValue != clampedMaxValue;

        maxValue = clampedMaxValue;
        currentValue = clampedValue;

        if (maxChanged)
        {
            RebuildSlots();
        }

        RefreshUI();
    }

    private void Initialize()
    {
        EnsureContainer();
        EnsureLayoutGroup();

        if (slotContainer != null)
        {
            slotImages.AddRange(slotContainer.GetComponentsInChildren<Image>());
        }
    }

    private void EnsureContainer()
    {
        if (slotContainer == null)
        {
            slotContainer = transform as RectTransform;
        }
    }

    private void EnsureLayoutGroup()
    {
        if (slotContainer == null)
        {
            return;
        }

        bool hasHorizontal = slotContainer.GetComponent<HorizontalLayoutGroup>() != null;
        bool hasVertical = slotContainer.GetComponent<VerticalLayoutGroup>() != null;
        if (!hasHorizontal && !hasVertical)
        {
            slotContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
        }
    }

    private void RebuildSlots()
    {
        if (slotContainer == null)
        {
            return;
        }

        slotImages.RemoveAll(item => item == null);
        while (slotImages.Count < maxValue)
        {
            slotImages.Add(CreateSlotImage(slotImages.Count));
        }

        while (slotImages.Count > maxValue)
        {
            int lastIndex = slotImages.Count - 1;
            Image lastImage = slotImages[lastIndex];
            slotImages.RemoveAt(lastIndex);
            if (lastImage != null)
            {
                Destroy(lastImage.gameObject);
            }
        }

        for (int i = 0; i < slotImages.Count; i++)
        {
            if (slotImages[i] != null)
            {
                slotImages[i].name = "CommandPointSlot_" + i;
            }
        }
    }

    private Image CreateSlotImage(int index)
    {
        Image image;
        if (slotPrefab != null)
        {
            image = Instantiate(slotPrefab, slotContainer);
        }
        else
        {
            GameObject go = new GameObject("CommandPointSlot_" + index, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(slotContainer, false);
            image = go.GetComponent<Image>();
        }

        RectTransform rect = image.rectTransform;
        rect.sizeDelta = slotSize;

        LayoutElement layoutElement = image.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = image.gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.preferredWidth = slotSize.x;
        layoutElement.preferredHeight = slotSize.y;

        image.preserveAspect = true;
        return image;
    }

    private void RefreshUI()
    {
        for (int i = 0; i < slotImages.Count; i++)
        {
            Image image = slotImages[i];
            if (image == null)
            {
                continue;
            }

            image.color = currentValue <= i ? Color.black : Color.grey;
        }
    }
}