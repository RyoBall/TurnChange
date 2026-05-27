using System.Collections.Generic;
using UnityEngine;

public class LevelCharacterSpawner : MonoBehaviour
{
    private const int EnemyStandPositionStart = 3;

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
        IReadOnlyList<BattleEnemySpawnData> runtimeFieldEnemies,
        out List<Character> spawnedAllCharacters,
        out List<Character> spawnedFieldCharacters,
        out List<Enemy> spawnedEnemies)
    {
        ClearSpawnedObjects();

        spawnedAllCharacters = new List<Character>();
        spawnedFieldCharacters = new List<Character>();
        spawnedEnemies = new List<Enemy>();

        var fieldOrderByData = new Dictionary<CharacterRosterData, int>();
        var fieldOrderByTag = new Dictionary<string, Queue<int>>();
        if (fieldPlayerCharacters != null)
        {
            for (int i = 0; i < fieldPlayerCharacters.Count; i++)
            {
                CharacterRosterData fieldData = fieldPlayerCharacters[i];
                if (fieldData == null)
                {
                    continue;
                }

                fieldOrderByData[fieldData] = i;

                string fieldTag = GetCharacterTag(fieldData);
                if (!string.IsNullOrEmpty(fieldTag))
                {
                    if (!fieldOrderByTag.TryGetValue(fieldTag, out Queue<int> orderQueue))
                    {
                        orderQueue = new Queue<int>();
                        fieldOrderByTag[fieldTag] = orderQueue;
                    }

                    orderQueue.Enqueue(i);
                }
            }
        }

        int reserveIndex = 0;
        var consumedFieldOrders = new HashSet<int>();
        if (allPlayerCharacters != null)
        {
            for (int i = 0; i < allPlayerCharacters.Count; i++)
            {
                CharacterRosterData data = allPlayerCharacters[i];
                int fieldOrderIndex = ResolveFieldOrderIndex(data, fieldOrderByData, fieldOrderByTag, consumedFieldOrders);
                Character character = SpawnCharacter(
                    data,
                    fieldOrderIndex,
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

        if (runtimeFieldEnemies != null && runtimeFieldEnemies.Count > 0)
        {
            for (int i = 0; i < runtimeFieldEnemies.Count; i++)
            {
                Enemy enemy = SpawnEnemy(runtimeFieldEnemies[i], i);
                if (enemy != null)
                {
                    spawnedEnemies.Add(enemy);
                }
            }
        }
        else if (fieldEnemies != null)
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

    public void SpawnEnemyWave(
        IReadOnlyList<BattleEnemySpawnData> runtimeFieldEnemies,
        out List<Enemy> spawnedEnemies,
        int standPositionStart = EnemyStandPositionStart)
    {
        spawnedEnemies = new List<Enemy>();
        if (runtimeFieldEnemies == null)
        {
            return;
        }

        for (int i = 0; i < runtimeFieldEnemies.Count; i++)
        {
            Enemy enemy = SpawnEnemy(runtimeFieldEnemies[i], i, standPositionStart);
            if (enemy != null)
            {
                spawnedEnemies.Add(enemy);
            }
        }
    }

    private Character SpawnCharacter(CharacterRosterData data, int fieldOrderIndex, ref int reserveIndex)
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

        bool isFieldCharacter = fieldOrderIndex >= 0;
        int assignedStandPosition = isFieldCharacter ? fieldOrderIndex + 1 : int.MaxValue;
        Transform spawnPoint = isFieldCharacter
            ? GetSpawnPoint(playerFieldSpawnPoints, fieldOrderIndex)
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

        ConfigureCharacter(instance, data, isFieldCharacter, assignedStandPosition);
        m_spawnedObjects.Add(spawnedObject);
        return instance;
    }

    private Enemy SpawnEnemy(EnemyRosterData data, int index)
    {
        if (data == null)
        {
            return null;
        }

        GameObject prefabToSpawn = ResolveEnemyPrefab(data);
        if (prefabToSpawn == null)
        {
            Debug.LogWarning("[LevelCharacterSpawner] 敌人预制体为空，无法生成敌人。", this);
            return null;
        }

        ResolveEnemySpawnPose(prefabToSpawn, null, index, out Vector3 spawnPosition, out Quaternion spawnRotation);
        GameObject spawnedObject = Instantiate(prefabToSpawn, spawnPosition, spawnRotation, enemyRoot);
        Enemy instance = spawnedObject.GetComponent<Enemy>();
        if (instance == null)
        {
            Debug.LogError("[LevelCharacterSpawner] 生成的敌人预制体未找到 Enemy 组件。", spawnedObject);
            Destroy(spawnedObject);
            return null;
        }

        ConfigureEnemy(instance, data, EnemyStandPositionStart + index);
        m_spawnedObjects.Add(spawnedObject);
        return instance;
    }

    private Enemy SpawnEnemy(BattleEnemySpawnData data, int index, int standPositionStart = EnemyStandPositionStart)
    {
        if (data == null || data.enemyData == null)
        {
            return null;
        }

        GameObject prefabToSpawn = ResolveEnemyPrefab(data.enemyData);
        if (prefabToSpawn == null)
        {
            Debug.LogWarning("[LevelCharacterSpawner] 敌人预制体为空，无法生成敌人。", this);
            return null;
        }

        ResolveEnemySpawnPose(prefabToSpawn, data, index, out Vector3 spawnPosition, out Quaternion spawnRotation);
        GameObject spawnedObject = Instantiate(prefabToSpawn, spawnPosition, spawnRotation, enemyRoot);
        Enemy instance = spawnedObject.GetComponent<Enemy>();
        if (instance == null)
        {
            Debug.LogError("[LevelCharacterSpawner] 生成的敌人预制体未找到 Enemy 组件。", spawnedObject);
            Destroy(spawnedObject);
            return null;
        }

        ConfigureEnemy(instance, data, standPositionStart + index);
        m_spawnedObjects.Add(spawnedObject);
        return instance;
    }

    private int ResolveFieldOrderIndex(
        CharacterRosterData data,
        Dictionary<CharacterRosterData, int> fieldOrderByData,
        Dictionary<string, Queue<int>> fieldOrderByTag,
        HashSet<int> consumedFieldOrders)
    {
        if (data == null)
        {
            return -1;
        }

        if (fieldOrderByData.TryGetValue(data, out int directOrder) && consumedFieldOrders.Add(directOrder))
        {
            return directOrder;
        }

        string characterTag = GetCharacterTag(data);
        if (string.IsNullOrEmpty(characterTag) || !fieldOrderByTag.TryGetValue(characterTag, out Queue<int> orderQueue))
        {
            return -1;
        }

        while (orderQueue.Count > 0)
        {
            int queuedOrder = orderQueue.Dequeue();
            if (consumedFieldOrders.Add(queuedOrder))
            {
                return queuedOrder;
            }
        }

        return -1;
    }

    private string GetCharacterTag(CharacterRosterData data)
    {
        if (data == null)
        {
            return string.Empty;
        }

        return string.IsNullOrEmpty(data.characterName) ? data.characterID : data.characterName;
    }

    private void ConfigureCharacter(Character instance, CharacterRosterData data, bool participateInTurnLoop, int standPosition)
    {
        if (instance == null || data == null)
        {
            return;
        }

        instance.characterID = data.characterID;
        instance.combatantName = string.IsNullOrEmpty(data.characterName) ? data.characterID : data.characterName;
        instance.skills = new List<CharacterSkillType>(data.skills);
        instance.enterSkill = data.enterSkill;
        instance.participateInTurnLoopAtStart = participateInTurnLoop;
        instance.standPosition = standPosition;
        instance.level = Datas.Instance != null ? Datas.Instance.GetTeamLevel() : Mathf.Max(1, instance.level);
        instance.LoadDataFromCSV();
    }

    private void ConfigureEnemy(Enemy instance, EnemyRosterData data, int standPosition)
    {
        ConfigureEnemy(instance, data, standPosition, instance != null ? instance.level : 1);
    }

    private void ConfigureEnemy(Enemy instance, BattleEnemySpawnData data, int standPosition)
    {
        if (instance == null || data == null)
        {
            return;
        }

        instance.ConfigureFromBattleSpawnData(data, standPosition);
    }

    private void ConfigureEnemy(Enemy instance, EnemyRosterData data, int standPosition, int level)
    {
        if (instance == null || data == null)
        {
            return;
        }

        instance.ConfigureFromRosterData(data, standPosition, level);
    }

    private GameObject ResolveEnemyPrefab(EnemyRosterData data)
    {
        if (data != null && data.PrefabOverride != null)
        {
            return data.PrefabOverride;
        }

        return enemyPrefab != null ? enemyPrefab.gameObject : null;
    }

    private void ResolveEnemySpawnPose(
        GameObject prefabToSpawn,
        BattleEnemySpawnData battleSpawnData,
        int index,
        out Vector3 position,
        out Quaternion rotation)
    {
        if (ShouldUseChessPieceSpawnPoints(prefabToSpawn, battleSpawnData)
            && ChessPieceSpawnPointManager.Instance != null
            && ChessPieceSpawnPointManager.Instance.TryGetSpawnPose(index, out position, out rotation))
        {
            return;
        }

        Transform spawnPoint = GetSpawnPoint(enemySpawnPoints, index);
        position = GetSpawnPosition(spawnPoint);
        rotation = GetSpawnRotation(spawnPoint);
    }

    private static bool ShouldUseChessPieceSpawnPoints(GameObject prefabToSpawn, BattleEnemySpawnData battleSpawnData)
    {
        if (battleSpawnData != null && battleSpawnData.chessBossData != null)
        {
            return true;
        }

        return prefabToSpawn != null && prefabToSpawn.GetComponent<ChessBossEnemy>() != null;
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
