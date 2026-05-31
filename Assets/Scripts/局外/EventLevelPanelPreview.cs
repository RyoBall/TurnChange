using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
[DisallowMultipleComponent]
public class EventLevelPanelView : MonoBehaviour
{
    public static EventLevelPanelView Instance { get; private set; }

    [Header("面板根节点")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private LevelSelectionListLoader levelListLoader;

    [Header("文本")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("三个选项")]
    [SerializeField] private List<Button> optionButtons = new List<Button>(3);
    [SerializeField] private List<TMP_Text> optionTexts = new List<TMP_Text>(3);

    private readonly List<LevelEventOptionData> m_visibleOptions = new List<LevelEventOptionData>(3);
    private LevelSelectionData m_currentLevelData;

    private void Awake()
    {
        Instance = this;

        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        BindOptionButtons();
        Close();
    }

    private void OnEnable()
    {
        BindOptionButtons();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void OpenWithLevelData(LevelSelectionData levelData)
    {
        if (levelData == null)
        {
            Debug.LogWarning("[EventLevelPanelView] 事件关卡数据为空，无法打开面板。", this);
            return;
        }

        m_currentLevelData = levelData;
        ResolveReferences();
        RebuildVisibleOptions();
        RefreshView();
        panelRoot.SetActive(true);
    }

    public void Close()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        panelRoot.SetActive(false);
        m_currentLevelData = null;
        m_visibleOptions.Clear();
    }

    private void BindOptionButtons()
    {
        for (int i = 0; i < optionButtons.Count; i++)
        {
            Button button = optionButtons[i];
            if (button == null)
            {
                continue;
            }

            int optionIndex = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnOptionClicked(optionIndex));
        }
    }

    private void ResolveReferences()
    {
        if (levelListLoader == null)
        {
            levelListLoader = FindObjectOfType<LevelSelectionListLoader>();
        }
    }

    private void RebuildVisibleOptions()
    {
        m_visibleOptions.Clear();

        IReadOnlyList<LevelEventOptionData> options = m_currentLevelData != null && m_currentLevelData.eventData != null
            ? m_currentLevelData.eventData.GetOptions()
            : Array.Empty<LevelEventOptionData>();

        for (int i = 0; i < options.Count; i++)
        {
            LevelEventOptionData option = options[i];
            if (option != null && option.HasContent)
            {
                m_visibleOptions.Add(option);
            }
        }
    }

    private void RefreshView()
    {
        string title = m_currentLevelData != null && m_currentLevelData.eventData != null
            ? m_currentLevelData.eventData.eventName
            : string.Empty;
        string description = m_currentLevelData != null && m_currentLevelData.eventData != null
            ? m_currentLevelData.eventData.eventDescription
            : string.Empty;

        if (titleText != null)
        {
            titleText.text = string.IsNullOrWhiteSpace(title) ? "事件" : title;
        }

        if (descriptionText != null)
        {
            descriptionText.text = string.IsNullOrWhiteSpace(description) ? "暂无事件描述" : description;
        }

        for (int i = 0; i < optionButtons.Count; i++)
        {
            bool shouldShow = i < m_visibleOptions.Count;
            Button button = optionButtons[i];
            if (button != null)
            {
                button.gameObject.SetActive(shouldShow);
            }

            if (!shouldShow)
            {
                continue;
            }

            string optionText = GetOptionDescription(m_visibleOptions[i]);
            if (i < optionTexts.Count && optionTexts[i] != null)
            {
                optionTexts[i].text = optionText;
            }
        }
    }

