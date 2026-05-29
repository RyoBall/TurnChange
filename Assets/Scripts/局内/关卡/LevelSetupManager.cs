using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-500)]
public class LevelSetupManager : MonoBehaviour
{
    public static LevelSetupManager Instance { get; private set; }

    [Header("Level Data")]
    [SerializeField] private List<CharacterRosterData> allPlayerCharacters = new List<CharacterRosterData>();
    [SerializeField] private List<CharacterRosterData> fieldPlayerCharacters = new List<CharacterRosterData>();
    [SerializeField] private List<EnemyRosterData> fieldEnemies = new List<EnemyRosterData>();

    [Header("References")]
    [SerializeField] private LevelCharacterSpawner characterSpawner;
    [SerializeField] private BattleSettlementView settlementView;
    [SerializeField] private float enemyWaveEnterDelay = 0.75f;

    private bool m_initialized;
    private bool m_battleResolved;
    private bool m_isCheckingBattleProgress;
    private int m_currentWaveIndex = -1;
    private PendingBattleLevelData m_pendingBattleLevelData;

    public List<CharacterRosterData> AllPlayerCharacters => allPlayerCharacters;
    public List<CharacterRosterData> FieldPlayerCharacters => fieldPlayerCharacters;
    public List<EnemyRosterData> FieldEnemies => fieldEnemies;
    public bool IsBattleResolved => m_battleResolved;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        InitializeLevel();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void InitializeLevel()
    {
        if (m_initialized)
        {
            return;
        }

        if (characterSpawner == null)
        {
            Debug.LogWarning("[LevelSetupManager] 缺少 LevelCharacterSpawner，无法初始化关卡");
            return;
        }

        m_pendingBattleLevelData = BattleLaunchContext.ConsumePendingLevelData();
        //导入局外数据，注意这里的时机是在角色生成之前，因此会影响角色生成的结果（如玩家角色池的确定），后续如果有需要也可以考虑增加一个接口在角色生成之后再导入一次，以覆盖掉角色生成时无法预知的数据（如玩家选择的上阵角色）
        Datas.Instance?.BeginBattleModifierSession();
        allPlayerCharacters = ResolveRuntimePlayerCharacters();
        IReadOnlyList<BattleEnemySpawnData> runtimeEnemies = m_pendingBattleLevelData != null
            ? m_pendingBattleLevelData.GetWaveEnemies(0)
            : null;
        m_currentWaveIndex = m_pendingBattleLevelData != null && m_pendingBattleLevelData.WaveCount > 0 ? 0 : -1;

        if (m_pendingBattleLevelData != null
            && m_pendingBattleLevelData.selectedFieldCharacters != null
            && m_pendingBattleLevelData.selectedFieldCharacters.Count > 0)
        {
            fieldPlayerCharacters = new List<CharacterRosterData>(m_pendingBattleLevelData.selectedFieldCharacters);
        }

        EnsureCharactersIncluded(allPlayerCharacters, fieldPlayerCharacters);

        characterSpawner.SpawnLevel(
            allPlayerCharacters,
            fieldPlayerCharacters,
            fieldEnemies,
            runtimeEnemies,
            out List<Character> spawnedAllCharacters,
            out List<Character> spawnedFieldCharacters,
            out List<Enemy> spawnedEnemies);

        InitializeSpawnedEnemies(spawnedEnemies);

        CharacterManager.Instance?.InitializeCharacters(spawnedAllCharacters, spawnedFieldCharacters);
        EnemyManager.Instance?.InitializeEnemies(spawnedEnemies);
        TurnManager.Instance?.InitializeTurnOrder(spawnedFieldCharacters, spawnedEnemies);

        m_initialized = true;
    }

