using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 龙Boss基类 — 三龙共享的强化/暴怒机制
/// 指挥点奖励：战斗开始+1, 混沌龙技能+1, 击杀第一头+1, 强化阶段+1, 击杀第二头+1, 暴怒阶段+2, 暴怒技能+1
/// 低保：阶段一200AV, 阶段二180AV, 阶段三240AV
/// </summary>
public class DragonBossEnemy : Enemy
{
    [Header("龙Boss配置")]
    [SerializeField] private string dragonGroupId = "dragon-boss";
    [SerializeField, Min(0)] private int rageThreshold = 2;

    // ============ 指挥点奖励静态追踪 ============
    private static int s_dragonKillCount;
    private static bool s_battleStartRewarded;
    private static bool s_firstKillRewarded;
    private static bool s_reinforcePhaseRewarded;
    private static bool s_secondKillRewarded;
    private static bool s_ragePhaseRewarded;
    private const float DragonPhaseOneGuaranteeThreshold = 200f;
    private const float DragonPhaseTwoGuaranteeThreshold = 180f;
    private const float DragonPhaseThreeGuaranteeThreshold = 240f;

    private int m_reinforceLevel;

    public string DragonGroupId => dragonGroupId;
    public int ReinforceLevel => m_reinforceLevel;
    public bool IsRaging => m_reinforceLevel >= rageThreshold;

    public override bool CanUseEnemySkill(EnemySkillBase skill)
    {
        if (!base.CanUseEnemySkill(skill))
        {
            return false;
        }

        // 暴怒状态下优先暴怒技能；暴怒技能 CD 中则随机释放普通/强化技能
        if (IsRaging && skill != null && !IsDragonRageSkill(skill.enemySkillType))
        {
            EnemySkillBase rageSkill = TryGetRageSkillInstance();
            if (rageSkill != null && rageSkill.CanUse(this))
            {
                return false;
            }
        }

        return true;
    }

    protected override EnemySkillBase GetForcedSkillForTurn()
    {
        if (!IsRaging)
        {
            return null;
        }

        EnemySkillBase rageSkill = TryGetRageSkillInstance();
        if (rageSkill != null && rageSkill.CanUse(this))
        {
            return rageSkill;
        }

        return null;
    }

    private static bool IsDragonRageSkill(EnemySkillType skillType)
    {
        return skillType == EnemySkillType.DragonDotRage
            || skillType == EnemySkillType.DragonDirectRage
            || skillType == EnemySkillType.DragonChaosRage;
    }

    private EnemySkillBase TryGetRageSkillInstance()
    {
        EnemySkillBase dotRage = GetSkillInstance(EnemySkillType.DragonDotRage);
        if (dotRage != null)
        {
            return dotRage;
        }

        EnemySkillBase directRage = GetSkillInstance(EnemySkillType.DragonDirectRage);
        if (directRage != null)
        {
            return directRage;
        }

        return GetSkillInstance(EnemySkillType.DragonChaosRage);
    }

    /// <summary>重置龙Boss指挥点奖励的静态状态（战斗开始时由第一个龙调用）</summary>
    public static void ResetDragonCommandPointState()
    {
        s_dragonKillCount = 0;
        s_battleStartRewarded = false;
        s_firstKillRewarded = false;
        s_reinforcePhaseRewarded = false;
        s_secondKillRewarded = false;
        s_ragePhaseRewarded = false;
        CombatDamageTracker.Reset();
    }

    private float m_baseAttack;
    private int m_baseDefense;
    private float m_reinforceIncomingDamageMultiplier = 1f;
    private float m_reinforceSpeedMultiplier = 1f;

    protected override void InitializeEnemyRuntime()
    {
        base.InitializeEnemyRuntime();
        m_baseAttack = attack;
        m_baseDefense = defense;
        DragonPositionSet();
    }

    protected override float GetSpeed()
    {
        return base.GetSpeed() * m_reinforceSpeedMultiplier;
    }

    public override float GetIncomingDamageMultiplier(bool isDotDamage, bool isTrueDamage)
    {
        return base.GetIncomingDamageMultiplier(isDotDamage, isTrueDamage) * m_reinforceIncomingDamageMultiplier;
    }

    /// <summary>根据 enemyID 从 DragonSpawnPositionManager 获取龙Boss的专属生成位置</summary>
    private void DragonPositionSet()
    {
        if (DragonSpawnPositionManager.Instance == null) return;
        if (!DragonSpawnPositionManager.Instance.TryGetDragonSpawnPosition(enemyID, out Vector3 position, out Quaternion rotation))
            return;

        transform.SetPositionAndRotation(position, rotation);
    }

