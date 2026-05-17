using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-500)]
public class LevelSetupManager : MonoBehaviour
{
    [Header("Level Data")]
    [SerializeField] private List<CharacterRosterData> allPlayerCharacters = new List<CharacterRosterData>();
    [SerializeField] private List<CharacterRosterData> fieldPlayerCharacters = new List<CharacterRosterData>();
    [SerializeField] private List<EnemyRosterData> fieldEnemies = new List<EnemyRosterData>();

    [Header("References")]
    [SerializeField] private LevelCharacterSpawner characterSpawner;

    private bool m_initialized;

    public List<CharacterRosterData> AllPlayerCharacters => allPlayerCharacters;
    public List<CharacterRosterData> FieldPlayerCharacters => fieldPlayerCharacters;
    public List<EnemyRosterData> FieldEnemies => fieldEnemies;

    private void Start()
    {
        InitializeLevel();
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

        characterSpawner.SpawnLevel(
            allPlayerCharacters,
            fieldPlayerCharacters,
            fieldEnemies,
            out List<Character> spawnedAllCharacters,
            out List<Character> spawnedFieldCharacters,
            out List<Enemy> spawnedEnemies);

        CharacterManager.Instance?.InitializeCharacters(spawnedAllCharacters, spawnedFieldCharacters);
        EnemyManager.Instance?.InitializeEnemies(spawnedEnemies);
        TurnManager.Instance?.InitializeTurnOrder(spawnedFieldCharacters, spawnedEnemies);
        m_initialized = true;
    }
}
