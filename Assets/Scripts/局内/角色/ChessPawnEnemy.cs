using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 棋局初始兵卒（PromotionPawn）
/// 每行动一次向前推进一格，到达升变线后触发皇后入场
/// </summary>
public class ChessPawnEnemy : Enemy
{
    [Header("兵卒配置")]
    [SerializeField] private string bossGroupId = "chess-boss";
    [SerializeField] private Vector3 pawnAdvanceOffset = new Vector3(0f, 0f, -0.8f);
    [SerializeField] private float pawnAdvanceDuration = 0.2f;
    [SerializeField, Min(1)] private int pawnPromotionSteps = 5;

    private ChessQueenEnemy m_linkedQueen;
    [SerializeField, Min(0)] private int m_pawnAdvanceCount;
    private bool m_hasTriggeredPromotion;

    public bool IsChessPromotionPawn => true;
    public int PawnAdvanceCount => m_pawnAdvanceCount;
    public int PawnPromotionSteps => pawnPromotionSteps;
    public string BossGroupId => bossGroupId;

    protected override void Start()
    {
        base.Start();
        if (ChessStandPositionManager.Instance != null)
        {
            Debug.Log($"ChessPawnEnemy {combatantName} occupying stand position {standPosition}");
            transform.position = ChessStandPositionManager.Instance.GetPawnStandPosition(standPosition).position    ;
        }
    }
    void Update()
    {
        Debug.Log($"ChessPawnEnemy {combatantName} at position {transform.position}, advance count {m_pawnAdvanceCount}, promotion triggered {m_hasTriggeredPromotion}");
    }
    public override void InitializeFromPendingLevelData(PendingBattleLevelData pendingData, IReadOnlyList<Enemy> spawnedEnemies)
    {
        base.InitializeFromPendingLevelData(pendingData, spawnedEnemies);
        if (spawnedEnemies != null)
        {
            m_linkedQueen = FindQueenByGroup(spawnedEnemies);
        }
    }

    public override void Die()
    {
        ChessQueenEnemy queen = m_linkedQueen;
        base.Die();

        // 兵卒阵亡后检测：如果场上已无存活兵卒，直接触发皇后入场
        if (queen != null && !queen.IsDead && !queen.IsBattleVisible && !m_hasTriggeredPromotion)
        {
            int remainingPawns = CountAlivePromotionPawns(queen);
            if (remainingPawns <= 0)
            {
                m_hasTriggeredPromotion = true;
                queen.EnterPhaseTwo(0);
            }
        }
    }

    /// <summary>兵卒推进逻辑（由 EnemySkillBase 调用）</summary>
    public IEnumerator AdvancePawn(float pawnAdvanceDurationOverride = -1f)
    {
        m_pawnAdvanceCount = Mathf.Min(pawnPromotionSteps, m_pawnAdvanceCount + 1);
        Vector3 targetPosition = transform.position + pawnAdvanceOffset;
        float duration = pawnAdvanceDurationOverride >= 0f ? pawnAdvanceDurationOverride : pawnAdvanceDuration;
        if (duration > 0f)
        {
            yield return transform.DOMove(targetPosition, duration).SetEase(Ease.InOutSine).WaitForCompletion();
        }
        else
        {
            transform.position = targetPosition;
        }

        FloatingTipGenerator.Instance?.ShowTipAtObject(transform, $"{combatantName}推进至第{m_pawnAdvanceCount}格");
        if (m_pawnAdvanceCount >= pawnPromotionSteps && !m_hasTriggeredPromotion)
        {
            m_hasTriggeredPromotion = true;
            if (m_linkedQueen != null)
            {
                int aliveCount = CountAlivePromotionPawns(m_linkedQueen);
                m_linkedQueen.EnterPhaseTwo(aliveCount);
            }
        }
    }

    private ChessQueenEnemy FindQueenByGroup(IReadOnlyList<Enemy> spawnedEnemies)
    {
        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            ChessQueenEnemy queen = spawnedEnemies[i] as ChessQueenEnemy;
            if (queen != null && string.Equals(queen.BossGroupId, bossGroupId, System.StringComparison.Ordinal))
            {
                return queen;
            }
        }
        return null;
    }

    private int CountAlivePromotionPawns(ChessQueenEnemy queen)
    {
        if (EnemyManager.Instance == null || queen == null)
        {
            return 0;
        }

        int aliveCount = 0;
        IReadOnlyList<Enemy> aliveEnemies = EnemyManager.Instance.AliveEnemies;
        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            ChessPawnEnemy pawn = aliveEnemies[i] as ChessPawnEnemy;
            if (pawn != null && !pawn.IsDead && string.Equals(pawn.BossGroupId, queen.BossGroupId, System.StringComparison.Ordinal))
            {
                aliveCount++;
                pawn.TakeDamage(new DamageInfo(pawn.currentHP).AsTrueDamage()); // 直接消灭剩余兵卒
            }
        }
        return aliveCount;
    }
}
