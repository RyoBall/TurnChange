using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 棋局召唤兵卒（SummonedPawn）
/// 皇后召唤的兵卒，行动时为皇后回血
/// </summary>
public class ChessSummonedPawnEnemy : Enemy
{
    [Header("召唤兵卒配置")]
    [SerializeField, Range(0f, 1f)] private float hpRatio = 0.3f;
    [SerializeField] private float healRatio = 0.03f;

    private ChessQueenEnemy m_linkedQueen;

    public bool IsChessSummonedPawn => true;

    protected override void Start()
    {
        base.Start();
        if (ChessStandPositionManager.Instance != null)
        {
            transform.position = ChessStandPositionManager.Instance.GetPawnStandPosition(standPosition).position;
        }
    }

    public void ConfigureAsSummonedPawn(ChessQueenEnemy queen, EnemyRosterData data, int standPosition, int level)
    {
        m_linkedQueen = queen;
        healRatio = queen != null ? queen.SummonedPawnHealRatio : healRatio;
        ConfigureFromRosterData(data, standPosition, level);
    }

    protected override void InitializeEnemyRuntime()
    {
        base.InitializeEnemyRuntime();
        ApplySummonedHpRatio();
    }

    /// <summary>按预制体配置的比例缩放 CSV 生命值（最大生命与当前生命）。</summary>
    private void ApplySummonedHpRatio()
    {
        int scaledMaxHp = Mathf.Max(1, Mathf.RoundToInt(maxHP * hpRatio));
        maxHP = scaledMaxHp;
        currentHP = scaledMaxHp;
    }

    public override void Die()
    {
        ChessQueenEnemy queen = m_linkedQueen;
        base.Die();

        if (queen != null && !queen.IsDead)
        {
            queen.NotifySummonedPawnKilled(this);
        }
    }

    /// <summary>为皇后回血（由 EnemySkillBase 调用）</summary>
    public void HealQueen(float skillHealRatio = 0f)
    {
        if (m_linkedQueen == null || m_linkedQueen.IsDead)
        {
            return;
        }

        float ratio = skillHealRatio > 0f ? skillHealRatio : healRatio;
        int healAmount = Mathf.Max(1, Mathf.RoundToInt(m_linkedQueen.maxHP * ratio));
        m_linkedQueen.Heal(healAmount);
        FloatingTipGenerator.Instance?.ShowTipAtObject(transform, $"{combatantName}为皇后回复{healAmount}");
    }
}
