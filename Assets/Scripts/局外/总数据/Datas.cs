using System;
using System.Collections.Generic;
using UnityEngine;

public enum TemporaryBattleModifierRuntimeEventType
{
    PlayerCharacterSwapped
}

public static class BattleRuntimeEvents//战斗事件
{
    public static event Action PlayerCharacterSwapped;

    public static void RaisePlayerCharacterSwapped()
    {
        PlayerCharacterSwapped?.Invoke();
    }
}

public interface ITemporaryBattleModifierBehavior//战斗增益行为接口
{
    void HandleRuntimeEvent(Datas datas, TemporaryBattleModifierData modifier, TemporaryBattleModifierRuntimeEventType runtimeEventType);
}

public sealed class SwapGoldTemporaryBattleModifierBehavior : ITemporaryBattleModifierBehavior
{
    public void HandleRuntimeEvent(Datas datas, TemporaryBattleModifierData modifier, TemporaryBattleModifierRuntimeEventType runtimeEventType)
    {
        if (datas == null || modifier == null || runtimeEventType != TemporaryBattleModifierRuntimeEventType.PlayerCharacterSwapped)
        {
            return;
        }

        datas.ModifyGold(modifier.goldPerSwap - modifier.goldPenaltyPerSwap);
    }
}

public static class TemporaryBattleModifierBehaviorRegistry
{
    private static readonly Dictionary<LevelEventOptionType, ITemporaryBattleModifierBehavior> s_runtimeBehaviors =
        new Dictionary<LevelEventOptionType, ITemporaryBattleModifierBehavior>
        {
            { LevelEventOptionType.SwapForProfit, new SwapGoldTemporaryBattleModifierBehavior() },
            { LevelEventOptionType.CashOutSwap, new SwapGoldTemporaryBattleModifierBehavior() }
        };

