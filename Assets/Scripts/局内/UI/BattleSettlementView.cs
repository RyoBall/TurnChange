using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

[DisallowMultipleComponent]
public class BattleSettlementView : MonoBehaviour//结算界面
{
    [Header("根节点")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private CanvasGroup rootCanvasGroup;

    [Header("奖励面板")]
    [SerializeField] private CanvasGroup rewardPanelGroup;
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
    [Header("经验条")]
    [SerializeField] private Slider expSlider;
    [SerializeField] private TMP_Text expSliderText;
    [SerializeField] private float expSliderFillDuration = 1.5f;
    private bool m_isShowing;

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

    public IEnumerator PlaySettlementSequence(int experienceReward, int goldReward)
    {
        if (m_isShowing)
        {
            yield break;
        }

        m_isShowing = true;
        BindExitButton();
        UpdateRewardTexts(experienceReward, goldReward);

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        SetCanvasGroupState(rootCanvasGroup, 1f, true);
        SetCanvasGroupState(rewardPanelGroup, 0f, false);

        if (CinemachineCameraManager.Instance != null)
        {
            yield return CinemachineCameraManager.Instance.TransitionIntoSettlementCamera(settlementCameraType);
        }

        if (cameraLeadDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(cameraLeadDuration);
        }

        yield return FadeCanvasGroup(rewardPanelGroup, 1f, panelFadeDuration, true);
        StartCoroutine(UpdateExpSlider(experienceReward));
    }
    IEnumerator UpdateExpSlider(float experienceReward = 0,float duration=-1)
    {
        if (Datas.Instance != null)
        {
            float startExp = Datas.Instance.GetCurrentExp();
            float advanceExp = experienceReward;
            float expToNextLevel = Datas.Instance.GetExpToNextLevel();
            DOTween.To(() => startExp, x => { expSlider.value = x / expToNextLevel % 1f; expSliderText.text = $"{Mathf.FloorToInt(x%expToNextLevel)} / {Mathf.FloorToInt(Datas.Instance.GetExpToNextLevel())}"; }, startExp + advanceExp, duration > 0 ? duration : expSliderFillDuration).SetEase(DG.Tweening.Ease.Linear);
            yield return new WaitForSecondsRealtime(duration > 0 ? duration : expSliderFillDuration);
        }
    }

    public void HideImmediate()
    {
        SetCanvasGroupState(rootCanvasGroup, 0f, false);
        SetCanvasGroupState(rewardPanelGroup, 0f, false);

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        m_isShowing = false;
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

        if (ScreenTransition.Instance != null)
        {
            yield return ScreenTransition.Instance.EnterTransition();
        }
        ScreenTransition.Instance.ExitTransition();
        SceneManager.LoadScene(exitSceneName);
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