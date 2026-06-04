using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 龙Boss战生成位置管理器（单例）
/// 管理三条龙的生成位置，通过 enemyID 获取对应位置
/// 首次获取时生成美术素材（仅一次）
/// </summary>
[DisallowMultipleComponent]
public class DragonSpawnPositionManager : MonoBehaviour
{
    public static DragonSpawnPositionManager Instance { get; private set; }

    [Header("龙生成点（按 enemyID 映射）")]
    [SerializeField] private DragonSpawnEntry[] dragonSpawnEntries;

    [Header("美术素材")]
    [SerializeField] private GameObject environmentArtPrefab;
    [SerializeField] private Transform environmentArtSpawnPoint;

    private readonly Dictionary<string, Transform> m_spawnMap = new Dictionary<string, Transform>();
    private bool m_hasSpawnedEnvironmentArt;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        BuildSpawnMap();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void BuildSpawnMap()
    {
        m_spawnMap.Clear();
        if (dragonSpawnEntries == null) return;

        for (int i = 0; i < dragonSpawnEntries.Length; i++)
        {
            DragonSpawnEntry entry = dragonSpawnEntries[i];
            if (entry == null || string.IsNullOrEmpty(entry.enemyID) || entry.spawnPoint == null) continue;

            if (!m_spawnMap.ContainsKey(entry.enemyID))
            {
                m_spawnMap[entry.enemyID] = entry.spawnPoint;
            }
        }
    }

    /// <summary>根据 enemyID 获取龙的生成位置，首次调用时生成美术素材</summary>
    public bool TryGetDragonSpawnPosition(string enemyID, out Vector3 spawnPosition, out Quaternion spawnRotation)
    {
        spawnPosition = Vector3.zero;
        spawnRotation = Quaternion.identity;

        if (string.IsNullOrEmpty(enemyID) || !m_spawnMap.TryGetValue(enemyID, out Transform spawnPoint))
        {
            return false;
        }

        spawnPosition = spawnPoint.position;
        spawnRotation = spawnPoint.rotation;

        // 首次成功获取时生成美术素材
        TrySpawnEnvironmentArt();

        return true;
    }

    /// <summary>尝试生成美术素材（仅一次）</summary>
    private void TrySpawnEnvironmentArt()
    {
        if (m_hasSpawnedEnvironmentArt) return;
        if (environmentArtPrefab == null || environmentArtSpawnPoint == null) return;

        Instantiate(environmentArtPrefab, environmentArtSpawnPoint.position, environmentArtSpawnPoint.rotation);
        m_hasSpawnedEnvironmentArt = true;
    }

    /// <summary>直接获取生成点 Transform（不触发美术素材生成）</summary>
    public bool TryGetDragonSpawnPoint(string enemyID, out Transform spawnPoint)
    {
        return m_spawnMap.TryGetValue(enemyID, out spawnPoint);
    }
}

[System.Serializable]
public class DragonSpawnEntry
{
    [Tooltip("对应 Enemy 的 enemyID")]
    public string enemyID;
    public Transform spawnPoint;
}
