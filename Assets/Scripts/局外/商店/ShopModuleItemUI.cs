using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class ShopModuleItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Color normalBackgroundColor = new Color(0.18f, 0.12f, 0.10f, 0.94f);
    [SerializeField] private Color highlightedBackgroundColor = new Color(0.32f, 0.20f, 0.12f, 0.98f);
    [SerializeField] private Color disabledBackgroundColor = new Color(0.13f, 0.13f, 0.13f, 0.72f);
    [SerializeField] private Color outlineColor = new Color(1f, 0.86f, 0.55f, 0.18f);
    [SerializeField] private Color priceColor = new Color(1f, 0.86f, 0.45f, 1f);
    [SerializeField] private Color unavailablePriceColor = new Color(0.72f, 0.46f, 0.46f, 1f);
    [SerializeField] private float headerHeight = 24f;
    [SerializeField] private float footerHeight = 28f;
    [SerializeField] private float padding = 10f;
    [SerializeField] private float disabledAlpha = 0.6f;

    [Header("可选 UI 引用（可在 Inspector 赋值，不赋值时自动补齐）")]
    [SerializeField] private Image m_background;
    [SerializeField] private Button m_button;
    [SerializeField] private CanvasGroup m_canvasGroup;
    [SerializeField] private TMP_Text m_titleText;
    [SerializeField] private TMP_Text m_priceText;
    [SerializeField] private RectTransform m_shapeRoot;
    [SerializeField] private Outline m_outline;

    private readonly List<Image> m_shapeCells = new List<Image>();
    private readonly List<Vector2Int> m_normalizedCells = new List<Vector2Int>();
    private readonly List<Material> m_cellMaterials = new List<Material>();

    // 以下变量属于运行期状态，只能由脚本初始化和维护。
    private Action<GridModuleDefinition> m_onPurchaseRequested;
    private Action<GridModuleDefinition> m_onPointerEntered;
    private Action m_onPointerExited;
    private GridModuleDefinition m_module;
    private bool m_canBuy;
    private bool m_isSoldOut;

    private void Awake()
    {
        EnsureView();
    }

    public void Bind(
        GridModuleDefinition module,
        int price,
        bool canBuy,
        bool isSoldOut,
        Vector2 drawCellSize,
        Action<GridModuleDefinition> onPurchaseRequested,
        Action<GridModuleDefinition> onPointerEntered,
        Action onPointerExited)
    {
        EnsureView();

        m_module = module;
        m_canBuy = canBuy;
        m_isSoldOut = isSoldOut;
        m_onPurchaseRequested = onPurchaseRequested;
        m_onPointerEntered = onPointerEntered;
        m_onPointerExited = onPointerExited;

        bool interactable = module != null && canBuy && !isSoldOut;
        m_background.color = isSoldOut || !canBuy ? disabledBackgroundColor : (interactable ? highlightedBackgroundColor : normalBackgroundColor);
        m_canvasGroup.alpha = interactable ? 1f : disabledAlpha;
        m_button.interactable = interactable;

        m_titleText.text = module != null ? module.moduleName : string.Empty;
        m_priceText.text = isSoldOut ? "已售空" : "价格: " + Mathf.Max(0, price);
        m_priceText.color = interactable ? priceColor : unavailablePriceColor;
    
        m_button.onClick.RemoveAllListeners();
        if (interactable)
        {
            m_button.onClick.AddListener(HandlePurchaseClick);
        }

        RedrawShape(drawCellSize);
    }

    private void HandlePurchaseClick()
    {
        if (m_module == null || !m_canBuy || m_isSoldOut)
        {
            return;
        }

        m_onPurchaseRequested?.Invoke(m_module);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (m_module == null)
        {
            return;
        }

        m_onPointerEntered?.Invoke(m_module);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        m_onPointerExited?.Invoke();
    }
