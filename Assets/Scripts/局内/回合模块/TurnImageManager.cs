using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public class TurnImageManager : MonoBehaviour
{
    public static TurnImageManager Instance { get; private set; }

    public GameObject turnImagePrefab;
    public RectTransform turnImageContainer;

    [Header("Layout")]
    public Vector2 cellSize = new Vector2(120, 120);
    public Vector2 spacing = new Vector2(0, 10);

    [Header("Animation")]
    public float fadeDuration = 0.25f;
    public float moveDuration = 0.3f;
    public float enterDelay = 0.08f;
    public float highlightScale = 1.2f;
    public float normalScale = 1f;
    public float slideDistance = 80f;
    public Ease moveEase = Ease.OutQuad;
    public Ease fadeEase = Ease.OutCubic;

    [Header("Auto Reorder")]
    public bool autoReorder = false;
    public float reorderInterval = 2f;

    // 使用链表维护回合图像的顺序，方便插入和重新排序
    private readonly LinkedList<TurnImage> turnOrder = new LinkedList<TurnImage>();
    private Coroutine autoReorderCoroutine;
    // 根据 Combatant 快速查找对应的 TurnImage
    private readonly Dictionary<Combatant, TurnImage> imageMap = new Dictionary<Combatant, TurnImage>();
    // 记录重排协程,如果已存在就停止协程
    private Coroutine reorderCoroutine;
    private Sequence currentAnimationSequence;

    private List<TurnImage> removedImages = new List<TurnImage>();
    private List<TurnImage> addedImages = new List<TurnImage>();

    /// 单例初始化：如果已存在另一个实例，则销毁当前对象。
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }


    /// 清理单例引用，避免场景切换后遗留引用。
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private TurnImage CreateTurnImage(Combatant combatant)
    {
        // 实例化回合图像预制体，并设置父级为容器
        var Instance = Instantiate(turnImagePrefab, turnImageContainer, false);
        var turnImage = Instance.GetComponent<TurnImage>();

        // 如果没有找到 TurnImage 组件，说明预制体配置错误，输出提示并销毁实例
        if (turnImage == null)
        {
            Debug.LogError("TurnImageManager: turnImagePrefab does not contain a TurnImage component.");
            Destroy(Instance);
            return null;
        }

        // 绑定角色数据并初始化尺寸、缩放等属性。
        turnImage.combatant = combatant;
        turnImage.Initialize(cellSize, normalScale);
        turnImage.SetTopRightAnchor();

        // 将回合图像添加到链表尾部，并在字典中记录映射关系
        turnOrder.AddLast(turnImage);
        imageMap[combatant] = turnImage;

        return turnImage;
    }

    /// <summary>
    /// 初始化回合图像列表：排序、布局、依次淡入，并高亮首位。
    /// </summary>
    public IEnumerator InitializeTurnImages()
    {
        SyncOrderFromTurnManager(out var addedImages, out var removedImages);
        var targetScales = BuildTargetScales();
        var targetPositions = BuildTargetPositions(targetScales);

        // 初始入场：每个元素从右侧与 0 尺寸进入，再过渡到自己的目标尺寸和目标位置。
        var sequence = DOTween.Sequence();
        float delay = 0;
        foreach (var turnImage in turnOrder)
        {
            turnImage.SetTopRightAnchor();
            float targetScale = targetScales[turnImage];
            Vector2 targetPosition = targetPositions[turnImage];

            turnImage.SetAlpha(0f);
            turnImage.SetLayoutScale(cellSize, 0f);
            turnImage.SetAnchoredPosition(new Vector2(slideDistance, targetPosition.y));

            sequence.Insert(delay, PlaySlideFadeScaleTween(turnImage, targetScale));
            delay += enterDelay;
        }
        yield return sequence.WaitForCompletion();
    }

    public Coroutine Reorder()
    {
        StopCurrentReorder();
        reorderCoroutine = StartCoroutine(ReorderCoroutine());
        return reorderCoroutine;
    }
    private void StopCurrentReorder()
    {
        if (reorderCoroutine != null)
        {
            StopCoroutine(reorderCoroutine);
            reorderCoroutine = null;
        }

        if (currentAnimationSequence != null && currentAnimationSequence.IsActive())
        {
            currentAnimationSequence.Kill();
            currentAnimationSequence = null;
        }
    }

    private IEnumerator ReorderCoroutine()
    {
        yield return null;
        SyncOrderFromTurnManager(out var naddedImages, out var nremovedImages);
        addedImages.AddRange(naddedImages);
        removedImages.AddRange(nremovedImages);

        var targetScales = BuildTargetScales();
        var targetPositions = BuildTargetPositions(targetScales);
        Debug.Log($"targetCounts:{targetPositions.Count}");

        if (currentAnimationSequence != null && currentAnimationSequence.IsActive())
        {
            currentAnimationSequence.Kill();
            currentAnimationSequence = null;
        }

        currentAnimationSequence = DOTween.Sequence();
        float delay = 0;

        if (removedImages.Count > 0)
        {
            currentAnimationSequence.Join(BuildFadeOutSequence(removedImages));
            delay = .1f;
        }

        currentAnimationSequence.Insert(delay, BuildReflowSequence(targetPositions, targetScales));
        delay += .1f;

        if (addedImages.Count > 0)
        {
            currentAnimationSequence.Insert(delay, BuildFadeInSequence(addedImages, targetPositions, targetScales));
        }

        yield return currentAnimationSequence.WaitForCompletion();

        currentAnimationSequence = null;
        reorderCoroutine = null;
    }
    #region 动画工具
    // 读取 TurnManager 的回合链表，并把图像链表同步成完全一致的顺序。
    private void SyncOrderFromTurnManager(out List<TurnImage> addedImages, out List<TurnImage> removedImages)
    {
        addedImages = new List<TurnImage>();
        removedImages = new List<TurnImage>();

        if (TurnManager.Instance == null)
        {
            return;
        }

        var currentOrder = TurnManager.Instance.CurrentTurnOrder.ToList();
        var previousCombatants = new HashSet<Combatant>(imageMap.Keys);
        var latestCombatants = new HashSet<Combatant>(currentOrder);
        // 找出新增与消失的角色。
        foreach (var combatant in latestCombatants.Except(previousCombatants))//对于新增的角色，创建新的回合图像并添加到 addedImages 列表中
        {
            var turnImage = CreateTurnImage(combatant);
            if (turnImage != null)
            {
                addedImages.Add(turnImage);
            }
        }

        foreach (var combatant in previousCombatants.Except(latestCombatants))//对于所有消失的角色，从 imageMap 中找到对应的回合图像，添加到 removedImages 列表中，并从 imageMap 中移除该角色的映射关系
        {
            Debug.Log($"TurnImageManager: Detected removed combatant from turn order: {combatant.name}");
            if (imageMap.TryGetValue(combatant, out var turnImage))
            {
                removedImages.Add(turnImage);
                imageMap.Remove(combatant);
            }
        }
        // 根据 TurnManager 的顺序重建 turnOrder 链表：遍历 TurnManager 的当前回合顺序列表，对于每个角色，如果在 imageMap 中找到对应的回合图像，则添加到 orderedImages 列表中。最后清空 turnOrder 链表，并按照 orderedImages 列表的顺序重新添加回合图像到 turnOrder 链表中。
        var orderedImages = new List<TurnImage>();
        foreach (var combatant in currentOrder)
        {
            if (imageMap.TryGetValue(combatant, out var turnImage))
            {
                orderedImages.Add(turnImage);
            }
        }

        turnOrder.Clear();
        foreach (var turnImage in orderedImages)
        {
            turnOrder.AddLast(turnImage);
        }
    }

    private void PrepareNewTurnImagesForFadeIn(List<TurnImage> addedImages, Dictionary<TurnImage, Vector2> targetPositions)
    {
        foreach (var turnImage in addedImages)
        {
            turnImage.SetTopRightAnchor();
            if (targetPositions.TryGetValue(turnImage, out var targetPosition))
            {
                turnImage.SetAlpha(0f);
                turnImage.SetLayoutScale(cellSize, 0f);
                turnImage.SetAnchoredPosition(new Vector2(-slideDistance, targetPosition.y));
            }
        }
    }
    #region 具体效果构建动画
    private Sequence BuildFadeInSequence(List<TurnImage> addedImages, Dictionary<TurnImage, Vector2> targetPositions, Dictionary<TurnImage, float> targetScales)
    {
        var sequence = DOTween.Sequence();

        if (addedImages == null || addedImages.Count == 0)
        {
            return sequence;
        }

        int c = 0;
        sequence.JoinCallback(() => PrepareNewTurnImagesForFadeIn(addedImages, targetPositions));
        foreach (var turnImage in addedImages)
        {
            Debug.Log($"动画开始:{turnImage.combatant.combatantName}");
            if (!targetScales.TryGetValue(turnImage, out var targetScale))
            {
                targetScale = normalScale;
            }
            sequence.Insert(0.2f + c * enterDelay, TweenAlpha(turnImage, 0f, 1f, moveDuration, fadeEase));
            sequence.Insert(0.2f + c * enterDelay, TweenLayoutScale(turnImage, targetScale, moveDuration, moveEase, 0));
            sequence.Insert(0.2f + c * enterDelay, turnImage.MoveTo(targetPositions[turnImage], moveDuration).SetEase(moveEase));
            c++;
        }
        sequence.AppendCallback(() => Debug.Log($"TurnImageManager: Completed fade-in sequence for {addedImages.Count} added turn images."));
        sequence.AppendCallback(() => addedImages.Clear());
        return sequence;
    }

    private Sequence BuildFadeOutSequence(List<TurnImage> removedImages)
    {
        var sequence = DOTween.Sequence();

        if (removedImages == null || removedImages.Count == 0)
        {
            return sequence;
        }

        int c = 0;
        foreach (var turnImage in removedImages)
        {
            sequence.Join(turnImage.FadeOut(moveDuration).SetDelay(enterDelay * c));
            sequence.Join(turnImage.MoveTo(new Vector2(slideDistance, turnImage.GetAnchoredPosition().y), moveDuration).SetEase(moveEase).SetDelay(enterDelay * c));
            c++;
        }
        sequence.AppendCallback(DestroyRemovedTurnImages);
        return sequence;
    }
    // 当前行动者淡出的同时，其他元素移动与缩放到新顺序。
    private Sequence BuildReflowSequence(
        Dictionary<TurnImage, Vector2> targetPositions,
        Dictionary<TurnImage, float> targetScales)
    {
        var sequence = DOTween.Sequence();

        foreach (var turnImage in turnOrder)
        {
            turnImage.SetTopRightAnchor();
            if (addedImages.Contains(turnImage) || removedImages.Contains(turnImage))
            {
                continue;
            }
            if (targetPositions.TryGetValue(turnImage, out var targetPos))
            {
                sequence.Join(turnImage.MoveTo(targetPos, moveDuration).SetEase(moveEase));
            }

            float targetScale = targetScales.TryGetValue(turnImage, out var scale) ? scale : normalScale;
            sequence.Join(TweenLayoutScale(turnImage, targetScale, moveDuration, moveEase));
        }

        return sequence;
    }
    //初始化专用动画
    private Tween PlaySlideFadeScaleTween(TurnImage turnImage, float targetScale)
    {
        float startAlpha = 0f;
        float endAlpha = 1f;
        float startX = -slideDistance;
        float endX = 0;
        float startScale = turnImage.CurrentLayoutScale;

        turnImage.SetAlpha(startAlpha);
        turnImage.SetLayoutScale(cellSize, startScale);
        turnImage.SetAnchoredPosition(new Vector2(startX, turnImage.GetAnchoredPosition().y));

        var sequence = DOTween.Sequence();
        sequence.Join(TweenAlpha(turnImage, startAlpha, endAlpha, fadeDuration, fadeEase));
        sequence.Join(turnImage.MoveTo(new Vector2(endX, turnImage.GetAnchoredPosition().y), moveDuration).SetEase(moveEase));
        sequence.Join(TweenLayoutScale(turnImage, targetScale, moveDuration, moveEase));


        return sequence;
    }
    #endregion
    private void DestroyRemovedTurnImages()
    {
        foreach (var turnImage in removedImages)
        {
            if (turnImage != null)
            {
                Destroy(turnImage.gameObject);
            }
        }
        removedImages.Clear();
    }

    // 按当前顺序为每个元素计算目标尺寸：首位高亮，其余常规。
    private Dictionary<TurnImage, float> BuildTargetScales()
    {
        var result = new Dictionary<TurnImage, float>();
        bool isFirst = true;
        foreach (var turnImage in turnOrder)
        {
            result[turnImage] = isFirst ? highlightScale : normalScale;
            isFirst = false;
        }

        return result;
    }

    // 根据“目标尺寸”计算每个元素应到达的目标坐标。
    private Dictionary<TurnImage, Vector2> BuildTargetPositions(Dictionary<TurnImage, float> targetScales)
    {
        var result = new Dictionary<TurnImage, Vector2>();
        float currentY = 0f;
        foreach (var turnImage in turnOrder)
        {
            float scale = targetScales.TryGetValue(turnImage, out var value) ? value : normalScale;
            float height = cellSize.y * scale;
            float centerY = -(currentY);
            result[turnImage] = new Vector2(0f, centerY);
            currentY += height + spacing.y;
        }

        return result;
    }


    private Tween TweenLayoutScale(TurnImage turnImage, float targetScale, float duration, Ease ease, float startScale = -1f)
    {
        if (startScale < 0f)
        {
            startScale = turnImage.CurrentLayoutScale;
        }

        turnImage.SetLayoutScale(cellSize, startScale);
        float value = startScale;
        Debug.Log($"targetScale:{targetScale}, startScale:{startScale}, currentScale:{turnImage.GetComponent<RectTransform>().localScale}");
        return DOTween.To(() => value, v =>
        {
            value = v;
            turnImage.SetLayoutScale(cellSize, value);
        }, targetScale, duration).SetEase(ease) ;
    }

    private Tween TweenAlpha(TurnImage turnImage, float from, float to, float duration, Ease ease)
    {
        float value = from;
        turnImage.SetAlpha(from);
        return DOTween.To(() => value, v =>
        {
            value = v;
            turnImage.SetAlpha(value);
        }, to, duration).SetEase(ease);
    }
    #endregion
    #region NotUsed
    /// <summary>
    /// 回合结束后重新排序回合图像：当前图像淡出、插入合适位置、其他图像移动、首位放大、高亮。
    /// </summary>
    /// <param name="combatant">结束回合的角色</param>
    /*   public IEnumerator ReorderAfterTurn(Combatant combatant)
       {
           if (!imageMap.TryGetValue(combatant, out var turnImage))
           {
               yield break;
           }

           // 第一阶段：TurnManager 已经把角色重新插回正确位置，这里只需要把图像顺序同步过来。
           SyncOrderFromTurnManager(out var addedImages, out var removedImages);

           var targetScales = BuildTargetScales();
           var targetPositions = BuildTargetPositions(targetScales);

           if (removedImages.Count > 0)
           {
               yield return FadeOutAndDestroyRemovedTurnImages(removedImages);
           }

           if (addedImages.Count > 0)
           {
               PrepareNewTurnImagesForFadeIn(addedImages, targetPositions, targetScales);
           }

           // 当前行动者淡出的同时，其他元素并行移动到新顺序的位置。
           yield return PlayReflowWithFadeOut(targetPositions, targetScales, turnImage);

           if (addedImages.Count > 0)
           {
               yield return PlayFadeInNewTurnImages(addedImages, targetPositions, targetScales);
           }

           // 第二阶段：当前行动者在新位置从右侧滑入并展开到目标尺寸。
           Vector2 actorTarget = targetPositions[turnImage];
           float actorTargetScale = targetScales.TryGetValue(turnImage, out var scale) ? scale : normalScale;
           turnImage.SetAlpha(0f);
           turnImage.SetLayoutScale(cellSize, 0f);
           turnImage.SetAnchoredPosition(new Vector2(slideDistance, actorTarget.y));
           yield return PlaySlideFadeScaleTo(turnImage, actorTargetScale, false);
       }*/
    #endregion
}
