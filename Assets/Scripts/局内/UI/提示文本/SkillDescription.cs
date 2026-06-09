using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;
using DG.Tweening;
public class SkillDescription : MonoBehaviour
{
    public static SkillDescription Instance { get; private set; }
    private Sequence currentSequence;
    private SkillKeywordConfig m_keywordConfig{get{return SkillKeywordConfig.Instance;}set{}}

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public TMP_Text skillDesText;
    public TMP_Text keywordDesText;
    public CanvasGroup canvasGroup;

    void Start()
    {
        if (skillDesText == null)
        {
            skillDesText = GetComponentInChildren<TMP_Text>();
        }
        skillDesText.text = "";
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 0;

        // 加载关键词配置
        m_keywordConfig = Resources.Load<SkillKeywordConfig>("配置可编程物体/技能/关键词配置/SkillKeywordConfig");
    }

    public void ChangeDescription(SkillBase skill = null)
    {
        if(currentSequence != null && currentSequence.IsActive())
        {
            currentSequence.Kill();
        }
        currentSequence = DOTween.Sequence();
        if (skill != null)
        {
            skillDesText.text = skill.shortDescription;
            UpdateKeywordText(skill);
            currentSequence.Join(canvasGroup.DOFade(1, 0.3f).SetEase(Ease.InOutQuad));
            currentSequence.Join(BackgroundManager.Instance.ChangeBackground(true));
        }
        else
        {
            currentSequence.Join(canvasGroup.DOFade(0, 0.3f).SetEase(Ease.InOutQuad));
            currentSequence.Join(BackgroundManager.Instance.ChangeBackground(false));
            currentSequence.AppendCallback(() =>
            {
                skillDesText.text = "";
                if (keywordDesText != null)
                {
                    keywordDesText.text = "";
                }
            });
        }
    }

    /// <summary>
    /// 根据技能的关键词列表，生成"关键词:关键词描述"格式的文本
    /// </summary>
    private void UpdateKeywordText(SkillBase skill)
    {
        if (keywordDesText == null)
        {
            return;
        }

        CharacterSkillBase characterSkill = skill as CharacterSkillBase;
        if (characterSkill == null || characterSkill.words == null || characterSkill.words.Count == 0)
        {
            keywordDesText.text = "";
            return;
        }

        if (m_keywordConfig == null)
        {
            keywordDesText.text = "";
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

        keywordDesText.text = sb.ToString();
    }
}
