using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 教程关二 W1 专用敌人：攻击只打副C(standPosition=2)，前若干回合禁止击杀，免疫震慑，绝剑士命中时受到等同于当前生命值的真实伤害
/// 数值从 EnemyData.csv（enemyID=TutorialSingle）读取
/// </summary>
public class TutorialSingleEnemy : Enemy
{
    private const int InvincibilityTurns = 3;
    private const string JueJianShiCharacterId = "绝剑士";

    private int m_turnsSinceLastBleed;

    public override bool ShouldRegisterAtBattleStart => true;

    protected override void InitializeEnemyRuntime()
    {
        base.InitializeEnemyRuntime();

        // 教程关二：初始指挥点置为0
        Commander.GetInstance()?.SetInitialCommandPoints(0);
    }

    /// <summary>
    /// 重写回合执行：只攻击副C(standPosition=2)，前若干回合禁止击杀
    /// </summary>
    public override IEnumerator PerformTurn()
    {
        yield return BeginTurnPreActions();
        if (!CanProceedWithTurn)
        {
            yield break;
        }

        // 无敌回合计数
        m_turnsSinceLastBleed++;
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

    protected override bool CanReceiveState(StateType stateType, UnitCombatant giver)
    {
        if (stateType == StateType.Daze)
        {
            FloatingTipGenerator.Instance?.ShowTipAtObject(transform, $"{combatantName}免疫{StateDictionaryManager.GetStateName(stateType)}");
            return false;
        }

        return base.CanReceiveState(stateType, giver);
    }

    /// <summary>
    /// 重写伤害处理：前若干回合禁止击杀；绝剑士命中时受到等同于当前生命值的真实伤害
    /// </summary>
    public override void TakeDamage(DamageInfo damageInfo)
    {
        if (dead) return;

        if (IsDamageFromJueJianShi(damageInfo.Source))
        {
            int trueDamage = currentHP;
            if (trueDamage <= 0) return;

            DamageInfo lethalTrueDamage = new DamageInfo(trueDamage, damageInfo.Source, damageInfo.DamageType).AsTrueDamage();
            base.TakeDamage(lethalTrueDamage);
            return;
        }

        int safeDamage = damageInfo.Damage;
        if (currentHP - safeDamage <= 0 && m_turnsSinceLastBleed < InvincibilityTurns)
        {
            safeDamage = currentHP - 1;
            if (safeDamage < 0) safeDamage = 0;
        }

        if (safeDamage <= 0) return;

        DamageInfo safeInfo = new DamageInfo(safeDamage, damageInfo.Source, damageInfo.DamageType);

        base.TakeDamage(safeInfo);
    }

    private static bool IsDamageFromJueJianShi(UnitCombatant source)
    {
        return source is Character character && character.characterID == JueJianShiCharacterId;
    }
}