#region 确认视图不为空
    private void EnsureView()
    {
        if (m_background == null)
        {
            m_background = GetComponent<Image>();
            if (m_background == null)
            {
                m_background = gameObject.AddComponent<Image>();
            }
        }

        if (m_button == null)
        {
            m_button = GetComponent<Button>();
            if (m_button == null)
            {
                m_button = gameObject.AddComponent<Button>();
            }
        }

        if (m_canvasGroup == null)
        {
            m_canvasGroup = GetComponent<CanvasGroup>();
            if (m_canvasGroup == null)
            {
                m_canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (m_titleText == null)
        {
            GameObject titleObject = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObject.transform.SetParent(transform, false);
            m_titleText = titleObject.GetComponent<TextMeshProUGUI>();
            RectTransform titleRect = m_titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(padding, -headerHeight);
            titleRect.offsetMax = new Vector2(-padding, -padding);
            m_titleText.fontSize = 18f;
            m_titleText.alignment = TextAlignmentOptions.Left;
            m_titleText.color = Color.white;
            m_titleText.enableWordWrapping = false;
        }

        if (m_shapeRoot == null)
        {
            GameObject shapeObject = new GameObject("Shape", typeof(RectTransform));
            shapeObject.transform.SetParent(transform, false);
            m_shapeRoot = shapeObject.GetComponent<RectTransform>();
            m_shapeRoot.anchorMin = new Vector2(0f, 0f);
            m_shapeRoot.anchorMax = new Vector2(1f, 1f);
            m_shapeRoot.pivot = new Vector2(0.5f, 0.5f);
            m_shapeRoot.offsetMin = new Vector2(padding, footerHeight + padding);
            m_shapeRoot.offsetMax = new Vector2(-padding, -headerHeight - padding);
        }

        if (m_priceText == null)
        {
            GameObject priceObject = new GameObject("Price", typeof(RectTransform), typeof(TextMeshProUGUI));
            priceObject.transform.SetParent(transform, false);
            m_priceText = priceObject.GetComponent<TextMeshProUGUI>();
            RectTransform priceRect = m_priceText.GetComponent<RectTransform>();
            priceRect.anchorMin = new Vector2(0f, 0f);
            priceRect.anchorMax = new Vector2(0.62f, 0f);
            priceRect.pivot = new Vector2(0f, 0f);
            priceRect.offsetMin = new Vector2(padding, padding);
            priceRect.offsetMax = new Vector2(-4f, padding + footerHeight);

            m_priceText.fontSize = 16f;
            m_priceText.alignment = TextAlignmentOptions.Left;
            m_priceText.enableWordWrapping = false;
        }

        if (m_outline == null)
        {
            m_outline = GetComponent<Outline>();
            if (m_outline == null)
            {
                m_outline = gameObject.AddComponent<Outline>();
            }
            m_outline.effectDistance = new Vector2(1f, -1f);
            m_outline.effectColor = outlineColor;
        }

    }
#endregion
    private void RedrawShape(Vector2 drawCellSize)
    {
        for (int i = 0; i < m_shapeCells.Count; i++)
        {
            if (m_shapeCells[i] != null)
            {
                Destroy(m_shapeCells[i].gameObject);
            }
        }

        m_shapeCells.Clear();

        for (int i = 0; i < m_cellMaterials.Count; i++)
        {
            if (m_cellMaterials[i] != null)
                Destroy(m_cellMaterials[i]);
        }
        m_cellMaterials.Clear();

        if (m_module == null)
        {
            return;
        }

        m_module.GetNormalizedCells(m_normalizedCells);
        Vector2 moduleCenter = m_module.GetNormalizedCenter();
        float cellStepX = Mathf.Max(1f, drawCellSize.x);
        float cellStepY = Mathf.Max(1f, drawCellSize.y);
        float previewWidth = Mathf.Max(1f, cellStepX - 2f);
        float previewHeight = Mathf.Max(1f, cellStepY - 2f);
        Vector2 cellStep = new Vector2(cellStepX, cellStepY);
        Vector2 cellDrawSize = new Vector2(previewWidth, previewHeight);

        ModuleCellFactory.ComputeShapeBounds(
            m_normalizedCells, moduleCenter, cellStep, cellDrawSize,
            out Vector2 boundsMin, out Vector2 boundsMax);

        Color moduleColor = m_module.color;
        float cellAlpha = m_canBuy && !m_isSoldOut ? 1f : disabledAlpha;

        for (int i = 0; i < m_normalizedCells.Count; i++)
        {
            Vector2Int cell = m_normalizedCells[i];
            ModuleCellFactory.ComputeCellPosition(cell, moduleCenter, cellStep,
                out Vector2 anchoredPos, out Vector2 cellOffset);

            Material createdMaterial;
            GameObject cellObject = ModuleCellFactory.CreateCell(
                m_shapeRoot,
                "Cell",
                cellDrawSize,
                anchoredPos,
                moduleColor,
                cellAlpha,
                ModuleCellConfig.Instance,
                m_module.gradientColorB,
                cellOffset,
                boundsMin,
                boundsMax,
                out createdMaterial);

            if (createdMaterial != null)
            {
                m_cellMaterials.Add(createdMaterial);
            }

            Image cellImage = cellObject.GetComponent<Image>();
            m_shapeCells.Add(cellImage);
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < m_cellMaterials.Count; i++)
        {
            if (m_cellMaterials[i] != null)
                Destroy(m_cellMaterials[i]);
        }
        m_cellMaterials.Clear();
    }
}
