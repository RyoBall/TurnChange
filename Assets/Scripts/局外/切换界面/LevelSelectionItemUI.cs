using System;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum LevelSelectionButtonType
{
    BattleLevel,
    EventLevel,
    NextFloor
}

public enum LevelEventOptionType
{
    None,
    WorshipSpeedGod,
    WorshipPowerGod,
    TakeAllIncenseMoney,
    SwapForProfit,
    CashOutSwap,
    TakeWindingPath,
    TakeBroadRoad
}

[Serializable]
public class LevelEnemyEntry//关卡数据单元
{
    public EnemyRosterData enemyData;
    public int level = 1;
    public bool isChessSeriesEnemy;
    public ChessBossPendingData chessBossData = new ChessBossPendingData();
}

[Serializable]
public class LevelEnemyWaveData//关卡敌人波次
{
    public List<LevelEnemyEntry> enemies = new List<LevelEnemyEntry>();
}
[Serializable]
public class LevelSelectionData//关卡数据
{
    public string levelId;
    public string levelName;
    public bool isUnlocked;
    public LevelSelectionButtonType buttonType;
    public LevelEventData eventData;
    public List<LevelEnemyWaveData> enemyWaves = new List<LevelEnemyWaveData>();
    [Min(0)] public int rewardExperience;
    [Min(0)] public int rewardGold;

    public IReadOnlyList<LevelEnemyWaveData> GetEnemyWaves()
    {
        return enemyWaves != null ? enemyWaves : Array.Empty<LevelEnemyWaveData>();
    }

    public IReadOnlyList<LevelEnemyEntry> GetWaveEnemies(int waveIndex)
    {
        if (enemyWaves == null || waveIndex < 0 || waveIndex >= enemyWaves.Count)
        {
            return Array.Empty<LevelEnemyEntry>();
        }

        LevelEnemyWaveData waveData = enemyWaves[waveIndex];
        if (waveData == null || waveData.enemies == null)
        {
            return Array.Empty<LevelEnemyEntry>();
        }

        return waveData.enemies;
    }
}

[Serializable]
public class LevelSelectionFloorData
{
    public string floorId;
    //public string floorName = "第1层";
    public List<LevelSelectionData> levels = new List<LevelSelectionData>();

    public IReadOnlyList<LevelSelectionData> GetLevels()
    {
        return levels != null ? levels : Array.Empty<LevelSelectionData>();
    }
}

[DisallowMultipleComponent]
public class LevelSelectionItemUI : MonoBehaviour
{
    [Header("关卡数据")]
    [SerializeField] private LevelSelectionData levelData = new LevelSelectionData();
    [SerializeField] private LevelSelectionButtonType levelType;

    [Header("按钮与目标")]
    [SerializeField] private Button prepareButton;
    [SerializeField] private PreparationPanelView preparationPanel;
    [SerializeField] private GameObject preparationPanelRoot;
    [SerializeField] private EventLevelPanelView eventPanel;
    [SerializeField] private LevelSelectionListLoader listLoader;

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
        if (ScreenTransition.Instance != null)
        {
            yield return ScreenTransition.Instance.EnterTransition();
        }

        action?.Invoke();

        if (ScreenTransition.Instance != null)
        {
            yield return ScreenTransition.Instance.ExitTransition();
        }
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
    }

    private void OpenEventPanel()
    {
        if (eventPanel == null)
        {
            Debug.LogWarning("[LevelSelectionItemUI] 缺少 EventLevelPanelView，无法打开事件关卡。", this);
            return;
        }
        if(levelData==null)
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
        levelData = data ?? new LevelSelectionData();
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
            eventPanel = EventLevelPanelView.Instance;
        }

        if (listLoader == null)
        {
            listLoader = GetComponentInParent<LevelSelectionListLoader>(true);
        }
    }

    private void RefreshView()
    {
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