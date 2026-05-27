using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ShopModuleManager : MonoBehaviour
{
    public enum ShopInitializationMode
    {
        Sequential,
        RandomUnique
    }

    [Serializable]
    public class ShopProductDefinition
    {
        public GridModuleDefinition module;
    }

    private class ShopRuntimeEntry
    {
        public int slotIndex;
        public RectTransform spawnPoint;
        public ShopModuleItemUI itemUI;
        public GridModuleDefinition module;
        public int price;
        public bool soldOut;
    }

    [Header("生成点")]
    [SerializeField] private List<RectTransform> spawnPoints = new List<RectTransform>();

    [Header("商品池")]
    [SerializeField] private List<ShopProductDefinition> productPool = new List<ShopProductDefinition>();
    [SerializeField] private GameObject shopItemPrefab;
    [SerializeField] private ShopInitializationMode initializationMode = ShopInitializationMode.RandomUnique;
    [SerializeField] private int itemsPerRefresh = 3;
    [SerializeField] private bool autoInitializeOnAwake = true;

    [Header("购买配置")]
    [SerializeField] private int pricePerCell = 5;
    [SerializeField] private bool spendCurrencyOnPurchase = true;
    [SerializeField] private bool markItemSoldOutOnPurchase = true;
    [SerializeField] private Vector2 previewMaxCellSize = new Vector2(22f, 22f);
    [SerializeField] private Vector2 previewMinCellSize = new Vector2(12f, 12f);
    [SerializeField] private int previewScaleMaxDimension = 5;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text currencyText;
    [SerializeField] private Button refreshButton;

    private readonly List<ShopRuntimeEntry> m_runtimeEntries = new List<ShopRuntimeEntry>();

    private int m_nextSequentialIndex;

    public event Action<GridModuleDefinition, int, int> ItemPurchased;
    public event Action<GridModuleDefinition, int, int> PurchaseFailed;
    public event Action ShopStateChanged;

    private void Awake()
    {
        ClampConfig();
        BindRefreshButton();

        if (autoInitializeOnAwake)
        {
            InitializeShopItems();
        }
    }

    private void OnDestroy()
    {
        UnbindRefreshButton();
    }

    public void SetSpawnPoints(IReadOnlyList<RectTransform> points)
    {
        spawnPoints.Clear();

        if (points == null)
        {
            return;
        }

        for (int i = 0; i < points.Count; i++)
        {
            if (points[i] != null)
            {
                spawnPoints.Add(points[i]);
            }
        }
    }

    public void SetStatusTextTarget(TMP_Text target)
    {
        statusText = target;
        UpdateCurrencyText();
    }

    public void SetCurrencyTextTarget(TMP_Text target)
    {
        currencyText = target;
        UpdateCurrencyText();
    }

    public void SetRefreshButton(Button button)
    {
        if (refreshButton == button)
        {
            return;
        }

        UnbindRefreshButton();
        refreshButton = button;
        BindRefreshButton();
    }

    public void InitializeShopItems()
    {
        ClampConfig();
        ClearRuntimeEntries();//清除商品

        List<RectTransform> validSpawnPoints = GetValidSpawnPoints();
        List<int> selectedProductIndices = SelectProductIndices();
        int spawnCount = selectedProductIndices.Count;

        for (int i = 0; i < spawnCount; i++)
        {
            RectTransform spawnPoint = validSpawnPoints[i];
            ShopProductDefinition product = productPool[selectedProductIndices[i]];
            if (spawnPoint == null || product == null || product.module == null)
            {
                continue;
            }

            GameObject itemObject;
            if (shopItemPrefab != null)
            {
                itemObject = Instantiate(shopItemPrefab, spawnPoint, false);
                itemObject.name = "ShopItem_" + i;
            }
            else
            {
                itemObject = new GameObject("ShopItem_" + i, typeof(RectTransform), typeof(ShopModuleItemUI));
                itemObject.transform.SetParent(spawnPoint, false);
            }

            ShopModuleItemUI itemUI = itemObject.GetComponent<ShopModuleItemUI>();
            if (itemUI == null)
            {
                itemUI = itemObject.AddComponent<ShopModuleItemUI>();
                Debug.LogWarning("Shop item prefab does not contain ShopModuleItemUI. Component was added automatically.", itemObject);
            }

            GridModuleDefinition runtimeModule = product.module.Clone();

            ShopRuntimeEntry entry = new ShopRuntimeEntry
            {
                slotIndex = i,
                spawnPoint = spawnPoint,
                itemUI = itemUI,
                module = runtimeModule,
                price = CalculateModulePrice(runtimeModule),
                soldOut = false
            };

            m_runtimeEntries.Add(entry);
        }

        RefreshVisualState();
        SetStatusText("商店已初始化");
    }

    public void RefreshCurrentItems()
    {
        InitializeShopItems();
        SetStatusText("当前商品已刷新");
    }

    public bool TryPurchaseItem(int slotIndex)
    {
        int currentCurrency = Datas.Instance.GetGold();
        ShopRuntimeEntry entry = GetRuntimeEntry(slotIndex);
        if (entry == null || entry.module == null)
        {
            return false;
        }

        if (entry.soldOut || currentCurrency < entry.price)
        {
            PurchaseFailed?.Invoke(entry.module, entry.price, slotIndex);
            SetStatusText(entry.soldOut ? "该商品已售罄" : "货币不足，无法购买");
            RefreshVisualState();
            return false;
        }

        if (spendCurrencyOnPurchase)
        {
            currentCurrency = Mathf.Max(0, currentCurrency - entry.price);
        }

        if (ModulePlacementController.Instance != null)
        {
            ModulePlacementController.Instance.AddModuleToInventory(entry.module);
        }

        if (markItemSoldOutOnPurchase)
        {
            entry.soldOut = true;
        }

        ItemPurchased?.Invoke(entry.module, entry.price, slotIndex);
        SetStatusText("购买成功: " + entry.module.moduleName);
        RefreshVisualState();
        return true;
    }

    private void RefreshVisualState()//重新初始化当前商品信息
    {
        for (int i = 0; i < m_runtimeEntries.Count; i++)
        {
            ShopRuntimeEntry entry = m_runtimeEntries[i];
            if (entry == null || entry.itemUI == null)
            {
                continue;
            }

            int slotIndex = entry.slotIndex;
            entry.itemUI.Bind(
                entry.module,
                entry.price,
                Datas.Instance.GetGold() >= entry.price,
                entry.soldOut,
                GetPreviewCellSize(entry.module),
                delegate
                {
                    TryPurchaseItem(slotIndex);
                });
        }

        UpdateCurrencyText();
        ShopStateChanged?.Invoke();
    }

    private List<int> SelectProductIndices(int count = -1)
    {
        if(count==-1)
        count=spawnPoints.Count;
        List<int> indices = new List<int>();
        for(int i=0;i<count;i++)
        {
            int rand=UnityEngine.Random.Range(0, productPool.Count);
            indices.Add(rand);
        }
        return indices;
    }

    private ShopRuntimeEntry GetRuntimeEntry(int slotIndex)
    {
        for (int i = 0; i < m_runtimeEntries.Count; i++)
        {
            if (m_runtimeEntries[i] != null && m_runtimeEntries[i].slotIndex == slotIndex)
            {
                return m_runtimeEntries[i];
            }
        }

        return null;
    }

    private int GetValidSpawnPointCount()
    {
        return GetValidSpawnPoints().Count;
    }

    private List<RectTransform> GetValidSpawnPoints()
    {
        List<RectTransform> validSpawnPoints = new List<RectTransform>();

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            if (spawnPoints[i] != null)
            {
                validSpawnPoints.Add(spawnPoints[i]);
            }
        }

        return validSpawnPoints;
    }

    private Vector2 GetPreviewCellSize(GridModuleDefinition module)
    {
        if (module == null)
        {
            return previewMaxCellSize;
        }

        int maxDimension = Mathf.Max(1, module.GetMaxDimension());
        float t = previewScaleMaxDimension <= 1 ? 1f : Mathf.InverseLerp(1f, previewScaleMaxDimension, maxDimension);
        return Vector2.Lerp(previewMaxCellSize, previewMinCellSize, t);
    }

    private int CalculateModulePrice(GridModuleDefinition module)
    {
        if (module == null)
        {
            return 0;
        }
        List<Vector2Int> m_priceCellBuffer = new List<Vector2Int>();
        module.GetNormalizedCells(m_priceCellBuffer);
        return Mathf.Max(0, m_priceCellBuffer.Count) * pricePerCell;
    }

    private void ClearRuntimeEntries()
    {
        for (int i = 0; i < m_runtimeEntries.Count; i++)
        {
            if (m_runtimeEntries[i] != null && m_runtimeEntries[i].itemUI != null)
            {
                Destroy(m_runtimeEntries[i].itemUI.gameObject);
            }
        }

        m_runtimeEntries.Clear();
    }

    private void ClampConfig()//只是保证所有参数不会低于最低值
    {
        pricePerCell = Mathf.Max(0, pricePerCell);
        itemsPerRefresh = Mathf.Max(0, itemsPerRefresh);
        previewMaxCellSize.x = Mathf.Max(1f, previewMaxCellSize.x);
        previewMaxCellSize.y = Mathf.Max(1f, previewMaxCellSize.y);
        previewMinCellSize.x = Mathf.Max(1f, previewMinCellSize.x);
        previewMinCellSize.y = Mathf.Max(1f, previewMinCellSize.y);
        previewScaleMaxDimension = Mathf.Max(1, previewScaleMaxDimension);
    }

    private void SetStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }

        ShopStateChanged?.Invoke();
    }

    private void UpdateCurrencyText()
    {
        if (currencyText != null)
        {
            currencyText.text = "货币: " + Datas.Instance.GetGold();
        }
    }

    private void BindRefreshButton()
    {
        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(RefreshCurrentItems);
        }
    }

    private void UnbindRefreshButton()
    {
        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveListener(RefreshCurrentItems);
        }
    }
}