using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class CommandButtonManager : MonoBehaviour
{
    public static CommandButtonManager Instance { get; private set; }
    //[SerializeField] private List<SkillBase> commandSkills;
    public List<CommandButton> commandButtons;
    public RectTransform buttonContainer;
    [Header("动画相关参数")]
    public float buttonMoveSpacing = 0.1f; // 按钮移动的间隔时间
    public float buttonMoveDistance = 50f; // 按钮移动的距离
    public float fadeDuration = 0.25f; // 单个按钮淡入/淡出时间

    private List<Vector2> m_initialAnchoredPositions;//所有按钮的起始位置(显示的位置)

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;    
    }

    void Start()
    {
        CacheInitialPositions();
        ResetButtonsImmediate(false);
    }
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {

    }
    public Coroutine FadeInButtons(Character character)
    {
        InitialButtons(character);
        return StartCoroutine(FadeInButtonsAnim());
    }
    public Coroutine FadeOutButtons()
    {
        return StartCoroutine(FadeOutButtonsAnim());
    }
    private IEnumerator FadeInButtonsAnim()
    {
        if (commandButtons == null || commandButtons.Count == 0)
            yield break;


        int activeButtonIndex = 0;
        for (int i = 0; i < commandButtons.Count; i++)
        {
            var button = commandButtons[i];
            if (button == null)
                continue;

            var rect = button.GetComponent<RectTransform>();
            var canvasGroup = GetOrAddCanvasGroup(button);
            canvasGroup.interactable = button.HasSkill;
            canvasGroup.blocksRaycasts = button.HasSkill;
            canvasGroup.alpha = 0;
            //如果按钮没有对应技能，则直接设置为不可见并跳过动画
            if (!button.HasSkill)
            {
                if (rect != null)
                {
                    rect.anchoredPosition = m_initialAnchoredPositions[i] + Vector2.down * buttonMoveDistance;
                }
                button.PlayDeselectAnimation(true);
                continue;
            }
            //获取动画延迟
            float delay = buttonMoveSpacing * activeButtonIndex;
            //将按钮移动到初始位置并淡入显示
            if (rect != null)
            {
                rect.anchoredPosition = m_initialAnchoredPositions[i] + Vector2.down * buttonMoveDistance;
                rect.DOAnchorPos(m_initialAnchoredPositions[i], fadeDuration)
                    .SetDelay(delay)
                    .SetEase(Ease.OutQuad);
            }

            canvasGroup.DOFade(1f, fadeDuration)
                .SetDelay(delay)
                .SetEase(Ease.Linear);
            button.PlayDeselectAnimation(true);
            //记录激活的按钮数量
            activeButtonIndex++;
        }

        if (activeButtonIndex == 0)
        {
            yield break;
        }
        //根据按钮数量推迟协程结束时间，确保所有动画完成后再结束
        yield return new WaitForSeconds(buttonMoveSpacing * activeButtonIndex + fadeDuration);

    }


    private IEnumerator FadeOutButtonsAnim()
    {
        if (commandButtons == null || commandButtons.Count == 0)
            yield break;

        int activeButtonIndex = 0;
        for (int i = 0; i < commandButtons.Count; i++)
        {
            var button = commandButtons[i];
            if (button == null)
                continue;

            var rect = button.GetComponent<RectTransform>();
            var canvasGroup = GetOrAddCanvasGroup(button);

            if (!button.HasSkill)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                button.PlayDeselectAnimation(true);
                continue;
            }

            float delay = buttonMoveSpacing * activeButtonIndex;

            if (rect != null)
            {
                rect.DOAnchorPos(rect.anchoredPosition + Vector2.down * buttonMoveDistance, fadeDuration)
                    .SetDelay(delay)
                    .SetEase(Ease.InQuad);
            }

            canvasGroup.DOFade(0f, fadeDuration)
                .SetDelay(delay)
                .SetEase(Ease.Linear);
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            button.PlayDeselectAnimation();

            activeButtonIndex++;
        }

        if (activeButtonIndex == 0)
        {
            yield break;
        }

        yield return new WaitForSeconds(buttonMoveSpacing * activeButtonIndex + fadeDuration);
    }

    private void InitialButtons(Character character)
    {
        if (commandButtons == null)
            return;

        for (int i = 0; i < commandButtons.Count; i++)
        {
            var button = commandButtons[i];
            if (button == null)
                continue;

            CharacterSkillBase skill = null;
            if (character != null && character.skills != null && i < character.skills.Count)
            {
                skill = character.GetSkillInstance(character.skills[i]);
            }

            button.BindSkill(character, skill);
        }

        if (character == null || buttonContainer == null)
            return;

        var worldCamera = Camera.main;
        if (worldCamera == null)
            return;

        var screenPos = worldCamera.WorldToScreenPoint(character.transform.position);
        var parentRect = buttonContainer.parent as RectTransform;
        if (parentRect == null)
            return;

        var canvas = buttonContainer.GetComponentInParent<Canvas>();
        Camera uiCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = canvas.worldCamera != null ? canvas.worldCamera : worldCamera;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPos, uiCamera, out Vector2 localPoint);
        buttonContainer.anchoredPosition = localPoint;
    }
    /*private void ConfigureCommandButtons()
    {
        if (commandButtons == null)
            return;

        for (int i = 0; i < Mathf.Min(commandButtons.Count, commandSkills.Count); i++)
        {
            var button = commandButtons[i];
            if (button == null)
                continue;

            button.BindSkill(null,commandSkills[i]);
        }
    }*/

    private void CacheInitialPositions()//记录所有按钮的初始位置
    {
        if (commandButtons == null)
            return;

        if (m_initialAnchoredPositions == null)
            m_initialAnchoredPositions = new List<Vector2>(commandButtons.Count);

        m_initialAnchoredPositions.Clear();

        foreach (var button in commandButtons)
        {
            if (button == null)
            {
                m_initialAnchoredPositions.Add(Vector2.zero);
                continue;
            }

            var rect = button.GetComponent<RectTransform>();
            m_initialAnchoredPositions.Add(rect != null ? rect.anchoredPosition : Vector2.zero);
        }
    }

    private CanvasGroup GetOrAddCanvasGroup(CommandButton button)
    {
        var group = button.GetComponent<CanvasGroup>();
        if (group == null)
            group = button.gameObject.AddComponent<CanvasGroup>();
        return group;
    }

    private void ResetButtonsImmediate(bool visible)
    {
        if (commandButtons == null)
            return;

        for (int i = 0; i < commandButtons.Count; i++)
        {
            var button = commandButtons[i];
            if (button == null)
                continue;

            var rect = button.GetComponent<RectTransform>();
            var canvasGroup = GetOrAddCanvasGroup(button);
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible && button.HasSkill;
            canvasGroup.blocksRaycasts = visible && button.HasSkill;

            if (rect != null)
            {
                rect.anchoredPosition = visible
                    ? m_initialAnchoredPositions[i]
                    : m_initialAnchoredPositions[i] + Vector2.down * buttonMoveDistance;
            }

            button.PlayDeselectAnimation(true);
        }

    }
    private int FindNextSelectableIndex(int startIndex, int direction)
    {
        if (commandButtons == null || commandButtons.Count == 0)
            return -1;

        int count = commandButtons.Count;
        int current = Mathf.Clamp(startIndex, -1, count - 1);

        for (int i = 0; i < count; i++)
        {
            current = (current + direction + count) % count;
            var button = commandButtons[current];
            if (button != null && button.HasSkill)
            {
                return current;
            }
        }

        return -1;
    }


}
