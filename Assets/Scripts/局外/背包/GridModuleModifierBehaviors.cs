using UnityEngine;
using System.Collections.Generic;
public sealed class PassiveStatModuleBehavior : BattleModifierBehaviorBase
{
    private readonly float m_speedMultiplier;
    private readonly float m_physicalDamageMultiplier;
    private readonly float m_magicalDamageMultiplier;
    private readonly float m_critDamageBonus;
    private readonly float m_critRateBonus;
    private readonly float m_healingReceivedMultiplier;
    private readonly float m_shieldGainMultiplier;
    private readonly float m_maxHealthMultiplier;
    private readonly float m_defenseMultiplier;

    public PassiveStatModuleBehavior(
        float speedMultiplier = 1f,
        float directDamageMultiplier = 1f,
        float dotDamageMultiplier = 1f,
        float critDamageBonus = 0f,
        float critRateBonus = 0f,
        float healingReceivedMultiplier = 1f,
        float shieldGainMultiplier = 1f,
        float maxHealthMultiplier = 1f,
        float defenseMultiplier = 1f)
    {
        m_speedMultiplier = speedMultiplier;
        m_physicalDamageMultiplier = directDamageMultiplier;
        m_magicalDamageMultiplier = dotDamageMultiplier;
        m_critDamageBonus = critDamageBonus;
        m_critRateBonus = critRateBonus;
        m_healingReceivedMultiplier = healingReceivedMultiplier;
        m_shieldGainMultiplier = shieldGainMultiplier;
        m_maxHealthMultiplier = maxHealthMultiplier;
        m_defenseMultiplier = defenseMultiplier;
    }

    public override float GetPlayerSpeedMultiplier(TemporaryBattleModifierData modifier, Character character) { return m_speedMultiplier; }
    public override float GetPlayerDamageMultiplier(TemporaryBattleModifierData modifier, UnitCombatant attacker, UnitCombatant target, DamageType damageType, bool isCriticalHit)
    {
        return damageType == DamageType.Magical ? m_magicalDamageMultiplier : m_physicalDamageMultiplier;
    }
    public override float GetPlayerCritDamageBonus(TemporaryBattleModifierData modifier, UnitCombatant attacker) { return m_critDamageBonus; }
    public override float GetPlayerCritRateBonus(TemporaryBattleModifierData modifier, UnitCombatant attacker) { return m_critRateBonus; }
    public override float GetPlayerHealingReceivedMultiplier(TemporaryBattleModifierData modifier, UnitCombatant target) { return m_healingReceivedMultiplier; }
    public override float GetPlayerShieldGainMultiplier(TemporaryBattleModifierData modifier, UnitCombatant target) { return m_shieldGainMultiplier; }
    public override float GetPlayerMaxHealthMultiplier(TemporaryBattleModifierData modifier, Character character) { return m_maxHealthMultiplier; }
    public override float GetPlayerDefenseMultiplier(TemporaryBattleModifierData modifier, Character character) { return m_defenseMultiplier; }
}

public sealed class BattleStartCommandModuleBehavior : BattleModifierBehaviorBase
{
    private readonly int m_bonusCommandPoints;

    public BattleStartCommandModuleBehavior(int bonusCommandPoints)
    {
        m_bonusCommandPoints = Mathf.Max(0, bonusCommandPoints);
    }

    public override void HandleRuntimeEvent(Datas datas, TemporaryBattleModifierData modifier, TemporaryBattleModifierRuntimeContext context)
    {
        if (context.EventType != TemporaryBattleModifierRuntimeEventType.BattleStarted)
        {
            return;
        }

        Commander.GetInstance()?.RecoverCommandPoints(m_bonusCommandPoints, $"开局指挥点+{m_bonusCommandPoints}");
    }
}

public sealed class BattleStartAdvanceModuleBehavior : BattleModifierBehaviorBase
{
    private readonly float m_advanceRatio;

    public BattleStartAdvanceModuleBehavior(float advanceRatio)
    {
        m_advanceRatio = Mathf.Max(0f, advanceRatio);
    }

