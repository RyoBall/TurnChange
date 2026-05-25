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

        if (enemyWaveEnterDelay > 0f)
        {
            yield return new WaitForSeconds(enemyWaveEnterDelay);
        }
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
        Datas.Instance?.ApplyBattleRewards(rewardExperience, rewardGold);

        if (settlementView != null)
        {
            yield return settlementView.PlaySettlementSequence(rewardExperience, rewardGold);
            yield break;
        }

        Debug.LogWarning("[LevelSetupManager] 缺少 BattleSettlementView，已结算奖励但未显示结算界面。", this);
    }
}
