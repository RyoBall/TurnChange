using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class LevelEnemyEntry
{
    public EnemyRosterData enemyData;
    public int level = 1;

    public string EnemyId => enemyData != null ? enemyData.enemyID : string.Empty;
    public string EnemyName => enemyData != null ? enemyData.enemyName : string.Empty;
}

[Serializable]
public class LevelSelectionData
{
    public string levelId;
    public string levelName;
    public List<LevelEnemyEntry> enemies = new List<LevelEnemyEntry>();
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
        if (preparationPanel != null)
        {
            preparationPanel.OpenWithLevelData(levelData);
            return;
        }

        if (preparationPanelRoot != null)
        {
            preparationPanelRoot.SetActive(true);
        }
    }

    public void SetLevelData(LevelSelectionData data)
    {
        levelData = data ?? new LevelSelectionData();
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