using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CommandPointSlotUI : MonoBehaviour
{
    public static CommandPointSlotUI Instance { get; private set; }

    [Header("Data")]
    [SerializeField] private int maxValue = 5;
    [SerializeField] private int currentValue = 0;

    [Header("Visual")]
    [SerializeField] private Image slotPrefab;
    [SerializeField] private RectTransform slotContainer;
    [SerializeField] private Vector2 slotSize = new Vector2(24f, 24f);

    [SerializeField] private List<Image> slotImages = new List<Image>();
    private Canvas m_parentCanvas;

    public int MaxValue => maxValue;
    public int CurrentValue => currentValue;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        m_parentCanvas = GetComponentInParent<Canvas>();
        Initialize();
    }
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
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

        maxValue = clampedMaxValue;
        currentValue = clampedValue;

        RefreshUI();
    }

    private void Initialize()
    {
        EnsureContainer();
    }

    private void EnsureContainer()
    {
        if (slotContainer == null)
        {
            slotContainer = transform as RectTransform;
        }
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

            image.color = currentValue <= i ? Color.clear : Color.white;
        }
    }

    /// <summary>
    /// 获取指定索引的指挥点槽位的屏幕坐标（用于飞行动画的目的地）
    /// </summary>
    public Vector3 GetSlotScreenPosition(int slotIndex)
    {
        int targetIndex = Mathf.Clamp(slotIndex, 0, slotImages.Count - 1);
        Image targetImage = slotImages[targetIndex];
        if(targetImage == null)
        {
            return m_parentCanvas.transform.position;
        }
        RectTransform slotRect = targetImage.rectTransform;
        Vector3[] corners = new Vector3[4];
        slotRect.GetWorldCorners(corners);
        // 取四个角的中心点
        return (corners[0] + corners[2]) * 0.5f;
    }

    /// <summary>
    /// 获取最后一个已填充的指挥点槽位的屏幕坐标（动画飞向最新获得的那个槽位）
    /// </summary>
    public Vector3 GetLastFilledSlotScreenPosition()
    {
        // 飞向 currentValue-1 索引的槽位（最新获得的那个）
        int targetIndex = Mathf.Clamp(currentValue, 0, slotImages.Count - 1);
        return GetSlotScreenPosition(targetIndex);
    }
}