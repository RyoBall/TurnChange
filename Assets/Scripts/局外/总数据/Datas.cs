using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ModuleData
{
    [SerializeField] private string moduleName = "新模块";
    [SerializeField] private Color color = new Color(0.28f, 0.78f, 1f, 0.9f);
    [SerializeField] private List<Vector2Int> cells = new List<Vector2Int>();

    public static ModuleData FromDefinition(GridModuleDefinition module)
    {
        ModuleData data = new ModuleData();
        if (module == null)
        {
            data.cells.Add(Vector2Int.zero);
            return data;
        }

        data.moduleName = module.moduleName;
        data.color = module.color;
        data.cells.Clear();

        if (module.cells != null && module.cells.Count > 0)
        {
            for (int i = 0; i < module.cells.Count; i++)
            {
                data.cells.Add(module.cells[i]);
            }
        }
        else
        {
            data.cells.Add(Vector2Int.zero);
        }

        return data;
    }

    public GridModuleDefinition ToDefinition()
    {
        GridModuleDefinition definition = new GridModuleDefinition();
        definition.moduleName = moduleName;
        definition.color = color;
        definition.cells = new List<Vector2Int>();

        if (cells != null && cells.Count > 0)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                definition.cells.Add(cells[i]);
            }
        }
        else
        {
            definition.cells.Add(Vector2Int.zero);
        }

        return definition;
    }
}

[System.Serializable]
public class PlacedModuleData
{
    [SerializeField] private int moduleIndex;
    [SerializeField] private Vector2Int anchorCell;

    public int ModuleIndex => moduleIndex;
    public Vector2Int AnchorCell => anchorCell;

    public PlacedModuleData()
    {
    }

    public PlacedModuleData(int moduleIndex, Vector2Int anchorCell)
    {
        this.moduleIndex = moduleIndex;
        this.anchorCell = anchorCell;
    }
}

public class Datas : MonoBehaviour
{
    private const int MaxTeamLevel = 99;

    public static Datas Instance;
    public event Action CharacterRosterChanged;
    public event Action<string> LevelCompleted;

    [Header("角色列表")]
    [SerializeField] private List<CharacterRosterData> characterDatas = new List<CharacterRosterData>();

    [Header("开局流派")]
    [SerializeField] private string starterChoiceSceneName = "Main";
    [SerializeField] private List<StarterBranchDefinition> starterBranches = new List<StarterBranchDefinition>();
    [SerializeField] private string selectedStarterBranchId;

    [Header("关卡进度")]
    [SerializeField] private List<LevelSelectionData> allLevels = new List<LevelSelectionData>();
    [SerializeField] private List<string> completedLevelIds = new List<string>();

    [Header("队伍成长")]
    [SerializeField] private int teamLevel = 1;
    [SerializeField] private float currentExp = 0f;
    [SerializeField] private float baseExpToNextLevel = 100f;
    [SerializeField] private float expGrowthFactor = 1f;
    [SerializeField] private int gold;

    [Header("模块数据")]
    [SerializeField] private bool hasModuleState;
    [SerializeField] private List<ModuleData> ownedModules = new List<ModuleData>();
    [SerializeField] private List<PlacedModuleData> placedModules = new List<PlacedModuleData>();
    private readonly Dictionary<CharacterType, CharacterRosterData> m_characterTypeLookup = new Dictionary<CharacterType, CharacterRosterData>();
    private bool m_characterLookupBuilt;

    public bool HasSelectedStarterBranch => !string.IsNullOrWhiteSpace(selectedStarterBranchId);
    public string SelectedStarterBranchId => selectedStarterBranchId;
    public string StarterChoiceSceneName => starterChoiceSceneName;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializeCharacterLookup();
        NormalizeUnlockedCharacters();

