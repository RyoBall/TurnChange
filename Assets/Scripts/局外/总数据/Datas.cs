using System;
using System.Collections.Generic;
using UnityEngine;

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
public enum CharacterType
{
    DotMain,
    DotSub,
    DotSupport,
    DirectMain,
    DirectSub,
    DirectSupport,
}
public class Datas : MonoBehaviour
{
    private const int MaxTeamLevel = 99;

    public static Datas Instance;
    public event Action CharacterRosterChanged;
    public event Action ModuleStateChanged;
    /// <summary>序体被装载时的静态事件（供教程系统监听）</summary>
    public static event Action ModulePlacedStatic;
    public event Action BackpackWidthChanged;
    public event Action GoldChanged;
    public event Action<string> LevelCompleted;

    [Header("角色列表")]
    [SerializeField] private List<CharacterRosterData> characterDatas = new List<CharacterRosterData>();

    [Header("关卡进度")]
    [SerializeField] private List<LevelSelectionFloorData> levelFloors = new List<LevelSelectionFloorData>();
    [SerializeField] private int currentFloorIndex;
    [SerializeField] private List<string> completedLevelIds = new List<string>();
    [SerializeField] private List<TemporaryBattleModifierData> activeBattleModifiers = new List<TemporaryBattleModifierData>();

    // 战队等级累计经验阈值表: index 0 = Lv1(0), index 1 = Lv2(100), index 2 = Lv3(250) ...
    private static readonly float[] s_levelExpThresholds = new float[]
    {
        0f,     // Lv1
        100f,   // Lv2
        250f,   // Lv3
        500f,   // Lv4
        900f,   // Lv5
        1350f,  // Lv6
        1900f,  // Lv7
        2700f,  // Lv8
        3700f,  // Lv9
        4800f,  // Lv10
    };

    [Header("队伍成长")]
    [SerializeField] private int teamLevel = 1;
    [SerializeField] private float currentExp = 0f;
    [SerializeField] private int gold;

    [Header("模块数据")]
    [SerializeField] private List<GridModuleDefinition> ownedModules = new List<GridModuleDefinition>();
    [SerializeField] private List<PlacedModuleData> placedModules = new List<PlacedModuleData>();

    [SerializeField] private int backpackWidth = 4;

    [Header("关卡角色解锁")]
    [SerializeField] private LevelCharacterUnlockConfig levelCharacterUnlockConfig;

    public readonly Dictionary<CharacterType, CharacterRosterData> m_characterTypeLookup = new Dictionary<CharacterType, CharacterRosterData>();
    private bool m_characterLookupBuilt;

