using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class ShopPanelView : MonoBehaviour
{
    [Header("基础引用")]
    [SerializeField] private ShopModuleManager shopManager;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private Button refreshButton;
    [SerializeField] private TMP_Text currencyText;
    [SerializeField] private TMP_Text statusText;

    [Header("面板布局")]
    [SerializeField] private Vector2 panelSize = new Vector2(480f, 180f);
    [SerializeField] private int columns = 3;
    [SerializeField] private int rows = 1;
    [SerializeField] private Vector2 itemSize = new Vector2(150f, 160f);
    [SerializeField] private Color slotBackgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.18f);
    [SerializeField] private bool initializeOnStart = true;

    private readonly List<RectTransform> m_spawnPoints = new List<RectTransform>();

    private void Awake()
    {
        EnsureLayout();
        RebuildSpawnPoints();
    }

    private void Start()
    {
        ConfigureManager();

        if (initializeOnStart && shopManager != null)
        {
            shopManager.InitializeShopItems();
        }
    }

    private void OnRectTransformDimensionsChange()
    {
        EnsureLayout();
    }

    private void EnsureLayout()
    {
        panelSize.x = Mathf.Max(1f, panelSize.x);
        panelSize.y = Mathf.Max(1f, panelSize.y);
        columns = Mathf.Max(1, columns);
        rows = Mathf.Max(1, rows);
        itemSize.x = Mathf.Max(1f, itemSize.x);
        itemSize.y = Mathf.Max(1f, itemSize.y);

        if (contentRoot == null)
        {
            contentRoot = transform as RectTransform;
        }

        contentRoot.anchorMin = new Vector2(0f, 1f);
        contentRoot.anchorMax = new Vector2(0f, 1f);
        contentRoot.pivot = new Vector2(0f, 1f);
        contentRoot.sizeDelta = panelSize;

        GridLayoutGroup gridLayout = contentRoot.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = contentRoot.gameObject.AddComponent<GridLayoutGroup>();
        }

        gridLayout.cellSize = itemSize;
        gridLayout.spacing = GetComputedSpacing();
        gridLayout.padding = new RectOffset(0, 0, 0, 0);
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.childAlignment = TextAnchor.UpperLeft;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = columns;

        ContentSizeFitter fitter = contentRoot.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            DestroyImmediate(fitter);
        }
    }

    private void RebuildSpawnPoints()
    {
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(contentRoot.GetChild(i).gameObject);
        }

        m_spawnPoints.Clear();

        int slotCount = columns * rows;
        for (int i = 0; i < slotCount; i++)
        {
            GameObject slotObject = new GameObject("ShopSlot_" + i, typeof(RectTransform), typeof(Image));
            slotObject.transform.SetParent(contentRoot, false);

            Image slotImage = slotObject.GetComponent<Image>();
            slotImage.color = slotBackgroundColor;
            slotImage.raycastTarget = false;

            RectTransform slotRect = slotObject.GetComponent<RectTransform>();
            m_spawnPoints.Add(slotRect);
        }
    }

    private void ConfigureManager()
    {
        if (shopManager == null)
        {
            return;
        }

        shopManager.SetSpawnPoints(m_spawnPoints);
        shopManager.SetCurrencyTextTarget(currencyText);
        shopManager.SetStatusTextTarget(statusText);
        shopManager.SetRefreshButton(refreshButton);
    }

    private Vector2 GetComputedSpacing()
    {
        float horizontalSpacing = columns <= 1
            ? 0f
            : (panelSize.x - columns * itemSize.x) / (columns - 1);
        float verticalSpacing = rows <= 1
            ? 0f
            : (panelSize.y - rows * itemSize.y) / (rows - 1);

        return new Vector2(horizontalSpacing, verticalSpacing);
    }
}