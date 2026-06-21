using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
[DisallowMultipleComponent]
public class PreparationPanelView : MonoBehaviour
{
    public static event System.Action CharacterChoosePanelOpened;
    public GameObject panelRoot;
    public static PreparationPanelView Instance { get; private set; }
    private static event System.Action FirstPreparationOpened;

    [Header("角色选择")]
    [SerializeField] private Button firstSlotButton;
    [SerializeField] private Button secondSlotButton;
    [SerializeField] private RectTransform characterListPanel;
    [SerializeField] private Transform characterButtonRoot;
    [SerializeField] private CharacterSelectButtonUI characterButtonPrefab;
    [SerializeField] private List<RectTransform> characterButtonPositions = new List<RectTransform>();
    [SerializeField] private Image firstSelectedCharacterImage;
    [SerializeField] private Image secondSelectedCharacterImage;
    [SerializeField] private Image firstCharacterIllustrationImage;
    [SerializeField] private Image secondCharacterIllustrationImage;
    [SerializeField] private float listHiddenX = -680f;
    [SerializeField] private float listShownX = 0f;
    [SerializeField] private float listSlideSmoothTime = 0.12f;

    [Header("敌人信息")]
    [SerializeField] private List<RectTransform> enemyInfoPositions = new List<RectTransform>();
    [SerializeField] private GameObject enemyInfoPrefab;

    [Header("关卡名称")]
    [SerializeField] private TMP_Text levelNameText;

    public LevelSelectionData CurrentLevelData { get; private set; }

    public bool HasEnoughSelectedCharacters
    {
        get
        {
            return m_selectedCharacters.Count >= 2 && m_selectedCharacters[0] != null && m_selectedCharacters[1] != null;
        }
    }

    public IReadOnlyList<LevelEnemyEntry> CurrentEnemies
    {
        get
        {
            if (CurrentLevelData == null)
            {
                return System.Array.Empty<LevelEnemyEntry>();
            }

            return CurrentLevelData.GetWaveEnemies(0);
        }
    }

    public IReadOnlyList<LevelEnemyWaveData> CurrentEnemyWaves => CurrentLevelData != null
        ? CurrentLevelData.GetEnemyWaves()
        : System.Array.Empty<LevelEnemyWaveData>();

    public IReadOnlyList<CharacterRosterData> SelectedFieldCharacters
    {
        get
        {
            return m_selectedCharacters;
        }

    }

    private readonly List<CharacterSelectButtonUI> m_characterButtons = new List<CharacterSelectButtonUI>();
    private readonly List<EnemyInfoDisplayUI> m_enemyInfoDisplays = new List<EnemyInfoDisplayUI>();
    private static readonly List<CharacterRosterData> m_selectedCharacters = new List<CharacterRosterData>();
    private static bool s_HasRaisedFirstPreparationOpenEvent;
    private Vector2 m_ListVelocity;
    private bool m_IsCharacterListVisible;
    private int m_activeTargetSlot = -1; // 0 = first slot, 1 = second slot, -1 = none

