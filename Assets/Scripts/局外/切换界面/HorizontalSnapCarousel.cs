using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class HorizontalSnapCarousel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
{
    [Header("基础引用")]
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private RectTransform itemsRoot;
    [SerializeField] private RectTransform centerReference;
    [SerializeField] private Canvas targetCanvas;

    [Header("自动生成")]
    [SerializeField] private RectTransform itemPrefab;
    [SerializeField] private Vector2 firstItemAnchoredPosition = Vector2.zero;
    [SerializeField] private float itemSpacing = 180f;
    [SerializeField] private bool clearExistingChildrenOnGenerate = true;

    [Header("拖拽参数")]
    [SerializeField] private float dragSensitivity = 1f;
    [SerializeField] private bool lockVerticalPosition = true;
    [SerializeField] private bool clampHorizontalBounds = true;
    [SerializeField] private float horizontalEdgePadding = 0f;

    [Header("松手惯性")]
    [SerializeField] private bool enableInertia = true;
    [SerializeField] private float inertiaVelocityMultiplier = 1f;
    [SerializeField] private float inertiaDeceleration = 4200f;
    [SerializeField] private float inertiaStopSpeed = 80f;

    [Header("中心缩放")]
    [SerializeField] private float enlargeDistance = 220f;
    [SerializeField] private Vector3 baseItemScale = Vector3.one;
    [SerializeField] private Vector3 maxItemScale = new Vector3(1.25f, 1.25f, 1f);

    [Header("中心透明度")]
    [SerializeField] private float minAlpha = 0.35f;

    [Header("松手吸附")]
    [SerializeField] private float snapSmoothTime = 0.12f;
    [SerializeField] private float snapMaxSpeed = 5000f;

    [Header("滚轮切换")]
    [SerializeField] private bool enableScrollWheelSnap = true;
    [SerializeField] private float scrollWheelThreshold = 0.1f;

    private readonly List<RectTransform> m_Items = new List<RectTransform>();
    private readonly List<CanvasGroup> m_ItemCanvasGroups = new List<CanvasGroup>();

    private RectTransform m_ContentParent;
    private Vector2 m_SnapVelocity;
    private Vector2 m_TargetAnchoredPosition;
    private float m_DragVelocityX;
    private float m_InertiaVelocityX;
    private bool m_IsDragging;
    private bool m_IsInertiaMoving;
    private bool m_IsSnapping;
    private float m_FixedY;
    private float m_MinAnchoredX;
    private float m_MaxAnchoredX;

    public int CurrentSelectedIndex { get; private set; } = -1;

    private void Awake()
    {
        ResolveReferences();
        CacheItems();
        RecalculateHorizontalBounds();
        m_TargetAnchoredPosition = contentRoot != null ? contentRoot.anchoredPosition : Vector2.zero;
        if (contentRoot != null)
        {
            m_FixedY = contentRoot.anchoredPosition.y;
            m_TargetAnchoredPosition = GetClampedAnchoredPosition(m_TargetAnchoredPosition);
            contentRoot.anchoredPosition = GetClampedAnchoredPosition(contentRoot.anchoredPosition);
        }
    }
    private void Update()
    {
        if (contentRoot == null)
        {
            return;
        }

        if (m_IsInertiaMoving && !m_IsDragging)
        {
            UpdateInertia();
        }

        if (m_IsSnapping && !m_IsDragging && !m_IsInertiaMoving)
        {
            float nextX = Mathf.SmoothDamp(
                contentRoot.anchoredPosition.x,
                m_TargetAnchoredPosition.x,
                ref m_SnapVelocity.x,
                snapSmoothTime,
                snapMaxSpeed,
                Time.unscaledDeltaTime);

            float nextY = lockVerticalPosition ? m_FixedY : m_TargetAnchoredPosition.y;
            contentRoot.anchoredPosition = GetClampedAnchoredPosition(new Vector2(nextX, nextY));

            if (Mathf.Abs(contentRoot.anchoredPosition.x - m_TargetAnchoredPosition.x) <= 0.1f)
            {
                contentRoot.anchoredPosition = GetClampedAnchoredPosition(new Vector2(m_TargetAnchoredPosition.x, nextY));
                m_SnapVelocity = Vector2.zero;
                m_IsSnapping = false;
            }
        }

        if (lockVerticalPosition && Mathf.Abs(contentRoot.anchoredPosition.y - m_FixedY) > 0.01f)
        {
            contentRoot.anchoredPosition = new Vector2(contentRoot.anchoredPosition.x, m_FixedY);
        }

        UpdateItemVisuals();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (contentRoot == null)
        {
            return;
        }

        m_IsDragging = true;
        m_IsInertiaMoving = false;
        m_IsSnapping = false;
        m_SnapVelocity = Vector2.zero;
        m_DragVelocityX = 0f;
        m_InertiaVelocityX = 0f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (contentRoot == null)
        {
            return;
        }

        float scaleFactor = targetCanvas != null ? targetCanvas.scaleFactor : 1f;
        float deltaTime = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        float deltaX = eventData.delta.x / Mathf.Max(0.01f, scaleFactor) * dragSensitivity;
        Vector2 currentPosition = contentRoot.anchoredPosition;
        Vector2 nextPosition = currentPosition + new Vector2(deltaX, 0f);

        if (lockVerticalPosition)
        {
            nextPosition.y = m_FixedY;
        }

        Vector2 clampedPosition = GetClampedAnchoredPosition(nextPosition);
        contentRoot.anchoredPosition = clampedPosition;

        float currentVelocity = (clampedPosition.x - currentPosition.x) / deltaTime;
        m_DragVelocityX = Mathf.Lerp(m_DragVelocityX, currentVelocity, 0.35f);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (contentRoot == null)
        {
            return;
        }

        m_IsDragging = false;

        if (ShouldStartInertia())
        {
            StartInertia();
            return;
        }

        SnapToClosestItem();
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (!enableScrollWheelSnap || contentRoot == null || m_Items.Count == 0)
        {
            return;
        }

        float scrollDeltaY = eventData.scrollDelta.y;
        if (Mathf.Abs(scrollDeltaY) < scrollWheelThreshold)
        {
            return;
        }

        int currentIndex = GetCurrentOrClosestActiveItemIndex();
        if (currentIndex < 0)
        {
            return;
        }

        int direction = scrollDeltaY < 0f ? 1 : -1;
        int targetIndex = GetNextActiveItemIndex(currentIndex, direction);
        if (targetIndex < 0 || targetIndex == currentIndex)
        {
            return;
        }

        GameAudioEvents.Raise(GameAudioEventType.LevelScroll, this, null, targetIndex);
        m_IsDragging = false;
        m_IsInertiaMoving = false;
        m_InertiaVelocityX = 0f;
        m_DragVelocityX = 0f;
        m_SnapVelocity = Vector2.zero;
        SnapToItemIndex(targetIndex);
    }
#region Public API
    public RectTransform ItemsRoot
    {
        get
        {
            ResolveReferences();
            return itemsRoot;
        }
    }

    public void RefreshItems()
    {
        ResolveReferences();
        CacheItems();
        RecalculateHorizontalBounds();
        SnapToClosestItemImmediate();
        UpdateItemVisuals();
    }

    public void RegenerateItems(int itemCount)
    {
        ResolveReferences();
        GenerateItems(itemCount);
        CacheItems();
        RecalculateHorizontalBounds();
        SnapToClosestItemImmediate();
        UpdateItemVisuals();
    }
#endregion

    private void ResolveReferences()
    {
        if (contentRoot == null)
        {
            contentRoot = transform as RectTransform;
        }

        if (itemsRoot == null)
        {
            itemsRoot = contentRoot;
        }

        if (contentRoot != null)
        {
            m_ContentParent = contentRoot.parent as RectTransform;
        }

        if (targetCanvas == null)
        {
            targetCanvas = GetComponentInParent<Canvas>();
        }
    }

    private void CacheItems()
    {
        m_Items.Clear();
        m_ItemCanvasGroups.Clear();

        if (itemsRoot == null)
        {
            return;
        }

        for (int i = 0; i < itemsRoot.childCount; i++)
        {
            RectTransform item = itemsRoot.GetChild(i) as RectTransform;
            if (item == null)
            {
                continue;
            }

            m_Items.Add(item);
            m_ItemCanvasGroups.Add(GetOrAddCanvasGroup(item));
            item.localScale = baseItemScale;
        }
    }

    private void GenerateItems(int itemCount)
    {
        if (itemsRoot == null || itemPrefab == null)
        {
            return;
        }

        itemCount = Mathf.Max(0, itemCount);

        if (clearExistingChildrenOnGenerate)
        {
            ClearExistingChildren();
        }

        int existingCount = itemsRoot.childCount;
        for (int i = existingCount; i < itemCount; i++)
        {
            RectTransform item = Instantiate(itemPrefab, itemsRoot, false);
            item.name = itemPrefab.name + "_" + i;
        }

        for (int i = 0; i < itemsRoot.childCount; i++)
        {
            RectTransform item = itemsRoot.GetChild(i) as RectTransform;
            if (item == null)
            {
                continue;
            }

            Vector2 anchoredPosition = firstItemAnchoredPosition + new Vector2(itemSpacing * i, 0f);
            item.anchoredPosition = anchoredPosition;
        }
    }

    private void ClearExistingChildren()
    {
        for (int i = itemsRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = itemsRoot.GetChild(i);
            if (child == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                // 运行时先把旧对象移出容器，避免当前帧仍被 CacheItems 计入并重复叠加缩放。
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    private void SnapToClosestItem()
    {
        if (!TryGetClosestItem(out RectTransform item, out int index))
        {
            return;
        }

        SnapToItem(index, item);
    }

    private void SnapToItemIndex(int index)
    {
        if (index < 0 || index >= m_Items.Count)
        {
            return;
        }

        RectTransform item = m_Items[index];
        if (item == null || !item.gameObject.activeInHierarchy)
        {
            return;
        }

        SnapToItem(index, item);
    }

    private void SnapToItem(int index, RectTransform item)
    {
        if (item == null)
        {
            return;
        }

        CurrentSelectedIndex = index;
        m_TargetAnchoredPosition = GetAnchoredPositionThatCentersItem(item);
        if (lockVerticalPosition)
        {
            m_TargetAnchoredPosition.y = m_FixedY;
        }

        m_TargetAnchoredPosition = GetClampedAnchoredPosition(m_TargetAnchoredPosition);

        m_IsSnapping = true;
    }

    private void SnapToClosestItemImmediate()
    {
        if (contentRoot == null || !TryGetClosestItem(out RectTransform item, out int index))
        {
            return;
        }

        CurrentSelectedIndex = index;
        m_TargetAnchoredPosition = GetAnchoredPositionThatCentersItem(item);
        if (lockVerticalPosition)
        {
            m_TargetAnchoredPosition.y = m_FixedY;
        }

        m_TargetAnchoredPosition = GetClampedAnchoredPosition(m_TargetAnchoredPosition);
        contentRoot.anchoredPosition = m_TargetAnchoredPosition;
        m_IsSnapping = false;
        m_SnapVelocity = Vector2.zero;
    }

    private void RecalculateHorizontalBounds()
    {
        if (!clampHorizontalBounds || contentRoot == null || m_ContentParent == null || m_Items.Count == 0)
        {
            m_MinAnchoredX = float.NegativeInfinity;
            m_MaxAnchoredX = float.PositiveInfinity;
            return;
        }

        RectTransform firstItem = GetFirstActiveItem();
        RectTransform lastItem = GetLastActiveItem();
        if (firstItem == null || lastItem == null)
        {
            m_MinAnchoredX = float.NegativeInfinity;
            m_MaxAnchoredX = float.PositiveInfinity;
            return;
        }

        float centeredFirstX = GetAnchoredPositionThatCentersItem(firstItem).x + horizontalEdgePadding;
        float centeredLastX = GetAnchoredPositionThatCentersItem(lastItem).x - horizontalEdgePadding;

        m_MinAnchoredX = Mathf.Min(centeredLastX, centeredFirstX);
        m_MaxAnchoredX = Mathf.Max(centeredLastX, centeredFirstX);
    }

    private RectTransform GetFirstActiveItem()
    {
        for (int i = 0; i < m_Items.Count; i++)
        {
            RectTransform item = m_Items[i];
            if (item != null && item.gameObject.activeInHierarchy)
            {
                return item;
            }
        }

        return null;
    }

    private RectTransform GetLastActiveItem()
    {
        for (int i = m_Items.Count - 1; i >= 0; i--)
        {
            RectTransform item = m_Items[i];
            if (item != null && item.gameObject.activeInHierarchy)
            {
                return item;
            }
        }

        return null;
    }

    private Vector2 GetClampedAnchoredPosition(Vector2 anchoredPosition)
    {
        if (!clampHorizontalBounds)
        {
            return anchoredPosition;
        }

        anchoredPosition.x = Mathf.Clamp(anchoredPosition.x, m_MinAnchoredX, m_MaxAnchoredX);
        return anchoredPosition;
    }

    private bool ShouldStartInertia()
    {
        return enableInertia && Mathf.Abs(m_DragVelocityX) > inertiaStopSpeed;
    }

    private void StartInertia()
    {
        m_IsInertiaMoving = true;
        m_IsSnapping = false;
        m_SnapVelocity = Vector2.zero;
        m_InertiaVelocityX = m_DragVelocityX * inertiaVelocityMultiplier;
    }

    private void UpdateInertia()
    {
        float deltaTime = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        Vector2 currentPosition = contentRoot.anchoredPosition;
        float nextX = currentPosition.x + m_InertiaVelocityX * deltaTime;
        float nextY = lockVerticalPosition ? m_FixedY : currentPosition.y;
        Vector2 clampedPosition = GetClampedAnchoredPosition(new Vector2(nextX, nextY));
        contentRoot.anchoredPosition = clampedPosition;

        bool hitHorizontalBounds = !Mathf.Approximately(clampedPosition.x, nextX);
        m_InertiaVelocityX = Mathf.MoveTowards(m_InertiaVelocityX, 0f, inertiaDeceleration * deltaTime);

        if (hitHorizontalBounds || Mathf.Abs(m_InertiaVelocityX) <= inertiaStopSpeed)
        {
            m_IsInertiaMoving = false;
            m_InertiaVelocityX = 0f;
            m_DragVelocityX = 0f;
            SnapToClosestItem();
        }
    }

    private bool TryGetClosestItem(out RectTransform closestItem, out int closestIndex)
    {
        closestItem = null;
        closestIndex = -1;

        if (m_Items.Count == 0)
        {
            return false;
        }

        Vector2 centerScreenPosition = GetCenterScreenPosition();
        Camera eventCamera = GetEventCamera();
        float closestDistance = float.MaxValue;

        for (int i = 0; i < m_Items.Count; i++)
        {
            RectTransform item = m_Items[i];
            if (item == null || !item.gameObject.activeInHierarchy)
            {
                continue;
            }

            Vector2 itemScreenPosition = RectTransformUtility.WorldToScreenPoint(eventCamera, item.position);
            float distance = Mathf.Abs(itemScreenPosition.x - centerScreenPosition.x);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestItem = item;
                closestIndex = i;
            }
        }

        return closestItem != null;
    }

    private int GetCurrentOrClosestActiveItemIndex()
    {
        if (CurrentSelectedIndex >= 0 && CurrentSelectedIndex < m_Items.Count)
        {
            RectTransform currentItem = m_Items[CurrentSelectedIndex];
            if (currentItem != null && currentItem.gameObject.activeInHierarchy)
            {
                return CurrentSelectedIndex;
            }
        }

        return TryGetClosestItem(out _, out int closestIndex) ? closestIndex : -1;
    }

    private int GetNextActiveItemIndex(int startIndex, int direction)
    {
        int index = startIndex + direction;
        while (index >= 0 && index < m_Items.Count)
        {
            RectTransform item = m_Items[index];
            if (item != null && item.gameObject.activeInHierarchy)
            {
                return index;
            }

            index += direction;
        }

        return startIndex;
    }

    private Vector2 GetAnchoredPositionThatCentersItem(RectTransform item)
    {
        if (contentRoot == null || m_ContentParent == null || item == null)
        {
            return contentRoot != null ? contentRoot.anchoredPosition : Vector2.zero;
        }

        Vector2 centerScreenPosition = GetCenterScreenPosition();
        Camera eventCamera = GetEventCamera();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(m_ContentParent, centerScreenPosition, eventCamera, out Vector2 centerLocalPoint);

        Vector3 itemLocalInParent = m_ContentParent.InverseTransformPoint(item.position);
        Vector2 offset = centerLocalPoint - new Vector2(itemLocalInParent.x, itemLocalInParent.y);
        return contentRoot.anchoredPosition + new Vector2(offset.x, lockVerticalPosition ? 0f : offset.y);
    }

    private void UpdateItemVisuals()
    {
        if (m_Items.Count == 0)
        {
            return;
        }

        Vector2 centerScreenPosition = GetCenterScreenPosition();
        Camera eventCamera = GetEventCamera();
        float validDistance = Mathf.Max(1f, enlargeDistance);

        for (int i = 0; i < m_Items.Count; i++)
        {
            RectTransform item = m_Items[i];
            if (item == null)
            {
                continue;
            }

            Vector2 itemScreenPosition = RectTransformUtility.WorldToScreenPoint(eventCamera, item.position);
            float distance = Mathf.Abs(itemScreenPosition.x - centerScreenPosition.x);
            float t = 1f - Mathf.Clamp01(distance / validDistance);
            item.localScale = Vector3.Lerp(baseItemScale, maxItemScale, t);

            CanvasGroup canvasGroup = i < m_ItemCanvasGroups.Count ? m_ItemCanvasGroups[i] : null;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(minAlpha, 1f, t);
            }
        }
    }

    private CanvasGroup GetOrAddCanvasGroup(RectTransform item)
    {
        if (item == null)
        {
            return null;
        }

        CanvasGroup canvasGroup = item.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = item.gameObject.AddComponent<CanvasGroup>();
        }

        return canvasGroup;
    }

    private Vector2 GetCenterScreenPosition()
    {
        Camera eventCamera = GetEventCamera();
        if (centerReference != null)
        {
            return RectTransformUtility.WorldToScreenPoint(eventCamera, centerReference.position);
        }

        return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }

    private Camera GetEventCamera()
    {
        if (targetCanvas == null)
        {
            return null;
        }

        if (targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return targetCanvas.worldCamera;
    }

    private void OnValidate()
    {
        itemSpacing = Mathf.Max(0f, itemSpacing);
        dragSensitivity = Mathf.Max(0.01f, dragSensitivity);
        horizontalEdgePadding = Mathf.Max(0f, horizontalEdgePadding);
        inertiaVelocityMultiplier = Mathf.Max(0f, inertiaVelocityMultiplier);
        inertiaDeceleration = Mathf.Max(1f, inertiaDeceleration);
        inertiaStopSpeed = Mathf.Max(1f, inertiaStopSpeed);
        enlargeDistance = Mathf.Max(1f, enlargeDistance);
        maxItemScale.x = Mathf.Max(baseItemScale.x, maxItemScale.x);
        maxItemScale.y = Mathf.Max(baseItemScale.y, maxItemScale.y);
        maxItemScale.z = Mathf.Max(baseItemScale.z, maxItemScale.z);
        minAlpha = Mathf.Clamp01(minAlpha);
        snapSmoothTime = Mathf.Max(0.01f, snapSmoothTime);
        snapMaxSpeed = Mathf.Max(1f, snapMaxSpeed);
        scrollWheelThreshold = Mathf.Max(0.01f, scrollWheelThreshold);
    }
}