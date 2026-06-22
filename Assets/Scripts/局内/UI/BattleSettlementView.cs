using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using System;

[DisallowMultipleComponent]
public class BattleSettlementView : MonoBehaviour//结算界面
{
    public static event Action ExitBattle;
    [Header("根节点")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private CanvasGroup rootCanvasGroup;

    [Header("奖励面板")]
    [SerializeField] private TMP_Text experienceText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private Button exitButton;

    [Header("结算镜头")]
    [SerializeField] private ManagedCameraType settlementCameraType = ManagedCameraType.Settlement;
    [SerializeField] private float cameraLeadDuration = 0.35f;
    [SerializeField] private float panelFadeDuration = 0.25f;

    [Header("退出行为")]
    [SerializeField] private string exitSceneName;
    [SerializeField] private UnityEvent onExitRequested;
    [SerializeField] private BGMPlayer.BGMType exitBgmType = BGMPlayer.BGMType.Lobby;
    [SerializeField] private float exitBgmDelay;
    [Header("经验条")]
    [SerializeField] private Slider expSlider;
    [SerializeField] private TMP_Text expSliderText;
    [SerializeField] private float expSliderFillDuration = 1.5f;

    [Header("失败状态")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private string victoryTitleText = "胜利";
    [SerializeField] private string defeatTitleText = "失败";
    [SerializeField] private RectTransform topCurtain;
    [SerializeField] private RectTransform bottomCurtain;
    [SerializeField] private CanvasGroup topCurtainCanvasGroup;
    [SerializeField] private CanvasGroup bottomCurtainCanvasGroup;
    [SerializeField] private float curtainCloseDuration = 1.5f;
    [SerializeField] private float curtainHoldDuration = 0.5f;

    private bool m_isShowing;
    private bool m_rewardsApplied;
    private float m_expBeforeReward;
    private bool m_isDefeat;
    private bool m_hideExperienceOnSettlement;

    private void Awake()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        BindExitButton();
        HideImmediate();
    }

    private void OnEnable()
    {
        BindExitButton();
    }

    public IEnumerator PlaySettlementSequence(int experienceReward, int goldReward, bool hideExperienceOnSettlement = false)
    {
        if (m_isShowing)
        {
            yield break;
        }

        m_isShowing = true;
        m_isDefeat = false;
        m_hideExperienceOnSettlement = hideExperienceOnSettlement;
        m_expBeforeReward = Datas.Instance != null ? Datas.Instance.GetCurrentLevelOverflowExp() : 0f;
        ApplyRewardsIfNeeded(experienceReward, goldReward);
        BindExitButton();

        // 恢复胜利标题
        SetTitleText(victoryTitleText);
        UpdateRewardTexts(experienceReward, goldReward);

        yield return ShowSettlementPanel();

        // 胜利时显示奖励面板；浓缩模式下隐藏经验相关 UI
        SetRewardPanelVisible(true);
        if (!m_hideExperienceOnSettlement)
        {
            StartCoroutine(UpdateExpSlider(experienceReward));
        }
    }

    /// <summary>
    /// 播放失败结算序列：先播放黑幕合拢动画（眼皮合拢效果），然后显示结算面板，
    /// 标题改为失败，不显示奖励面板和经验条
    /// </summary>
    public IEnumerator PlayDefeatSettlementSequence()
    {
        if (m_isShowing)
        {
            yield break;
        }

        m_isShowing = true;
        m_isDefeat = true;
        m_expBeforeReward = Datas.Instance != null ? Datas.Instance.GetCurrentLevelOverflowExp() : 0f;
        // 失败不发放奖励
        m_rewardsApplied = true;
        BindExitButton();

        // 先隐藏奖励面板，避免淡入时闪现
        SetRewardPanelVisible(false);

        // 提前激活面板根节点（透明不可见），让幕布合拢时面板已在后面就位
        ActivatePanelRoot();

        // 播放黑幕合拢动画（眼皮合拢效果）
        yield return PlayCurtainClose();

        // 设置失败标题
        SetTitleText(defeatTitleText);
        UpdateRewardTexts(0, 0);

        // 切换相机并淡入面板
        yield return TransitionCameraAndFadeIn();
    }

    /// <summary>
    /// 显示结算面板的公共逻辑：激活根节点、切换相机、淡入面板
    private IEnumerator ShowSettlementPanel()
    {
        ActivatePanelRoot();
        yield return TransitionCameraAndFadeIn();
    }

    private void ActivatePanelRoot()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        SetCanvasGroupState(rootCanvasGroup, 0f, false);
    }

