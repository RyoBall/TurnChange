using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class CommandButtonManager : MonoBehaviour
{
    public static CommandButtonManager Instance { get; private set; }
    public List<CommandButton> commandButtons;

    [Header("动画相关参数")]
    public float buttonMoveSpacing = 0.1f; // 按钮移动的间隔时间
    public float buttonMoveDistance = 50f; // 按钮移动的距离
    public float fadeDuration = 0.25f; // 单个按钮淡入/淡出时间

    private List<Vector2> m_initialAnchoredPositions;//所有按钮的起始位置(显示的位置)
    private int m_selectedIndex = -1;
    private bool m_inputEnabled;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
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
        if (!m_inputEnabled)
            return;

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            MoveSelection(1);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            MoveSelection(-1);
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            TriggerSelectedButton();
        }
    }

    public Coroutine FadeInButtons(Character character)
    {
        ConfigureButtons(character);
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

        m_inputEnabled = false;
        ClearSelection(true);

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

        SelectFirstAvailableButton();
        m_inputEnabled = true;
    }


    private IEnumerator FadeOutButtonsAnim()
    {
        if (commandButtons == null || commandButtons.Count == 0)
            yield break;

        m_inputEnabled = false;

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
        ClearSelection(true);
    }

    private void ConfigureButtons(Character character)
    {
        if (commandButtons == null)
            return;

        for (int i = 0; i < commandButtons.Count; i++)
        {
            var button = commandButtons[i];
            if (button == null)
                continue;

            SkillBase skill = null;
            if (character != null && character.skills != null && i < character.skills.Count)
            {
                skill = character.skills[i];
            }

            button.BindSkill(character, skill);
        }
    }

    public void OnButtonPointerEnter(CommandButton button)
    {
        if (!m_inputEnabled || button == null)
            return;

        int index = commandButtons.IndexOf(button);
        if (index < 0 || !button.HasSkill)
            return;

        SelectIndex(index);
    }

    public void OnButtonPointerExit(CommandButton button)
    {
        if (!m_inputEnabled || button == null)
            return;

        int index = commandButtons.IndexOf(button);
        if (index >= 0 && index == m_selectedIndex)
        {
            button.PlayDeselectAnimation();
            m_selectedIndex = -1;
        }
    }

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

        m_inputEnabled = false;
        m_selectedIndex = -1;
    }

    private void MoveSelection(int direction)
    {
        if (commandButtons == null || commandButtons.Count == 0)
            return;

        int startIndex = m_selectedIndex;
        if (startIndex < 0)
        {
            startIndex = direction > 0 ? -1 : 0;
        }

        int nextIndex = FindNextSelectableIndex(startIndex, direction);
        if (nextIndex >= 0)
        {
            SelectIndex(nextIndex);
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

    private void TriggerSelectedButton()
    {
        if (m_selectedIndex < 0 || m_selectedIndex >= commandButtons.Count)
            return;

        var button = commandButtons[m_selectedIndex];
        if (button == null || !button.HasSkill)
            return;

        button.OnButtonClicked();
    }

    private void SelectFirstAvailableButton()
    {
        int index = FindNextSelectableIndex(-1, 1);
        if (index >= 0)
        {
            SelectIndex(index);
        }
    }

    private void SelectIndex(int index)
    {
        if (index < 0 || index >= commandButtons.Count)
            return;

        if (m_selectedIndex == index)
            return;

        if (m_selectedIndex >= 0 && m_selectedIndex < commandButtons.Count)
        {
            var oldButton = commandButtons[m_selectedIndex];
            oldButton?.PlayDeselectAnimation();
        }

        m_selectedIndex = index;
        var newButton = commandButtons[m_selectedIndex];
        newButton?.PlaySelectAnimation();
    }

    private void ClearSelection(bool immediate)
    {
        if (m_selectedIndex >= 0 && m_selectedIndex < commandButtons.Count)
        {
            var oldButton = commandButtons[m_selectedIndex];
            if (oldButton != null)
            {
                oldButton.PlayDeselectAnimation(immediate);
            }
        }

        m_selectedIndex = -1;
    }
}
