using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    private readonly List<Enemy> m_aliveEnemies = new List<Enemy>();
    private readonly List<Enemy> m_pendingEnemies = new List<Enemy>();
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
        if (enemy == null)
            return;

        m_pendingEnemies.Remove(enemy);
        if (m_aliveEnemies.Contains(enemy))
            return;

        m_aliveEnemies.Add(enemy);
    }

    public void RegisterPendingEnemy(Enemy enemy)
    {
        if (enemy == null || m_pendingEnemies.Contains(enemy) || m_aliveEnemies.Contains(enemy))
            return;

        m_pendingEnemies.Add(enemy);
    }

    public void InitializeEnemies(List<Enemy> runtimeEnemies)
    {
        m_aliveEnemies.Clear();
        m_pendingEnemies.Clear();

        if (runtimeEnemies == null)
        {
            return;
        }

        for (int i = 0; i < runtimeEnemies.Count; i++)
        {
            Enemy enemy = runtimeEnemies[i];
            if (enemy == null)
            {
                continue;
            }

            if (enemy.ShouldRegisterAtBattleStart)
            {
                RegisterEnemy(enemy);
            }
            else
            {
                RegisterPendingEnemy(enemy);
            }
        }
    }

    public void UnregisterEnemy(Enemy enemy)
    {
        if (enemy == null)
            return;

        bool removed = m_aliveEnemies.Remove(enemy);
        removed = m_pendingEnemies.Remove(enemy) || removed;
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

        for (int i = m_pendingEnemies.Count - 1; i >= 0; i--)
        {
            if (m_pendingEnemies[i] == null)
            {
                m_pendingEnemies.RemoveAt(i);
            }
        }

        if (m_aliveEnemies.Count > 0 || m_pendingEnemies.Count > 0)
            return;

        Victory();
    }

    private void Victory()
    {
        FloatingTipGenerator.Instance?.ShowDefaultTip("所有敌人已清空");
    }
}