    private IEnumerator TransitionCameraAndFadeIn()
    {
        if (CinemachineCameraManager.Instance != null)
        {
            yield return CinemachineCameraManager.Instance.TransitionIntoSettlementCamera(settlementCameraType);
        }

        if (cameraLeadDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(cameraLeadDuration);
        }

        yield return FadeCanvasGroup(rootCanvasGroup, 1f, panelFadeDuration, true);
    }

    /// <summary>
    /// 播放黑幕合拢动画：上下两块黑幕从屏幕边缘向中间合拢，同时渐显
    /// </summary>
    private IEnumerator PlayCurtainClose()
    {
        if (topCurtain == null || bottomCurtain == null)
        {
            yield break;
        }

        float screenHeight = topCurtain.parent != null
            ? ((RectTransform)topCurtain.parent).rect.height
            : Screen.height;

        // 初始状态：上下幕布收起（高度为0，完全透明）
        SetCurtainHeight(topCurtain, 0f);
        SetCurtainHeight(bottomCurtain, 0f);
        SetCurtainAlpha(topCurtainCanvasGroup, 0f);
        SetCurtainAlpha(bottomCurtainCanvasGroup, 0f);
        topCurtain.gameObject.SetActive(true);
        bottomCurtain.gameObject.SetActive(true);

        // 上下幕布向中间合拢，同时渐显
        float targetHeight = screenHeight * 0.5f;
        float elapsed = 0f;
        while (elapsed < curtainCloseDuration)
        {
            float progress = Mathf.Clamp01(elapsed / curtainCloseDuration);
            float currentHeight = Mathf.Lerp(0f, targetHeight, progress);
            SetCurtainHeight(topCurtain, currentHeight);
            SetCurtainHeight(bottomCurtain, currentHeight);
            SetCurtainAlpha(topCurtainCanvasGroup, progress);
            SetCurtainAlpha(bottomCurtainCanvasGroup, progress);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        SetCurtainHeight(topCurtain, targetHeight);
        SetCurtainHeight(bottomCurtain, targetHeight);
        SetCurtainAlpha(topCurtainCanvasGroup, 1f);
        SetCurtainAlpha(bottomCurtainCanvasGroup, 1f);

        if (curtainHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(curtainHoldDuration);
        }
    }

    /// <summary>
    /// 设置奖励面板的可见性
    /// </summary>
    private void SetRewardPanelVisible(bool visible)
    {
        bool showRewards = visible && !m_isDefeat;
        bool showExperience = showRewards && !m_hideExperienceOnSettlement;
        if (experienceText != null)
        {
            experienceText.gameObject.SetActive(showExperience);
        }

        if (goldText != null)
        {
            goldText.gameObject.SetActive(showRewards);
        }

        if (expSlider != null)
        {
            expSlider.gameObject.SetActive(showExperience);
        }

        if (expSliderText != null)
        {
            expSliderText.gameObject.SetActive(showExperience);
        }
    }

    /// <summary>
    /// 设置标题文本
    /// </summary>
    private void SetTitleText(string text)
    {
        if (titleText != null)
        {
            titleText.text = text;
        }
    }

    private void ResetCurtains()
    {
        if (topCurtain != null)
        {
            SetCurtainHeight(topCurtain, 0f);
            SetCurtainAlpha(topCurtainCanvasGroup, 0f);
            topCurtain.gameObject.SetActive(false);
        }

        if (bottomCurtain != null)
        {
            SetCurtainHeight(bottomCurtain, 0f);
            SetCurtainAlpha(bottomCurtainCanvasGroup, 0f);
            bottomCurtain.gameObject.SetActive(false);
        }
    }

    private static void SetCurtainHeight(RectTransform curtain, float height)
    {
        Vector2 size = curtain.sizeDelta;
        size.y = height;
        curtain.sizeDelta = size;
    }

    private static void SetCurtainAlpha(CanvasGroup group, float alpha)
    {
        if (group != null)
        {
            group.alpha = alpha;
        }
    }

    IEnumerator UpdateExpSlider(float experienceReward = 0, float duration = -1)
    {
        if (Datas.Instance != null && expSlider != null && expSliderText != null)
        {
            float startExp = m_expBeforeReward;
            float advanceExp = experienceReward;
            float expToNextLevel = Datas.Instance.GetExpToNextLevel();
            float targetExp = startExp + advanceExp;
            DOTween.To(() => startExp, x => { expSlider.value = Mathf.Clamp01(x / expToNextLevel); expSliderText.text = $"{Mathf.FloorToInt(x)} / {Mathf.FloorToInt(Datas.Instance.GetExpToNextLevel())}"; }, targetExp, duration > 0 ? duration : expSliderFillDuration).SetEase(DG.Tweening.Ease.Linear);
            yield return new WaitForSecondsRealtime(duration > 0 ? duration : expSliderFillDuration);
        }
    }

    public void HideImmediate()
    {
        SetCanvasGroupState(rootCanvasGroup, 0f, false);
        ResetCurtains();

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        m_isShowing = false;
        m_rewardsApplied = false;
        m_expBeforeReward = 0f;
        m_isDefeat = false;
        m_hideExperienceOnSettlement = false;
    }

    private void ApplyRewardsIfNeeded(int experienceReward, int goldReward)
    {
        if (m_rewardsApplied || Datas.Instance == null)
        {
            return;
        }

        Datas.Instance.ApplyBattleRewards(experienceReward, goldReward);
        m_rewardsApplied = true;
    }

    private void BindExitButton()
    {
        if (exitButton == null)
        {
            return;
        }

        exitButton.onClick.RemoveListener(HandleExitButtonClicked);
        exitButton.onClick.AddListener(HandleExitButtonClicked);
    }

    private void HandleExitButtonClicked()
    {
        if (BGMPlayer.Instance != null)
        {
            BGMPlayer.Instance.PlayBGM(exitBgmType, exitBgmDelay);
        }

        if (!string.IsNullOrWhiteSpace(exitSceneName))
        {
            StartCoroutine(LoadExitSceneCoroutine());
            return;
        }

        onExitRequested?.Invoke();
    }

    private IEnumerator LoadExitSceneCoroutine()
    {
        if (exitButton != null)
        {
            exitButton.interactable = false;
        }

        yield return ScreenTransition.Instance.Transition(() =>
        {
            ExitBattle?.Invoke();
            ScreenTransition.Instance.ExitTransition();
            SceneManager.LoadScene(exitSceneName);
        });
    }

    private void UpdateRewardTexts(int experienceReward, int goldReward)
    {
        if (experienceText != null)
        {
            experienceText.text = $"EXP +{Mathf.Max(0, experienceReward)}";
        }

        if (goldText != null)
        {
            goldText.text = $"Gold +{Mathf.Max(0, goldReward)}";
        }
    }

    private static void SetCanvasGroupState(CanvasGroup group, float alpha, bool interactive)
    {
        if (group == null)
        {
            return;
        }

        group.alpha = alpha;
        group.interactable = interactive;
        group.blocksRaycasts = interactive;
    }

    private static IEnumerator FadeCanvasGroup(CanvasGroup group, float targetAlpha, float duration, bool interactiveAfterFade)
    {
        if (group == null)
        {
            yield break;
        }

        float startAlpha = group.alpha;
        if (duration <= Mathf.Epsilon)
        {
            SetCanvasGroupState(group, targetAlpha, interactiveAfterFade);
            yield break;
        }

        group.interactable = false;
        group.blocksRaycasts = false;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float progress = Mathf.Clamp01(elapsed / duration);
            group.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        SetCanvasGroupState(group, targetAlpha, interactiveAfterFade);
    }
}