    private void OnOptionClicked(int optionIndex)
    {
        if (optionIndex < 0 || optionIndex >= m_visibleOptions.Count)
        {
            return;
        }

        ExecuteOption(m_visibleOptions[optionIndex]);
        Datas.Instance.MarkLevelCompleted(m_currentLevelData.levelId);
        ScreenTransition.Instance.Transition(Close);
    }
    private void ExecuteOption(LevelEventOptionData option)
    {
        if (option == null)
        {
            return;
        }

        switch (option.optionType)
        {
            case LevelEventOptionType.WorshipSpeedGod:
                TemporaryBattleModifierRuntimeManager.AddTemporaryBattleModifier(new TemporaryBattleModifierData
                {
                    optionType = option.optionType,
                    remainingBattles = Mathf.Max(1, option.battleCount),
                    playerSpeedMultiplier = 1.25f
                });
                break;
            case LevelEventOptionType.WorshipPowerGod:
                TemporaryBattleModifierRuntimeManager.AddTemporaryBattleModifier(new TemporaryBattleModifierData
                {
                    optionType = option.optionType,
                    remainingBattles = Mathf.Max(1, option.battleCount),
                    playerDirectDamageMultiplier = 1.2f
                });
                break;
            case LevelEventOptionType.TakeAllIncenseMoney:
                Datas.Instance?.AddGold(Mathf.Max(0, option.battleCount) * 20);
                break;
            case LevelEventOptionType.SwapForProfit:
                TemporaryBattleModifierRuntimeManager.AddTemporaryBattleModifier(new TemporaryBattleModifierData
                {
                    optionType = option.optionType,
                    remainingBattles = Mathf.Max(1, option.battleCount),
                    goldPerSwap = 5
                });
                break;
            case LevelEventOptionType.CashOutSwap:
                TemporaryBattleModifierRuntimeManager.AddTemporaryBattleModifier(new TemporaryBattleModifierData
                {
                    optionType = option.optionType,
                    remainingBattles = Mathf.Max(1, option.battleCount),
                    goldPenaltyPerSwap = Mathf.Max(0, option.extraValue)
                });
                break;
            case LevelEventOptionType.TakeWindingPath:
                TemporaryBattleModifierRuntimeManager.AddTemporaryBattleModifier(new TemporaryBattleModifierData
                {
                    optionType = option.optionType,
                    remainingBattles = Mathf.Max(1, option.battleCount),
                    playerDotDamageMultiplier = 1.15f,
                    playerCritDamageBonus = -0.2f
                });
                break;
            case LevelEventOptionType.TakeBroadRoad:
                TemporaryBattleModifierRuntimeManager.AddTemporaryBattleModifier(new TemporaryBattleModifierData
                {
                    optionType = option.optionType,
                    remainingBattles = Mathf.Max(1, option.battleCount),
                    playerDotDamageMultiplier = 0.85f,
                    playerCritDamageBonus = 0.2f
                });
                break;
            case LevelEventOptionType.None:
            default:
                break;
        }

        Datas.Instance?.MarkLevelCompleted(m_currentLevelData != null ? m_currentLevelData.levelId : string.Empty);
    }

    private static string GetOptionDescription(LevelEventOptionData option)
    {
        if (option == null)
        {
            return string.Empty;
        }

        switch (option.optionType)
        {
            case LevelEventOptionType.WorshipSpeedGod:
                return $"朝拜速度之神(下{Mathf.Max(1, option.battleCount)}场战斗时,我方整体速度提升25%)";
            case LevelEventOptionType.WorshipPowerGod:
                return $"朝拜力量之神(下{Mathf.Max(1, option.battleCount)}场战斗时,我方整体伤害提升20%)";
            case LevelEventOptionType.TakeAllIncenseMoney:
                return $"顺走所有香火钱(获得{Mathf.Max(0, option.battleCount)}*20金币)";
            case LevelEventOptionType.SwapForProfit:
                return $"换人（下{Mathf.Max(1, option.battleCount)}场战斗每进行一次换人操作，则获得5金币）";
            case LevelEventOptionType.CashOutSwap:
                return $"换钱（立即获得{Mathf.Max(0, option.battleCount)}*{Mathf.Max(0, option.value)}金币，在下{Mathf.Max(1, option.battleCount)}场战斗中，每换一次人扣除{Mathf.Max(0, option.extraValue)}金币）";
            case LevelEventOptionType.TakeWindingPath:
                return $"走曲折的小径(下{Mathf.Max(1, option.battleCount)}场战斗,我方全体持续伤害提升15%,但暴击伤害减少20%)";
            case LevelEventOptionType.TakeBroadRoad:
                return $"走宽敞的大道(下{Mathf.Max(1, option.battleCount)}场战斗,我方全体暴击伤害提升20%,但持续伤害下降15%)";
            case LevelEventOptionType.None:
            default:
                return option.optionDescription;
        }
    }

    private void AdvanceToNextFloor()
    {
        if (Datas.Instance == null)
        {
            Debug.LogWarning("[EventLevelPanelView] Datas.Instance 为空，无法进入下一层。", this);
            return;
        }

        if (!Datas.Instance.AdvanceToNextFloor())
        {
            Debug.LogWarning("[EventLevelPanelView] 当前已经是最后一层，无法继续前进。", this);
            return;
        }

        levelListLoader?.ApplyLevels();
    }
}