    public override void HandleRuntimeEvent(Datas datas, TemporaryBattleModifierData modifier, TemporaryBattleModifierRuntimeContext context)
    {
        if (context.EventType != TemporaryBattleModifierRuntimeEventType.BattleStarted || CharacterManager.Instance == null)
        {
            return;
        }

        for (int i = 0; i < CharacterManager.Instance.fieldCharacters.Count; i++)
        {
            Character character = CharacterManager.Instance.fieldCharacters[i];
            if (character == null || character.IsDead)
            {
                continue;
            }

            character.ChangeActionValue(Mathf.Max(0f, character.currentActionValue - character.BaseActionValue * m_advanceRatio));
        }
    }
}

public sealed class ExtraCommandModuleBehavior : BattleModifierBehaviorBase
{
    private readonly int m_spendThreshold;
    private readonly int m_recoverAmount;

    public ExtraCommandModuleBehavior(int spendThreshold, int recoverAmount)
    {
        m_spendThreshold = Mathf.Max(1, spendThreshold);
        m_recoverAmount = Mathf.Max(1, recoverAmount);
    }

    public override void HandleRuntimeEvent(Datas datas, TemporaryBattleModifierData modifier, TemporaryBattleModifierRuntimeContext context)
    {
        if (context.EventType != TemporaryBattleModifierRuntimeEventType.CommandPointsSpent || modifier.sourceModuleIndex < 0)
        {
            return;
        }

        TemporaryBattleModifierRuntimeManager.AddTrackedCommandSpend(modifier.sourceModuleIndex, context.Amount);
        int trackedSpend = TemporaryBattleModifierRuntimeManager.GetTrackedCommandSpend(modifier.sourceModuleIndex);
        while (trackedSpend >= m_spendThreshold)
        {
            trackedSpend -= m_spendThreshold;
            Commander.GetInstance()?.RecoverCommandPoints(m_recoverAmount, $"额外指挥+{m_recoverAmount}");
        }

        TemporaryBattleModifierRuntimeManager.SetTrackedCommandSpend(modifier.sourceModuleIndex, trackedSpend);
    }
}

public sealed class SwapNextDamageBoostModuleBehavior : BattleModifierBehaviorBase
{
    private readonly float m_damageBonus;

    public SwapNextDamageBoostModuleBehavior(float damageBonus)
    {
        m_damageBonus = Mathf.Max(0f, damageBonus);
    }

    public override void HandleRuntimeEvent(Datas datas, TemporaryBattleModifierData modifier, TemporaryBattleModifierRuntimeContext context)
    {
        if (context.EventType != TemporaryBattleModifierRuntimeEventType.PlayerCharacterSwapped || context.CurrentCharacter == null)
        {
            return;
        }

        TemporaryBattleModifierRuntimeManager.AddPendingNextDamageBonus(context.CurrentCharacter, m_damageBonus);
    }
}

public sealed class SwapAdvanceModuleBehavior : BattleModifierBehaviorBase
{
    private readonly float m_advanceRatio;

    public SwapAdvanceModuleBehavior(float advanceRatio)
    {
        m_advanceRatio = Mathf.Max(0f, advanceRatio);
    }

    public override void HandleRuntimeEvent(Datas datas, TemporaryBattleModifierData modifier, TemporaryBattleModifierRuntimeContext context)
    {
        if (context.EventType != TemporaryBattleModifierRuntimeEventType.PlayerCharacterSwapped || context.CurrentCharacter == null)
        {
            return;
        }

        context.CurrentCharacter.ChangeActionValue(Mathf.Max(0f, context.CurrentCharacter.currentActionValue - context.CurrentCharacter.BaseActionValue * m_advanceRatio));
    }
}

public sealed class SwapHealModuleBehavior : BattleModifierBehaviorBase
{
    private readonly float m_healRatio;

    public SwapHealModuleBehavior(float healRatio)
    {
        m_healRatio = Mathf.Max(0f, healRatio);
    }

    public override void HandleRuntimeEvent(Datas datas, TemporaryBattleModifierData modifier, TemporaryBattleModifierRuntimeContext context)
    {
        if (context.EventType != TemporaryBattleModifierRuntimeEventType.PlayerCharacterSwapped || context.CurrentCharacter == null)
        {
            return;
        }

        int healAmount = Mathf.RoundToInt(context.CurrentCharacter.maxHP * m_healRatio);
        context.CurrentCharacter.Heal(healAmount);
    }
}