    private void Awake()
    {
        if (!TryClaimSingleton())
        {
            return;
        }

        InitializeCharacterLookup();
        NormalizeUnlockedCharacters();
        ClampProgressionData();
        SubscribeInternalEvents();
        MarkAsPersistent();
        EnsureTimeScaleController();
    }
    private bool TryClaimSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return false;
        }

        Instance = this;
        return true;
    }

    private void ClampProgressionData()
    {
        currentFloorIndex = Mathf.Clamp(currentFloorIndex, 0, Mathf.Max(0, levelFloors.Count - 1));
        teamLevel = Mathf.Clamp(teamLevel, 1, MaxTeamLevel);
        currentExp = Mathf.Max(0f, currentExp);
        gold = Mathf.Max(0, gold);
    }

    private void SubscribeInternalEvents()
    {
        LevelCompleted += OnLevelCompletedForCharacterUnlock;
    }

    private void MarkAsPersistent()
    {
        DontDestroyOnLoad(gameObject.transform.parent.gameObject);
    }

    private void EnsureTimeScaleController()
    {
        if (TimeScaleController.Instance == null)
        {
            gameObject.AddComponent<TimeScaleController>();
        }
    }

    private void OnDestroy()
    {
        UnsubscribeInternalEvents();
        ClearSingleton();
    }

    private void UnsubscribeInternalEvents()
    {
        LevelCompleted -= OnLevelCompletedForCharacterUnlock;
    }

    private void ClearSingleton()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    #region 背包扩容
    public void AddBackpackSlot()
    {
        backpackWidth++;
        BackpackWidthChanged?.Invoke();
    }
    public int GetBackpackWidth()
    {
        return backpackWidth;
    }
    #endregion
    #region 角色解锁
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

    public bool TryGetCharacterId(CharacterType characterType, out string characterId)
    {
        CharacterRosterData rosterData = GetCharacterRoster(characterType);
        characterId = rosterData != null ? rosterData.GetCharacterId() : string.Empty;
        return !string.IsNullOrWhiteSpace(characterId);
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
    #endregion
    #region 关卡进度相关
    public IReadOnlyList<LevelSelectionFloorData> GetLevelFloors()
    {
        return levelFloors;
    }

    public int GetLevelFloorCount()
    {
        return levelFloors != null ? levelFloors.Count : 0;
    }

    public int GetCurrentFloorIndex()
    {
        return Mathf.Clamp(currentFloorIndex, 0, Mathf.Max(0, GetLevelFloorCount() - 1));
    }

    public LevelSelectionFloorData GetCurrentFloorData()
    {
        if (levelFloors == null || levelFloors.Count == 0)
        {
            return null;
        }

        int floorIndex = GetCurrentFloorIndex();
        return floorIndex >= 0 && floorIndex < levelFloors.Count ? levelFloors[floorIndex] : null;
    }

    public IReadOnlyList<LevelSelectionData> GetCurrentFloorLevels()
    {
        LevelSelectionFloorData floorData = GetCurrentFloorData();
        UpdateLevelUnlockStates(floorData);
        return floorData != null ? floorData.GetLevels() : Array.Empty<LevelSelectionData>();
    }

    public IReadOnlyList<string> GetCompletedLevelIds()
    {
        return completedLevelIds;
    }

    public int GetCompletedLevelCount()
    {
        return completedLevelIds != null ? completedLevelIds.Count : 0;
    }

    public bool SetCurrentFloorIndex(int floorIndex)
    {
        if (levelFloors == null || floorIndex < 0 || floorIndex >= levelFloors.Count)
        {
            return false;
        }

        currentFloorIndex = floorIndex;
        return true;
    }

    public bool AdvanceToNextFloor()
    {
        if (levelFloors == null || currentFloorIndex >= levelFloors.Count - 1)
        {
            return false;
        }

        currentFloorIndex++;
        return true;
    }

    public bool IsLevelCompleted(string levelId)
    {
        if (string.IsNullOrWhiteSpace(levelId))
        {
            return false;
        }

        return completedLevelIds.Contains(levelId);
    }

    public bool IsLevelUnlocked(string levelId)
    {
        if (string.IsNullOrWhiteSpace(levelId))
        {
            return false;
        }

        LevelSelectionFloorData floorData = GetCurrentFloorData();
        if (floorData == null)
        {
            return false;
        }

        UpdateLevelUnlockStates(floorData);

        IReadOnlyList<LevelSelectionData> levels = floorData.GetLevels();
        for (int i = 0; i < levels.Count; i++)
        {
            LevelSelectionData level = levels[i];
            if (level != null && string.Equals(level.levelId, levelId, StringComparison.Ordinal))
            {
                return level.isUnlocked;
            }
        }

        return false;
    }

    public void MarkLevelCompleted(string levelId)
    {
        if (string.IsNullOrWhiteSpace(levelId) || completedLevelIds.Contains(levelId))
        {
            return;
        }

        completedLevelIds.Add(levelId);
        UpdateLevelUnlockStates(GetCurrentFloorData());
        LevelCompleted?.Invoke(levelId);
    }

    private void UpdateLevelUnlockStates(LevelSelectionFloorData floorData)
    {
        if (floorData == null)
        {
            return;
        }

        IReadOnlyList<LevelSelectionData> levels = floorData.GetLevels();
        for (int i = 0; i < levels.Count; i++)
        {
            LevelSelectionData currentLevel = levels[i];
            if (currentLevel == null)
            {
                continue;
            }

            LevelSelectionData previousLevel = GetPreviousLevel(levels, i);
            currentLevel.isUnlocked = previousLevel == null || IsLevelCompleted(previousLevel.levelId);
        }
    }

    private LevelSelectionData GetPreviousLevel(IReadOnlyList<LevelSelectionData> levels, int currentIndex)
    {
        if (levels == null)
        {
            return null;
        }

        for (int i = currentIndex - 1; i >= 0; i--)
        {
            if (levels[i] != null)
            {
                return levels[i];
            }
        }

        return null;
    }

    public int GetTeamLevel()
    {
        return Mathf.Clamp(teamLevel, 1, MaxTeamLevel);
    }
    #endregion
    #region 金币与经验相关
    public float GetCurrentExp()
    {
        return currentExp;
    }

    public float GetExpToNextLevel()
    {
        int level = GetTeamLevel();
        // 当前等级在表中没有下一级 -> 满级，返回极大值表示不可再升级
        if (level >= s_levelExpThresholds.Length)
        {
            return float.MaxValue;
        }
        // 从当前等级升到下一级所需的经验 = 下一级累计阈值 - 当前级累计阈值
        return s_levelExpThresholds[level] - s_levelExpThresholds[level - 1];
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

        ModifyGold(amount);
    }

    public void ModifyGold(int amount)
    {
        if (amount == 0)
        {
            return;
        }

        gold = Mathf.Max(0, gold + amount);
        GoldChanged?.Invoke();
    }

    public void AddExperience(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        currentExp += amount;
        int level = GetTeamLevel();

        // 用累计阈值表判断是否能升级
        while (level < s_levelExpThresholds.Length && currentExp >= s_levelExpThresholds[level])
        {
            level++;
        }

        teamLevel = Mathf.Min(level, MaxTeamLevel);
    }

    public void ApplyBattleRewards(int experienceReward, int goldReward)
    {
        AddGold(goldReward);
        AddExperience(experienceReward);
    }
    #endregion
    #region 局外接入战斗增益
    public IReadOnlyList<TemporaryBattleModifierData> GetActiveBattleModifiers()
    {
        return activeBattleModifiers;
    }

    public bool AddActiveBattleModifier(TemporaryBattleModifierData modifier)
    {
        if (modifier == null || modifier.remainingBattles <= 0)
        {
            return false;
        }

        activeBattleModifiers.Add(modifier.Clone());
        return true;
    }

    public bool RemoveActiveBattleModifierAt(int modifierIndex)
    {
        if (modifierIndex < 0 || modifierIndex >= activeBattleModifiers.Count)
        {
            return false;
        }

        activeBattleModifiers.RemoveAt(modifierIndex);
        return true;
    }
    #endregion
    #region 模块系统
    public IReadOnlyList<GridModuleDefinition> GetOwnedModuleDefinitions()
    {
        return ownedModules;
    }

    public IReadOnlyList<PlacedModuleData> GetPlacedModuleEntries()
    {
        return placedModules;
    }

    public GridModuleDefinition AddOwnedModule(GridModuleDefinition module)
    {
        if (module == null)
        {
            return null;
        }

        GridModuleDefinition runtimeModule = module.Clone();
        ownedModules.Add(runtimeModule);
        ModuleStateChanged?.Invoke();
        return runtimeModule;
    }

    public void AddPlacedModuleEntry(PlacedModuleData placedModule)
    {
        if (placedModule == null)
        {
            return;
        }

        placedModules.Add(placedModule);
    }

    public bool RemovePlacedModuleEntryAt(int entryIndex)
    {
        if (entryIndex < 0 || entryIndex >= placedModules.Count)
        {
            return false;
        }

        placedModules.RemoveAt(entryIndex);
        return true;
    }

    public void NotifyModuleStateChanged()
    {
        ModuleStateChanged?.Invoke();
        ModulePlacedStatic?.Invoke();
    }
    #endregion
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
    #region 关卡完成解锁角色
    /// <summary>
    /// 监听关卡完成事件，根据 LevelCharacterUnlockConfig 配置向角色列表中添加对应角色。
    /// </summary>
    private void OnLevelCompletedForCharacterUnlock(string levelId)
    {
        if (levelCharacterUnlockConfig == null)
        {
            return;
        }

        IReadOnlyList<CharacterType> characterTypesToUnlock = levelCharacterUnlockConfig.GetCharacterTypesForLevel(levelId);
        if (characterTypesToUnlock == null || characterTypesToUnlock.Count == 0)
        {
            return;
        }

        bool anyAdded = false;
        for (int i = 0; i < characterTypesToUnlock.Count; i++)
        {
            if (AddCharacterData(characterTypesToUnlock[i]))
            {
                anyAdded = true;
            }
        }

        if (anyAdded)
        {
            CharacterRosterChanged?.Invoke();
        }
    }
    #endregion

    public void NotifyCharacterRosterChanged()
    {
        CharacterRosterChanged?.Invoke();
    }
}
