using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CharacterPanelView : MonoBehaviour
{
    [Header("数据")]
    [SerializeField] private Datas dataSource;

    [Header("左侧角色列表")]
    [SerializeField] private Transform characterButtonRoot;
    [SerializeField] private CharacterSelectButtonUI characterButtonPrefab;
    [SerializeField] private List<RectTransform> characterButtonPositions = new List<RectTransform>();

    [Header("顶部页签")]

    [Header("角色基础信息")]
    private Image characterIconImage;
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text experienceText;
    [SerializeField] private TMP_Text attackText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text critRateText;
    [SerializeField] private TMP_Text critDamageText;
    [SerializeField] private TMP_Text defenseText;
    [SerializeField] private TMP_Text speedText;

    [Header("技能列表")]
    [SerializeField] private Transform skillButtonRoot;
    [SerializeField] private CharacterSkillButtonUI skillButtonPrefab;
    [SerializeField] private List<RectTransform> skillButtonPositions = new List<RectTransform>();
    [SerializeField] private TMP_Text emptySkillText;

    [Header("技能介绍")]
    [SerializeField] private GameObject skillDescriptionPanel;
    [SerializeField] private TMP_Text skillDescriptionTitleText;
    [SerializeField] private TMP_Text skillDescriptionContentText;

    private readonly List<CharacterSelectButtonUI> m_characterButtons = new List<CharacterSelectButtonUI>();
    private readonly List<CharacterSkillButtonUI> m_skillButtons = new List<CharacterSkillButtonUI>();

    private CharacterRosterData m_currentCharacter;
    private Canvas m_parentCanvas;

    private void Awake()
    {
        m_parentCanvas = GetComponentInParent<Canvas>();

        HideSkillDescription();
    }

    private void Start()
    {
        ResolveDataSource();
        SubscribeToDataSource();
        RebuildCharacterButtons();
    }

    private void OnEnable()
    {
        SubscribeToDataSource();
    }

    private void OnDisable()
    {
        UnsubscribeFromDataSource();
    }

    private void Update()
    {
        if (skillDescriptionPanel == null || !skillDescriptionPanel.activeSelf)
        {
            return;
        }

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        Vector2 pointerPosition = Input.mousePosition;
        if (IsPointerInside(skillDescriptionPanel.transform as RectTransform, pointerPosition))
        {
            return;
        }

        for (int i = 0; i < m_skillButtons.Count; i++)
        {
            if (IsPointerInside(m_skillButtons[i].RectTransform, pointerPosition))
            {
                return;
            }
        }

        HideSkillDescription();
    }

    private void ResolveDataSource()//解析数据来源
    {
        if (dataSource == null)
        {
            dataSource = Datas.Instance;
        }
    }

    private void SubscribeToDataSource()
    {
        ResolveDataSource();
        if (dataSource == null)
        {
            return;
        }

        dataSource.CharacterRosterChanged -= HandleCharacterRosterChanged;
        dataSource.CharacterRosterChanged += HandleCharacterRosterChanged;
    }

    private void UnsubscribeFromDataSource()
    {
        if (dataSource == null)
        {
            return;
        }

        dataSource.CharacterRosterChanged -= HandleCharacterRosterChanged;
    }

    private void HandleCharacterRosterChanged()
    {
        ResolveDataSource();
        RebuildCharacterButtons();
    }

    private void RebuildCharacterButtons()//重建角色选择按钮
    {
        ClearSpawnedButtons(m_characterButtons);
        //安全检查
        if (dataSource == null || characterButtonRoot == null || characterButtonPrefab == null)
        {
            RefreshCharacterDisplay(null);
            return;
        }

        IReadOnlyList<CharacterRosterData> characters = dataSource.GetUnlockedCharacterRosters();
        for (int i = 0; i < characters.Count; i++)
        {
            CharacterRosterData data = characters[i];
            CharacterSelectButtonUI button = Instantiate(characterButtonPrefab, characterButtonRoot);
            ApplyCharacterButtonPosition(button.RectTransform, i);
            m_characterButtons.Add(button);
            button.Bind(data, SelectCharacter, false);
        }
        //默认选择第一个角色
        SelectCharacter(characters.Count > 0 ? characters[0] : null);
    }

    private void SelectCharacter(CharacterRosterData data)
    {
        m_currentCharacter = data;
        RefreshCharacterButtons();
        RefreshCharacterDisplay(data);
        RefreshSkillButtons(data);
        HideSkillDescription();
    }

    private void RefreshCharacterButtons()
    {
        IReadOnlyList<CharacterRosterData> characters = dataSource != null ? dataSource.GetUnlockedCharacterRosters() : System.Array.Empty<CharacterRosterData>();
        for (int i = 0; i < m_characterButtons.Count; i++)
        {
            bool selected = dataSource != null
                && i < characters.Count
                && characters[i] == m_currentCharacter;
            m_characterButtons[i].SetSelected(selected);
        }
    }

    private void RefreshCharacterDisplay(CharacterRosterData data)
    {
        bool hasData = data != null;
        bool hasLevelData = TryGetCharacterLevelData(data, out CharacterLevelData levelData);

        if (characterIconImage != null)
        {
            characterIconImage.sprite = hasData ? data.GetPortraitSprite() : null;
            characterIconImage.enabled = characterIconImage.sprite != null;
        }

        if (characterNameText != null)
        {
            characterNameText.text = hasData ? data.GetDisplayName() : "未选择角色";
        }

        if (levelText != null)
        {
            levelText.text = hasData && dataSource != null ? $" {dataSource.GetTeamLevel()}" : "等级 -";
        }

        if (experienceText != null)
        {
            experienceText.text = hasData && dataSource != null ? $"{dataSource.GetCurrentExp()}/{dataSource.GetExpToNextLevel()}" : "经验 -";
        }

        if (attackText != null)
        {
            attackText.text = hasLevelData ? $"{levelData.attack}" : "-";
        }

        if (hpText != null)
        {
            hpText.text = hasLevelData ? $"{levelData.maxHP}" : "-";
        }

        if (critRateText != null)
        {
            critRateText.text = hasLevelData ? $"{levelData.critRate * 100f:0.#}%" : "-";
        }

        if (critDamageText != null)
        {
            critDamageText.text = hasLevelData ? $"{levelData.critDamage * 100f:0.#}%" : "-";
        }

        if (defenseText != null)
        {
            defenseText.text = hasLevelData ? $"{levelData.defense}" : "-";
        }

        if (speedText != null)
        {
            speedText.text = hasLevelData ? $"{levelData.speed}" : "-";
        }
    }

    private void RefreshSkillButtons(CharacterRosterData data)
    {
        ClearSpawnedButtons(m_skillButtons);

        if (skillButtonRoot == null || skillButtonPrefab == null)
        {
            return;
        }

        int createdCount = 0;
        CharacterRosterData rosterData = data;
        if (rosterData != null)
        {
            for (int i = 0; i < rosterData.skills.Count; i++)
            {
                SkillBase skill = SkillDictionaryManager.GetSkill(rosterData.skills[i]);
                if (skill == null)
                {
                    continue;
                }

                CharacterSkillButtonUI button = Instantiate(skillButtonPrefab, skillButtonRoot);
                button.Bind(skill);
                m_skillButtons.Add(button);
                createdCount++;
                ApplySkillButtonPosition(button.RectTransform, i);
            }
            //出场技能
            var enterSkill = SkillDictionaryManager.GetSkill(rosterData.enterSkill);
            CharacterSkillButtonUI enterButton = Instantiate(skillButtonPrefab, skillButtonRoot);
            enterButton.Bind(enterSkill);
            m_skillButtons.Add(enterButton);
            createdCount++;
            ApplySkillButtonPosition(enterButton.RectTransform, createdCount - 1);
        }
        if (emptySkillText != null)
        {
            emptySkillText.gameObject.SetActive(createdCount == 0);
            if (createdCount == 0)
            {
                HideSkillDescription();
            }
        }
    }

    public void ShowSkillDescription(SkillBase skill)
    {
        if (skillDescriptionPanel == null)
        {
            return;
        }

        if (skill == null)
        {
            // treat null as hide request
            HideSkillDescription();
            return;
        }

        if (skillDescriptionTitleText != null)
        {
            skillDescriptionTitleText.text = string.IsNullOrWhiteSpace(skill.skillName) ? "技能介绍" : skill.skillName;
        }

        if (skillDescriptionContentText != null)
        {
            string description = !string.IsNullOrWhiteSpace(skill.description)
                ? skill.description
                : skill.shortDescription;
            skillDescriptionContentText.text = string.IsNullOrWhiteSpace(description) ? "暂无技能说明" : description;
        }

        StartSkillDescriptionFade(true);
    }

    public void HideSkillDescription()
    {
        StartSkillDescriptionFade(false);
    }

    [Header("技能描述淡入淡出")]
    [SerializeField] private float skillFadeDuration = 0.15f;
    private CanvasGroup m_skillDescriptionCanvasGroup;
    private Coroutine m_skillDescriptionFadeCoroutine;

    private CanvasGroup EnsureSkillDescriptionCanvasGroup()
    {
        if (skillDescriptionPanel == null)
            return null;

        if (m_skillDescriptionCanvasGroup == null)
        {
            m_skillDescriptionCanvasGroup = skillDescriptionPanel.GetComponent<CanvasGroup>();
            if (m_skillDescriptionCanvasGroup == null)
            {
                m_skillDescriptionCanvasGroup = skillDescriptionPanel.AddComponent<CanvasGroup>();
            }
        }

        return m_skillDescriptionCanvasGroup;
    }

    private void StartSkillDescriptionFade(bool show)
    {
        if (skillDescriptionPanel == null)
            return;

        CanvasGroup cg = EnsureSkillDescriptionCanvasGroup();

        if (m_skillDescriptionFadeCoroutine != null)
        {
            StopCoroutine(m_skillDescriptionFadeCoroutine);
            m_skillDescriptionFadeCoroutine = null;
        }

        m_skillDescriptionFadeCoroutine = StartCoroutine(FadeSkillDescriptionCoroutine(show, cg));
    }

    private System.Collections.IEnumerator FadeSkillDescriptionCoroutine(bool show, CanvasGroup cg)
    {
        if (cg == null)
            yield break;

        float startAlpha = cg.alpha;
        float target = show ? 1f : 0f;
        float elapsed = 0f;

        if (show)
        {
            skillDescriptionPanel.SetActive(true);
        }

        while (elapsed < skillFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, skillFadeDuration));
            cg.alpha = Mathf.Lerp(startAlpha, target, t);
            yield return null;
        }

        cg.alpha = target;

        if (!show)
        {
            skillDescriptionPanel.SetActive(false);
        }

        m_skillDescriptionFadeCoroutine = null;
    }

    private void ApplyCharacterButtonPosition(RectTransform buttonRectTransform, int index)//应用角色选择按钮的位置
    {
        if (buttonRectTransform == null)
        {
            return;
        }

        if (characterButtonPositions == null || index < 0 || index >= characterButtonPositions.Count)
        {
            return;
        }

        RectTransform targetRectTransform = characterButtonPositions[index];
        if (targetRectTransform == null)
        {
            return;
        }
        buttonRectTransform.anchoredPosition = targetRectTransform.anchoredPosition;
    }

    private void ApplySkillButtonPosition(RectTransform buttonRectTransform, int index)//应用技能按钮的位置
    {
        if (buttonRectTransform == null)
        {
            return;
        }

        if (skillButtonPositions == null || index < 0 || index >= skillButtonPositions.Count)
        {
            return;
        }

        RectTransform targetRectTransform = skillButtonPositions[index];
        if (targetRectTransform == null)
        {
            return;
        }

        buttonRectTransform.anchoredPosition = targetRectTransform.anchoredPosition;
    }

    private bool TryGetCharacterLevelData(CharacterRosterData data, out CharacterLevelData levelData)
    {
        levelData = default;
        if (data == null || dataSource == null)
        {
            return false;
        }

        string characterId = data.GetCharacterId();
        if (string.IsNullOrWhiteSpace(characterId))
        {
            return false;
        }

        return LevelDataContainer.TryGetCharacterLevelData(characterId, dataSource.GetTeamLevel(), out levelData);
    }

    private bool IsPointerInside(RectTransform rectTransform, Vector2 screenPosition)
    {
        if (rectTransform == null)
        {
            return false;
        }

        Camera eventCamera = m_parentCanvas != null && m_parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? m_parentCanvas.worldCamera
            : null;
        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition, eventCamera);
    }

    private void ClearSpawnedButtons<T>(List<T> buttonList) where T : Component
    {
        for (int i = buttonList.Count - 1; i >= 0; i--)
        {
            if (buttonList[i] == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(buttonList[i].gameObject);
            }
            else
            {
                DestroyImmediate(buttonList[i].gameObject);
            }
        }

        buttonList.Clear();
    }
}