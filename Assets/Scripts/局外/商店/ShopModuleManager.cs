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
    [SerializeField] private List<GridModuleDefinition> productPool = new List<GridModuleDefinition>();
    [SerializeField] private GameObject shopItemPrefab;
    [SerializeField] private int itemsPerRefresh = 3;
    [SerializeField] private bool autoInitializeOnAwake = true;

    [Header("购买配置")]
    [SerializeField] private ModulePlacementController backpackController;
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
    private string m_statusMessage = string.Empty;
    private string m_hoverDescription;

    public event Action<GridModuleDefinition, int, int> ItemPurchased;
    /// <summary>购买序体时的静态事件（供教程系统监听）</summary>
    public static event Action<GridModuleDefinition, int, int> ItemPurchasedStatic;
    public event Action<GridModuleDefinition, int, int> PurchaseFailed;
    public event Action ShopStateChanged;

    public int CurrentCurrency => GetCurrentCurrency();

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
        List<GridModuleDefinition> selectedModules = SelectModulesForRefresh(validSpawnPoints.Count);
        int spawnCount = selectedModules.Count;

        for (int i = 0; i < spawnCount; i++)
        {
            RectTransform spawnPoint = validSpawnPoints[i];
            GridModuleDefinition selectedModule = selectedModules[i];
            if (spawnPoint == null || selectedModule == null)
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

            GridModuleDefinition runtimeModule = selectedModule.Clone();

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
        ShopRuntimeEntry entry = GetRuntimeEntry(slotIndex);
        if (entry == null || entry.module == null)
        {
            return false;
        }

        int currentCurrency = GetCurrentCurrency();
        if (entry.soldOut || currentCurrency < entry.price)
        {
            PurchaseFailed?.Invoke(entry.module, entry.price, slotIndex);
            SetStatusText(entry.soldOut ? "该商品已售罄" : "货币不足，无法购买");
            RefreshVisualState();
            return false;
        }

        if (spendCurrencyOnPurchase && Datas.Instance != null)
        {
            Datas.Instance.ModifyGold(-entry.price);
        }

        if (backpackController != null)
        {
            backpackController.AddModuleToInventory(entry.module);
        }
        else if (Datas.Instance != null)
        {
            Datas.Instance.AddOwnedModule(entry.module);
        }

        if (markItemSoldOutOnPurchase)
        {
            entry.soldOut = true;
        }

        ItemPurchased?.Invoke(entry.module, entry.price, slotIndex);//激活事件
        ItemPurchasedStatic?.Invoke(entry.module, entry.price, slotIndex);
        SetStatusText("购买成功: " + entry.module.moduleName);
        RefreshVisualState();
        return true;
    }

    private void RefreshVisualState()//重新初始化当前商品信息
    {
        int currentCurrency = GetCurrentCurrency();

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
                currentCurrency >= entry.price,
                entry.soldOut,
                GetPreviewCellSize(entry.module),
                delegate
                {
                    TryPurchaseItem(slotIndex);
                },
                HandleItemPointerEnter,
                HandleItemPointerExit);
        }

        UpdateCurrencyText();
        ShopStateChanged?.Invoke();
    }

    private List<GridModuleDefinition> SelectModulesForRefresh(int spawnPointCount)
    {
        List<GridModuleDefinition> candidateModules = BuildModuleCandidateList();
        List<GridModuleDefinition> selectedModules = new List<GridModuleDefinition>();
        int targetCount = Mathf.Min(itemsPerRefresh, spawnPointCount, candidateModules.Count);

        for (int i = 0; i < targetCount; i++)
        {
            int selectedIndex = UnityEngine.Random.Range(0, candidateModules.Count);
            selectedModules.Add(candidateModules[selectedIndex]);
            candidateModules.RemoveAt(selectedIndex);
        }

        return selectedModules;
    }

    private List<GridModuleDefinition> BuildModuleCandidateList()
    {
        List<GridModuleDefinition> candidateModules = new List<GridModuleDefinition>();

        for (int i = 0; i < productPool.Count; i++)
        {
            GridModuleDefinition product = productPool[i];
            if (product == null)
            {
                continue;
            }

            candidateModules.Add(product);
        }

        return candidateModules;
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
        return Mathf.Max(0, m_priceCellBuffer.Count) * module.GetPricePerCell();
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
        itemsPerRefresh = Mathf.Max(0, itemsPerRefresh);
        previewMaxCellSize.x = Mathf.Max(1f, previewMaxCellSize.x);
        previewMaxCellSize.y = Mathf.Max(1f, previewMaxCellSize.y);
        previewMinCellSize.x = Mathf.Max(1f, previewMinCellSize.x);
        previewMinCellSize.y = Mathf.Max(1f, previewMinCellSize.y);
        previewScaleMaxDimension = Mathf.Max(1, previewScaleMaxDimension);
    }

    private int GetCurrentCurrency()
    {
        return Datas.Instance != null ? Datas.Instance.GetGold() : 0;
    }

    private void SetStatusText(string message)
    {
        m_statusMessage = message ?? string.Empty;

        if (string.IsNullOrWhiteSpace(m_hoverDescription) && statusText != null)
        {
            statusText.text = m_statusMessage;
        }

        ShopStateChanged?.Invoke();
    }

    private void HandleItemPointerEnter(GridModuleDefinition module)
    {
        m_hoverDescription = GetModuleDescription(module);

        if (statusText != null)
        {
            statusText.text = m_hoverDescription;
        }

        ShopStateChanged?.Invoke();
    }

    private void HandleItemPointerExit()
    {
        m_hoverDescription = null;

        if (statusText != null)
        {
            statusText.text = m_statusMessage;
        }

        ShopStateChanged?.Invoke();
    }

    private static string GetModuleDescription(GridModuleDefinition module)
    {
        if (module == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(module.description))
        {
            return module.description;
        }

        return module.moduleName;
    }

    private void UpdateCurrencyText()
    {
        if (currencyText != null)
        {
            currencyText.text = "货币: " + GetCurrentCurrency();
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