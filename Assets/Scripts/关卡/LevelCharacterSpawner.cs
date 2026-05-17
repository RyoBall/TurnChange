using System.Collections.Generic;
using UnityEngine;

public class LevelCharacterSpawner : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private Transform[] playerFieldSpawnPoints;
    [SerializeField] private Transform[] playerReserveSpawnPoints;
    [SerializeField] private Transform[] enemySpawnPoints;

    [Header("Spawn Parents")]
    [SerializeField] private Transform playerRoot;
    [SerializeField] private Transform reserveRoot;
    [SerializeField] private Transform enemyRoot;
    [Header("Prefabs")]
    [SerializeField] private GameObject characterPrefab;
    [SerializeField] private GameObject enemyPrefab;

    private readonly List<GameObject> m_spawnedObjects = new List<GameObject>();

    public void SpawnLevel(
        List<CharacterRosterData> allPlayerCharacters,
        List<CharacterRosterData> fieldPlayerCharacters,
        List<EnemyRosterData> fieldEnemies,
        out List<Character> spawnedAllCharacters,
        out List<Character> spawnedFieldCharacters,
        out List<Enemy> spawnedEnemies)
    {
        ClearSpawnedObjects();

        spawnedAllCharacters = new List<Character>();
        spawnedFieldCharacters = new List<Character>();
        spawnedEnemies = new List<Enemy>();

        var fieldIdSet = new HashSet<string>();
        if (fieldPlayerCharacters != null)
        {
            for (int i = 0; i < fieldPlayerCharacters.Count; i++)
            {
                CharacterRosterData fieldData = fieldPlayerCharacters[i];
                if (fieldData == null)
                {
                    continue;
                }

                string fieldTag = string.IsNullOrEmpty(fieldData.characterName)
                    ? fieldData.characterID
                    : fieldData.characterName;
                if (!string.IsNullOrEmpty(fieldTag))
                {
                    fieldIdSet.Add(fieldTag);
                }
            }
        }

        int fieldIndex = 0;
        int reserveIndex = 0;
        if (allPlayerCharacters != null)
        {
            for (int i = 0; i < allPlayerCharacters.Count; i++)
            {
                CharacterRosterData data = allPlayerCharacters[i];
                string characterTag = data == null
                    ? string.Empty
                    : (string.IsNullOrEmpty(data.characterName) ? data.characterID : data.characterName);
                Character character = SpawnCharacter(
                    data,
                    fieldIdSet.Contains(characterTag),
                    ref fieldIndex,
                    ref reserveIndex);

                if (character == null)
                {
                    continue;
                }

                spawnedAllCharacters.Add(character);
                if (character.participateInTurnLoopAtStart)
                {
                    spawnedFieldCharacters.Add(character);
                }
            }
        }

        if (fieldEnemies != null)
        {
            for (int i = 0; i < fieldEnemies.Count; i++)
            {
                Enemy enemy = SpawnEnemy(fieldEnemies[i], i);
                if (enemy != null)
                {
                    spawnedEnemies.Add(enemy);
                }
            }
        }
    }

    public void ClearSpawnedObjects()
    {
        for (int i = m_spawnedObjects.Count - 1; i >= 0; i--)
        {
            if (m_spawnedObjects[i] != null)
            {
                Destroy(m_spawnedObjects[i]);
            }
        }

        m_spawnedObjects.Clear();
    }

    private Character SpawnCharacter(CharacterRosterData data, bool isFieldCharacter, ref int fieldIndex, ref int reserveIndex)
    {
        if (data == null)
        {
            return null;
        }

        GameObject prefabToSpawn = characterPrefab != null ? characterPrefab.gameObject : null;
        if (prefabToSpawn == null)
        {
            Debug.LogWarning("[LevelCharacterSpawner] 角色预制体为空，无法生成角色。", this);
            return null;
        }

        Transform spawnPoint = isFieldCharacter
            ? GetSpawnPoint(playerFieldSpawnPoints, fieldIndex++)
            : GetSpawnPoint(playerReserveSpawnPoints, reserveIndex++);
        Transform parent = isFieldCharacter ? playerRoot : reserveRoot;

        GameObject spawnedObject = Instantiate(prefabToSpawn, GetSpawnPosition(spawnPoint), GetSpawnRotation(spawnPoint), parent);
        Character instance = spawnedObject.GetComponent<Character>();
        if (instance == null)
        {
            Debug.LogError("[LevelCharacterSpawner] 生成的角色预制体未找到 Character 组件。", spawnedObject);
            Destroy(spawnedObject);
            return null;
        }

        ConfigureCharacter(instance, data, isFieldCharacter);
        m_spawnedObjects.Add(spawnedObject);
        return instance;
    }

    private Enemy SpawnEnemy(EnemyRosterData data, int index)
    {
        if (data == null)
        {
            return null;
        }

        GameObject prefabToSpawn = enemyPrefab != null ? enemyPrefab.gameObject : null;
        if (prefabToSpawn == null)
        {
            Debug.LogWarning("[LevelCharacterSpawner] 敌人预制体为空，无法生成敌人。", this);
            return null;
        }

        Transform spawnPoint = GetSpawnPoint(enemySpawnPoints, index);

        GameObject spawnedObject = Instantiate(prefabToSpawn, GetSpawnPosition(spawnPoint), GetSpawnRotation(spawnPoint), enemyRoot);
        Enemy instance = spawnedObject.GetComponent<Enemy>();
        if (instance == null)
        {
            Debug.LogError("[LevelCharacterSpawner] 生成的敌人预制体未找到 Enemy 组件。", spawnedObject);
            Destroy(spawnedObject);
            return null;
        }

        ConfigureEnemy(instance, data);
        m_spawnedObjects.Add(spawnedObject);
        return instance;
    }

    private void ConfigureCharacter(Character instance, CharacterRosterData data, bool participateInTurnLoop)
    {
        if (instance == null || data == null)
        {
            return;
        }

        instance.characterID = data.characterID;
        instance.combatantName = string.IsNullOrEmpty(data.characterName) ? data.characterID : data.characterName;
        instance.skills = new List<CharacterSkillType>(data.skills);
        instance.enterSkill = data.enterSkill;
        instance.exitSkill = data.exitSkill;
        instance.participateInTurnLoopAtStart = participateInTurnLoop;
        instance.LoadDataFromCSV();
    }

    private void ConfigureEnemy(Enemy instance, EnemyRosterData data)
    {
        if (instance == null || data == null)
        {
            return;
        }

        instance.enemyID = data.enemyID;
        instance.combatantName = string.IsNullOrEmpty(data.enemyName) ? data.enemyID : data.enemyName;
        instance.skills = new List<EnemySkillType>(data.skills);
        instance.participateInTurnLoopAtStart = true;
        instance.LoadDataFromCSV();
    }

    private Transform GetSpawnPoint(Transform[] spawnPoints, int index)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return null;
        }

        int safeIndex = Mathf.Clamp(index, 0, spawnPoints.Length - 1);
        return spawnPoints[safeIndex];
    }

    private Vector3 GetSpawnPosition(Transform spawnPoint)
    {
        return spawnPoint != null ? spawnPoint.position : Vector3.zero;
    }

    private Quaternion GetSpawnRotation(Transform spawnPoint)
    {
        return spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;
    }
}