public sealed class HealChaosCleanseModuleBehavior : BattleModifierBehaviorBase
{
    private readonly float m_triggerChance;
    private readonly int m_reduceAmount;

    public HealChaosCleanseModuleBehavior(float triggerChance, int reduceAmount)
    {
        m_triggerChance = Mathf.Clamp01(triggerChance);
        m_reduceAmount = Mathf.Max(1, reduceAmount);
    }

    public override void HandleRuntimeEvent(Datas datas, TemporaryBattleModifierData modifier, TemporaryBattleModifierRuntimeContext context)
    {
        if (context.EventType != TemporaryBattleModifierRuntimeEventType.UnitHealed || !(context.Target is Character healedCharacter))
        {
            return;
        }

        if (Random.value > m_triggerChance)
        {
            return;
        }

        healedCharacter.ReduceChaos(m_reduceAmount);
    }
}

public sealed class EmergencyEvadeModuleBehavior : BattleModifierBehaviorBase
{
    private readonly float m_hpThreshold;

    public EmergencyEvadeModuleBehavior(float hpThreshold)
    {
        m_hpThreshold = Mathf.Clamp01(hpThreshold);
    }

    public override void HandleRuntimeEvent(Datas datas, TemporaryBattleModifierData modifier, TemporaryBattleModifierRuntimeContext context)
    {
        if (context.EventType != TemporaryBattleModifierRuntimeEventType.DamageSettled
            || !(context.Target is Character targetCharacter)
            || targetCharacter.IsDead
            || targetCharacter.maxHP <= 0
            || targetCharacter.currentHP > Mathf.RoundToInt(targetCharacter.maxHP * m_hpThreshold)
            || modifier.sourceModuleIndex < 0)
        {
            return;
        }

        if (!TemporaryBattleModifierRuntimeManager.TryConsumeEmergencyEvade(modifier.sourceModuleIndex))
        {
            return;
        }

        CharacterManager.Instance?.TryAutoSwapToFirstReserve(targetCharacter, false);
    }
}

public sealed class HeavyPoisonModuleBehavior : BattleModifierBehaviorBase
{
    private readonly float m_speedPenaltyPerDebuff;
    private readonly float m_bossSpeedPenaltyPerDebuff;

    public HeavyPoisonModuleBehavior(float speedPenaltyPerDebuff, float bossSpeedPenaltyPerDebuff)
    {
        m_speedPenaltyPerDebuff = Mathf.Max(0f, speedPenaltyPerDebuff);
        m_bossSpeedPenaltyPerDebuff = Mathf.Max(0f, bossSpeedPenaltyPerDebuff);
    }

    public override float GetEnemyTurnEndActionValueMultiplier(TemporaryBattleModifierData modifier, Enemy enemy)
    {
        if (enemy == null)
        {
            return 1f;
        }

        int debuffCount = 0;
        for (int i = 0; i < enemy.States.Count; i++)
        {
            State state = enemy.States[i];
            if (state != null && state.isDebuff)
            {
                debuffCount++;
            }
        }

        if (debuffCount <= 0)
        {
            return 1f;
        }

        float penalty = enemy is ChessQueenEnemy ? m_bossSpeedPenaltyPerDebuff : m_speedPenaltyPerDebuff;
        return 1f + debuffCount * penalty;
    }
}

public sealed class HeavyTurretModuleBehavior : BattleModifierBehaviorBase
{
    private readonly float m_damageMultiplierWhileShielded;
    private readonly float m_actionValueMultiplierWhileShielded;
    private readonly float m_shieldBreakAdvanceRatio;

    public HeavyTurretModuleBehavior(float damageMultiplierWhileShielded, float actionValueMultiplierWhileShielded, float shieldBreakAdvanceRatio)
    {
        m_damageMultiplierWhileShielded = Mathf.Max(1f, damageMultiplierWhileShielded);
        m_actionValueMultiplierWhileShielded = Mathf.Max(1f, actionValueMultiplierWhileShielded);
        m_shieldBreakAdvanceRatio = Mathf.Max(0f, shieldBreakAdvanceRatio);
    }