        teamLevel = Mathf.Clamp(teamLevel, 1, MaxTeamLevel);
        currentExp = Mathf.Max(0f, currentExp);
        gold = Mathf.Max(0, gold);
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool AddCharacterData(CharacterType characterType)
    {
        InitializeCharacterLookup();
        if (!m_characterTypeLookup.TryGetValue(characterType, out CharacterRosterData rosterData) || rosterData == null)
        {
            Debug.LogWarning($"[Datas] 未找到 CharacterType 对应的角色资源: {characterType}", this);
            return false;
        }

        if (ContainsCharacter(rosterData))
        {
            return false;
        }

        characterDatas.Add(rosterData);
        return true;
    }

    public CharacterRosterData GetCharacterRoster(CharacterType characterType)
    {
        InitializeCharacterLookup();
        return m_characterTypeLookup.TryGetValue(characterType, out CharacterRosterData data) ? data : null;
    }

    public IReadOnlyList<CharacterRosterData> GetUnlockedCharacterRosters()
    {
        return characterDatas;
    }

    public void ClearUnlockedCharacters()
    {
        if (characterDatas.Count == 0)
        {
            return;
        }

        characterDatas.Clear();
    }

    public List<StarterBranchDefinition> GetStarterBranchesBuffer()
    {
        if (starterBranches == null)
        {
            starterBranches = new List<StarterBranchDefinition>();
        }

        return starterBranches;
    }

    public IReadOnlyList<LevelSelectionData> GetAllLevels()
    {
        return allLevels;
    }

    public IReadOnlyList<string> GetCompletedLevelIds()
    {
        return completedLevelIds;
    }

    public int GetCompletedLevelCount()
    {
        return completedLevelIds != null ? completedLevelIds.Count : 0;
    }

    public void SetAllLevels(IEnumerable<LevelSelectionData> levels)
    {
        allLevels.Clear();
        if (levels == null)
        {
            return;
        }

        var registeredIds = new HashSet<string>();
        foreach (LevelSelectionData level in levels)
        {
            if (level == null)
            {
                continue;
            }

            string levelId = level.levelId ?? string.Empty;
            if (!registeredIds.Add(levelId))
            {
                continue;
            }

            allLevels.Add(level);
        }
    }

    public bool IsLevelCompleted(string levelId)
    {
        if (string.IsNullOrWhiteSpace(levelId))
        {
            return false;
        }

        return completedLevelIds.Contains(levelId);
    }

    public void MarkLevelCompleted(string levelId)
    {
        if (string.IsNullOrWhiteSpace(levelId) || completedLevelIds.Contains(levelId))
        {
            return;
        }

        completedLevelIds.Add(levelId);
        LevelCompleted?.Invoke(levelId);
    }

    public int GetTeamLevel()
    {
        return Mathf.Clamp(teamLevel, 1, MaxTeamLevel);
    }

    public float GetCurrentExp()
    {
        return currentExp;
    }

    public float GetExpToNextLevel()
    {
        float baseExp = Mathf.Max(1f, baseExpToNextLevel);
        float growth = Mathf.Max(1f, expGrowthFactor);
        return baseExp * Mathf.Pow(growth, Mathf.Max(0, GetTeamLevel() - 1));
    }

    public int GetGold()
    {
        return Mathf.Max(0, gold);
    }

    public void AddGold(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        gold = Mathf.Max(0, gold + amount);
    }

    public void AddExperience(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        currentExp += amount;

        while (currentExp >= GetExpToNextLevel())
        {
            currentExp -= GetExpToNextLevel();
            teamLevel = Mathf.Min(GetTeamLevel() + 1, MaxTeamLevel);
        }
    }

    public void ApplyBattleRewards(int experienceReward, int goldReward)
    {
        AddGold(goldReward);
        AddExperience(experienceReward);
    }

    public void SetSelectedStarterBranchId(string branchId)
    {
        selectedStarterBranchId = branchId;
    }

    public bool HasModuleState()
    {
        return hasModuleState;
    }

    public List<GridModuleDefinition> CreateOwnedModuleDefinitions()
    {
        List<GridModuleDefinition> modules = new List<GridModuleDefinition>();

        for (int i = 0; i < ownedModules.Count; i++)
        {
            if (ownedModules[i] != null)
            {
                modules.Add(ownedModules[i].ToDefinition());
            }
        }

        return modules;
    }

