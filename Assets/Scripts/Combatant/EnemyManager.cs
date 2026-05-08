using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    private readonly List<Enemy> m_aliveEnemies = new List<Enemy>();
    [Header("胜利回调")]
    [SerializeField] private UnityEvent onVictory;


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

        bool removed = m_aliveEnemies.Remove(enemy);
        if (!removed)
            return;

        CheckAllEnemiesDefeated();
    }

    private void CheckAllEnemiesDefeated()
    {
        for (int i = m_aliveEnemies.Count - 1; i >= 0; i--)
        {
            if (m_aliveEnemies[i] == null)
            {
                m_aliveEnemies.RemoveAt(i);
            }
        }

        if (m_aliveEnemies.Count > 0)
            return;

        Victory();
    }

    private void Victory()
    {
        FloatingTipGenerator.Instance?.ShowDefaultTip("所有敌人已清空");
    }
}
