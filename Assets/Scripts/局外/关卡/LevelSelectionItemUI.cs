using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LevelSelectionItemUI : MonoBehaviour
{
    public static event Action BattleLevelSelected;
    [Header("关卡数据")]
    private LevelSelectionData levelData;
    private LevelSelectionButtonType levelType;

    [Header("按钮与目标")]
    [SerializeField] private Button prepareButton;
    private PreparationPanelView preparationPanel;
    private GameObject preparationPanelRoot;
    private EventLevelPanelPreview eventPanel;
    private LevelSelectionListLoader listLoader;

    [Header("显示")]
    [SerializeField] private TMP_Text levelNameText;
    [SerializeField] private Text levelNameLegacyText;
    [SerializeField] private TMP_Text completedText;
    [SerializeField] private Text completedLegacyText;

    private bool m_isCompleted;
    private bool m_isUnlocked = true;

    public LevelSelectionData LevelData => levelData;
    public LevelSelectionButtonType LevelType => levelType;

    private void Awake()
    {
        BindButton();
        ResolveTargetReferences();
        ResolveTextReferences();
        RefreshView();
    }

    private void Start()
    {
        ResolveTargetReferences();
        RefreshView();
    }

    private void OnEnable()
    {
        BindButton();
        ResolveTargetReferences();
        ResolveTextReferences();
        RefreshView();
    }

    private void OnDisable()
    {
        if (prepareButton != null)
        {
            prepareButton.onClick.RemoveListener(OnPrepareButtonClicked);
        }
    }

    public void OnPrepareButtonClicked()
    {
        switch (levelType)
        {
            case LevelSelectionButtonType.BattleLevel:
                OpenBattleLevel();
                break;
            case LevelSelectionButtonType.EventLevel:
                OpenEventLevel();
                break;
            case LevelSelectionButtonType.NextFloor:
                EnterNextFloor();
                break;
        }
    }

    private void OpenBattleLevel()
    {
        if (m_isCompleted || !m_isUnlocked)
        {
            return;
        }

        StartCoroutine(ExecuteWithTransition(OpenBattlePreparation));
    }

    private void OpenEventLevel()
    {
        if (!m_isUnlocked)
        {
            return;
        }

        StartCoroutine(ExecuteWithTransition(OpenEventPanel));
    }

    private void EnterNextFloor()
    {
        if (!m_isUnlocked)
        {
            return;
        }

        StartCoroutine(ExecuteWithTransition(AdvanceToNextFloor));
    }

    private IEnumerator ExecuteWithTransition(Action action)
    {
        yield return ScreenTransition.Instance.Transition(action);
    }
    #region  三种关卡进入按钮的函数
    private void OpenBattlePreparation()
    {
        if (preparationPanel != null)
        {
            preparationPanel.OpenWithLevelData(levelData);
        }
        else if (preparationPanelRoot != null)
        {
            preparationPanelRoot.SetActive(true);
        }
        BattleLevelSelected?.Invoke();
    }

    private void OpenEventPanel()
    {
        if (eventPanel == null)
        {
            Debug.LogWarning("[LevelSelectionItemUI] 缺少 EventLevelPanelView，无法打开事件关卡。", this);
            return;
        }
        if (levelData == null)
            Debug.LogWarning("缺少关卡数据");
        eventPanel.OpenWithLevelData(levelData);
    }

    private void AdvanceToNextFloor()
    {
        if (Datas.Instance == null)
        {
            Debug.LogWarning("[LevelSelectionItemUI] Datas.Instance 为空，无法进入下一层。", this);
            return;
        }

        if (!Datas.Instance.AdvanceToNextFloor())
        {
            Debug.LogWarning("[LevelSelectionItemUI] 当前已经是最后一层，无法继续前进。", this);
            return;
        }

        if (listLoader == null)
        {
            listLoader = GetComponentInParent<LevelSelectionListLoader>(true);
        }

        listLoader?.ApplyLevels();
    }
    #endregion
    public void SetLevelData(LevelSelectionData data)
    {
        levelData = data;
        levelType = levelData.buttonType;
        m_isUnlocked = levelData.isUnlocked;
        RefreshView();
    }

    public void SetLevelType(LevelSelectionButtonType buttonType)
    {
        levelType = buttonType;
        if (levelData != null)
        {
            levelData.buttonType = buttonType;
        }

        RefreshView();
    }

    public void SetCompletedState(bool isCompleted)
    {
        m_isCompleted = isCompleted;
        RefreshView();
    }

    public void SetUnlockedState(bool isUnlocked)
    {
        m_isUnlocked = isUnlocked;
        if (levelData != null)
        {
            levelData.isUnlocked = isUnlocked;
        }

        RefreshView();
    }

    private void BindButton()
    {
        if (prepareButton == null)
        {
            prepareButton = GetComponentInChildren<Button>();
        }

        if (prepareButton == null)
        {
            return;
        }

        prepareButton.onClick.RemoveListener(OnPrepareButtonClicked);
        prepareButton.onClick.AddListener(OnPrepareButtonClicked);
    }

    private void ResolveTextReferences()
    {
        if (levelNameText == null)
        {
            levelNameText = GetComponentInChildren<TMP_Text>(true);
        }

        if (levelNameLegacyText == null)
        {
            levelNameLegacyText = GetComponentInChildren<Text>(true);
        }
    }
    //获取引用
    private void ResolveTargetReferences()
    {
        if (preparationPanel == null)
        {
            preparationPanel = PreparationPanelView.Instance;
        }

        if (preparationPanelRoot == null)
        {
            preparationPanelRoot = preparationPanel != null ? preparationPanel.panelRoot : PreparationPanelView.Instance?.panelRoot;
        }

        if (eventPanel == null)
        {
            eventPanel = EventLevelPanelPreview.Instance;
        }

        if (listLoader == null)
        {
            listLoader = GetComponentInParent<LevelSelectionListLoader>(true);
        }
    }

    private void RefreshView()
    {
        if (levelData == null)
        {
            return;
        }

        string displayName = GetDisplayName();
        bool canInteract = m_isUnlocked && !m_isCompleted;
        bool showStatusText = m_isCompleted || !m_isUnlocked;
        string statusTextValue = m_isCompleted ? "已通过" : "未解锁";

        if (levelNameText != null)
        {
            levelNameText.text = displayName;
        }

        if (levelNameLegacyText != null)
        {
            levelNameLegacyText.text = displayName;
        }

        if (prepareButton != null)
        {
            prepareButton.interactable = canInteract;
        }

        if (completedText != null)
        {
            completedText.gameObject.SetActive(showStatusText);
            if (showStatusText)
            {
                completedText.text = statusTextValue;
            }
        }

        if (completedLegacyText != null)
        {
            completedLegacyText.gameObject.SetActive(showStatusText);
            if (showStatusText)
            {
                completedLegacyText.text = statusTextValue;
            }
        }
    }

    private string GetDisplayName()
    {
        if (levelData == null)
        {
            return string.Empty;
        }

        if (levelType == LevelSelectionButtonType.EventLevel)
        {
            if (levelData.eventData != null && !string.IsNullOrWhiteSpace(levelData.eventData.eventName))
            {
                return levelData.eventData.eventName;
            }
        }

        if (levelType == LevelSelectionButtonType.NextFloor && string.IsNullOrWhiteSpace(levelData.levelName))
        {
            return "进入下一层";
        }

        if (!string.IsNullOrWhiteSpace(levelData.levelName))
        {
            return levelData.levelName;
        }

        if (!string.IsNullOrWhiteSpace(levelData.levelId))
        {
            return levelData.levelId;
        }

        return string.Empty;
    }
}