using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class UnitStateTextDisplay : MonoBehaviour
{
    public static UnitStateTextDisplay Instance { get; private set; }
    public IReadOnlyList<UnitCombatant> TrackedUnits => trackedUnits;

    [Header("单位管理")]
    [SerializeField] private List<UnitCombatant> trackedUnits = new List<UnitCombatant>();
    [SerializeField] private bool autoCollectSceneUnits = true;
    [SerializeField, Min(0.1f)] private float unitListRefreshInterval = 0.5f;

    [Header("显示位置")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.2f, 0f);
    [SerializeField] private Vector2 screenOffset = Vector2.zero;
    [SerializeField] private RectTransform parentCanvasRect;

    [Header("文本样式")]
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private float fontSize = 24f;
    [SerializeField] private Vector2 textSize = new Vector2(260f, 120f);
    [SerializeField] private string noStateText = string.Empty;
    [SerializeField] private TMP_FontAsset defaultFont;

    [Header("刷新")]
    [SerializeField, Min(0.02f)] private float refreshInterval = 0.1f;

    private readonly List<UnitBinding> bindings = new List<UnitBinding>();
    private readonly Dictionary<UnitCombatant, UnitBinding> bindingMap = new Dictionary<UnitCombatant, UnitBinding>();

    private Canvas rootCanvas;
    private float nextUnitListRefreshTime;

    private sealed class UnitBinding//UI对象
    {
        public UnitCombatant Unit;
        public RectTransform TextRoot;
        public TextMeshProUGUI TextMesh;
        public float NextRefreshTime;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        TryResolveCanvas();
        ValidateTrackedUnits();
        RebuildBindings();
    }

    private void OnEnable()
    {
        nextUnitListRefreshTime = 0f;
        for (int i = 0; i < bindings.Count; i++)
        {
            bindings[i].NextRefreshTime = 0f;
        }
    }

    private void LateUpdate()
    {
        if (parentCanvasRect == null)
        {
            return;
        }

        if (autoCollectSceneUnits && Time.time >= nextUnitListRefreshTime)
        {
            nextUnitListRefreshTime = Time.time + unitListRefreshInterval;
            CollectSceneUnits();
        }

        ValidateTrackedUnits();
        SyncBindingsWithTrackedUnits();

        for (int i = 0; i < bindings.Count; i++)
        {
            UnitBinding binding = bindings[i];
            if (binding == null || binding.Unit == null || binding.TextRoot == null || binding.TextMesh == null)
            {
                continue;
            }

            UpdateTextPosition(binding);

            if (Time.time >= binding.NextRefreshTime)
            {
                binding.NextRefreshTime = Time.time + refreshInterval;
                RefreshText(binding);
            }
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < bindings.Count; i++)
        {
            if (bindings[i] != null && bindings[i].TextRoot != null)
            {
                Destroy(bindings[i].TextRoot.gameObject);
            }
        }

        bindings.Clear();
        bindingMap.Clear();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void RegisterUnit(UnitCombatant unit)
    {
        if (unit == null)
        {
            return;
        }

        if (!trackedUnits.Contains(unit))
        {
            trackedUnits.Add(unit);
        }
    }

    public void UnregisterUnit(UnitCombatant unit)
    {
        if (unit == null)
        {
            return;
        }

        trackedUnits.Remove(unit);
    }

    private void TryResolveCanvas()
    {
        if (parentCanvasRect != null)
        {
            rootCanvas = parentCanvasRect.GetComponent<Canvas>();
            return;
        }

        rootCanvas = FindObjectOfType<Canvas>();
        if (rootCanvas != null)
        {
            parentCanvasRect = rootCanvas.GetComponent<RectTransform>();
        }
    }

    private void EnsureTextObject()
    {
        if (parentCanvasRect != null)
        {
            return;
        }

        Debug.LogWarning($"[UnitStateTextDisplay] {name} 未找到 Canvas，无法显示状态文本");
    }

    private void ValidateTrackedUnits()
    {
        HashSet<UnitCombatant> seen = new HashSet<UnitCombatant>();
        for (int i = trackedUnits.Count - 1; i >= 0; i--)
        {
            UnitCombatant unit = trackedUnits[i];
            if (unit == null || !seen.Add(unit))
            {
                trackedUnits.RemoveAt(i);
            }
        }
    }

    private void CollectSceneUnits()
    {
        UnitCombatant[] sceneUnits = FindObjectsOfType<UnitCombatant>(true);
        for (int i = 0; i < sceneUnits.Length; i++)
        {
            UnitCombatant unit = sceneUnits[i];
            if (unit != null && !trackedUnits.Contains(unit))
            {
                trackedUnits.Add(unit);
            }
        }
    }

    private void RebuildBindings()
    {
        SyncBindingsWithTrackedUnits();

        for (int i = 0; i < bindings.Count; i++)
        {
            UpdateTextPosition(bindings[i]);
            RefreshText(bindings[i]);
        }
    }

    private void SyncBindingsWithTrackedUnits()
    {
        for (int i = bindings.Count - 1; i >= 0; i--)
        {
            UnitBinding binding = bindings[i];
            if (binding == null || binding.Unit == null || !trackedUnits.Contains(binding.Unit))
            {
                if (binding != null && binding.TextRoot != null)
                {
                    Destroy(binding.TextRoot.gameObject);
                }

                if (binding != null && binding.Unit != null)
                {
                    bindingMap.Remove(binding.Unit);
                }

                bindings.RemoveAt(i);
            }
        }

        for (int i = 0; i < trackedUnits.Count; i++)
        {
            UnitCombatant unit = trackedUnits[i];
            if (unit == null || bindingMap.ContainsKey(unit))
            {
                continue;
            }

            UnitBinding binding = CreateBinding(unit);
            if (binding == null)
            {
                continue;
            }

            bindings.Add(binding);
            bindingMap.Add(unit, binding);
        }
    }

    private UnitBinding CreateBinding(UnitCombatant unit)
    {
        if (parentCanvasRect == null)
        {
            return null;
        }

        GameObject textObject = new GameObject($"StateTextUI_{unit.name}");
        RectTransform textRoot = textObject.AddComponent<RectTransform>();
        textRoot.SetParent(parentCanvasRect, false);
        textRoot.sizeDelta = textSize;

        TextMeshProUGUI textMesh = textObject.AddComponent<TextMeshProUGUI>();
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.verticalAlignment = VerticalAlignmentOptions.Bottom;
        textMesh.fontSize = fontSize;
        textMesh.font = defaultFont;    
        textMesh.color = textColor;
        textMesh.text = string.Empty;
        textMesh.raycastTarget = false;

        return new UnitBinding
        {
            Unit = unit,
            TextRoot = textRoot,
            TextMesh = textMesh,
            NextRefreshTime = 0f
        };
    }

    private void UpdateTextPosition(UnitBinding binding)
    {
        Camera renderCamera = GetRenderCamera();
        Camera worldCamera = Camera.main != null ? Camera.main : renderCamera;
        if (worldCamera == null)
        {
            return;
        }

        Vector3 worldPosition = binding.Unit.transform.position + worldOffset;
        Vector3 screenPosition = worldCamera.WorldToScreenPoint(worldPosition);
        if (screenPosition.z < 0f)
        {
            if (binding.TextRoot.gameObject.activeSelf)
            {
                binding.TextRoot.gameObject.SetActive(false);
            }
            return;
        }

        if (!binding.TextRoot.gameObject.activeSelf)
        {
            binding.TextRoot.gameObject.SetActive(true);
        }

        Vector2 localPoint;
        Vector2 finalScreenPosition = new Vector2(screenPosition.x, screenPosition.y) + screenOffset;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentCanvasRect, finalScreenPosition, renderCamera, out localPoint);
        binding.TextRoot.anchoredPosition = localPoint;
    }

    private Camera GetRenderCamera()
    {
        if (rootCanvas == null)
        {
            return null;
        }

        return rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
    }

    private void RefreshText(UnitBinding binding)
    {
        UnitCombatant unit = binding.Unit;
        if (unit.States == null || unit.States.Count == 0)
        {
            binding.TextMesh.text = noStateText;
            return;
        }

        StringBuilder sb = new StringBuilder(64);
        for (int i = 0; i < unit.States.Count; i++)
        {
            State state = unit.States[i];
            if (state == null)
            {
                continue;
            }

            if (sb.Length > 0)
            {
                sb.Append('\n');
            }

            sb.Append(state.name);
            sb.Append($" Lv.{state.StackCount}");
            sb.Append(" ");
            sb.Append(FormatDuration(state));
        }

        binding.TextMesh.text = sb.ToString();
    }

    private static string FormatDuration(State state)
    {
        switch (state.DurationType)
        {
            case StateDurationType.Turn:
                return $"{state.RemainingTurns}T";
            case StateDurationType.ActionValue:
                return $"{state.RemainingActionValue}AV";
            default:
                return "Special";
        }
    }
}