    private void InitializeSpawnedEnemies(List<Enemy> spawnedEnemies)
    {
        if (spawnedEnemies == null)
        {
            return;
        }

        IReadOnlyList<Enemy> readonlyEnemies = spawnedEnemies;
        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            if (spawnedEnemies[i] == null)
            {
                continue;
            }

            spawnedEnemies[i].InitializeFromPendingLevelData(m_pendingBattleLevelData, readonlyEnemies);
        }
    }

    public IEnumerator ResolveBattleProgressAfterTurn()
    {
        if (!m_initialized || m_battleResolved || m_isCheckingBattleProgress)
        {
            yield break;
        }

        yield return EvaluateBattleProgressAfterTurn();
    }

    private IEnumerator EvaluateBattleProgressAfterTurn()
    {
        m_isCheckingBattleProgress = true;
        try
        {
            yield return UnitCombatant.WaitForPendingDeaths();

            if (EnemyManager.Instance != null && EnemyManager.Instance.HasRemainingEnemies)
            {
                yield break;
            }

            int nextWaveIndex = FindNextWaveIndex(m_currentWaveIndex + 1);
            if (nextWaveIndex >= 0)
            {
                yield return SpawnEnemyWave(nextWaveIndex);
                yield break;
            }

            yield return PlaySettlementSequence();
        }
        finally
        {
            m_isCheckingBattleProgress = false;
        }
    }

    private int FindNextWaveIndex(int startIndex)
    {
        if (m_pendingBattleLevelData == null)
        {
            return -1;
        }

        int safeStartIndex = Mathf.Max(0, startIndex);
        for (int i = safeStartIndex; i < m_pendingBattleLevelData.WaveCount; i++)
        {
            if (m_pendingBattleLevelData.GetWaveEnemies(i).Count > 0)
            {
                return i;
            }
        }

        return -1;
    }

    private IEnumerator SpawnEnemyWave(int waveIndex)
    {
        if (characterSpawner == null || m_pendingBattleLevelData == null)
        {
            yield break;
        }

        IReadOnlyList<BattleEnemySpawnData> waveEnemies = m_pendingBattleLevelData.GetWaveEnemies(waveIndex);
        characterSpawner.SpawnEnemyWave(waveEnemies, out List<Enemy> spawnedEnemies);
        InitializeSpawnedEnemies(spawnedEnemies);
        EnemyManager.Instance?.RegisterEnemies(spawnedEnemies);

        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            Enemy enemy = spawnedEnemies[i];
            if (enemy == null || !enemy.participateInTurnLoopAtStart)
            {
                continue;
            }

            enemy.ChangeActionValue(enemy.BaseActionValue, false);
            TurnManager.Instance?.InsertCombatant(enemy);
        }

        m_currentWaveIndex = waveIndex;
        FloatingTipGenerator.Instance?.ShowDefaultTip($"第 {waveIndex + 1} 波敌人出现");

        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            spawnedEnemies[i]?.PlaySpawnEnterFeedback();
        }

        yield return new WaitForSeconds(enemyWaveEnterDelay+2f);
    }

    private IEnumerator PlaySettlementSequence()
    {
        if (m_battleResolved)
        {
            yield break;
        }

        m_battleResolved = true;

        int rewardExperience = m_pendingBattleLevelData != null ? m_pendingBattleLevelData.rewardExperience : 0;
        int rewardGold = m_pendingBattleLevelData != null ? m_pendingBattleLevelData.rewardGold : 0;
        Datas.Instance?.MarkLevelCompleted(m_pendingBattleLevelData != null ? m_pendingBattleLevelData.levelId : string.Empty);
        //结束战斗增益会话，结算界面可能需要读取一些数据来显示，因此放在前面执行，耦合度略高
        Datas.Instance?.CompleteBattleModifierSession();

        if (settlementView != null)
        {
            yield return settlementView.PlaySettlementSequence(rewardExperience, rewardGold);
            yield break;
        }
        Datas.Instance?.ApplyBattleRewards(rewardExperience, rewardGold);

        Debug.LogWarning("[LevelSetupManager] 缺少 BattleSettlementView，已结算奖励但未显示结算界面。", this);
    }

    private List<CharacterRosterData> ResolveRuntimePlayerCharacters()
    {
        if (Datas.Instance != null)
        {
            IReadOnlyList<CharacterRosterData> unlockedCharacters = Datas.Instance.GetUnlockedCharacterRosters();
            if (Datas.Instance.HasSelectedStarterBranch)
            {
                return unlockedCharacters != null ? new List<CharacterRosterData>(unlockedCharacters) : new List<CharacterRosterData>();
            }

            if (unlockedCharacters != null && unlockedCharacters.Count > 0)
            {
                return new List<CharacterRosterData>(unlockedCharacters);
            }
        }

        return allPlayerCharacters != null ? new List<CharacterRosterData>(allPlayerCharacters) : new List<CharacterRosterData>();
    }

    private static void EnsureCharactersIncluded(List<CharacterRosterData> allCharacters, List<CharacterRosterData> requiredCharacters)
    {
        if (allCharacters == null || requiredCharacters == null)
        {
            return;
        }

        for (int i = 0; i < requiredCharacters.Count; i++)
        {
            CharacterRosterData requiredCharacter = requiredCharacters[i];
            if (requiredCharacter != null && !allCharacters.Contains(requiredCharacter))
            {
                allCharacters.Add(requiredCharacter);
            }
        }
    }
}
