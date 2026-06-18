using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public interface IBackpackExpansionShopItem
{
    bool TryPurchase();
}

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class BackpackExpansionShopItem : MonoBehaviour, IBackpackExpansionShopItem, IPointerEnterHandler, IPointerExitHandler
{
    [Header("展示配置")]
    [SerializeField] private ShopModuleManager shopManager;
    [SerializeField] private string itemName = "背包扩容";
    [SerializeField] private string itemDescription = "购买后背包大小加一。最多可购买三次。";
    [SerializeField] private Color normalColor = new Color(0.18f, 0.12f, 0.10f, 0.94f);
    [SerializeField] private Color disabledColor = new Color(0.13f, 0.13f, 0.13f, 0.72f);

    [Header("价格配置")]
    [SerializeField] private int firstUpgradePrice = 150;
    [SerializeField] private int secondUpgradePrice = 200;
    [SerializeField] private int thirdUpgradePrice = 250;
    [SerializeField] private int initialBackpackWidth = 4;
    [SerializeField] private int maxBackpackWidth = 7;

    [Header("UI 引用")]
    [SerializeField] private Image background;
    [SerializeField] private Button button;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text priceText;

    #region 生命周期

    private void Awake()
    {
        EnsureView();
    }

    private void Start()
    {
        RefreshView();
    }
    #endregion

    public bool TryPurchase()
    {
        Datas datas = Datas.Instance;
        if (datas == null)
        {
            return false;
        }

        if (!CanPurchase(datas, out int price))
        {
            return false;
        }

        datas.ModifyGold(-price);
        datas.AddBackpackSlot();
        RefreshView();
        return true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (shopManager == null)
        {
            return;
        }

        shopManager.ShowExternalStatusText(BuildPurchaseEffectDescription());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (shopManager == null)
        {
            return;
        }

        shopManager.ClearExternalStatusText();
    }

    private void RefreshView()
    {
        Datas datas = Datas.Instance;
        bool canPurchase = CanPurchase(datas, out int price);
        bool isSoldOut = IsSoldOut(datas);
        bool interactable = canPurchase && !isSoldOut;

        if (background != null)
        {
            background.color = isSoldOut || !canPurchase ? disabledColor : normalColor;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = interactable ? 1f : 0.6f;
        }

        if (button != null)
        {
            button.interactable = interactable;
            button.onClick.RemoveAllListeners();
            if (interactable)
            {
                button.onClick.AddListener(delegate { TryPurchase(); });
            }
        }

        if (titleText != null)
        {
            titleText.text = itemName;
        }

        if (priceText != null)
        {
            priceText.text = isSoldOut ? "已售空" : "价格: " + Mathf.Max(0, price);
        }
    }

    private void EnsureView()
    {
        if (background == null)
        {
            background = GetComponent<Image>();
            if (background == null)
            {
                background = gameObject.AddComponent<Image>();
            }
        }

        if (button == null)
        {
            button = GetComponent<Button>();
            if (button == null)
            {
                button = gameObject.AddComponent<Button>();
            }
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (titleText == null)
        {
            GameObject titleObject = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObject.transform.SetParent(transform, false);
            titleText = titleObject.GetComponent<TextMeshProUGUI>();
            RectTransform titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(10f, -24f);
            titleRect.offsetMax = new Vector2(-10f, -10f);
            titleText.fontSize = 18f;
            titleText.alignment = TextAlignmentOptions.Left;
            titleText.color = Color.white;
            titleText.enableWordWrapping = false;
        }

        if (priceText == null)
        {
            GameObject priceObject = new GameObject("Price", typeof(RectTransform), typeof(TextMeshProUGUI));
            priceObject.transform.SetParent(transform, false);
            priceText = priceObject.GetComponent<TextMeshProUGUI>();
            RectTransform priceRect = priceText.GetComponent<RectTransform>();
            priceRect.anchorMin = new Vector2(0f, 0f);
            priceRect.anchorMax = new Vector2(0.62f, 0f);
            priceRect.pivot = new Vector2(0f, 0f);
            priceRect.offsetMin = new Vector2(10f, 10f);
            priceRect.offsetMax = new Vector2(-4f, 38f);
            priceText.fontSize = 16f;
            priceText.alignment = TextAlignmentOptions.Left;
            priceText.enableWordWrapping = false;
        }
    }

    private bool CanPurchase(Datas datas, out int price)
    {
        price = GetCurrentPrice(datas);
        if (datas == null || IsSoldOut(datas))
        {
            return false;
        }

        return datas.GetGold() >= price;
    }

    private bool IsSoldOut(Datas datas)
    {
        return datas == null || datas.GetBackpackWidth() >= maxBackpackWidth;
    }

    private int GetCurrentPrice(Datas datas)
    {
        int width = datas != null ? datas.GetBackpackWidth() : initialBackpackWidth;
        if (width <= initialBackpackWidth)
        {
            return Mathf.Max(0, firstUpgradePrice);
        }

        if (width == initialBackpackWidth + 1)
        {
            return Mathf.Max(0, secondUpgradePrice);
        }

        if (width == initialBackpackWidth + 2)
        {
            return Mathf.Max(0, thirdUpgradePrice);
        }

        return 0;
    }

    private string BuildPurchaseEffectDescription()
    {
        Datas datas = Datas.Instance;
        if (datas == null)
        {
            return itemDescription;
        }

        int currentWidth = datas.GetBackpackWidth();
        if (IsSoldOut(datas))
        {
            return "背包已扩容至上限";
        }

        return itemDescription + " 当前效果: " + currentWidth + " -> " + (currentWidth + 1) + "，价格: " + GetCurrentPrice(datas);
    }
}