using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PreparationPanelView : MonoBehaviour
{
    public GameObject panelRoot;
    public static PreparationPanelView Instance { get; private set; }

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
            m_selectedRosterCache.Clear();
            for (int i = 0; i < m_selectedCharacters.Count; i++)
            {
                CharacterRosterData rosterData = m_selectedCharacters[i] != null
                    ? m_selectedCharacters[i].GetRosterDataOrNull()
                    : null;

                if (rosterData != null)
                {
                    m_selectedRosterCache.Add(rosterData);
                }
            }

            return m_selectedRosterCache;
        }
    }

    private readonly List<CharacterSelectButtonUI> m_characterButtons = new List<CharacterSelectButtonUI>();
    private readonly List<CharacterData> m_selectedCharacters = new List<CharacterData>();
    private readonly List<CharacterRosterData> m_selectedRosterCache = new List<CharacterRosterData>();
    private Vector2 m_ListVelocity;
    private bool m_IsCharacterListVisible;

    private void Awake()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }
        Instance = this;
        BindToggleButton();
        SetCharacterListVisible(false, true);
        RefreshSelectedCharacterImages();
    }

    private void OnEnable()
    {
        BindToggleButton();
        RebuildCharacterButtons();
        RefreshSelectedCharacterImages();
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

    private void RebuildCharacterButtons()
    {
        ClearCharacterButtons();

        if (characterButtonRoot == null || characterButtonPrefab == null || Datas.Instance == null)
        {
            return;
        }

        IReadOnlyList<CharacterData> characterDatas = Datas.Instance.GetCharacterDatas();
        if (characterDatas == null)
        {
            return;
        }

        for (int i = 0; i < characterDatas.Count; i++)
        {
            CharacterData characterData = characterDatas[i];
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

    private void ToggleCharacterSelection(CharacterData characterData)
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

            CharacterData buttonCharacter = button.BoundData;
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

        CharacterData characterData = index >= 0 && index < m_selectedCharacters.Count ? m_selectedCharacters[index] : null;
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