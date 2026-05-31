using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public enum CharacterType
{
    DotMain,
    DotSub,
    DotSupport,
    DirectMain,
    DirectSub,
    DirectSupport
}

[DisallowMultipleComponent]
public class StarterBranchRuntimeController : MonoBehaviour
{
    private const string DotBranchId = "Dot";
    private const string DirectDamageBranchId = "直伤";

    [Header("开局选择UI")]
    [SerializeField] private RectTransform backGround;
    [SerializeField] private StarterBranchConfig starterBranchConfig;
    [SerializeField] private RectTransform choiceRoot;
    [SerializeField] private RectTransform choicePanel;
    [SerializeField] private CanvasGroup choiceCanvasGroup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private StarterBranchChoiceButtonUI choiceButtonPrefab;

    [Header("动画参数")]
    [SerializeField] private float enterAnimationDuration = 0.25f;
    [SerializeField] private float exitAnimationDuration = 0.2f;
    [SerializeField] private Vector3 hiddenPanelScale = new Vector3(0.92f, 0.92f, 1f);
    [SerializeField] private RectTransform dotParentTransform;
    [SerializeField] private RectTransform directParentTransform;

    private Datas m_datas;
    private readonly List<StarterBranchChoiceButtonUI> m_spawnedButtons = new List<StarterBranchChoiceButtonUI>();
    private bool m_sceneLoadedSubscribed;
    private bool m_isAnimating;
    #region 动画
    // 开局按钮出现动画：需要自定义入场表现时，优先修改这个函数。
    private void PlayStarterChoiceEnterAnimation()
    {
        if (choiceRoot == null || choicePanel == null || choiceCanvasGroup == null)
        {
            return;
        }

        StopAllCoroutines();
        choiceRoot.gameObject.SetActive(true);
        StartCoroutine(AnimateStarterChoiceCanvas(0f, 1f, hiddenPanelScale, Vector3.one, enterAnimationDuration, null));
    }
    #endregion
    // 开局按钮离场动画：需要自定义选中后的离场表现时，优先修改这个函数。
    private void PlayStarterChoiceExitAnimation(Action onCompleted)
    {
        if (choiceRoot == null || choicePanel == null || choiceCanvasGroup == null)
        {
            onCompleted?.Invoke();
            return;
        }

        StopAllCoroutines();
        StartCoroutine(AnimateStarterChoiceCanvas(1f, 0f, Vector3.one, hiddenPanelScale, exitAnimationDuration, onCompleted));
    }

    private void Start()
    {
        Initialize(Datas.Instance);
        ApplyInitialRosterState();
        TryShowStarterChoiceOverlay();
    }

    private void OnDestroy()
    {
        UnsubscribeDatasEvents();
        UnsubscribeSceneLoaded();
    }

    private void Initialize(Datas datas)
    {
        if (datas == null)
        {
            return;
        }

        if (m_datas == datas && m_sceneLoadedSubscribed)
        {
            return;
        }

        UnsubscribeDatasEvents();
        UnsubscribeSceneLoaded();

        m_datas = datas;
        m_datas.LevelCompleted += HandleLevelCompleted;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        m_sceneLoadedSubscribed = true;
    }
    // 根据当前已选流派和关卡完成情况，确保角色列表正确。通常在游戏开始时调用一次，之后每次关卡完成后调用以同步角色解锁。
    private void ApplyInitialRosterState()
    {
        if (m_datas == null)
        {
            return;
        }

        if (!m_datas.HasSelectedStarterBranch)
        {
            bool hadCharacters = m_datas.GetUnlockedCharacterRosters().Count > 0;
            if (hadCharacters)
            {
                m_datas.NotifyCharacterRosterChanged();
            }

            return;
        }

        SynchronizeStarterBranchProgression(false);
    }
    // 关卡完成后同步角色解锁状态，确保玩家获得已选流派对应的角色。
    private void HandleLevelCompleted(string levelId)
    {
        SynchronizeStarterBranchProgression();
    }
    //加载场景时调用
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (m_datas == null)
        {
            Initialize(Datas.Instance);
        }