    public override float GetPlayerDamageMultiplier(TemporaryBattleModifierData modifier, UnitCombatant attacker, UnitCombatant target, DamageType damageType, bool isCriticalHit)
    {
        return attacker != null && attacker.currentShield > 0 ? m_damageMultiplierWhileShielded : 1f;
    }

    public override float GetCharacterTurnEndActionValueMultiplier(TemporaryBattleModifierData modifier, Character character)
    {
        return character != null && character.currentShield > 0 ? m_actionValueMultiplierWhileShielded : 1f;
    }

    public override void HandleRuntimeEvent(Datas datas, TemporaryBattleModifierData modifier, TemporaryBattleModifierRuntimeContext context)
    {
        if (context.EventType != TemporaryBattleModifierRuntimeEventType.ShieldBroken || !(context.Target is Character targetCharacter))
        {
            return;
        }

        targetCharacter.ChangeActionValue(Mathf.Max(0f, targetCharacter.currentActionValue - targetCharacter.BaseActionValue * m_shieldBreakAdvanceRatio));
    }
}

public sealed class SupportSwapAdvanceModuleBehavior : BattleModifierBehaviorBase
{
    private readonly float m_advanceRatio;

    public SupportSwapAdvanceModuleBehavior(float advanceRatio)
    {
        m_advanceRatio = Mathf.Max(0f, advanceRatio);
    }

    public override void HandleRuntimeEvent(Datas datas, TemporaryBattleModifierData modifier, TemporaryBattleModifierRuntimeContext context)
    {
        if (context.EventType != TemporaryBattleModifierRuntimeEventType.PlayerCharacterSwapped
            || context.CurrentCharacter == null
            || !TemporaryBattleModifierRuntimeManager.IsSupportCharacter(context.PreviousCharacter))
        {
            return;
        }

        context.CurrentCharacter.ChangeActionValue(Mathf.Max(0f, context.CurrentCharacter.currentActionValue - context.CurrentCharacter.BaseActionValue * m_advanceRatio));
    }
}

public sealed class ChaosImmunityModuleBehavior : BattleModifierBehaviorBase
{
    private readonly float m_healRatio;

    public ChaosImmunityModuleBehavior(float healRatio)
    {
        m_healRatio = Mathf.Max(0f, healRatio);
    }

    public override void HandleRuntimeEvent(Datas datas, TemporaryBattleModifierData modifier, TemporaryBattleModifierRuntimeContext context)
    {
        if (context.EventType != TemporaryBattleModifierRuntimeEventType.ChaosMaxReached
            || !(context.Target is Character targetCharacter)
            || modifier.sourceModuleIndex < 0
            || !TemporaryBattleModifierRuntimeManager.TryConsumeChaosImmunity(modifier.sourceModuleIndex))
        {
            return;
        }

        targetCharacter.SetChaos(targetCharacter.ChaosValue / 2);
        targetCharacter.Heal(Mathf.RoundToInt(targetCharacter.maxHP * m_healRatio));
        context.Handled = true;
    }
}

public sealed class SwapChargeBurstModuleBehavior : BattleModifierBehaviorBase
{
    private readonly float m_chargeThreshold;
    private readonly int m_maxStacks;
    private readonly float m_damageBonusPerStack;

    public SwapChargeBurstModuleBehavior(float chargeThreshold, int maxStacks, float damageBonusPerStack)
    {
        m_chargeThreshold = Mathf.Max(1f, chargeThreshold);
        m_maxStacks = Mathf.Max(1, maxStacks);
        m_damageBonusPerStack = Mathf.Max(0f, damageBonusPerStack);
    }

    public override void HandleRuntimeEvent(Datas datas, TemporaryBattleModifierData modifier, TemporaryBattleModifierRuntimeContext context)
    {
        if (modifier.sourceModuleIndex < 0)
        {
            return;
        }

        if (context.EventType == TemporaryBattleModifierRuntimeEventType.ReserveActionValueAdvanced && context.Target is Character reserveCharacter)
        {
            TemporaryBattleModifierRuntimeManager.AddReserveChargeProgress(modifier.sourceModuleIndex, reserveCharacter, context.FloatValue, m_chargeThreshold, m_maxStacks);
            return;
        }

        if (context.EventType != TemporaryBattleModifierRuntimeEventType.PlayerCharacterSwapped || context.CurrentCharacter == null)
        {
            return;
        }

        int stackCount = TemporaryBattleModifierRuntimeManager.ConsumeSwapChargeStacks(modifier.sourceModuleIndex, context.CurrentCharacter);
        if (stackCount <= 0)
        {
            return;
        }

        TemporaryBattleModifierRuntimeManager.AddPendingNextDamageBonus(context.CurrentCharacter, stackCount * m_damageBonusPerStack);
    }
}

