using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 教程关二 W1 专用敌人：攻击只打副C(standPosition=2)，禁止击杀，每2回合自动掉430血
/// 继承自 Enemy，重写 PerformTurn 实现定制行为
/// </summary>
public class TutorialSingleEnemy : Enemy
{
    [Header("教程定制数值")]
    [SerializeField] private int m_customMaxHP = 600;
    [SerializeField] private int m_customAttack = 171;
    [SerializeField] private float m_customK = 120f;

    [Header("掉血机制")]
    [SerializeField] private int m_bleedDamage = 430;
    [SerializeField] private int m_bleedIntervalTurns = 2;

    private int m_turnsSinceLastBleed;

    public override bool ShouldRegisterAtBattleStart => true;

    protected override void InitializeEnemyRuntime()
    {
        if (m_runtimeInitialized)
        {
            return;
        }

        participateInTurnLoopAtStart = true;
        InitializeSkill();
        LoadCustomData();
        m_defaultScale = transform.localScale;
        SetBattleVisibility(true);

        // 教程关二：初始指挥点置为0
        Commander.GetInstance()?.SetInitialCommandPoints(0);

        m_runtimeInitialized = true;
    }

    private void LoadCustomData()
    {
        maxHP = m_customMaxHP;
        currentHP = maxHP;
        attack = m_customAttack;
        K = m_customK;

        if (!string.IsNullOrEmpty(enemyID))
        {
            if (LevelDataContainer.TryGetEnemyLevelData(enemyID, level, out EnemyLevelData levelData))
            {
                defense = levelData.defense;
                speed = levelData.speed;
            }
        }
    }

    /// <summary>
    /// 重写回合执行：添加掉血机制 + 只攻击副C(standPosition=2) + 禁止击杀
    /// </summary>
    public override IEnumerator PerformTurn()
    {
        yield return BeginTurnPreActions();
        if (!CanProceedWithTurn)
        {
            yield break;
        }

        // 掉血机制
        m_turnsSinceLastBleed++;
        if (m_turnsSinceLastBleed >= m_bleedIntervalTurns)
        {
            m_turnsSinceLastBleed = 0;
            if (m_bleedDamage > 0 && !dead)
            {
                int safeBleed = m_bleedDamage;
                if (currentHP - safeBleed <= 0)
                {
                    safeBleed = Mathf.Max(0, currentHP - 1);
                }

                if (safeBleed > 0)
                {
                    TakeDamage(new DamageInfo(safeBleed, this, DamageType.Physical));
                }
            }
        }

        if (dead)
        {
            yield break;
        }

        // 执行攻击：只打副C
        yield return TutorialAttackCoroutine();
        InvokeOnEnemyActEvent();
    }

    /// <summary>
    /// 教程攻击协程：强制目标为副C(standPosition=2)，禁止击杀
    /// </summary>
    private IEnumerator TutorialAttackCoroutine()
    {
        // 获取可用技能
        EnemySkillBase skill = SelectSkillForTurn();
        if (skill == null)
        {
            FloatingTipGenerator.Instance?.ShowTipAtObject(transform, $"{combatantName}暂无可用技能");
            yield break;
        }

        yield return new WaitForSeconds(0.2f);
        FloatingTipGenerator.Instance?.ShowDefaultTip(skill.skillName);
        yield return new WaitForSeconds(0.5f);
        yield return new WaitForSeconds(0.5f);

        // 获取副C（standPosition=2）
        Character target = CharacterManager.Instance?.GetFieldCharacterByStandPosition(2);
        if (target == null || target.IsDead)
        {
            // 副C不存在则随机选一个
            target = CharacterManager.Instance?.GetCharacterByRand();
        }

        if (target != null && !target.IsDead)
        {
            yield return CinemachineCameraManager.Instance?.TransitionIntoSkillCamera(ManagedCameraType.Help);
            // 计算伤害
            var damageInfo = DamageCounter.CountDamage(this, target, skill, DamageType.Physical, false);
            // 禁止击杀：确保伤害不致死
            if (target.currentHP - damageInfo.Damage <= 0)
            {
                int safeDamage = target.currentHP - 1;
                if (safeDamage < 0) safeDamage = 0;
                damageInfo = new UnitCombatant.DamageInfo(safeDamage, this, DamageType.Physical);
            }

            if (damageInfo.Damage > 0)
            {
                target.TakeDamage(damageInfo);
            }

            target.TryAddChaos(1);
            yield return new WaitForSeconds(0.5f);
            yield return CinemachineCameraManager.Instance?.TransitionOutOfSkillCamera();
        }

        yield return WaitForDeathEvents();
    }

    /// <summary>
    /// 重写伤害处理：禁止击杀
    /// </summary>
    public override void TakeDamage(DamageInfo damageInfo)
    {
        if (dead) return;

        int safeDamage = damageInfo.Damage;
        if (currentHP - safeDamage <= 0&&m_turnsSinceLastBleed<2)
        {
            safeDamage = currentHP - 1;
            if (safeDamage < 0) safeDamage = 0;
        }

        if (safeDamage <= 0) return;

        DamageInfo safeInfo = new DamageInfo(safeDamage, damageInfo.Source, damageInfo.DamageType);

        base.TakeDamage(safeInfo);
    }
}
