using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 教程控制器单例，管理教程的启动、推进与结束
/// 挂载到包含对话框 UI 的 Canvas GameObject 上
/// </summary>
public class TutorialController : MonoBehaviour
{
    public static TutorialController Instance { get; private set; }

    [Header("教程数据列表")]
    [SerializeField] private List<TutorialData> m_tutorialList = new List<TutorialData>();

    [Header("高亮聚焦")]
    [SerializeField] private GuideDisplayController m_guideDisplay;

    [Header("对话框 UI")]
    [SerializeField] private Image guideImage;
    [SerializeField] private CanvasGroup m_dialogCanvasGroup;
    [SerializeField] private TMP_Text m_dialogText;

    [Header("动画参数")]
    [SerializeField] private float m_showAnimDuration = 0.3f;
    [SerializeField] private float m_hideAnimDuration = 0.25f;
    [SerializeField] private Ease m_showEase = Ease.OutBack;
    [SerializeField] private Ease m_hideEase = Ease.InBack;

    // 根据数据创建的行为实例列表
    private List<TutorialBehavior> m_behaviorList = new List<TutorialBehavior>();

    // 当前正在执行的教程行为
    private TutorialBehavior m_currentBehavior;

    /// <summary>当前是否有教程正在执行</summary>
    public bool IsTutorialActive => m_currentBehavior != null;

    // 对话框动画 Tween 引用
    private Tween m_dialogTween;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        guideImage.alphaHitTestMinimumThreshold = 0.1f; // 设置图片的点击穿透阈值
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        // 停止所有行为的事件监听
        foreach (TutorialBehavior behavior in m_behaviorList)
        {
            behavior.StopListening();
        }
    }

    private void Start()
    {
        // 遍历 tutorialList，用工厂创建对应 TutorialBehavior 并存入 behaviorList
        foreach (TutorialData data in m_tutorialList)
        {
            if (data == null)
            {
                Debug.LogWarning("TutorialController: tutorialList 中存在空数据项");
                continue;
            }

            TutorialBehavior behavior = TutorialBehaviorFactory.Create(data.Type);
            behavior.Initialize(data, this);
            m_behaviorList.Add(behavior);
        }

        // 初始时隐藏对话框
        if (m_dialogCanvasGroup != null)
        {
            m_dialogCanvasGroup.alpha = 0f;
            m_dialogCanvasGroup.gameObject.SetActive(false);
        }

        // 通知所有行为开始监听事件
        foreach (TutorialBehavior behavior in m_behaviorList)
        {
            behavior.StartListening();
        }
    }

    private void Update()
    {
        // 如果有当前行为且可以推进，则推进教程
        if (m_currentBehavior != null && m_currentBehavior.CanProgress())
        {
            m_currentBehavior.Progress();
        }
    }

    /// <summary>
    /// 供行为子类调用：显示指定类型的高亮聚焦
    /// </summary>
    public void ShowGuideHighlight(GuideHighlightType type)
    {
        if (m_guideDisplay != null)
            m_guideDisplay.ShowHighlight(type);
    }

    /// <summary>
    /// 供行为子类调用：取消高亮聚焦
    /// </summary>
    public void HideGuideHighlight()
    {
        if (m_guideDisplay != null)
            m_guideDisplay.HideHighlight();
    }

    /// <summary>
    /// 根据 TutorialType 启动对应的教程
    /// </summary>
    /// <param name="type">要启动的教程类型</param>
    public void StartTutorial(TutorialType type)
    {
        TutorialBehavior behavior = m_behaviorList.Find(b => b.Data.Type == type);
        if (behavior == null)
        {
            Debug.LogWarning($"TutorialController: 未找到类型 {type} 的教程行为");
            return;
        }
        Debug.Log($"TutorialController: 启动教程 {type}");
        StartTutorial(behavior);
    }

    /// <summary>
    /// 启动指定的教程行为
    /// </summary>
    private void StartTutorial(TutorialBehavior behavior)
    {
        // 如果该教程已完成过，不再触发
        if (behavior.HasCompleted)
        {
            Debug.Log($"TutorialController: 教程 {behavior.Data.Type} 已完成过，跳过触发");
            return;
        }

        // 如果已有教程在执行，先结束
        if (m_currentBehavior != null)
        {
            EndTutorial();
        }

        m_currentBehavior = behavior;
        behavior.OnTutorialStart();

        // 播放对话框出现动画
        // 显示第一条文本
        if (behavior.Data.TextList.Count > 0)
        {
            behavior.Progress();
        }
        ShowDialog();
    }

    /// <summary>
    /// 更新对话框文本
    /// </summary>
    public void UpdateDialogText(string text)
    {
        if (m_dialogText != null)
        {
            m_dialogText.text = text;
        }
    }

    /// <summary>
    /// 结束当前教程
    /// </summary>
    public void EndTutorial()
    {
        if (m_currentBehavior == null)
            return;

        TutorialBehavior behavior = m_currentBehavior;
        m_currentBehavior = null; // 先置空，防止递归

        HideDialog();
        behavior.OnTutorialEnd();
        Debug.Log($"TutorialController: 结束教程 {behavior.Data.Type}");
    }

    /// <summary>
    /// 播放对话框出现动画（DOTween）
    /// </summary>
    private void ShowDialog(TweenCallback onComplete = null)
    {
        if (m_dialogCanvasGroup == null)
        {
            onComplete?.Invoke();
            return;
        }

        KillDialogTween();

        m_dialogCanvasGroup.gameObject.SetActive(true);
        m_dialogCanvasGroup.alpha = 0f;
        m_dialogCanvasGroup.transform.localScale = Vector3.one * 0.5f;

        Sequence seq = DOTween.Sequence();
        seq.Join(m_dialogCanvasGroup.DOFade(1f, m_showAnimDuration).SetEase(Ease.OutQuad));
        seq.Join(m_dialogCanvasGroup.transform.DOScale(1f, m_showAnimDuration).SetEase(m_showEase));
        seq.OnComplete(onComplete);
        m_dialogTween = seq;
    }

    /// <summary>
    /// 播放对话框消失动画（DOTween）
    /// </summary>
    private void HideDialog(TweenCallback onComplete = null)
    {
        if (m_dialogCanvasGroup == null)
        {
            onComplete?.Invoke();
            return;
        }

        KillDialogTween();

        Sequence seq = DOTween.Sequence();
        seq.Join(m_dialogCanvasGroup.DOFade(0f, m_hideAnimDuration).SetEase(Ease.InQuad));
        seq.Join(m_dialogCanvasGroup.transform.DOScale(0.5f, m_hideAnimDuration).SetEase(m_hideEase));
        seq.OnComplete(() =>
        {
            m_dialogCanvasGroup.gameObject.SetActive(false);
            onComplete?.Invoke();
        }).OnKill(() => onComplete?.Invoke());
        m_dialogTween = seq;
    }

    /// <summary>
    /// 终止当前的对话框动画
    /// </summary>
    private void KillDialogTween()
    {
        if (m_dialogTween != null && m_dialogTween.IsActive())
        {
            m_dialogTween.Kill();
        }
        m_dialogTween = null;
    }
}