public sealed class EmergencySwapInModuleBehavior : BattleModifierBehaviorBase
{
    private readonly float m_baseAdvanceRatio;
    private readonly float m_enemyAheadAdvanceRatio;

    public EmergencySwapInModuleBehavior(float baseAdvanceRatio, float enemyAheadAdvanceRatio)
    {
        m_baseAdvanceRatio = Mathf.Max(0f, baseAdvanceRatio);
        m_enemyAheadAdvanceRatio = Mathf.Max(0f, enemyAheadAdvanceRatio);
    }

    public override void HandleRuntimeEvent(Datas datas, TemporaryBattleModifierData modifier, TemporaryBattleModifierRuntimeContext context)
    {
        if (context.EventType != TemporaryBattleModifierRuntimeEventType.PlayerCharacterSwapped || context.CurrentCharacter == null)
        {
            return;
        }

        float advanceRatio = TemporaryBattleModifierRuntimeManager.IsNextCombatantEnemy() ? m_enemyAheadAdvanceRatio : m_baseAdvanceRatio;
        context.CurrentCharacter.ChangeActionValue(Mathf.Max(0f, context.CurrentCharacter.currentActionValue - context.CurrentCharacter.BaseActionValue * advanceRatio));
    }
}

// ============================================================
// 起死回生 (FatalGuard) — 致命伤害免疫+回血+清debuff
// ============================================================
public sealed class FatalGuardModuleBehavior : BattleModifierBehaviorBase
{
    private readonly float m_healRatio;

    public FatalGuardModuleBehavior(float healRatio)
    {
        m_healRatio = Mathf.Clamp01(healRatio);
    }

    public override void HandleRuntimeEvent(Datas datas, TemporaryBattleModifierData modifier, TemporaryBattleModifierRuntimeContext context)
    {
        if (context.EventType != TemporaryBattleModifierRuntimeEventType.DamageSettled
            || !(context.Target is Character targetCharacter)
            || targetCharacter.currentHP > 0
            || targetCharacter.IsDead
            || modifier.sourceModuleIndex < 0)
        {
            return;
        }

        if (!TemporaryBattleModifierRuntimeManager.TryConsumeFatalGuard(modifier.sourceModuleIndex))
        {
            return;
        }

        // 回复到最大生命值的指定比例
        int healAmount = Mathf.RoundToInt(targetCharacter.maxHP * m_healRatio);
        targetCharacter.currentHP = Mathf.Max(1, healAmount);

        // 清除所有负面状态
        targetCharacter.ClearAllDebuffs();
    }
}

// ============================================================
// 燃血逆转 (BloodReverse) — 燃血效果反转
// ============================================================
public sealed class BloodReverseModuleBehavior : BattleModifierBehaviorBase
{
    private readonly float m_killPenaltyRatio;
    private readonly float m_critDamageBonus;

    public BloodReverseModuleBehavior(float killPenaltyRatio, float critDamageBonus)
    {
        m_killPenaltyRatio = Mathf.Clamp01(killPenaltyRatio);
        m_critDamageBonus = Mathf.Max(0f, critDamageBonus);
    }

