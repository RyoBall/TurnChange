using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelCharacterSpawner : MonoBehaviour//用于生成并初始化角色
{
    private const int EnemyStandPositionStart = 3;
    private static readonly Dictionary<int, Vector3> s_spawnPositionByStandPosition = new Dictionary<int, Vector3>();

    public static LevelCharacterSpawner Instance { get; private set; }

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

    // 站位占用管理：记录每个 standPosition 上是否已有敌人
    private readonly HashSet<int> m_occupiedEnemyStandPositions = new HashSet<int>();
    private readonly List<int> m_availableEnemyStandPositions = new List<int>();

    public static IReadOnlyDictionary<int, Vector3> SpawnPositionByStandPosition => s_spawnPositionByStandPosition;

    public static bool TryGetSpawnPosition(int standPosition, out Vector3 spawnPosition)
    {
        return s_spawnPositionByStandPosition.TryGetValue(standPosition, out spawnPosition);
    }

    /// <summary>获取一个随机的未被占用的敌人生成站位</summary>
    public bool TryGetRandomAvailableEnemyStandPosition(out int standPosition)
    {
        standPosition = 0;
        if (m_availableEnemyStandPositions.Count == 0)
        {
            // 如果没有可用站位，回退到使用下一个未使用的序号
            int maxUsed = m_occupiedEnemyStandPositions.Count > 0 ? m_occupiedEnemyStandPositions.Max() : EnemyStandPositionStart - 1;
            standPosition = maxUsed + 1;
            m_occupiedEnemyStandPositions.Add(standPosition);
            return true;
        }

        int randomIndex = Random.Range(0, m_availableEnemyStandPositions.Count);
        standPosition = m_availableEnemyStandPositions[randomIndex];
        m_availableEnemyStandPositions.RemoveAt(randomIndex);
        m_occupiedEnemyStandPositions.Add(standPosition);
        return true;
    }

    /// <summary>标记一个站位已被占用</summary>
    public void MarkEnemyStandPositionOccupied(int standPosition)
    {
        m_occupiedEnemyStandPositions.Add(standPosition);
        m_availableEnemyStandPositions.Remove(standPosition);
    }

    /// <summary>释放一个站位（敌人死亡时调用）</summary>
    public void ReleaseEnemyStandPosition(int standPosition)
    {
        m_occupiedEnemyStandPositions.Remove(standPosition);
        if (!m_availableEnemyStandPositions.Contains(standPosition))
        {
            m_availableEnemyStandPositions.Add(standPosition);
        }
    }

    /// <summary>初始化可用站位列表</summary>
    public void InitializeEnemyStandPositions(int startPosition, int count)
    {
        m_occupiedEnemyStandPositions.Clear();
        m_availableEnemyStandPositions.Clear();
        for (int i = 0; i < count; i++)
        {
            m_availableEnemyStandPositions.Add(startPosition + i);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void SpawnLevel(
        List<CharacterRosterData> allPlayerCharacters,
        List<CharacterRosterData> fieldPlayerCharacters,
        List<EnemyRosterData> fieldEnemies,
        IReadOnlyList<BattleEnemySpawnData> runtimeFieldEnemies,
        out List<Character> spawnedAllCharacters,
        out List<Character> spawnedFieldCharacters,
        out List<Enemy> spawnedEnemies,
        int playerLevel = 1)
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
                    ref reserveIndex,
                    playerLevel);

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
        s_spawnPositionByStandPosition.Clear();
        m_occupiedEnemyStandPositions.Clear();
        m_availableEnemyStandPositions.Clear();
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

    private Character SpawnCharacter(CharacterRosterData data, int fieldOrderIndex, ref int reserveIndex, int playerLevel)
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
        Vector3 spawnPosition = GetSpawnPosition(spawnPoint);
        Quaternion spawnRotation = GetSpawnRotation(spawnPoint);

        GameObject spawnedObject = Instantiate(prefabToSpawn, spawnPosition, spawnRotation, parent);
        Character instance = spawnedObject.GetComponent<Character>();
        if (instance == null)
        {
            Debug.LogError("[LevelCharacterSpawner] 生成的角色预制体未找到 Character 组件。", spawnedObject);
            Destroy(spawnedObject);
            return null;
        }

        ConfigureCharacter(instance, data, isFieldCharacter, assignedStandPosition, playerLevel);
        RegisterSpawnPosition(assignedStandPosition, spawnPosition);
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
        RegisterSpawnPosition(EnemyStandPositionStart + index, spawnPosition);
        MarkEnemyStandPositionOccupied(EnemyStandPositionStart + index);
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
        RegisterSpawnPosition(standPositionStart + index, spawnPosition);
        MarkEnemyStandPositionOccupied(standPositionStart + index);
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

    private void ConfigureCharacter(Character instance, CharacterRosterData data, bool participateInTurnLoop, int standPosition, int playerLevel)
    {
        if (instance == null || data == null)
        {
            return;
        }

        instance.characterID = data.characterID;
        instance.characterType = data.characterType;
        instance.combatantName = string.IsNullOrEmpty(data.characterName) ? data.characterID : data.characterName;
        instance.skills = new List<CharacterSkillType>(data.skills);
        instance.enterSkill = data.enterSkill;
        instance.additionalSkillType = data.additionalSkill;
        instance.participateInTurnLoopAtStart = participateInTurnLoop;
        instance.standPosition = standPosition;
        instance.SetTurnImageSprite(data.portraitSprite);
        instance.SetIllustration(data.illustrationSprite, data.illustrationSize);
        // Debug 模式使用关卡配置的角色等级，否则使用全局战队等级
        if (DebugMode.Instance != null && DebugMode.Instance.IsDebugMode)
        {
            instance.level = playerLevel > 0 ? playerLevel : Mathf.Max(1, instance.level);
        }
        else
        {
            instance.level = Datas.Instance != null ? Datas.Instance.GetTeamLevel() : Mathf.Max(1, instance.level);
        }
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
        Transform spawnPoint = GetSpawnPoint(enemySpawnPoints, index);
        position = GetSpawnPosition(spawnPoint);
        rotation = GetSpawnRotation(spawnPoint);
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

    private void RegisterSpawnPosition(int standPosition, Vector3 spawnPosition)
    {
        if (standPosition == int.MaxValue)
        {
            return;
        }

        s_spawnPositionByStandPosition[standPosition] = spawnPosition;
    }
}
