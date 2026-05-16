using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChaosPointSlotUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private int maxValue = 5;
    [SerializeField] private int currentValue = 0;
    [SerializeField] private Character targetCharacter;

    [Header("Visual")]
    [SerializeField] private Image slotPrefab;
    [SerializeField] private RectTransform slotContainer;
    [SerializeField] private Vector2 slotSize = new Vector2(24f, 24f);

    private readonly List<Image> slotImages = new List<Image>();

    public int MaxValue => maxValue;
    public int CurrentValue => currentValue;
    void Awake() 
    {
        Initialize();
    }
    void Update()
    {
        SetValue(targetCharacter != null ? targetCharacter.ChaosValue : 0);
    }
    #region 数值获取与更新

    public void InitializeTarget(Character character)
    {
        targetCharacter = character;
        SetMaxAndValue(character.MaxChaosValueConst, character.ChaosValue);
    }
    private void SetValue(int newValue)
    {
        currentValue = Mathf.Clamp(newValue, 0, maxValue);
        RefreshUI();
    }
    private void SetMaxAndValue(int newMaxValue, int newValue)
    {
        maxValue = Mathf.Max(0, newMaxValue);
        currentValue = Mathf.Clamp(newValue, 0, maxValue);
        RebuildSlots();
        RefreshUI();
    }

    private void Initialize()
    {
        EnsureContainer();
        EnsureLayoutGroup();
        slotImages.AddRange(slotContainer.GetComponentsInChildren<Image>());
    }
    #endregion 

    #region UI构建与刷新
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
        Debug.Log(slotImages.Count);
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
                slotImages[i].name = "ChaosSlot_" + i;
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
            var go = new GameObject("ChaosSlot_" + index, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(slotContainer, false);
            image = go.GetComponent<Image>();
        }

        var rect = image.rectTransform;
        rect.sizeDelta = slotSize;

        var layoutElement = image.GetComponent<LayoutElement>();
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
            var image = slotImages[i];
            if (image == null)
            {
                continue;
            }

            image.color = currentValue <= i ? Color.white : Color.black;
        }
    }
    #endregion
}
