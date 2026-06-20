using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EnterCharacterButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Character character;

    [Header("悬停缩放")]
    [SerializeField] private float m_hoverScale = 1.08f;
    [SerializeField] private float m_enterDuration = 0.35f;
    [SerializeField] private float m_exitDuration = 0.2f;

    private Button m_button;
    private RectTransform m_rectTransform;
    private Vector3 m_defaultScale;
    private Tween m_scaleTween;
    private bool m_isPointerOver;

    [Header("换人卡片信息")]
    [SerializeField] private TMP_Text m_nameText;
    [SerializeField] private TMP_Text m_hpText;
    [SerializeField] private TMP_Text m_chaosText;
    [SerializeField] private TMP_Text m_enterSkillText;
    [SerializeField] private Image m_portraitImage;

    [Header("血条与盾条")]
    [SerializeField] private Slider m_hpSlider;
    [SerializeField] private Slider m_shieldSlider;

    [Header("切入技标签")]
    [SerializeField] private RectTransform m_tagAnchor;
    [SerializeField] private GameObject m_tagPrefab;
    [SerializeField] private float m_tagSpacing = 8f;
    private readonly List<GameObject> m_spawnedTags = new List<GameObject>();

    private void Awake()
    {
        m_button = GetComponent<Button>();
        m_rectTransform = GetComponent<RectTransform>();
        m_defaultScale = m_rectTransform != null ? m_rectTransform.localScale : transform.localScale;
    }

    private void OnDisable()
    {
        // 安全复位：防止鼠标离开事件丢失导致按钮保持放大
        ResetScaleImmediate();
        m_isPointerOver = false;
    }

    private void OnDestroy()
    {
        DestroySpawnedTags();
    }

    public void Initialize(Character character)
    {
        this.character = character;
        if (m_button == null)
        {
            m_button = GetComponent<Button>();
        }

        if (m_button != null)
        {
            m_button.interactable = character != null && !character.IsSwapOnCooldown;
        }

        RefreshCardInfo();
    }

    /// <summary>
    /// 刷新换人卡片上的血量、混沌点、切入技描述和立绘显示
    /// </summary>
    private void RefreshCardInfo()
    {
        if (character == null)
        {
            return;
        }

        // 名字显示
        if (m_nameText != null)
        {
            m_nameText.text = character.combatantName;
        }

        // 血量显示
        if (m_hpText != null)
        {
            m_hpText.text = $"HP: {character.currentHP} / {character.maxHP}";
        }

        // 混沌点显示
        if (m_chaosText != null)
        {
            m_chaosText.text = $"{character.ChaosValue} / {character.MaxChaosValueConst}";
        }

        // 切入技描述
        if (m_enterSkillText != null)
        {
            CharacterSkillBase enterSkill = character.GetEnterSkillInstance();
            if (enterSkill != null)
            {
                m_enterSkillText.text = enterSkill.shortDescription;
            }
            else
            {
                m_enterSkillText.text = "无切入技";
            }
        }

        // 立绘显示
        if (m_portraitImage != null)
        {
            Sprite illustration = character.IllustrationSprite;
            if (illustration != null)
            {
                m_portraitImage.sprite = illustration;
                m_portraitImage.enabled = true;

                // 应用立绘尺寸
                Vector2 illustrationSize = character.IllustrationSize;
                if (illustrationSize.x > 0 && illustrationSize.y > 0)
                {
                    RectTransform imageRect = m_portraitImage.rectTransform;
                    imageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, illustrationSize.x);
                    imageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, illustrationSize.y);
                }
            }
            else
            {
                m_portraitImage.enabled = false;
            }
        }

        // 血条
        if (m_hpSlider != null)
        {
            m_hpSlider.maxValue = character.maxHP;
            m_hpSlider.value = character.currentHP;
        }

        // 盾条
        if (m_shieldSlider != null)
        {
            int maxShield = character.maxHP;
            m_shieldSlider.maxValue = maxShield;
            m_shieldSlider.value = character.currentShield;
        }

        // 切入技标签
        SpawnSkillTags();
    }

    /// <summary>
    /// 以 m_tagAnchor 为中心对称生成切入技的标签
    /// </summary>
    private void SpawnSkillTags()
    {
        DestroySpawnedTags();

        CharacterSkillBase enterSkill = character.GetEnterSkillInstance();
        if (enterSkill == null || enterSkill.tags == null || enterSkill.tags.Count == 0)
        {
            return;
        }

        if (m_tagAnchor == null || m_tagPrefab == null)
        {
            return;
        }

        int count = enterSkill.tags.Count;
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
                tagText.text = enterSkill.tags[i];
            }

            TagColorConfig tagColorConfig = TagColorConfig.Instance;
            if (tagColorConfig != null)
            {
                tagColorConfig.ApplyColorsToTag(tagInstance, enterSkill.tags[i]);
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

    private bool CanRespondToPointer()
    {
        return CharacterManager.Instance != null
            && CharacterManager.Instance.IsSelectingReserveCharacter
            && m_button != null
            && m_button.interactable;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        m_isPointerOver = true;
        if (CanRespondToPointer())
        {
            var targetCharacter = character != null ? character : GetComponentInParent<Character>();
            if (targetCharacter != null)
            {
                SkillDescription.Instance.ChangeDescription(targetCharacter.GetEnterSkillInstance());
            }
            PlayEnterAnimation();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        m_isPointerOver = false;
        // 无论 CanRespondToPointer 状态如何，都要播放退出动画，防止按钮卡在放大状态
        PlayExitAnimation();
        // 隐藏描述只在可响应时执行
        if (CanRespondToPointer())
        {
            SkillDescription.Instance.HideDescription();
        }
    }

    private void PlayEnterAnimation()
    {
        KillScaleTween();
        var target = m_defaultScale * m_hoverScale;
        var t = m_rectTransform != null ? m_rectTransform : (RectTransform)transform;
        // 弹性进入：先弹到更大再回落到目标大小
        m_scaleTween = t.DOScale(target, m_enterDuration)
            .SetEase(Ease.OutBack, overshoot: 0.5f);
    }

    private void PlayExitAnimation()
    {
        KillScaleTween();
        var t = m_rectTransform != null ? m_rectTransform : (RectTransform)transform;
        // 平滑离开：缓缓恢复原状
        m_scaleTween = t.DOScale(m_defaultScale, m_exitDuration)
            .SetEase(Ease.OutQuad);
    }

    private void ResetScaleImmediate()
    {
        KillScaleTween();
        var t = m_rectTransform != null ? m_rectTransform : (RectTransform)transform;
        if (t != null)
        {
            t.localScale = m_defaultScale;
        }
    }

    private void KillScaleTween()
    {
        if (m_scaleTween == null)
            return;

        m_scaleTween.Kill();
        m_scaleTween = null;
    }
}