    public override void HandleRuntimeEvent(Datas datas, TemporaryBattleModifierData modifier, TemporaryBattleModifierRuntimeContext context)
    {
        if (context.EventType != TemporaryBattleModifierRuntimeEventType.DamageSettled
            || !(context.Source is Character sourceCharacter)
            || !(context.Target is UnitCombatant target)
            || target.currentHP > 0
            || context.Source == context.Target)
        {
            return;
        }

        // 检查攻击者是否持有燃血状态
        if (!HasBurningBlood(sourceCharacter))
        {
            return;
        }

        // 反转逻辑：击杀成功 → 扣血25% + 获得必暴buff
        int selfDamage = Mathf.RoundToInt(sourceCharacter.maxHP * m_killPenaltyRatio);
        sourceCharacter.TakeDamage(new UnitCombatant.DamageInfo(selfDamage, sourceCharacter).AsTrueDamage());

        // 添加"下一次攻击必定暴击，暴击伤害提高"的buff
        // 通过 pendingNextDamageBonus 系统实现暴伤加成，暴击率通过临时buff
        TemporaryBattleModifierRuntimeManager.AddPendingNextDamageBonus(sourceCharacter, m_critDamageBonus);
        sourceCharacter.AddState(StateType.CritRhythm, sourceCharacter, 99, 1);
    }

    private static bool HasBurningBlood(Character character)
    {
        if (character == null)
        {
            return false;
        }

        for (int i = 0; i < character.States.Count; i++)
        {
            if (character.States[i] != null && character.States[i].stateType == StateType.BurningBlood)
            {
                return true;
            }
        }

        return false;
    }
}

// ============================================================
// 域场共鸣 (DomainResonance) — 双域场触发共鸣
// ============================================================
public sealed class DomainResonanceModuleBehavior : BattleModifierBehaviorBase
{
    private readonly float m_durationExtensionRatio;
    private readonly float m_enemyDamageMultiplier;

    public DomainResonanceModuleBehavior(float durationExtensionRatio, float enemyDamageMultiplier)
    {
        m_durationExtensionRatio = Mathf.Max(0f, durationExtensionRatio);
        m_enemyDamageMultiplier = Mathf.Max(1f, enemyDamageMultiplier);
    }

    // 域场内敌方受伤倍率
    public override float GetPlayerDamageMultiplier(TemporaryBattleModifierData modifier, UnitCombatant attacker, UnitCombatant target, DamageType damageType, bool isCriticalHit)
    {
        if (target is Enemy && IsDomainResonanceActive())
        {
            return m_enemyDamageMultiplier;
        }

        return 1f;
    }

    private static bool IsDomainResonanceActive()
    {
        if (EnvironmentManager.Instance == null)
        {
            return false;
        }

        // 检查是否存在至少两种不同的域场（其中之一为奇迹域场也算）
        bool hasMiracleField = EnvironmentManager.Instance.HasEnvironment(EnvironmentType.MiracleField);
        int nonMiracleDomainCount = 0;

        EnvironmentType[] domainTypes = { EnvironmentType.Gravity, EnvironmentType.Cutdown, EnvironmentType.DesperationField };
        for (int i = 0; i < domainTypes.Length; i++)
        {
            if (EnvironmentManager.Instance.HasEnvironment(domainTypes[i]))
            {
                nonMiracleDomainCount++;
            }
        }

        // 奇迹域场 + 任意其他域场，或两个非奇迹域场同时存在
        return (hasMiracleField && nonMiracleDomainCount >= 1) || nonMiracleDomainCount >= 2;
    }
}

// ============================================================
// 蓄势逆击·共鸣 (ChargeCounterResonance) — 满层触发共鸣
// ============================================================
public sealed class ChargeCounterResonanceModuleBehavior : BattleModifierBehaviorBase
{
    private readonly float m_dotSettleRatio;
    private readonly float m_dotDamageBonus;
    private readonly int m_dotDamageBonusTurns;

    public ChargeCounterResonanceModuleBehavior(float dotSettleRatio, float dotDamageBonus, int dotDamageBonusTurns)
    {
        m_dotSettleRatio = Mathf.Clamp01(dotSettleRatio);
        m_dotDamageBonus = Mathf.Max(0f, dotDamageBonus);
        m_dotDamageBonusTurns = Mathf.Max(1, dotDamageBonusTurns);
    }