    private void Awake()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        Instance = this;
        BindSlotButtons();
        SetCharacterListVisible(false, true);
        RefreshSelectedCharacterImages();
    }

    private void OnEnable()
    {
        BindSlotButtons();
        SubscribeToDataSource();
        RebuildCharacterButtons();
        RefreshSelectedCharacterImages();
    }

    private void OnDisable()
    {
        UnsubscribeFromDataSource();
        UnbindSlotButtons();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        UnsubscribeFromDataSource();
        UnbindSlotButtons();
    }

    private void BindSlotButtons()
    {
        if (firstSlotButton != null)
        {
            firstSlotButton.onClick.RemoveAllListeners();
            firstSlotButton.onClick.AddListener(() => OpenCharacterListForSlot(0));
        }

        if (secondSlotButton != null)
        {
            secondSlotButton.onClick.RemoveAllListeners();
            secondSlotButton.onClick.AddListener(() => OpenCharacterListForSlot(1));
        }
    }

    private void UnbindSlotButtons()
    {
        if (firstSlotButton != null)
        {
            firstSlotButton.onClick.RemoveAllListeners();
        }

        if (secondSlotButton != null)
        {
            secondSlotButton.onClick.RemoveAllListeners();
        }
    }

    private void OpenCharacterListForSlot(int slotIndex)
    {
        CharacterChoosePanelOpened?.Invoke();
        m_activeTargetSlot = slotIndex;
        SetCharacterListVisible(true);
        RebuildCharacterButtons();
    }

    private void Update()
    {
        UpdateCharacterListPosition();

        if (m_IsCharacterListVisible && Input.GetMouseButtonDown(1))
        {
            SetCharacterListVisible(false);
        }
    }

    public void OpenWithLevelData(LevelSelectionData levelData)
    {
        CurrentLevelData = levelData;
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        RaiseFirstPreparationOpenEventIfNeeded();

        panelRoot.SetActive(true);
        RebuildCharacterButtons();
        RefreshSelectedCharacterImages();
        RebuildEnemyInfoDisplays();

        if (levelNameText != null && levelData != null)
        {
            levelNameText.text = levelData.levelName;
        }
    }

    public void Close()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        SetCharacterListVisible(false, true);
        panelRoot.SetActive(false);
    }

    private void RaiseFirstPreparationOpenEventIfNeeded()
    {
        if (s_HasRaisedFirstPreparationOpenEvent)
        {
            return;
        }

        s_HasRaisedFirstPreparationOpenEvent = true;
        FirstPreparationOpened?.Invoke();
    }

    private void SubscribeToDataSource()
    {
        if (Datas.Instance == null)
        {
            return;
        }

        Datas.Instance.CharacterRosterChanged -= HandleCharacterRosterChanged;
        Datas.Instance.CharacterRosterChanged += HandleCharacterRosterChanged;
    }

    private void UnsubscribeFromDataSource()
    {
        if (Datas.Instance == null)
        {
            return;
        }

        Datas.Instance.CharacterRosterChanged -= HandleCharacterRosterChanged;
    }

    private void HandleCharacterRosterChanged()
    {
        RemoveUnavailableSelectedCharacters();
        RebuildCharacterButtons();
        RefreshSelectedCharacterImages();
    }

    private void RebuildCharacterButtons()
    {
        ClearCharacterButtons();

        if (characterButtonRoot == null || characterButtonPrefab == null || Datas.Instance == null)
        {
            return;
        }

        IReadOnlyList<CharacterRosterData> characterDatas = Datas.Instance.GetUnlockedCharacterRosters();
        if (characterDatas == null)
        {
            return;
        }

        for (int i = 0; i < characterDatas.Count; i++)
        {
            CharacterRosterData characterData = characterDatas[i];
            if (characterData == null)
            {
                continue;
            }

            CharacterSelectButtonUI button = Instantiate(characterButtonPrefab, characterButtonRoot);
            ApplyCharacterButtonPosition(button.RectTransform, m_characterButtons.Count);
            button.Bind(characterData, ToggleCharacterSelection, m_selectedCharacters.Contains(characterData));
            m_characterButtons.Add(button);
        }

        RefreshCharacterButtonSelectionState();
    }

    private void ClearCharacterButtons()
    {
        for (int i = m_characterButtons.Count - 1; i >= 0; i--)
        {
            if (m_characterButtons[i] == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(m_characterButtons[i].gameObject);
            }
            else
            {
                DestroyImmediate(m_characterButtons[i].gameObject);
            }
        }

        m_characterButtons.Clear();
    }

    private void RebuildEnemyInfoDisplays()
    {
        ClearEnemyInfoDisplays();

        if (enemyInfoPrefab == null || CurrentLevelData == null)
        {
            return;
        }

        List<LevelEnemyEntry> distinctEnemies = CurrentLevelData.GetDistinctEnemies();
        if (distinctEnemies == null || distinctEnemies.Count == 0)
        {
            Debug.LogWarning("[PreparationPanelView] 当前关卡没有敌人数据，无法显示敌人信息。", this);
            return;
        }

        for (int i = 0; i < distinctEnemies.Count; i++)
        {
            EnemyInfoDisplayUI display = Instantiate(enemyInfoPrefab, enemyInfoPositions[i]).GetComponent<EnemyInfoDisplayUI>();

            display.SetEnemyData(distinctEnemies[i]);
            m_enemyInfoDisplays.Add(display);
            Debug.Log($"[PreparationPanelView] 显示敌人信息：{distinctEnemies[i].enemyData.enemyName} (Lv.{distinctEnemies[i].level})", this);
        }
    }

    private void ClearEnemyInfoDisplays()
    {
        for (int i = m_enemyInfoDisplays.Count - 1; i >= 0; i--)
        {
            if (m_enemyInfoDisplays[i] == null) continue;

            if (Application.isPlaying)
            {
                Destroy(m_enemyInfoDisplays[i].gameObject);
            }
            else
            {
                DestroyImmediate(m_enemyInfoDisplays[i].gameObject);
            }
        }

        m_enemyInfoDisplays.Clear();
    }

    private void RemoveUnavailableSelectedCharacters()
    {
        if (Datas.Instance == null)
        {
            m_selectedCharacters.Clear();
            return;
        }

        IReadOnlyList<CharacterRosterData> currentCharacters = Datas.Instance.GetUnlockedCharacterRosters();
        for (int i = m_selectedCharacters.Count - 1; i >= 0; i--)
        {
            CharacterRosterData selectedCharacter = m_selectedCharacters[i];
            bool stillExists = false;

            for (int j = 0; j < currentCharacters.Count; j++)
            {
                if (currentCharacters[j] == selectedCharacter)
                {
                    stillExists = true;
                    break;
                }
            }

            if (!stillExists)
            {
                m_selectedCharacters.RemoveAt(i);
            }
        }
    }
    private void ToggleCharacterSelection(CharacterRosterData characterData)
    {
        if (characterData == null)
        {
            return;
        }
        // If a target slot is active, place character into that slot
        if (m_activeTargetSlot >= 0 && m_activeTargetSlot <= 1)
        {
            // ensure two slots
            while (m_selectedCharacters.Count < 2)
            {
                m_selectedCharacters.Add(null);
            }

            // if the character is in the other slot, remove it there first
            int otherSlot = m_activeTargetSlot == 0 ? 1 : 0;
            if (otherSlot < m_selectedCharacters.Count && m_selectedCharacters[otherSlot] == characterData)
            {
                m_selectedCharacters[otherSlot] = null;
            }

            m_selectedCharacters[m_activeTargetSlot] = characterData;
            m_activeTargetSlot = -1;
            RefreshCharacterButtonSelectionState();
            RefreshSelectedCharacterImages();
            SetCharacterListVisible(false);
            return;
        }

        // fallback: toggle behavior (keep existing semantics)
        if (m_selectedCharacters.Contains(characterData))
        {
            m_selectedCharacters.Remove(characterData);
        }
        else
        {
            if (m_selectedCharacters.Count >= 2)
            {
                Debug.LogWarning("[PreparationPanelView] 优先出场角色最多只能选择两名。", this);
                return;
            }

            m_selectedCharacters.Add(characterData);
        }

        RefreshCharacterButtonSelectionState();
        RefreshSelectedCharacterImages();
    }

    private void RefreshCharacterButtonSelectionState()
    {
        for (int i = 0; i < m_characterButtons.Count; i++)
        {
            CharacterSelectButtonUI button = m_characterButtons[i];
            if (button == null)
            {
                continue;
            }

            CharacterRosterData buttonCharacter = button.BoundData;
            bool selected = buttonCharacter != null && m_selectedCharacters.Contains(buttonCharacter);
            button.SetSelected(selected);
        }
    }

    private void ApplyCharacterButtonPosition(RectTransform buttonRectTransform, int index)
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

    private void RefreshSelectedCharacterImages()
    {
        ApplySelectedCharacterImage(firstSelectedCharacterImage, 0);
        ApplySelectedCharacterImage(secondSelectedCharacterImage, 1);
        ApplyCharacterIllustrationImage(firstCharacterIllustrationImage, 0);
        ApplyCharacterIllustrationImage(secondCharacterIllustrationImage, 1);
    }

    private void ApplySelectedCharacterImage(Image targetImage, int index)
    {
        if (targetImage == null)
        {
            return;
        }

        CharacterRosterData characterData = index >= 0 && index < m_selectedCharacters.Count ? m_selectedCharacters[index] : null;
        targetImage.sprite = characterData != null ? characterData.GetPortraitSprite() : null;
        targetImage.enabled = targetImage.sprite != null;
    }

    private void ApplyCharacterIllustrationImage(Image targetImage, int index)
    {
        if (targetImage == null)
        {
            return;
        }

        CharacterRosterData characterData = index >= 0 && index < m_selectedCharacters.Count ? m_selectedCharacters[index] : null;
        if (characterData == null)
        {
            targetImage.sprite = null;
            targetImage.enabled = false;
            return;
        }

        Sprite illustration = characterData.GetPreparationIllustrationSprite();
        targetImage.sprite = illustration;
        targetImage.enabled = illustration != null;

        if (illustration != null)
        {
            Vector2 size = characterData.GetPreparationIllustrationSize();
            if (size.x > 0 && size.y > 0)
            {
                targetImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
                targetImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
            }
            else
            {
                targetImage.SetNativeSize();
            }
        }
    }

    private void SetCharacterListVisible(bool visible, bool immediate = false)
    {
        m_IsCharacterListVisible = visible;
        if (characterListPanel == null)
        {
            return;
        }

        if (immediate)
        {
            Vector2 anchoredPosition = characterListPanel.anchoredPosition;
            anchoredPosition.x = visible ? listShownX : listHiddenX;
            characterListPanel.anchoredPosition = anchoredPosition;
            m_ListVelocity = Vector2.zero;
        }
    }

    private void UpdateCharacterListPosition()
    {
        if (characterListPanel == null)
        {
            return;
        }

        float targetX = m_IsCharacterListVisible ? listShownX : listHiddenX;
        float nextX = Mathf.SmoothDamp(
            characterListPanel.anchoredPosition.x,
            targetX,
            ref m_ListVelocity.x,
            Mathf.Max(0.01f, listSlideSmoothTime),
            Mathf.Infinity,
            Time.unscaledDeltaTime);

        characterListPanel.anchoredPosition = new Vector2(nextX, characterListPanel.anchoredPosition.y);
    }
}