    public void GetPlacedModuleData(List<PlacedModuleData> results)
    {
        if (results == null)
        {
            return;
        }

        results.Clear();
        for (int i = 0; i < placedModules.Count; i++)
        {
            if (placedModules[i] != null)
            {
                results.Add(new PlacedModuleData(placedModules[i].ModuleIndex, placedModules[i].AnchorCell));
            }
        }
    }

    public void SaveModuleState(IReadOnlyList<GridModuleDefinition> modules, IReadOnlyList<PlacedModuleData> placements)
    {
        ownedModules.Clear();
        placedModules.Clear();

        if (modules != null)
        {
            for (int i = 0; i < modules.Count; i++)
            {
                if (modules[i] != null)
                {
                    ownedModules.Add(ModuleData.FromDefinition(modules[i]));
                }
            }
        }

        if (placements != null)
        {
            for (int i = 0; i < placements.Count; i++)
            {
                if (placements[i] != null)
                {
                    placedModules.Add(new PlacedModuleData(placements[i].ModuleIndex, placements[i].AnchorCell));
                }
            }
        }

        hasModuleState = true;
    }

    private void InitializeCharacterLookup()
    {
        if (m_characterLookupBuilt)
        {
            return;
        }

        m_characterLookupBuilt = true;
        m_characterTypeLookup.Clear();

        CharacterRosterData[] resourceCharacters = Resources.LoadAll<CharacterRosterData>("配置可编程物体/参战者/角色");
        for (int i = 0; i < resourceCharacters.Length; i++)
        {
            CharacterRosterData rosterData = resourceCharacters[i];
            if (rosterData == null)
            {
                continue;
            }

            if (m_characterTypeLookup.TryGetValue(rosterData.characterType, out CharacterRosterData existingData)
                && existingData != null
                && existingData != rosterData)
            {
                Debug.LogWarning($"[Datas] CharacterType {rosterData.characterType} 重复映射到多个角色资源，将使用最后加载到的资源: {rosterData.name}", this);
            }

            m_characterTypeLookup[rosterData.characterType] = rosterData;
        }
    }

    private void NormalizeUnlockedCharacters()
    {
        if (characterDatas == null)
        {
            characterDatas = new List<CharacterRosterData>();
            return;
        }

        var normalizedCharacters = new List<CharacterRosterData>(characterDatas.Count);
        var registeredIds = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < characterDatas.Count; i++)
        {
            CharacterRosterData rosterData = characterDatas[i];
            if (rosterData == null)
            {
                continue;
            }

            if (m_characterTypeLookup.TryGetValue(rosterData.characterType, out CharacterRosterData mappedRosterData)
                && mappedRosterData != null)
            {
                rosterData = mappedRosterData;
            }

            string characterId = rosterData.GetCharacterId();
            if (string.IsNullOrWhiteSpace(characterId) || !registeredIds.Add(characterId))
            {
                continue;
            }

            normalizedCharacters.Add(rosterData);
        }

        if (normalizedCharacters.Count == characterDatas.Count)
        {
            bool sameOrder = true;
            for (int i = 0; i < normalizedCharacters.Count; i++)
            {
                if (normalizedCharacters[i] != characterDatas[i])
                {
                    sameOrder = false;
                    break;
                }
            }

            if (sameOrder)
            {
                return;
            }
        }

        characterDatas.Clear();
        characterDatas.AddRange(normalizedCharacters);
    }

    private bool ContainsCharacter(CharacterRosterData rosterData)
    {
        if (rosterData == null)
        {
            return false;
        }

        string characterId = rosterData.GetCharacterId();
        if (string.IsNullOrWhiteSpace(characterId))
        {
            return false;
        }

        for (int i = 0; i < characterDatas.Count; i++)
        {
            CharacterRosterData data = characterDatas[i];
            if (data != null && string.Equals(data.GetCharacterId(), characterId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
    public void NotifyCharacterRosterChanged()
    {
        CharacterRosterChanged?.Invoke();
    }
}
