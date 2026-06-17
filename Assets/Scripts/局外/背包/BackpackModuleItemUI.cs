using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class BackpackModuleItemUI : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler, IBackpackModuleItem
{
    [SerializeField] private Color normalBackgroundColor = new Color(0.12f, 0.14f, 0.18f, 0.92f);
    [SerializeField] private Color selectedBackgroundColor = new Color(0.22f, 0.35f, 0.18f, 0.98f);
    [SerializeField] private Color loadedBackgroundColor = new Color(0.12f, 0.14f, 0.18f, 0.55f);
    [SerializeField] private Color outlineColor = new Color(1f, 1f, 1f, 0.15f);
    [SerializeField] private float headerHeight = 24f;
    [SerializeField] private float padding = 10f;
    [SerializeField] private float loadedAlpha = 0.45f;

    private readonly List<Image> m_shapeCells = new List<Image>();
    private readonly List<Material> m_cellMaterials = new List<Material>();

    [SerializeField] private Shader m_gradientShader;
    [SerializeField] private Color m_gradientColorB = new Color(0.1f, 0.3f, 0.85f, 0.9f);
    [SerializeField] private float m_gradientAngle = 45f;

    [SerializeField] private Image m_background;
    [SerializeField] private Button m_button;
    [SerializeField] private CanvasGroup m_canvasGroup;
    [SerializeField] private TMP_Text m_titleText;
    [SerializeField] private RectTransform m_shapeRoot;
    private Action<IGridModule> m_onPressed;
    private Action<IGridModule> m_onHovered;
    private Action m_onHoverExited;
    private IGridModule m_module;

    private void Awake()
    {
        EnsureView();
    }

    public void Bind(IGridModule module, bool selected, bool isLoaded, Vector2 drawCellSize, Action<IGridModule> onPressed, Action<IGridModule> onHovered = null, Action onHoverExited = null)
    {
        EnsureView();

        m_module = module;
        m_onPressed = onPressed;
        m_onHovered = onHovered;
        m_onHoverExited = onHoverExited;

        GridModuleDefinition moduleDef = module as GridModuleDefinition;
        m_titleText.text = moduleDef != null ? moduleDef.moduleName : string.Empty;
        m_background.color = isLoaded ? loadedBackgroundColor : (selected ? selectedBackgroundColor : normalBackgroundColor);
        m_titleText.alpha = isLoaded ? loadedAlpha : 1f;
        m_button.interactable = !isLoaded;
        m_canvasGroup.alpha = isLoaded ? loadedAlpha : 1f;

        RedrawShape(drawCellSize, isLoaded);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || m_module == null || m_button == null || !m_button.interactable)
        {
            return;
        }

        m_onPressed?.Invoke(m_module);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (m_module != null)
        {
            m_onHovered?.Invoke(m_module);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        m_onHoverExited?.Invoke();
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
                Destroy(m_shapeCells[i].gameObject);
        }
        m_shapeCells.Clear();

        for (int i = 0; i < m_cellMaterials.Count; i++)
        {
            if (m_cellMaterials[i] != null)
                Destroy(m_cellMaterials[i]);
        }
        m_cellMaterials.Clear();

        if (m_module == null)
            return;

        List<Vector2Int> normalizedCells = new List<Vector2Int>();
        m_module.GetNormalizedCells(normalizedCells);
        Vector2 moduleCenter = m_module.GetNormalizedCenter();
        float cellWidth  = Mathf.Max(1f, drawCellSize.x);
        float cellHeight = Mathf.Max(1f, drawCellSize.y);
        float previewWidth  = Mathf.Max(1f, cellWidth  - 2f);
        float previewHeight = Mathf.Max(1f, cellHeight - 2f);

        // Compute shape bounding box in shapeRoot local space (pixels)
        // anchoredPosition of each cell = (cell.x - centerX)*cellWidth, -(cell.y - centerY)*cellHeight
        // Bounds extend ±half-cell around each anchor.
        float halfW = previewWidth  * 0.5f;
        float halfH = previewHeight * 0.5f;
        float boundsMinX = float.MaxValue, boundsMinY = float.MaxValue;
        float boundsMaxX = float.MinValue, boundsMaxY = float.MinValue;
        for (int i = 0; i < normalizedCells.Count; i++)
        {
            Vector2Int cell = normalizedCells[i];
            float cx =  (cell.x - moduleCenter.x) * cellWidth;
            float cy = -(cell.y - moduleCenter.y) * cellHeight;
            boundsMinX = Mathf.Min(boundsMinX, cx - halfW);
            boundsMinY = Mathf.Min(boundsMinY, cy - halfH);
            boundsMaxX = Mathf.Max(boundsMaxX, cx + halfW);
            boundsMaxY = Mathf.Max(boundsMaxY, cy + halfH);
        }

        GridModuleDefinition moduleDef = m_module as GridModuleDefinition;
        Color colorA = moduleDef != null ? moduleDef.color : Color.white;
        colorA.a *= isLoaded ? loadedAlpha : 1f;

        bool useGradient = m_gradientShader != null;

        for (int i = 0; i < normalizedCells.Count; i++)
        {
            Vector2Int cell = normalizedCells[i];
            GameObject cellObject = new GameObject("Cell", typeof(RectTransform), typeof(Image));
            cellObject.transform.SetParent(m_shapeRoot, false);

            RectTransform cellRect = cellObject.GetComponent<RectTransform>();
            cellRect.anchorMin = new Vector2(0.5f, 0.5f);
            cellRect.anchorMax = new Vector2(0.5f, 0.5f);
            cellRect.pivot     = new Vector2(0.5f, 0.5f);
            cellRect.sizeDelta = new Vector2(previewWidth, previewHeight);
            float ax =  (cell.x - moduleCenter.x) * cellWidth;
            float ay = -(cell.y - moduleCenter.y) * cellHeight;
            cellRect.anchoredPosition = new Vector2(ax, ay);

            Image cellImage = cellObject.GetComponent<Image>();

            if (useGradient)
            {
                // White vertex color — gradient colors are fully in the shader
                cellImage.color = new Color(1f, 1f, 1f, isLoaded ? loadedAlpha : 1f);

                Material mat = new Material(m_gradientShader);
                Color colorB = m_gradientColorB;
                colorB.a = colorA.a;
                mat.SetColor("_ColorA", colorA);
                mat.SetColor("_ColorB", colorB);
                mat.SetFloat("_GradientAngle", m_gradientAngle);
                mat.SetVector("_CellOffset",  new Vector4(ax, ay, 0f, 0f));
                mat.SetVector("_CellSize",    new Vector4(previewWidth, previewHeight, 0f, 0f));
                mat.SetVector("_BoundsMin",   new Vector4(boundsMinX, boundsMinY, 0f, 0f));
                mat.SetVector("_BoundsMax",   new Vector4(boundsMaxX, boundsMaxY, 0f, 0f));
                cellImage.material = mat;
                m_cellMaterials.Add(mat);
            }
            else
            {
                cellImage.color = colorA;
            }

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