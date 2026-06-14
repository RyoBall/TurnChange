using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 龙Boss基类 — 三龙共享的强化/暴怒机制
/// </summary>
public class DragonBossEnemy : Enemy
{
    [Header("龙Boss配置")]
    [SerializeField] private string dragonGroupId = "dragon-boss";
    [SerializeField, Min(0)] private int rageThreshold = 2;

    private int m_reinforceLevel;

    public string DragonGroupId => dragonGroupId;
    public int ReinforceLevel => m_reinforceLevel;
    public bool IsRaging => m_reinforceLevel >= rageThreshold;

    protected override void Start()
    {
        base.Start();
        // 三头龙入场对话（由第一个龙触发）
        BattleDialogEvents.Raise(BattleDialogEventType.DragonEnter, enemy: this);
    }

    protected override void InitializeEnemyRuntime()
    {
        base.InitializeEnemyRuntime();
        DragonPositionSet();
    }

    /// <summary>根据 enemyID 从 DragonSpawnPositionManager 获取龙Boss的专属生成位置</summary>
    private void DragonPositionSet()
    {
        if (DragonSpawnPositionManager.Instance == null) return;
        if (!DragonSpawnPositionManager.Instance.TryGetDragonSpawnPosition(enemyID, out Vector3 position, out Quaternion rotation))
            return;

        transform.SetPositionAndRotation(position, rotation);
    }

    /// <summary>被其他龙死亡时调用，提升强化等级</summary>
    public void ApplyReinforcement()
    {
        int previousLevel = m_reinforceLevel;
        m_reinforceLevel = Mathf.Min(m_reinforceLevel + 1, rageThreshold);
        if (m_reinforceLevel >= rageThreshold)
        {
            FloatingTipGenerator.Instance?.ShowTipAtObject(transform, $"{combatantName}进入暴怒状态！");
        }
    }

    /// <summary>强化后回复生命（仅最后一头龙）</summary>
    public void ApplyFinalDragonHeal()
    {
        int healAmount = Mathf.RoundToInt(maxHP * 0.12f);
        Heal(healAmount);
        FloatingTipGenerator.Instance?.ShowTipAtObject(transform, $"{combatantName}回复{healAmount}生命");
    }

    public override void Die()
    {
        base.Die();
        // 遍历场上存活的同类龙，调用强化
        NotifyOtherDragonsReinforce();
    }

    private void NotifyOtherDragonsReinforce()
    {
        if (EnemyManager.Instance == null) return;

        IReadOnlyList<Enemy> aliveEnemies = EnemyManager.Instance.AliveEnemies;
        int aliveDragonCount = 0;
        List<DragonBossEnemy> aliveDragons = new List<DragonBossEnemy>();

        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            DragonBossEnemy dragon = aliveEnemies[i] as DragonBossEnemy;
            if (dragon == null || dragon == this || dragon.IsDead) continue;
            if (string.Equals(dragon.DragonGroupId, dragonGroupId, System.StringComparison.Ordinal))
            {
                aliveDragons.Add(dragon);
                aliveDragonCount++;
            }
        }

        if (aliveDragonCount == 0) return;

        // 第一头龙死亡：黑雾被吸收
        BattleDialogEvents.Raise(BattleDialogEventType.DragonFirstDeath);

        // 当只剩最后一头龙时，回复生命
        if (aliveDragonCount == 1)
        {
            // 第二头龙死亡：进入暴怒
            BattleDialogEvents.Raise(BattleDialogEventType.DragonSecondDeath);
            aliveDragons[0].ApplyReinforcement();
            aliveDragons[0].ApplyFinalDragonHeal();
            BattleDialogEvents.Raise(BattleDialogEventType.DragonLastStand);
        }
        else
        {
            // 技能强化
            BattleDialogEvents.Raise(BattleDialogEventType.DragonSkillReinforced);
            for (int i = 0; i < aliveDragons.Count; i++)
            {
                aliveDragons[i].ApplyReinforcement();
            }
        }
    }
}
