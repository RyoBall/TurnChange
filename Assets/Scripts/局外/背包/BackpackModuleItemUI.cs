using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class BackpackModuleItemUI : MonoBehaviour
{
    [SerializeField] private Color normalBackgroundColor = new Color(0.12f, 0.14f, 0.18f, 0.92f);
    [SerializeField] private Color selectedBackgroundColor = new Color(0.22f, 0.35f, 0.18f, 0.98f);
    [SerializeField] private Color loadedBackgroundColor = new Color(0.12f, 0.14f, 0.18f, 0.55f);
    [SerializeField] private Color outlineColor = new Color(1f, 1f, 1f, 0.15f);
    [SerializeField] private float headerHeight = 24f;
    [SerializeField] private float padding = 10f;
    [SerializeField] private float loadedAlpha = 0.45f;

    private readonly List<Image> m_shapeCells = new List<Image>();

    [SerializeField] private Image m_background;
    [SerializeField] private Button m_button;
    [SerializeField] private CanvasGroup m_canvasGroup;
    [SerializeField] private TMP_Text m_titleText;
    [SerializeField] private RectTransform m_shapeRoot;
    private Action<GridModuleDefinition> m_onClicked;
    private GridModuleDefinition m_module;

    private void Awake()
    {
        EnsureView();
    }

    public void Bind(GridModuleDefinition module, bool selected, bool isLoaded, Vector2 drawCellSize, Action<GridModuleDefinition> onClicked)
    {
        EnsureView();

        m_module = module;
        m_onClicked = onClicked;

        m_titleText.text = module != null ? module.moduleName : string.Empty;
        m_background.color = isLoaded ? loadedBackgroundColor : (selected ? selectedBackgroundColor : normalBackgroundColor);
        m_titleText.alpha = isLoaded ? loadedAlpha : 1f;
        m_button.interactable = !isLoaded;
        m_canvasGroup.alpha = isLoaded ? loadedAlpha : 1f;

        m_button.onClick.RemoveAllListeners();
        if (!isLoaded)
        {
            m_button.onClick.AddListener(HandleClick);
        }

        RedrawShape(drawCellSize, isLoaded);
    }

    private void HandleClick()
    {
        if (m_module == null)
        {
            return;
        }

        m_onClicked?.Invoke(m_module);
    }

    private void EnsureView()
    {
        if (m_background == null)
        {
            m_background = gameObject.GetComponent<Image>();
            if (m_background == null)
            {
                m_background = gameObject.AddComponent<Image>();
            }
        }

        if (m_button == null)
        {
            m_button = gameObject.GetComponent<Button>();
            if (m_button == null)
            {
                m_button = gameObject.AddComponent<Button>();
            }
        }

        if (m_canvasGroup == null)
        {
            m_canvasGroup = gameObject.GetComponent<CanvasGroup>();
            if (m_canvasGroup == null)
            {
                m_canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (m_titleText == null)
        {
            GameObject textObject = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.offsetMin = new Vector2(padding, -headerHeight);
            textRect.offsetMax = new Vector2(-padding, -padding);

            m_titleText = textObject.GetComponent<TextMeshProUGUI>();
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
        }

        m_shapeRoot.anchorMin = new Vector2(0f, 0f);
        m_shapeRoot.anchorMax = new Vector2(1f, 1f);
        m_shapeRoot.pivot = new Vector2(0.5f, 0.5f);
        m_shapeRoot.offsetMin = new Vector2(padding, padding);
        m_shapeRoot.offsetMax = new Vector2(-padding, -headerHeight - padding);

        Outline outline = gameObject.GetComponent<Outline>();
        if (outline == null)
        {
            outline = gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, -1f);
        }

        outline.effectColor = outlineColor;
    }

    private void RedrawShape(Vector2 drawCellSize, bool isLoaded)
    {
        for (int i = 0; i < m_shapeCells.Count; i++)
        {
            if (m_shapeCells[i] != null)
            {
                Destroy(m_shapeCells[i].gameObject);
            }
        }

        m_shapeCells.Clear();

        if (m_module == null)
        {
            return;
        }

        List<Vector2Int> normalizedCells = new List<Vector2Int>();
        m_module.GetNormalizedCells(normalizedCells);
        Vector2 moduleCenter = m_module.GetNormalizedCenter();
        float cellWidth = Mathf.Max(1f, drawCellSize.x);
        float cellHeight = Mathf.Max(1f, drawCellSize.y);
        float previewWidth = Mathf.Max(1f, cellWidth - 2f);
        float previewHeight = Mathf.Max(1f, cellHeight - 2f);

        for (int i = 0; i < normalizedCells.Count; i++)
        {
            Vector2Int cell = normalizedCells[i];
            GameObject cellObject = new GameObject("Cell", typeof(RectTransform), typeof(Image));
            cellObject.transform.SetParent(m_shapeRoot, false);

            RectTransform cellRect = cellObject.GetComponent<RectTransform>();
            cellRect.anchorMin = new Vector2(0.5f, 0.5f);
            cellRect.anchorMax = new Vector2(0.5f, 0.5f);
            cellRect.pivot = new Vector2(0.5f, 0.5f);
            cellRect.sizeDelta = new Vector2(previewWidth, previewHeight);
            cellRect.anchoredPosition = new Vector2(
                (cell.x - moduleCenter.x) * cellWidth,
                -(cell.y - moduleCenter.y) * cellHeight);

            Image cellImage = cellObject.GetComponent<Image>();
            Color cellColor = m_module.color;
            cellColor.a *= isLoaded ? loadedAlpha : 1f;
            cellImage.color = cellColor;
            m_shapeCells.Add(cellImage);
        }
    }
}