using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PreparationPanelView : MonoBehaviour
{
    public GameObject panelRoot;
    public static PreparationPanelView Instance { get; private set; }
    private static event System.Action FirstPreparationOpened;

    [Header("角色选择")]
    [SerializeField] private Button toggleCharacterListButton;
    [SerializeField] private RectTransform characterListPanel;
    [SerializeField] private Transform characterButtonRoot;
    [SerializeField] private CharacterSelectButtonUI characterButtonPrefab;
    [SerializeField] private List<RectTransform> characterButtonPositions = new List<RectTransform>();
    [SerializeField] private Image firstSelectedCharacterImage;
    [SerializeField] private Image secondSelectedCharacterImage;
    [SerializeField] private float listHiddenX = -680f;
    [SerializeField] private float listShownX = 0f;
    [SerializeField] private float listSlideSmoothTime = 0.12f;

    public LevelSelectionData CurrentLevelData { get; private set; }

    public bool HasEnoughSelectedCharacters => m_selectedCharacters.Count >= 2;

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
    private static readonly List<CharacterRosterData> m_selectedCharacters = new List<CharacterRosterData>();
    private static bool s_HasRaisedFirstPreparationOpenEvent;
    private Vector2 m_ListVelocity;
    private bool m_IsCharacterListVisible;

    private void Awake()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        Instance = this;
        BindInternalEvents();
        BindToggleButton();
        SetCharacterListVisible(false, true);
        RefreshSelectedCharacterImages();
    }

    private void OnEnable()
    {
        BindToggleButton();
        SubscribeToDataSource();
        RebuildCharacterButtons();
        RefreshSelectedCharacterImages();
    }

    private void OnDisable()
    {
        UnsubscribeFromDataSource();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        UnbindInternalEvents();
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

    public void ToggleCharacterList()
    {
        SetCharacterListVisible(!m_IsCharacterListVisible);
    }

    private void BindToggleButton()
    {
        if (toggleCharacterListButton == null)
        {
            return;
        }

        toggleCharacterListButton.onClick.RemoveListener(ToggleCharacterList);
        toggleCharacterListButton.onClick.AddListener(ToggleCharacterList);
    }

    private void BindInternalEvents()
    {
        FirstPreparationOpened -= HandleFirstPreparationOpened;
        FirstPreparationOpened += HandleFirstPreparationOpened;
    }

    private void UnbindInternalEvents()
    {
        FirstPreparationOpened -= HandleFirstPreparationOpened;
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

    private void HandleFirstPreparationOpened()
    {
        TryFillSelectedCharactersToMinimum();
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

    private void TryFillSelectedCharactersToMinimum()
    {
        RemoveUnavailableSelectedCharacters();

        if (Datas.Instance == null || m_selectedCharacters.Count >= 2)
        {
            return;
        }

        IReadOnlyList<CharacterRosterData> currentCharacters = Datas.Instance.GetUnlockedCharacterRosters();
        if (currentCharacters == null || currentCharacters.Count == 0)
        {
            return;
        }

        List<CharacterRosterData> candidates = new List<CharacterRosterData>(currentCharacters.Count);
        for (int i = 0; i < currentCharacters.Count; i++)
        {
            CharacterRosterData character = currentCharacters[i];
            if (character == null || m_selectedCharacters.Contains(character))
            {
                continue;
            }

            candidates.Add(character);
        }

        while (m_selectedCharacters.Count < 2 && candidates.Count > 0)
        {
            int randomIndex = Random.Range(0, candidates.Count);
            m_selectedCharacters.Add(candidates[randomIndex]);
            candidates.RemoveAt(randomIndex);
        }
    }

    private void ToggleCharacterSelection(CharacterRosterData characterData)
    {
        if (characterData == null)
        {
            return;
        }

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