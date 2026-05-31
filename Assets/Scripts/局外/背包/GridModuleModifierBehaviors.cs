using UnityEngine;

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

        float penalty = enemy is ChessBossEnemy ? m_bossSpeedPenaltyPerDebuff : m_speedPenaltyPerDebuff;
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