    protected override void Start()
    {
        base.Start();
        BattleDialogEvents.Raise(BattleDialogEventType.DragonEnter, enemy: this);

        if (!s_battleStartRewarded)
        {
            s_battleStartRewarded = true;
            ResetDragonCommandPointState();
            s_battleStartRewarded = true;
            Commander.GetInstance().RecoverCommandPoints(1, "龙Boss战斗开始+1");
            Commander.GetInstance().SetBossGuaranteeThreshold(DragonPhaseOneGuaranteeThreshold);
        }
    }

    /// <summary>
    /// 混沌龙使用技能时调用，奖励指挥点+1
    /// </summary>
    public void NotifyChaosDragonSkillUsed()
    {
        Commander.GetInstance().RecoverCommandPoints(1, "混沌龙技能+1");
    }

    /// <summary>
    /// 暴怒技能释放后调用，奖励指挥点+1
    /// </summary>
    public void NotifyRageSkillUsed()
    {
        Commander.GetInstance().RecoverCommandPoints(1, "暴怒技能+1");
    }

    /// <summary>被其他龙死亡时调用，提升强化等级</summary>
    public void ApplyReinforcement()
    {
        m_reinforceLevel = Mathf.Min(m_reinforceLevel + 1, rageThreshold);
        ApplyReinforceStatScaling();
        if (m_reinforceLevel >= rageThreshold)
        {
            FloatingTipGenerator.Instance?.ShowTipAtObject(transform, $"{combatantName}进入暴怒状态！");
        }
    }

    private void ApplyReinforceStatScaling()
    {
        if (m_reinforceLevel <= 0)
        {
            return;
        }

        if (m_reinforceLevel >= rageThreshold)
        {
            attack = m_baseAttack * 1.30f;
            defense = Mathf.RoundToInt(m_baseDefense * 1.10f);
            m_reinforceIncomingDamageMultiplier = 0.8f;
            m_reinforceSpeedMultiplier = 0.85f;
            return;
        }

        attack = m_baseAttack * 1.15f;
        defense = Mathf.RoundToInt(m_baseDefense * 1.10f);
        m_reinforceIncomingDamageMultiplier = 1f;
        m_reinforceSpeedMultiplier = 1f;
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

        // 指挥点奖励：击杀第一头龙 +1
        s_dragonKillCount++;
        if (s_dragonKillCount == 1 && !s_firstKillRewarded)
        {
            s_firstKillRewarded = true;
            Commander.GetInstance().RecoverCommandPoints(1, "击杀第一头龙+1");
        }

        if (aliveDragonCount == 0) return;

        // 第一头龙死亡：黑雾被吸收
        BattleDialogEvents.Raise(BattleDialogEventType.DragonFirstDeath);

        // 当只剩最后一头龙时，回复生命
        if (aliveDragonCount == 1)
        {
            // 指挥点奖励：击杀第二头龙 +1
            if (!s_secondKillRewarded)
            {
                s_secondKillRewarded = true;
                Commander.GetInstance().RecoverCommandPoints(1, "击杀第二头龙+1");
            }

            // 第二头龙死亡：进入暴怒
            BattleDialogEvents.Raise(BattleDialogEventType.DragonSecondDeath);
            aliveDragons[0].ApplyReinforcement();
            aliveDragons[0].ApplyFinalDragonHeal();

            // 指挥点奖励：暴怒阶段 +2
            if (!s_ragePhaseRewarded)
            {
                s_ragePhaseRewarded = true;
                Commander.GetInstance().RecoverCommandPoints(2, "暴怒阶段+2");
                // 阶段三低保阈值：240AV
                Commander.GetInstance().SetBossGuaranteeThreshold(DragonPhaseThreeGuaranteeThreshold);
            }

            BattleDialogEvents.Raise(BattleDialogEventType.DragonLastStand);
        }
        else
        {
            // 指挥点奖励：强化阶段 +1
            if (!s_reinforcePhaseRewarded)
            {
                s_reinforcePhaseRewarded = true;
                Commander.GetInstance().RecoverCommandPoints(1, "强化阶段+1");
                // 阶段二低保阈值：180AV
                Commander.GetInstance().SetBossGuaranteeThreshold(DragonPhaseTwoGuaranteeThreshold);
            }

            // 技能强化
            BattleDialogEvents.Raise(BattleDialogEventType.DragonSkillReinforced);
            for (int i = 0; i < aliveDragons.Count; i++)
            {
                aliveDragons[i].ApplyReinforcement();
            }
        }
    }
}
