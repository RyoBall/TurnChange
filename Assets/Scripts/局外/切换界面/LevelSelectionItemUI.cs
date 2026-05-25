using System;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    public string waveId = "Wave 1";
    public List<LevelEnemyEntry> enemies = new List<LevelEnemyEntry>();
}

[Serializable]
public class LevelSelectionData//关卡数据
{
    public string levelId;
    public string levelName;
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

[DisallowMultipleComponent]
public class LevelSelectionItemUI : MonoBehaviour
{
    [Header("关卡数据")]
    [SerializeField] private LevelSelectionData levelData = new LevelSelectionData();

    [Header("按钮与目标")]
    [SerializeField] private Button prepareButton;
    [SerializeField] private PreparationPanelView preparationPanel;
    [SerializeField] private GameObject preparationPanelRoot;

    [Header("显示")]
    [SerializeField] private TMP_Text levelNameText;
    [SerializeField] private Text levelNameLegacyText;
    [SerializeField] private TMP_Text completedText;
    [SerializeField] private Text completedLegacyText;

    private bool m_isCompleted;

    public LevelSelectionData LevelData => levelData;

    private void Awake()
    {
        BindButton();
        ResolveTextReferences();
        RefreshView();
    }
    void Start()
    {
        preparationPanel = PreparationPanelView.Instance;
        preparationPanelRoot = PreparationPanelView.Instance?.panelRoot;
        RefreshView();
    }

    private void OnEnable()
    {
        BindButton();
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
        if (m_isCompleted)
        {
            return;
        }

        StartCoroutine(SwitchPanelCoroutine());
    }
    private IEnumerator SwitchPanelCoroutine()
    {
        // 在这里可以添加切换动画或过渡效果
        yield return ScreenTransition.Instance.EnterTransition(); // 等待转场完成
        if (preparationPanel != null)
        {
            preparationPanel.OpenWithLevelData(levelData);
        }
        else if (preparationPanelRoot != null)
        {
            preparationPanelRoot.SetActive(true);
        }
        yield return ScreenTransition.Instance.ExitTransition(); // 等待转场完成
    }

    public void SetLevelData(LevelSelectionData data)
    {
        levelData = data ?? new LevelSelectionData();
        RefreshView();
    }

    public void SetCompletedState(bool isCompleted)
    {
        m_isCompleted = isCompleted;
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

    private void RefreshView()
    {
        string displayName = GetDisplayName();

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
            prepareButton.interactable = !m_isCompleted;
        }

        if (completedText != null)
        {
            completedText.gameObject.SetActive(m_isCompleted);
            if (m_isCompleted)
            {
                completedText.text = "已通过";
            }
        }

        if (completedLegacyText != null)
        {
            completedLegacyText.gameObject.SetActive(m_isCompleted);
            if (m_isCompleted)
            {
                completedLegacyText.text = "已通过";
            }
        }
    }

    private string GetDisplayName()
    {
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