using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("图标样式")]
    [SerializeField] private Vector2 iconSize = new Vector2(32f, 32f);
    [SerializeField] private float iconSpacing = 2f;
    [SerializeField] private Color iconColor = Color.white;
    [SerializeField] private Sprite defaultIcon;

    [Header("刷新")]
    [SerializeField, Min(0.02f)] private float refreshInterval = 0.1f;

    private readonly List<UnitBinding> bindings = new List<UnitBinding>();
    private readonly Dictionary<UnitCombatant, UnitBinding> bindingMap = new Dictionary<UnitCombatant, UnitBinding>();

    private Canvas rootCanvas;
    private float nextUnitListRefreshTime;

    private sealed class UnitBinding
    {
        public UnitCombatant Unit;
        public RectTransform RootRect;
        public HorizontalLayoutGroup LayoutGroup;
        public readonly List<StateIconBinding> IconBindings = new List<StateIconBinding>();
        public float NextRefreshTime;
    }

    private sealed class StateIconBinding
    {
        public RectTransform RectTransform;
        public Image Image;
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
            if (binding == null || binding.Unit == null || binding.RootRect == null)
            {
                continue;
            }

            UpdateRootPosition(binding);

            if (Time.time >= binding.NextRefreshTime)
            {
                binding.NextRefreshTime = Time.time + refreshInterval;
                RefreshIcons(binding);
            }
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < bindings.Count; i++)
        {
            if (bindings[i] != null && bindings[i].RootRect != null)
            {
                Destroy(bindings[i].RootRect.gameObject);
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
            UpdateRootPosition(bindings[i]);
            RefreshIcons(bindings[i]);
        }
    }

    private void SyncBindingsWithTrackedUnits()
    {
        for (int i = bindings.Count - 1; i >= 0; i--)
        {
            UnitBinding binding = bindings[i];
            if (binding == null || binding.Unit == null || !trackedUnits.Contains(binding.Unit))
            {
                if (binding != null && binding.RootRect != null)
                {
                    Destroy(binding.RootRect.gameObject);
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

        GameObject rootObject = new GameObject($"StateIconUI_{unit.name}");
        RectTransform rootRect = rootObject.AddComponent<RectTransform>();
        rootRect.SetParent(parentCanvasRect, false);

        HorizontalLayoutGroup layoutGroup = rootObject.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = iconSpacing;

        ContentSizeFitter sizeFitter = rootObject.AddComponent<ContentSizeFitter>();
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return new UnitBinding
        {
            Unit = unit,
            RootRect = rootRect,
            LayoutGroup = layoutGroup,
            NextRefreshTime = 0f
        };
    }

    private void UpdateRootPosition(UnitBinding binding)
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
            if (binding.RootRect.gameObject.activeSelf)
            {
                binding.RootRect.gameObject.SetActive(false);
            }
            return;
        }

        if (!binding.RootRect.gameObject.activeSelf)
        {
            binding.RootRect.gameObject.SetActive(true);
        }

        Vector2 localPoint;
        Vector2 finalScreenPosition = new Vector2(screenPosition.x, screenPosition.y) + screenOffset;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentCanvasRect, finalScreenPosition, renderCamera, out localPoint);
        binding.RootRect.anchoredPosition = localPoint;
    }

    private Camera GetRenderCamera()
    {
        if (rootCanvas == null)
        {
            return null;
        }

        return rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
    }

    private void RefreshIcons(UnitBinding binding)
    {
        UnitCombatant unit = binding.Unit;

        // 收集有效状态，跳过 Sprite 为 null 的状态
        List<State> sortedStates = new List<State>();
        if (unit.States != null)
        {
            for (int i = 0; i < unit.States.Count; i++)
            {
                State state = unit.States[i];
                if (state == null)
                {
                    continue;
                }

                if (state.icon == null && defaultIcon == null)
                {
                    continue;
                }

                sortedStates.Add(state);
            }
        }

        // 按 priority 升序排列，priority 相同按 name 排序确保唯一解
        sortedStates.Sort((a, b) =>
        {
            int priorityCompare = a.priority.CompareTo(b.priority);
            if (priorityCompare != 0)
            {
                return priorityCompare;
            }

            return string.CompareOrdinal(a.name, b.name);
        });

        // 确保图标数量匹配
        while (binding.IconBindings.Count < sortedStates.Count)
        {
            CreateIconChild(binding);
        }

        while (binding.IconBindings.Count > sortedStates.Count)
        {
            int lastIndex = binding.IconBindings.Count - 1;
            StateIconBinding removed = binding.IconBindings[lastIndex];
            if (removed.RectTransform != null)
            {
                Destroy(removed.RectTransform.gameObject);
            }

            binding.IconBindings.RemoveAt(lastIndex);
        }

        // 更新图标
        for (int i = 0; i < sortedStates.Count; i++)
        {
            State state = sortedStates[i];
            StateIconBinding iconBinding = binding.IconBindings[i];
            Sprite sprite = state.icon != null ? state.icon : defaultIcon;
            iconBinding.Image.sprite = sprite;
            iconBinding.Image.color = iconColor;
            iconBinding.RectTransform.gameObject.SetActive(true);
        }
    }

    private void CreateIconChild(UnitBinding binding)
    {
        GameObject iconObject = new GameObject($"StateIcon_{binding.IconBindings.Count}");
        RectTransform iconRect = iconObject.AddComponent<RectTransform>();
        iconRect.SetParent(binding.RootRect, false);
        iconRect.sizeDelta = iconSize;

        Image image = iconObject.AddComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;

        binding.IconBindings.Add(new StateIconBinding
        {
            RectTransform = iconRect,
            Image = image
        });
    }
}