        if (choiceRoot != null)
        {
            choiceRoot.gameObject.SetActive(false);
        }
    }

    private void UnsubscribeDatasEvents()
    {
        if (m_datas == null)
        {
            return;
        }

        m_datas.LevelCompleted -= HandleLevelCompleted;
    }

    private void UnsubscribeSceneLoaded()
    {
        if (!m_sceneLoadedSubscribed)
        {
            return;
        }

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        m_sceneLoadedSubscribed = false;
    }
    // 根据当前已选流派和关卡完成情况，确保角色列表正确。通常在游戏开始时调用一次，之后每次关卡完成后调用以同步角色解锁。
    private void SynchronizeStarterBranchProgression(bool notify = true)
    {
        if (m_datas == null)
        {
            return;
        }

        if (!m_datas.HasSelectedStarterBranch)
        {
            if (notify)
            {
                m_datas.NotifyCharacterRosterChanged();
            }

            return;
        }

        bool rosterChanged = false;
        StarterBranchDefinition selectedBranch = GetStarterBranch(m_datas.SelectedStarterBranchId);

        rosterChanged |= AddBranchCoreCharacters(selectedBranch);

        rosterChanged |= AddConfiguredFollowupUnlocks(selectedBranch);

        if (notify && (rosterChanged || m_datas.GetUnlockedCharacterRosters().Count > 0))
        {
            m_datas.NotifyCharacterRosterChanged();
        }
    }
    // 添加开局流派的核心角色（主C和副C）到角色列表。返回是否有新增角色被添加。通常在选择流派后调用以确保玩家获得开局角色。
    private bool AddBranchCoreCharacters(StarterBranchDefinition branch)
    {
        if (branch == null)
        {
            return false;
        }

        bool changed = false;
        changed |= m_datas.AddCharacterData(branch.primaryCharacterType);
        changed |= m_datas.AddCharacterData(branch.secondaryCharacterType);
        return changed;
    }
    // 根据已选流派的后续解锁配置和当前关卡完成情况，添加对应角色到角色列表。通常在关卡完成后调用以同步角色解锁。
    private bool AddConfiguredFollowupUnlocks(StarterBranchDefinition branch)
    {
        if (branch == null || branch.followupUnlocks == null)
        {
            return false;
        }

        bool changed = false;
        for (int i = 0; i < branch.followupUnlocks.Count; i++)
        {
            StarterBranchUnlockEntry unlockEntry = branch.followupUnlocks[i];
            if (unlockEntry == null
                || string.IsNullOrWhiteSpace(unlockEntry.levelId)
                || !m_datas.IsLevelCompleted(unlockEntry.levelId))
            {
                continue;
            }

            changed |= m_datas.AddCharacterData(unlockEntry.characterType);
        }

        return changed;
    }
    //根据ID获取流派定义
    private StarterBranchDefinition GetStarterBranch(string branchId)
    {
        if (string.IsNullOrWhiteSpace(branchId))
        {
            return null;
        }

        IReadOnlyList<StarterBranchDefinition> starterBranches = GetStarterBranches();
        for (int i = 0; i < starterBranches.Count; i++)
        {
            StarterBranchDefinition branch = starterBranches[i];
            if (branch != null && string.Equals(branch.branchId, branchId, StringComparison.Ordinal))
            {
                return branch;
            }
        }

        return null;
    }

    private bool ShouldShowStarterChoiceInScene(Scene scene)
    {
        if (m_datas == null || m_datas.HasSelectedStarterBranch)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(m_datas.StarterChoiceSceneName))
        {
            return true;
        }

        return string.Equals(scene.name, m_datas.StarterChoiceSceneName, StringComparison.Ordinal);
    }
    //尝试显示开局选择界面：在场景加载时调用，如果当前没有已选流派且配置允许在当前场景显示，则创建并展示开局选择UI。玩家可以通过该UI选择开局流派，之后会同步角色解锁状态。
    private void TryShowStarterChoiceOverlay()
    {
        if (m_datas == null || !ShouldShowStarterChoiceInScene(SceneManager.GetActiveScene()))
        {
            Debug.Log("1[StarterBranchRuntimeController] 当前场景不适合显示开局选择界面，跳过显示。", this);
            choiceRoot.gameObject.SetActive(false);
            return;
        }

        IReadOnlyList<StarterBranchDefinition> starterBranches = GetStarterBranches();
        if (starterBranches.Count < 2)
        {
            Debug.LogWarning("[StarterBranchRuntimeController] 流派配置不足，无法显示开局选择弹窗。", this);
            return;
        }

        ClearSpawnedButtons();

        if (choiceRoot != null)
        {
            choiceRoot.gameObject.SetActive(true);
        }

        if (titleText != null)
        {
            titleText.text = "请选择开局流派";
        }

        if (subtitleText != null)
        {
            subtitleText.text = "选择后立即获得该流派主C与副C，之后按指定关卡 ID 追加新角色。";
        }

        for (int i = 0; i < starterBranches.Count && i < 2; i++)
        {
            StarterBranchDefinition branch = starterBranches[i];
            if (branch == null)
            {
                continue;
            }

            CreateStarterChoiceButton(branch);
        }

        PlayStarterChoiceEnterAnimation();
    }

    private bool SelectStarterBranch(string branchId)
    {
        if (m_datas == null || m_datas.HasSelectedStarterBranch || m_isAnimating)
        {
            return false;
        }

        StarterBranchDefinition branch = GetStarterBranch(branchId);
        if (branch == null)
        {
            Debug.LogWarning($"[StarterBranchRuntimeController] 未找到开局流派: {branchId}", this);
            return false;
        }

        m_datas.SetSelectedStarterBranchId(branch.branchId);
        SynchronizeStarterBranchProgression();
        ScreenTransition.Instance?.Transition(() =>
        {
            PlayStarterChoiceExitAnimation(() =>
            {
                if (choiceRoot != null)
                {
                    choiceRoot.gameObject.SetActive(false);
                }
            });
        });
        return true;
    }

    private IReadOnlyList<StarterBranchDefinition> GetStarterBranches()
    {
        return starterBranchConfig != null ? starterBranchConfig.StarterBranches : Array.Empty<StarterBranchDefinition>();
    }

    private void CreateStarterChoiceButton(StarterBranchDefinition branch)
    {
        RectTransform choiceButtonparent = null;
        if(branch.branchId== DotBranchId)
        {
            choiceButtonparent = dotParentTransform;
        }
        else if(branch.branchId == DirectDamageBranchId)
        {
            choiceButtonparent = directParentTransform;
        }
        else
        {
            Debug.LogWarning($"[StarterBranchRuntimeController] 未知的流派ID: {branch.branchId}", this);
            return;
        }
        StarterBranchChoiceButtonUI buttonUi = Instantiate(choiceButtonPrefab, choiceButtonparent);

        if (buttonUi == null)
        {
            return;
        }

        buttonUi.Bind(branch, BuildBranchButtonText(branch), branchId =>
        {
            SelectStarterBranch(branchId);
        });
        m_spawnedButtons.Add(buttonUi);
    }

    private string BuildBranchButtonText(StarterBranchDefinition branch)
    {
        if (branch == null)
        {
            return string.Empty;
        }

        string primaryCharacterName = ResolveCharacterLabel(branch.primaryCharacterType);
        string secondaryCharacterName = ResolveCharacterLabel(branch.secondaryCharacterType);
        string supportCharacterName = ResolveCharacterLabel(branch.supportCharacterType);

        if (!string.IsNullOrWhiteSpace(branch.description))
        {
            return $"{branch.description}\n\n开局：{primaryCharacterName} + {secondaryCharacterName}";
        }

        return $"开局：{primaryCharacterName} + {secondaryCharacterName}\n后续：{supportCharacterName}";
    }

    private string ResolveCharacterLabel(CharacterType characterType)
    {
        if (m_datas == null)
        {
            return characterType.ToString();
        }

        CharacterRosterData rosterData = m_datas.GetCharacterRoster(characterType);
        if (rosterData != null)
        {
            string displayName = rosterData.GetDisplayName();
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName;
            }

            string characterId = rosterData.GetCharacterId();
            if (!string.IsNullOrWhiteSpace(characterId))
            {
                return characterId;
            }
        }

        return characterType.ToString();
    }

    private void ClearSpawnedButtons()
    {
        for (int i = m_spawnedButtons.Count - 1; i >= 0; i--)
        {
            StarterBranchChoiceButtonUI buttonUi = m_spawnedButtons[i];
            if (buttonUi == null)
            {
                continue;
            }

            Destroy(buttonUi.gameObject);
        }

        m_spawnedButtons.Clear();
    }


    private System.Collections.IEnumerator AnimateStarterChoiceCanvas(
        float fromAlpha,
        float toAlpha,
        Vector3 fromScale,
        Vector3 toScale,
        float duration,
        Action onCompleted)
    {
        m_isAnimating = true;
        choiceCanvasGroup.alpha = fromAlpha;
        choiceCanvasGroup.interactable = false;
        choiceCanvasGroup.blocksRaycasts = false;
        choicePanel.localScale = fromScale;

        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;
        while (elapsed < safeDuration)
        {
            float t = Mathf.Clamp01(elapsed / safeDuration);
            choiceCanvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
            choicePanel.localScale = Vector3.LerpUnclamped(fromScale, toScale, t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        choiceCanvasGroup.alpha = toAlpha;
        choicePanel.localScale = toScale;
        choiceCanvasGroup.interactable = toAlpha > 0.99f;
        choiceCanvasGroup.blocksRaycasts = toAlpha > 0.99f;
        m_isAnimating = false;
        onCompleted?.Invoke();
    }
}