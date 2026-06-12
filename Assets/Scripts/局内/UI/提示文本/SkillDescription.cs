using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;
using DG.Tweening;
public class SkillDescription : MonoBehaviour
{
    public static SkillDescription Instance { get; private set; }
    private Sequence m_currentSequence;
    private SkillKeywordConfig m_keywordConfig { get { return SkillKeywordConfig.Instance; } set { } }
    private float m_previousTimeScale;
    private bool m_hasSavedTimeScale;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    [SerializeField] private TMP_Text m_skillDesText;
    [SerializeField] private TMP_Text m_keywordDesText;
    [SerializeField] private CanvasGroup m_canvasGroup;
    [SerializeField] private GameObject m_keywordBackground;

    void Start()
    {
        if (m_skillDesText == null)
        {
            m_skillDesText = GetComponentInChildren<TMP_Text>();
        }
        m_skillDesText.text = "";
        if (m_canvasGroup == null)
        {
            m_canvasGroup = GetComponent<CanvasGroup>();
        }
        m_canvasGroup.alpha = 0;

        // 加载关键词配置
        m_keywordConfig = Resources.Load<SkillKeywordConfig>("配置可编程物体/技能/关键词配置/SkillKeywordConfig");
    }

    public void ChangeDescription(SkillBase skill = null)
    {
        KillCurrentSequence();
        m_currentSequence = DOTween.Sequence().SetUpdate(true);
        if (skill != null)
        {
            SlowDownTimeScale();
            m_skillDesText.text = skill.shortDescription;
            UpdateKeywordText(skill);
            m_currentSequence.Join(m_canvasGroup.DOFade(1, 0.3f).SetEase(Ease.InOutQuad));
            m_currentSequence.Join(BackgroundManager.Instance.ChangeBackground(true));
        }
        else
        {
            RestoreTimeScale();
            m_currentSequence.Join(m_canvasGroup.DOFade(0, 0.3f).SetEase(Ease.InOutQuad));
            m_currentSequence.Join(BackgroundManager.Instance.ChangeBackground(false));
            m_currentSequence.AppendCallback(ClearDescriptionText);
        }
    }

    /// <summary>
    /// 显示状态描述（状态没有关键词，只显示纯文本描述）
    /// </summary>
    public void ChangeDescription(State state)
    {
        KillCurrentSequence();
        m_currentSequence = DOTween.Sequence().SetUpdate(true);
        if (state != null)
        {
            SlowDownTimeScale();
            m_skillDesText.text = state.description;
            if (m_keywordDesText != null)
            {
                m_keywordDesText.text = "";
            }

            UpdateKeywordBackground();
            m_currentSequence.Join(m_canvasGroup.DOFade(1, 0.3f).SetEase(Ease.InOutQuad));
            m_currentSequence.Join(BackgroundManager.Instance.ChangeBackground(true));
        }
        else
        {
            RestoreTimeScale();
            m_currentSequence.Join(m_canvasGroup.DOFade(0, 0.3f).SetEase(Ease.InOutQuad));
            m_currentSequence.Join(BackgroundManager.Instance.ChangeBackground(false));
            m_currentSequence.AppendCallback(ClearDescriptionText);
        }
    }

    /// <summary>
    /// 取消显示描述文本，隐藏整个描述面板
    /// </summary>
    public void HideDescription()
    {
        ChangeDescription((SkillBase)null);
    }

    /// <summary>
    /// 根据 keywordDesText 是否为空来控制关键词背景的显隐
    /// </summary>
    private void UpdateKeywordBackground()
    {
        if (m_keywordBackground == null)
        {
            return;
        }

        bool hasKeyword = m_keywordDesText != null && !string.IsNullOrEmpty(m_keywordDesText.text);
        m_keywordBackground.SetActive(hasKeyword);
    }

    /// <summary>
    /// 根据技能的关键词列表，生成"关键词:关键词描述"格式的文本
    /// </summary>
    private void UpdateKeywordText(SkillBase skill)
    {
        if (m_keywordDesText == null)
        {
            return;
        }

        CharacterSkillBase characterSkill = skill as CharacterSkillBase;
        if (characterSkill == null || characterSkill.words == null || characterSkill.words.Count == 0)
        {
            m_keywordDesText.text = "";
            UpdateKeywordBackground();
            return;
        }

        if (m_keywordConfig == null)
        {
            m_keywordDesText.text = "";
            UpdateKeywordBackground();
            return;
        }

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < characterSkill.words.Count; i++)
        {
            string keyword = characterSkill.words[i];
            if (string.IsNullOrWhiteSpace(keyword))
            {
                continue;
            }

            string description = m_keywordConfig.GetDescription(keyword);
            if (sb.Length > 0)
            {
                sb.AppendLine();
            }

            if (!string.IsNullOrEmpty(description))
            {
                sb.Append(keyword);
                sb.Append(":");
                sb.Append(description);
            }
            else
            {
                sb.Append(keyword);
            }
        }

        m_keywordDesText.text = sb.ToString();
        UpdateKeywordBackground();
    }

    private void KillCurrentSequence()
    {
        if (m_currentSequence != null && m_currentSequence.IsActive())
        {
            m_currentSequence.Kill();
        }
    }

    private void ClearDescriptionText()
    {
        m_skillDesText.text = "";
        if (m_keywordDesText != null)
        {
            m_keywordDesText.text = "";
        }

        UpdateKeywordBackground();
    }

    /// <summary>
    /// 将时间流速降为 0.1 倍速，并保存之前的流速以便恢复
    /// </summary>
    private void SlowDownTimeScale()
    {
        if (m_hasSavedTimeScale)
        {
            return;
        }

        ITimeScaleController controller = TimeScaleController.Instance;
        if (controller != null)
        {
            m_previousTimeScale = controller.CurrentTimeScale;
            m_hasSavedTimeScale = true;
            controller.SetTimeScale(0.1f);
        }
    }

    /// <summary>
    /// 恢复为之前保存的时间流速
    /// </summary>
    private void RestoreTimeScale()
    {
        if (!m_hasSavedTimeScale)
        {
            return;
        }

        ITimeScaleController controller = TimeScaleController.Instance;
        if (controller != null)
        {
            controller.SetTimeScale(m_previousTimeScale);
        }

        m_hasSavedTimeScale = false;
    }
}
