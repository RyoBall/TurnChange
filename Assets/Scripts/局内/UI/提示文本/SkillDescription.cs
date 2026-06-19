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

    [Header("标签")]
    [SerializeField] private RectTransform m_tagAnchor;
    [SerializeField] private GameObject m_tagPrefab;
    [SerializeField] private float m_tagSpacing = 8f;
    private readonly List<GameObject> m_spawnedTags = new List<GameObject>();

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
            PauseCameraSway();
            string description = skill.shortDescription;
            if (m_keywordConfig != null)
            {
                description = m_keywordConfig.ApplyKeywordRichText(description);
            }
            m_skillDesText.text = description;
            UpdateKeywordText(skill);
            SpawnTags(skill);
            m_currentSequence.Join(m_canvasGroup.DOFade(1, 0.3f).SetEase(Ease.InOutQuad));
            m_currentSequence.Join(BackgroundManager.Instance.ChangeBackground(true));
        }
        else
        {
            ResumeCameraSway();
            DestroySpawnedTags();
            m_currentSequence.Join(m_canvasGroup.DOFade(0, 0.3f).SetEase(Ease.InOutQuad));
            m_currentSequence.Join(BackgroundManager.Instance.ChangeBackground(false));
            m_currentSequence.AppendCallback(ClearDescriptionText);
        }
    }

    /// <summary>
    /// 显示状态描述（格式：状态名：状态解释）
    /// </summary>
    public void ChangeDescription(State state)
    {
        KillCurrentSequence();
        m_currentSequence = DOTween.Sequence().SetUpdate(true);
        if (state != null)
        {
            PauseCameraSway();
            string stateName = StateDictionaryManager.GetStateName(state.stateType);
            string text = $"{stateName}：{state.description}";
            text += BuildStateDurationText(state);
            m_skillDesText.text = text;
            if (m_keywordDesText != null)
            {
                m_keywordDesText.text = "";
            }

            DestroySpawnedTags();
            UpdateKeywordBackground();
            m_currentSequence.Join(m_canvasGroup.DOFade(1, 0.3f).SetEase(Ease.InOutQuad));
            m_currentSequence.Join(BackgroundManager.Instance.ChangeBackground(true));
        }
        else
        {
            ResumeCameraSway();
            DestroySpawnedTags();
            m_currentSequence.Join(m_canvasGroup.DOFade(0, 0.3f).SetEase(Ease.InOutQuad));
            m_currentSequence.Join(BackgroundManager.Instance.ChangeBackground(false));
            m_currentSequence.AppendCallback(ClearDescriptionText);
        }
    }

    /// <summary>
    /// 构建状态的层数和持续时间文本
    /// </summary>
    private string BuildStateDurationText(State state)
    {
        string result = $"\n层数：{state.StackCount}";
        switch (state.DurationType)
        {
            case StateDurationType.Turn:
                result += $"\n持续时间：{state.RemainingTurns}回合";
                break;
            case StateDurationType.ActionValue:
                result += $"\n持续时间：{state.RemainingActionValue}行动值";
                break;
            case StateDurationType.Special:
                // Special 类型不写持续时间
                break;
        }
        return result;
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
                sb.Append(SkillKeywordConfig.WrapKeyword(keyword));
                sb.Append(":");
                sb.Append(description);
            }
            else
            {
                sb.Append(SkillKeywordConfig.WrapKeyword(keyword));
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
    /// 暂停摄像机摆动动画
    /// </summary>
    private void PauseCameraSway()
    {
        if (CinemachineCameraManager.Instance != null)
        {
            CinemachineCameraManager.Instance.PauseSway();
        }
    }

    /// <summary>
    /// 继续摄像机摆动动画
    /// </summary>
    private void ResumeCameraSway()
    {
        if (CinemachineCameraManager.Instance != null)
        {
            CinemachineCameraManager.Instance.ResumeSway();
        }
    }

    /// <summary>
    /// 根据技能的标签列表，以 m_tagAnchor 为中心对称实例化标签预制体
    /// </summary>
    private void SpawnTags(SkillBase skill)
    {
        DestroySpawnedTags();

        CharacterSkillBase characterSkill = skill as CharacterSkillBase;
        if (characterSkill == null || characterSkill.tags == null || characterSkill.tags.Count == 0)
        {
            return;
        }

        if (m_tagAnchor == null || m_tagPrefab == null)
        {
            return;
        }

        int count = characterSkill.tags.Count;
        float totalWidth = (count - 1) * m_tagSpacing;
        float startX = -totalWidth * 0.5f;

        for (int i = 0; i < count; i++)
        {
            GameObject tagInstance = Instantiate(m_tagPrefab, m_tagAnchor);
            RectTransform tagRect = tagInstance.GetComponent<RectTransform>();
            if (tagRect != null)
            {
                tagRect.anchoredPosition = new Vector2(startX + i * m_tagSpacing, 0f);
            }

            TMP_Text tagText = tagInstance.GetComponentInChildren<TMP_Text>();
            if (tagText != null)
            {
                tagText.text = characterSkill.tags[i];
            }

            TagColorConfig tagColorConfig = TagColorConfig.Instance;
            if (tagColorConfig != null)
            {
                tagColorConfig.ApplyColorsToTag(tagInstance, characterSkill.tags[i]);
            }

            m_spawnedTags.Add(tagInstance);
        }
    }

    private void DestroySpawnedTags()
    {
        for (int i = m_spawnedTags.Count - 1; i >= 0; i--)
        {
            if (m_spawnedTags[i] != null)
            {
                Destroy(m_spawnedTags[i]);
            }
        }

        m_spawnedTags.Clear();
    }
}