    public static bool TryGetRuntimeBehavior(LevelEventOptionType optionType, out ITemporaryBattleModifierBehavior behavior)
    {
        return s_runtimeBehaviors.TryGetValue(optionType, out behavior);
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

[Serializable]
public class TemporaryBattleModifierData
{
    public LevelEventOptionType optionType;
    [Min(0)] public int remainingBattles;
    public float playerSpeedMultiplier = 1f;
    public float playerDirectDamageMultiplier = 1f;
    public float playerDotDamageMultiplier = 1f;
    public float playerCritDamageBonus;
    public int goldPerSwap;
    public int goldPenaltyPerSwap;

    public TemporaryBattleModifierData Clone()
    {
        return new TemporaryBattleModifierData
        {
            optionType = optionType,
            remainingBattles = remainingBattles,
            playerSpeedMultiplier = playerSpeedMultiplier,
            playerDirectDamageMultiplier = playerDirectDamageMultiplier,
            playerDotDamageMultiplier = playerDotDamageMultiplier,
            playerCritDamageBonus = playerCritDamageBonus,
            goldPerSwap = goldPerSwap,
            goldPenaltyPerSwap = goldPenaltyPerSwap
        };
    }
}

public class Datas : MonoBehaviour
{
    private const int MaxTeamLevel = 99;

    public static Datas Instance;
    public event Action CharacterRosterChanged;
    public event Action ModuleStateChanged;
    public event Action<string> LevelCompleted;

    [Header("角色列表")]
    [SerializeField] private List<CharacterRosterData> characterDatas = new List<CharacterRosterData>();

    [Header("开局流派")]
    [SerializeField] private string starterChoiceSceneName = "Main";
    [SerializeField] private List<StarterBranchDefinition> starterBranches = new List<StarterBranchDefinition>();
    [SerializeField] private string selectedStarterBranchId;

    [Header("关卡进度")]
    [SerializeField] private List<LevelSelectionFloorData> levelFloors = new List<LevelSelectionFloorData>();
    [SerializeField] private int currentFloorIndex;
    [SerializeField] private List<string> completedLevelIds = new List<string>();
    [SerializeField] private List<TemporaryBattleModifierData> activeBattleModifiers = new List<TemporaryBattleModifierData>();

    [Header("队伍成长")]
    [SerializeField] private int teamLevel = 1;
    [SerializeField] private float currentExp = 0f;
    [SerializeField] private float baseExpToNextLevel = 100f;
    [SerializeField] private float expGrowthFactor = 1f;
    [SerializeField] private int gold;

    [Header("模块数据")]
    [SerializeField] private bool hasModuleState;
    [SerializeField] private List<GridModuleDefinition> ownedModules = new List<GridModuleDefinition>();
    [SerializeField] private List<PlacedModuleData> placedModules = new List<PlacedModuleData>();

    [SerializeField] private int backpackWidth = 4;
    
    private readonly Dictionary<CharacterType, CharacterRosterData> m_characterTypeLookup = new Dictionary<CharacterType, CharacterRosterData>();
    private readonly List<TemporaryBattleModifierData> m_battleModifierSnapshot = new List<TemporaryBattleModifierData>();
    private bool m_characterLookupBuilt;
    private bool m_ownedModulesRuntimePrepared;
    private bool m_hasBattleModifierSnapshot;
    private bool m_isSubscribedToBattleRuntimeEvents;

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

        currentFloorIndex = Mathf.Clamp(currentFloorIndex, 0, Mathf.Max(0, levelFloors.Count - 1));
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
    #region 背包扩容
    public void AddBackpackSlot()
    {
        backpackWidth++;
    }
    public int GetBackpackWidth()
    {
        return backpackWidth;
    }
    #endregion
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

    public void SetLevelFloors(IEnumerable<LevelSelectionFloorData> floors)
    {
        levelFloors.Clear();
        currentFloorIndex = 0;

        if (floors == null)
        {
            return;
        }

        var registeredIds = new HashSet<string>();
        foreach (LevelSelectionFloorData floor in floors)
        {
            if (floor == null)
            {
                continue;
            }

            string floorId = string.IsNullOrWhiteSpace(floor.floorId)
                ? $"Floor_{levelFloors.Count + 1}"
                : floor.floorId;
            if (!registeredIds.Add(floorId))
            {
                continue;
            }

            levelFloors.Add(floor);
        }

        currentFloorIndex = Mathf.Clamp(currentFloorIndex, 0, Mathf.Max(0, levelFloors.Count - 1));
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

        ModifyGold(amount);
    }

    public void ModifyGold(int amount)
    {
        if (amount == 0)
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
    #region 局外接入战斗增益
    public void AddTemporaryBattleModifier(TemporaryBattleModifierData modifier)
    {
        if (modifier == null || modifier.remainingBattles <= 0)
        {
            return;
        }

        activeBattleModifiers.Add(modifier.Clone());
    }

    public void BeginBattleModifierSession()
    {
        SubscribeBattleRuntimeEvents();
        m_battleModifierSnapshot.Clear();

        for (int i = 0; i < activeBattleModifiers.Count; i++)
        {
            TemporaryBattleModifierData modifier = activeBattleModifiers[i];
            if (modifier == null || modifier.remainingBattles <= 0)
            {
                continue;
            }

            m_battleModifierSnapshot.Add(modifier.Clone());
        }

        m_hasBattleModifierSnapshot = m_battleModifierSnapshot.Count > 0;
    }

    public void CompleteBattleModifierSession(bool consumeBattleCount = true)
    {
        UnsubscribeBattleRuntimeEvents();

        if (consumeBattleCount)
        {
            for (int i = activeBattleModifiers.Count - 1; i >= 0; i--)
            {
                TemporaryBattleModifierData modifier = activeBattleModifiers[i];
                if (modifier == null)
                {
                    activeBattleModifiers.RemoveAt(i);
                    continue;
                }

                modifier.remainingBattles = Mathf.Max(0, modifier.remainingBattles - 1);
                if (modifier.remainingBattles <= 0)
                {
                    activeBattleModifiers.RemoveAt(i);
                }
            }
        }

        m_battleModifierSnapshot.Clear();
        m_hasBattleModifierSnapshot = false;
    }

    public float GetPlayerSpeedMultiplier()
    {
        IReadOnlyList<TemporaryBattleModifierData> modifiers = GetEffectiveBattleModifiers();
        float multiplier = 1f;
        for (int i = 0; i < modifiers.Count; i++)
        {
            TemporaryBattleModifierData modifier = modifiers[i];
            if (modifier != null)
            {
                multiplier *= Mathf.Max(0.01f, modifier.playerSpeedMultiplier);
            }
        }

        return multiplier;
    }

    public float GetPlayerDirectDamageMultiplier()
    {
        IReadOnlyList<TemporaryBattleModifierData> modifiers = GetEffectiveBattleModifiers();
        float multiplier = 1f;
        for (int i = 0; i < modifiers.Count; i++)
        {
            TemporaryBattleModifierData modifier = modifiers[i];
            if (modifier != null)
            {
                multiplier *= Mathf.Max(0.01f, modifier.playerDirectDamageMultiplier);
            }
        }

        return multiplier;
    }

    public float GetPlayerDotDamageMultiplier()
    {
        IReadOnlyList<TemporaryBattleModifierData> modifiers = GetEffectiveBattleModifiers();
        float multiplier = 1f;
        for (int i = 0; i < modifiers.Count; i++)
        {
            TemporaryBattleModifierData modifier = modifiers[i];
            if (modifier != null)
            {
                multiplier *= Mathf.Max(0.01f, modifier.playerDotDamageMultiplier);
            }
        }

        return multiplier;
    }

    public float GetPlayerCritDamageBonus()
    {
        IReadOnlyList<TemporaryBattleModifierData> modifiers = GetEffectiveBattleModifiers();
        float bonus = 0f;
        for (int i = 0; i < modifiers.Count; i++)
        {
            TemporaryBattleModifierData modifier = modifiers[i];
            if (modifier != null)
            {
                bonus += modifier.playerCritDamageBonus;
            }
        }

        return bonus;
    }

    public void NotifyBattleModifierRuntimeEvent(TemporaryBattleModifierRuntimeEventType runtimeEventType)
    {
        IReadOnlyList<TemporaryBattleModifierData> modifiers = GetEffectiveBattleModifiers();
        for (int i = 0; i < modifiers.Count; i++)
        {
            TemporaryBattleModifierData modifier = modifiers[i];
            if (modifier == null)
            {
                continue;
            }

            if (!TemporaryBattleModifierBehaviorRegistry.TryGetRuntimeBehavior(modifier.optionType, out ITemporaryBattleModifierBehavior behavior))
            {
                continue;
            }

            behavior.HandleRuntimeEvent(this, modifier, runtimeEventType);
        }
    }

    private void SubscribeBattleRuntimeEvents()
    {
        if (m_isSubscribedToBattleRuntimeEvents)
        {
            return;
        }

        BattleRuntimeEvents.PlayerCharacterSwapped += HandlePlayerCharacterSwapped;
        m_isSubscribedToBattleRuntimeEvents = true;
    }

    private void UnsubscribeBattleRuntimeEvents()
    {
        if (!m_isSubscribedToBattleRuntimeEvents)
        {
            return;
        }

        BattleRuntimeEvents.PlayerCharacterSwapped -= HandlePlayerCharacterSwapped;
        m_isSubscribedToBattleRuntimeEvents = false;
    }

    private void HandlePlayerCharacterSwapped()
    {
        NotifyBattleModifierRuntimeEvent(TemporaryBattleModifierRuntimeEventType.PlayerCharacterSwapped);
    }

    private IReadOnlyList<TemporaryBattleModifierData> GetEffectiveBattleModifiers()
    {
        return m_hasBattleModifierSnapshot ? m_battleModifierSnapshot : activeBattleModifiers;
    }
    #endregion
    public void SetSelectedStarterBranchId(string branchId)
    {
        selectedStarterBranchId = branchId;
    }
#region 模块系统
    public bool HasModuleState()
    {
        return hasModuleState || ownedModules.Count > 0 || placedModules.Count > 0;
    }

    public IReadOnlyList<GridModuleDefinition> GetOwnedModules()
    {
        EnsureOwnedModulesRuntimePrepared();
        return ownedModules;
    }

    public void GetPlacedModuleData(List<PlacedModuleData> results)
    {
        EnsureOwnedModulesRuntimePrepared();

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

    public GridModuleDefinition AddOwnedModule(GridModuleDefinition module)
    {
        if (module == null)
        {
            return null;
        }

        EnsureOwnedModulesRuntimePrepared();

        GridModuleDefinition runtimeModule = module.Clone();
        runtimeModule.RemoveFromBoard();
        ownedModules.Add(runtimeModule);
        hasModuleState = true;
        ModuleStateChanged?.Invoke();
        return runtimeModule;
    }

    public bool TryPlaceOwnedModule(GridModuleDefinition module, Vector2Int anchorCell)
    {
        EnsureOwnedModulesRuntimePrepared();

        int moduleIndex = ownedModules.IndexOf(module);
        if (moduleIndex < 0 || module == null || module.IsLoaded)
        {
            return false;
        }

        for (int i = placedModules.Count - 1; i >= 0; i--)
        {
            if (placedModules[i] != null && placedModules[i].ModuleIndex == moduleIndex)
            {
                placedModules.RemoveAt(i);
            }
        }

        placedModules.Add(new PlacedModuleData(moduleIndex, anchorCell));
        module.ApplyToBoard();
        hasModuleState = true;
        ModuleStateChanged?.Invoke();
        return true;
    }

    public bool TryPickupOwnedModule(GridModuleDefinition module)
    {
        EnsureOwnedModulesRuntimePrepared();

        int moduleIndex = ownedModules.IndexOf(module);
        if (moduleIndex < 0 || module == null)
        {
            return false;
        }

        bool removedPlacement = false;
        for (int i = placedModules.Count - 1; i >= 0; i--)
        {
            if (placedModules[i] != null && placedModules[i].ModuleIndex == moduleIndex)
            {
                placedModules.RemoveAt(i);
                removedPlacement = true;
            }
        }

        if (!removedPlacement && !module.IsLoaded)
        {
            return false;
        }

        module.RemoveFromBoard();
        hasModuleState = HasModuleState();
        ModuleStateChanged?.Invoke();
        return true;
    }

    private void EnsureOwnedModulesRuntimePrepared()
    {
        if (m_ownedModulesRuntimePrepared)
        {
            return;
        }

        m_ownedModulesRuntimePrepared = true;

        for (int i = 0; i < ownedModules.Count; i++)
        {
            if (ownedModules[i] != null)
            {
                GridModuleDefinition runtimeModule = ownedModules[i].Clone();
                runtimeModule.RemoveFromBoard();
                ownedModules[i] = runtimeModule;
            }
        }

        for (int i = 0; i < placedModules.Count; i++)
        {
            if (placedModules[i] == null)
            {
                continue;
            }

            int moduleIndex = placedModules[i].ModuleIndex;
            if (moduleIndex < 0 || moduleIndex >= ownedModules.Count)
            {
                continue;
            }

            GridModuleDefinition module = ownedModules[moduleIndex];
            if (module != null && !module.IsLoaded)
            {
                module.ApplyToBoard();
            }
        }

        hasModuleState = HasModuleState();
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
    public void NotifyCharacterRosterChanged()
    {
        CharacterRosterChanged?.Invoke();
    }
}
