using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CharacterData
{
    [SerializeField] private CharacterRosterData characterRosterData;
    [SerializeField, HideInInspector] private int characterLevel = 1;
    [SerializeField, HideInInspector] private float currentExp = 0f;
    [SerializeField, HideInInspector] private float expToNextLevel = 100f;

    public float GetCurrentExp() => Datas.Instance != null ? Datas.Instance.GetCurrentExp() : currentExp;

    public float GetExpToNextLevel() => Datas.Instance != null ? Datas.Instance.GetExpToNextLevel() : expToNextLevel;

    public CharacterRosterData GetRosterData()
    {
        return characterRosterData;
    }

    public string GetCharacterName()
    {
        if (characterRosterData == null)
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(characterRosterData.characterName)
            ? characterRosterData.characterID
            : characterRosterData.characterName;
    }

    public string GetCharacterID()
    {
        return characterRosterData != null ? characterRosterData.characterID : string.Empty;
    }

    public Sprite GetPortraitSprite()
    {
        return characterRosterData != null ? characterRosterData.portraitSprite : null;
    }

    public CharacterRosterData GetRosterDataOrNull()
    {
        return characterRosterData;
    }

    public int GetLevel()
    {
        return Datas.Instance != null ? Datas.Instance.GetTeamLevel() : Mathf.Clamp(characterLevel, 1, 99);
    }
}

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

    [Header("角色列表")]
    public List<CharacterData> characterDatas = new List<CharacterData>();

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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
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

    public IReadOnlyList<CharacterData> GetCharacterDatas()
    {
        return characterDatas;
    }

    public IReadOnlyList<LevelSelectionData> GetAllLevels()
    {
        return allLevels;
    }

    public IReadOnlyList<string> GetCompletedLevelIds()
    {
        return completedLevelIds;
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
        int guard = 0;
        while (GetTeamLevel() < MaxTeamLevel && guard++ < MaxTeamLevel)
        {
            float requiredExp = GetExpToNextLevel();
            if (currentExp + 0.0001f < requiredExp)
            {
                break;
            }

            currentExp -= requiredExp;
            teamLevel = Mathf.Min(MaxTeamLevel, teamLevel + 1);
        }

        if (GetTeamLevel() >= MaxTeamLevel)
        {
            currentExp = Mathf.Min(currentExp, GetExpToNextLevel());
        }
        else
        {
            currentExp = Mathf.Max(0f, currentExp);
        }
    }

    public void ApplyBattleRewards(int experienceReward, int goldReward)
    {
        AddGold(goldReward);
        AddExperience(experienceReward);
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
}