    public override void HandleRuntimeEvent(Datas datas, TemporaryBattleModifierData modifier, TemporaryBattleModifierRuntimeContext context)
    {
        if (context.EventType != TemporaryBattleModifierRuntimeEventType.CounterChargeTriggered
            || !(context.Source is Character sourceCharacter))
        {
            return;
        }

        // 对敌方全体立即触发所有DOT的50%结算
        if (EnemyManager.Instance != null)
        {
            IReadOnlyList<Enemy> aliveEnemies = EnemyManager.Instance.AliveEnemies;
            for (int i = 0; i < aliveEnemies.Count; i++)
            {
                Enemy enemy = aliveEnemies[i];
                if (enemy == null || enemy.IsDead)
                {
                    continue;
                }

                SettleDotsOnTarget(enemy, m_dotSettleRatio);
            }
        }

        // 我方全体DOT伤害提升，持续指定回合
        if (CharacterManager.Instance != null)
        {
            for (int i = 0; i < CharacterManager.Instance.allCharacters.Count; i++)
            {
                Character character = CharacterManager.Instance.allCharacters[i];
                if (character == null || character.IsDead)
                {
                    continue;
                }

                character.AddState(StateType.DamageChange, sourceCharacter, m_dotDamageBonusTurns, 1);
            }
        }
    }

    private static void SettleDotsOnTarget(UnitCombatant target, float ratio)
    {
        if (target == null || target.IsDead)
        {
            return;
        }

        for (int i = 0; i < target.States.Count; i++)
        {
            State state = target.States[i];
            if (state == null || !state.isDot)
            {
                continue;
            }

            // 立即触发一次DOT伤害的指定比例
            state.DotTrigger(ratio);
        }
    }
}

// ============================================================
// 物法双修 (HybridDamage) — 伤害类型交替叠层
// ============================================================
public sealed class HybridDamageModuleBehavior : BattleModifierBehaviorBase
{
    private readonly float m_damageBonusPerStack;

    // 静态追踪：记录每个模块的上一次伤害类型和当前叠层
    // Key: sourceModuleIndex, Value: (lastDamageType, stackCount)
    private static readonly Dictionary<int, DamageType> s_lastDamageTypeByModule = new Dictionary<int, DamageType>();
    private static readonly Dictionary<int, int> s_hybridStackByModule = new Dictionary<int, int>();

    public HybridDamageModuleBehavior(float damageBonusPerStack)
    {
        m_damageBonusPerStack = Mathf.Max(0f, damageBonusPerStack);
    }

    public override float GetPlayerDamageMultiplier(TemporaryBattleModifierData modifier, UnitCombatant attacker, UnitCombatant target, DamageType damageType, bool isCriticalHit)
    {
        if (modifier.sourceModuleIndex < 0)
        {
            return 1f;
        }

        int moduleIndex = modifier.sourceModuleIndex;
        bool hasLastType = s_lastDamageTypeByModule.TryGetValue(moduleIndex, out DamageType lastType);

        if (hasLastType && lastType != damageType)
        {
            // 类型不同 → 叠层+1
            int newStack = s_hybridStackByModule.TryGetValue(moduleIndex, out int stack) ? stack + 1 : 1;
            s_hybridStackByModule[moduleIndex] = newStack;
        }
        else if (hasLastType && lastType == damageType)
        {
            // 类型相同 → 层数清零
            s_hybridStackByModule[moduleIndex] = 0;
        }

        s_lastDamageTypeByModule[moduleIndex] = damageType;

        int currentStack = s_hybridStackByModule.TryGetValue(moduleIndex, out int s) ? s : 0;
        return 1f + currentStack * m_damageBonusPerStack;
    }

    public static void ResetModuleTracking(int moduleIndex)
    {
        s_lastDamageTypeByModule.Remove(moduleIndex);
        s_hybridStackByModule.Remove(moduleIndex);
    }

    public static void ResetAllModuleTracking()
    {
        s_lastDamageTypeByModule.Clear();
        s_hybridStackByModule.Clear();
    }
}

// ============================================================
// 暴噬蔓延 (CritDotSpread) — 暴击扩散DOT+延长
// ============================================================
public sealed class CritDotSpreadModuleBehavior : BattleModifierBehaviorBase
{
    private readonly float m_triggerChance;
    private readonly int m_extendTurns;
    private readonly int m_maxExtraApplyCount;

    private static readonly StateType[] s_randomDotTypes =
    {
        StateType.Ice,
        StateType.Corrosion,
        StateType.Wind
    };

