using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    private readonly List<Enemy> m_aliveEnemies = new List<Enemy>();

    public IReadOnlyList<Enemy> AliveEnemies => m_aliveEnemies;

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

    public void RegisterEnemy(Enemy enemy)
    {
        if (enemy == null || m_aliveEnemies.Contains(enemy))
            return;

        m_aliveEnemies.Add(enemy);
    }

    public void UnregisterEnemy(Enemy enemy)
    {
        if (enemy == null)
            return;

        m_aliveEnemies.Remove(enemy);
    }
}