    public CritDotSpreadModuleBehavior(float triggerChance, int extendTurns, int maxExtraApplyCount)
    {
        m_triggerChance = Mathf.Clamp01(triggerChance);
        m_extendTurns = Mathf.Max(1, extendTurns);
        m_maxExtraApplyCount = Mathf.Max(0, maxExtraApplyCount);
    }

    public override void HandleRuntimeEvent(Datas datas, TemporaryBattleModifierData modifier, TemporaryBattleModifierRuntimeContext context)
    {
        if (context.EventType != TemporaryBattleModifierRuntimeEventType.CriticalHit
            || modifier.sourceModuleIndex < 0)
        {
            return;
        }

        if (Random.value > m_triggerChance)
        {
            return;
        }

        // 敌方全体DOT延长1回合
        if (EnemyManager.Instance != null)
        {
            IReadOnlyList<Enemy> aliveEnemies = EnemyManager.Instance.AliveEnemies;
            for (int i = 0; i < aliveEnemies.Count; i++)
            {
                Enemy enemy = aliveEnemies[i];
                if (enemy == null || enemy.IsDead)
                {
                    continue;
                }

                ExtendAllDotsOnTarget(enemy, m_extendTurns);
            }
        }

        // 每场战斗前3次触发时，额外施加1种随机DOT
        int triggerCount = TemporaryBattleModifierRuntimeManager.GetAndIncrementCritDotSpreadTrigger(modifier.sourceModuleIndex);
        if (triggerCount < m_maxExtraApplyCount && context.Target is UnitCombatant target && target != null)
        {
            StateType randomDot = s_randomDotTypes[Random.Range(0, s_randomDotTypes.Length)];
            target.AddState(randomDot, context.Source, 2, 1);
        }
    }

    private static void ExtendAllDotsOnTarget(UnitCombatant target, int turns)
    {
        if (target == null)
        {
            return;
        }

        for (int i = 0; i < target.States.Count; i++)
        {
            State state = target.States[i];
            if (state != null && state.isDot)
            {
                state.ExtendTurns(turns);
            }
        }
    }
}

// ============================================================
// 专注机枪 (FocusFire) — 伤害类型相同叠层
// ============================================================
public sealed class FocusFireModuleBehavior : BattleModifierBehaviorBase
{
    private readonly float m_damageBonusPerStack;

    // 静态追踪：记录每个模块的上一次伤害类型和当前叠层
    private static readonly Dictionary<int, DamageType> s_lastDamageTypeByModule = new Dictionary<int, DamageType>();
    private static readonly Dictionary<int, int> s_focusFireStackByModule = new Dictionary<int, int>();

    public FocusFireModuleBehavior(float damageBonusPerStack)
    {
        m_damageBonusPerStack = Mathf.Max(0f, damageBonusPerStack);
    }

    public override float GetPlayerDamageMultiplier(TemporaryBattleModifierData modifier, UnitCombatant attacker, UnitCombatant target, DamageType damageType, bool isCriticalHit)
    {
        if (modifier.sourceModuleIndex < 0)
        {
            return 1f;
        }

        int moduleIndex = modifier.sourceModuleIndex;
        bool hasLastType = s_lastDamageTypeByModule.TryGetValue(moduleIndex, out DamageType lastType);

        if (hasLastType && lastType == damageType)
        {
            // 类型相同 → 叠层+1
            int newStack = s_focusFireStackByModule.TryGetValue(moduleIndex, out int stack) ? stack + 1 : 1;
            s_focusFireStackByModule[moduleIndex] = newStack;
        }
        else if (hasLastType && lastType != damageType)
        {
            // 类型不同 → 层数清零
            s_focusFireStackByModule[moduleIndex] = 0;
        }

        s_lastDamageTypeByModule[moduleIndex] = damageType;

        int currentStack = s_focusFireStackByModule.TryGetValue(moduleIndex, out int s) ? s : 0;
        return 1f + currentStack * m_damageBonusPerStack;
    }

    public static void ResetModuleTracking(int moduleIndex)
    {
        s_lastDamageTypeByModule.Remove(moduleIndex);
        s_focusFireStackByModule.Remove(moduleIndex);
    }

    public static void ResetAllModuleTracking()
    {
        s_lastDamageTypeByModule.Clear();
        s_focusFireStackByModule.Clear();
    }
}