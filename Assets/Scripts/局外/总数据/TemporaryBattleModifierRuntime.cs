using System;
using System.Collections.Generic;
using UnityEngine;

public enum TemporaryBattleModifierRuntimeEventType
{
    None,
    BattleStarted,
    PlayerCharacterSwapped,
    CommandPointsSpent,
    UnitHealed,
    ShieldAdded,
    ShieldBroken,
    DamageSettled,
    CriticalHit,
    ChaosReduced,
    ChaosMaxReached,
    ReserveActionValueAdvanced
}

public static class BattleRuntimeEvents
{
    public static event Action PlayerCharacterSwapped;

    public static void RaisePlayerCharacterSwapped()
    {
        PlayerCharacterSwapped?.Invoke();
    }
}

public sealed class TemporaryBattleModifierRuntimeContext
{
    public TemporaryBattleModifierRuntimeEventType EventType;
    public UnitCombatant Source;
    public UnitCombatant Target;
    public Character PreviousCharacter;
    public Character CurrentCharacter;
    public int Amount;
    public float FloatValue;
    public bool IsDotDamage;
    public bool IsTrueDamage;
    public bool IsCriticalHit;
    public DamageType DamageType;
    public bool Handled;
}

[Serializable]
public class TemporaryBattleModifierData
{
    public LevelEventOptionType optionType;
    public GridModuleType moduleType;
    [Min(0)] public int remainingBattles;
    public float playerSpeedMultiplier = 1f;
    public float playerDirectDamageMultiplier = 1f;
    public float playerDotDamageMultiplier = 1f;
    public float playerCritDamageBonus;
    public int goldPerSwap;
    public int goldPenaltyPerSwap;
    public int sourceModuleIndex = -1;

    public TemporaryBattleModifierData Clone()
    {
        return new TemporaryBattleModifierData
        {
            optionType = optionType,
            moduleType = moduleType,
            remainingBattles = remainingBattles,
            playerSpeedMultiplier = playerSpeedMultiplier,
            playerDirectDamageMultiplier = playerDirectDamageMultiplier,
            playerDotDamageMultiplier = playerDotDamageMultiplier,
            playerCritDamageBonus = playerCritDamageBonus,
            goldPerSwap = goldPerSwap,
            goldPenaltyPerSwap = goldPenaltyPerSwap,
            sourceModuleIndex = sourceModuleIndex
        };
    }
}

public interface ITemporaryBattleModifierBehavior
{
    void HandleRuntimeEvent(Datas datas, TemporaryBattleModifierData modifier, TemporaryBattleModifierRuntimeContext context);
    float GetPlayerSpeedMultiplier(TemporaryBattleModifierData modifier, Character character);
    float GetPlayerDamageMultiplier(TemporaryBattleModifierData modifier, UnitCombatant attacker, UnitCombatant target, DamageType damageType, bool isCriticalHit);
    float GetPlayerCritDamageBonus(TemporaryBattleModifierData modifier, UnitCombatant attacker);
    float GetPlayerCritRateBonus(TemporaryBattleModifierData modifier, UnitCombatant attacker);
    float GetPlayerHealingReceivedMultiplier(TemporaryBattleModifierData modifier, UnitCombatant target);
    float GetPlayerShieldGainMultiplier(TemporaryBattleModifierData modifier, UnitCombatant target);
    float GetPlayerMaxHealthMultiplier(TemporaryBattleModifierData modifier, Character character);
    float GetPlayerDefenseMultiplier(TemporaryBattleModifierData modifier, Character character);
    float GetCharacterTurnEndActionValueMultiplier(TemporaryBattleModifierData modifier, Character character);
    float GetEnemyTurnEndActionValueMultiplier(TemporaryBattleModifierData modifier, Enemy enemy);
}

public abstract class BattleModifierBehaviorBase : ITemporaryBattleModifierBehavior
{
    public virtual void HandleRuntimeEvent(Datas datas, TemporaryBattleModifierData modifier, TemporaryBattleModifierRuntimeContext context) { }
    public virtual float GetPlayerSpeedMultiplier(TemporaryBattleModifierData modifier, Character character) { return 1f; }
    public virtual float GetPlayerDamageMultiplier(TemporaryBattleModifierData modifier, UnitCombatant attacker, UnitCombatant target, DamageType damageType, bool isCriticalHit) { return 1f; }
    public virtual float GetPlayerCritDamageBonus(TemporaryBattleModifierData modifier, UnitCombatant attacker) { return 0f; }
    public virtual float GetPlayerCritRateBonus(TemporaryBattleModifierData modifier, UnitCombatant attacker) { return 0f; }
    public virtual float GetPlayerHealingReceivedMultiplier(TemporaryBattleModifierData modifier, UnitCombatant target) { return 1f; }
    public virtual float GetPlayerShieldGainMultiplier(TemporaryBattleModifierData modifier, UnitCombatant target) { return 1f; }
    public virtual float GetPlayerMaxHealthMultiplier(TemporaryBattleModifierData modifier, Character character) { return 1f; }
    public virtual float GetPlayerDefenseMultiplier(TemporaryBattleModifierData modifier, Character character) { return 1f; }
    public virtual float GetCharacterTurnEndActionValueMultiplier(TemporaryBattleModifierData modifier, Character character) { return 1f; }
    public virtual float GetEnemyTurnEndActionValueMultiplier(TemporaryBattleModifierData modifier, Enemy enemy) { return 1f; }
}

public sealed class SwapGoldTemporaryBattleModifierBehavior : BattleModifierBehaviorBase
{
    public override void HandleRuntimeEvent(Datas datas, TemporaryBattleModifierData modifier, TemporaryBattleModifierRuntimeContext context)
    {
        if (datas == null || modifier == null || context == null || context.EventType != TemporaryBattleModifierRuntimeEventType.PlayerCharacterSwapped)
        {
            return;
        }

        datas.ModifyGold(modifier.goldPerSwap - modifier.goldPenaltyPerSwap);
    }
}

public static class TemporaryBattleModifierBehaviorRegistry
{
    private static readonly Dictionary<LevelEventOptionType, ITemporaryBattleModifierBehavior> s_levelEventBehaviors =
        new Dictionary<LevelEventOptionType, ITemporaryBattleModifierBehavior>
        {
            { LevelEventOptionType.SwapForProfit, new SwapGoldTemporaryBattleModifierBehavior() },
            { LevelEventOptionType.CashOutSwap, new SwapGoldTemporaryBattleModifierBehavior() }
        };

    public static bool TryGetRuntimeBehavior(TemporaryBattleModifierData modifier, out ITemporaryBattleModifierBehavior behavior)
    {
        behavior = null;
        if (modifier == null)
        {
            return false;
        }

        if (modifier.moduleType != GridModuleType.None)
        {
            behavior = GridModuleRuntimeBehaviorFactory.Create(modifier.moduleType);
            return behavior != null;
        }

        return s_levelEventBehaviors.TryGetValue(modifier.optionType, out behavior);
    }
}

public static class GridModuleRuntimeBehaviorFactory
{
    public static ITemporaryBattleModifierBehavior Create(GridModuleType moduleType)
    {
        switch (moduleType)
        {
            case GridModuleType.BattleCommandBonus:
                return new BattleStartCommandModuleBehavior(1);
            case GridModuleType.OpeningAdvance:
                return new BattleStartAdvanceModuleBehavior(0.2f);
            case GridModuleType.ExtraCommand:
                return new ExtraCommandModuleBehavior(4, 1);
            case GridModuleType.SwapDamageBoost:
                return new SwapNextDamageBoostModuleBehavior(0.10f);
            case GridModuleType.SwapSpeedBoost:
                return new SwapAdvanceModuleBehavior(0.10f);
            case GridModuleType.SwapSelfHeal:
                return new SwapHealModuleBehavior(0.05f);
            case GridModuleType.HealingBoost:
                return new PassiveStatModuleBehavior(healingReceivedMultiplier: 1.08f);
            case GridModuleType.HealChaosCleanse:
                return new HealChaosCleanseModuleBehavior(0.2f, 1);
            case GridModuleType.DotBoost:
                return new PassiveStatModuleBehavior(dotDamageMultiplier: 1.12f);
            case GridModuleType.DirectDamageBoost:
                return new PassiveStatModuleBehavior(directDamageMultiplier: 1.10f);
            case GridModuleType.EmergencyEvade:
                return new EmergencyEvadeModuleBehavior(0.30f);
            case GridModuleType.MaxHealthBoost:
                return new PassiveStatModuleBehavior(maxHealthMultiplier: 1.05f);
            case GridModuleType.DefenseBoost:
                return new PassiveStatModuleBehavior(defenseMultiplier: 1.03f);
            case GridModuleType.CritDamageBoost:
                return new PassiveStatModuleBehavior(critDamageBonus: 0.15f);
            case GridModuleType.CritRateBoost:
                return new PassiveStatModuleBehavior(critRateBonus: 0.08f);
            case GridModuleType.HeavyPoison:
                return new HeavyPoisonModuleBehavior(0.04f, 0.02f);
            case GridModuleType.HeavyTurret:
                return new HeavyTurretModuleBehavior(1.30f, 1.25f, 0.50f);
            case GridModuleType.GamblerStride:
                return new PassiveStatModuleBehavior(speedMultiplier: 1.25f, healingReceivedMultiplier: 0.50f, shieldGainMultiplier: 0.50f);
            case GridModuleType.SupportSwapAdvance:
                return new SupportSwapAdvanceModuleBehavior(0.30f);
            case GridModuleType.ChaosImmunity:
                return new ChaosImmunityModuleBehavior(0.20f);
            case GridModuleType.SwapChargeBurst:
                return new SwapChargeBurstModuleBehavior(100f, 3, 0.15f);
            case GridModuleType.EmergencySwapIn:
                return new EmergencySwapInModuleBehavior(0.10f, 0.50f);
            default:
                return null;
        }
    }
}

public static class TemporaryBattleModifierRuntimeManager
{
    private const int UnlimitedModuleBattleCount = 100;

    private static readonly List<TemporaryBattleModifierData> s_battleModifierSnapshot = new List<TemporaryBattleModifierData>();
    private static readonly Dictionary<int, int> s_commandPointsSpentByModule = new Dictionary<int, int>();
    private static readonly HashSet<int> s_emergencyEvadeUsedModules = new HashSet<int>();
    private static readonly HashSet<int> s_chaosImmunityUsedModules = new HashSet<int>();
    private static readonly Dictionary<int, Dictionary<Character, float>> s_reserveAdvanceProgressByModule = new Dictionary<int, Dictionary<Character, float>>();
    private static readonly Dictionary<int, Dictionary<Character, int>> s_swapChargeStacksByModule = new Dictionary<int, Dictionary<Character, int>>();
    private static readonly Dictionary<Character, float> s_pendingNextDamageBonusByCharacter = new Dictionary<Character, float>();

    private static bool s_hasBattleModifierSnapshot;

    public static void AddTemporaryBattleModifier(TemporaryBattleModifierData modifier)
    {
        Datas datas = Datas.Instance;
        if (datas == null)
        {
            return;
        }

        datas.AddActiveBattleModifier(modifier);
    }

    public static void SyncModuleModifier(GridModuleDefinition module)
    {
        Datas datas = Datas.Instance;
        if (datas == null || module == null)
        {
            return;
        }

        if (!TryGetModuleIndex(module, out int moduleIndex))
        {
            return;
        }

        RemoveActiveModuleModifier(datas, moduleIndex);

        if (module.modifierData == null)
        {
            return;
        }

        TemporaryBattleModifierData modifier = module.modifierData.Clone();
        modifier.moduleType = module.moduleType;
        modifier.sourceModuleIndex = moduleIndex;
        modifier.remainingBattles = UnlimitedModuleBattleCount;
        datas.AddActiveBattleModifier(modifier);
    }

    public static void RemoveModuleModifier(GridModuleDefinition module)
    {
        Datas datas = Datas.Instance;
        if (datas == null || module == null)
        {
            return;
        }

        if (!TryGetModuleIndex(module, out int moduleIndex))
        {
            return;
        }

        RemoveActiveModuleModifier(datas, moduleIndex);
    }

    public static void BeginBattleModifierSession()
    {
        Datas datas = Datas.Instance;
        s_battleModifierSnapshot.Clear();
        s_commandPointsSpentByModule.Clear();
        s_emergencyEvadeUsedModules.Clear();
        s_chaosImmunityUsedModules.Clear();
        s_reserveAdvanceProgressByModule.Clear();
        s_swapChargeStacksByModule.Clear();
        s_pendingNextDamageBonusByCharacter.Clear();

        if (datas != null)
        {
            IReadOnlyList<TemporaryBattleModifierData> modifiers = datas.GetActiveBattleModifiers();
            for (int i = 0; i < modifiers.Count; i++)
            {
                TemporaryBattleModifierData modifier = modifiers[i];
                if (modifier == null || modifier.remainingBattles <= 0)
                {
                    continue;
                }

                s_battleModifierSnapshot.Add(modifier.Clone());
            }
        }

        s_hasBattleModifierSnapshot = s_battleModifierSnapshot.Count > 0;
    }

    public static void CompleteBattleModifierSession(bool consumeBattleCount = true)
    {
        Datas datas = Datas.Instance;

        if (datas != null && consumeBattleCount)
        {
            IReadOnlyList<TemporaryBattleModifierData> modifiers = datas.GetActiveBattleModifiers();
            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                TemporaryBattleModifierData modifier = modifiers[i];
                if (modifier == null)
                {
                    datas.RemoveActiveBattleModifierAt(i);
                    continue;
                }

                if (IsUnlimitedModuleModifier(modifier))
                {
                    continue;
                }

                modifier.remainingBattles = Mathf.Max(0, modifier.remainingBattles - 1);
                if (modifier.remainingBattles <= 0)
                {
                    datas.RemoveActiveBattleModifierAt(i);
                }
            }
        }

        s_battleModifierSnapshot.Clear();
        s_commandPointsSpentByModule.Clear();
        s_emergencyEvadeUsedModules.Clear();
        s_chaosImmunityUsedModules.Clear();
        s_reserveAdvanceProgressByModule.Clear();
        s_swapChargeStacksByModule.Clear();
        s_pendingNextDamageBonusByCharacter.Clear();
        s_hasBattleModifierSnapshot = false;
    }

    public static void NotifyBattleStarted()
    {
        DispatchRuntimeEvent(new TemporaryBattleModifierRuntimeContext
        {
            EventType = TemporaryBattleModifierRuntimeEventType.BattleStarted
        });
    }

    public static void NotifyPlayerCharacterSwapped(Character oldCharacter, Character newCharacter)
    {
        DispatchRuntimeEvent(new TemporaryBattleModifierRuntimeContext
        {
            EventType = TemporaryBattleModifierRuntimeEventType.PlayerCharacterSwapped,
            PreviousCharacter = oldCharacter,
            CurrentCharacter = newCharacter,
            Source = oldCharacter,
            Target = newCharacter,
        });
    }

    public static void NotifyCommandPointsSpent(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        DispatchRuntimeEvent(new TemporaryBattleModifierRuntimeContext
        {
            EventType = TemporaryBattleModifierRuntimeEventType.CommandPointsSpent,
            Amount = amount,
        });
    }

    public static void NotifyUnitHealed(UnitCombatant target, int amount)
    {
        if (target == null || amount <= 0)
        {
            return;
        }

        DispatchRuntimeEvent(new TemporaryBattleModifierRuntimeContext
        {
            EventType = TemporaryBattleModifierRuntimeEventType.UnitHealed,
            Target = target,
            Amount = amount,
        });
    }

    public static void NotifyShieldAdded(UnitCombatant target, int amount)
    {
        if (target == null || amount <= 0)
        {
            return;
        }

        DispatchRuntimeEvent(new TemporaryBattleModifierRuntimeContext
        {
            EventType = TemporaryBattleModifierRuntimeEventType.ShieldAdded,
            Target = target,
            Amount = amount,
        });
    }

    public static void NotifyShieldBroken(UnitCombatant target, UnitCombatant source)
    {
        if (target == null)
        {
            return;
        }

        DispatchRuntimeEvent(new TemporaryBattleModifierRuntimeContext
        {
            EventType = TemporaryBattleModifierRuntimeEventType.ShieldBroken,
            Target = target,
            Source = source,
        });
    }

    public static void NotifyDamageSettled(UnitCombatant source, UnitCombatant target, int damage, bool isDotDamage, bool isTrueDamage, DamageType damageType)
    {
        if (target == null)
        {
            return;
        }

        DispatchRuntimeEvent(new TemporaryBattleModifierRuntimeContext
        {
            EventType = TemporaryBattleModifierRuntimeEventType.DamageSettled,
            Source = source,
            Target = target,
            Amount = damage,
            IsDotDamage = isDotDamage,
            IsTrueDamage = isTrueDamage,
            DamageType = damageType,
        });
    }

    public static void NotifyCriticalHit(UnitCombatant source, UnitCombatant target)
    {
        if (source == null)
        {
            return;
        }

        DispatchRuntimeEvent(new TemporaryBattleModifierRuntimeContext
        {
            EventType = TemporaryBattleModifierRuntimeEventType.CriticalHit,
            Source = source,
            Target = target,
            IsCriticalHit = true,
        });
    }

    public static void NotifyChaosReduced(Character character, int amount)
    {
        if (character == null || amount <= 0)
        {
            return;
        }

        DispatchRuntimeEvent(new TemporaryBattleModifierRuntimeContext
        {
            EventType = TemporaryBattleModifierRuntimeEventType.ChaosReduced,
            Target = character,
            Amount = amount,
        });
    }

    public static bool TryHandleChaosMaxReached(Character character)
    {
        if (character == null)
        {
            return false;
        }

        TemporaryBattleModifierRuntimeContext context = new TemporaryBattleModifierRuntimeContext
        {
            EventType = TemporaryBattleModifierRuntimeEventType.ChaosMaxReached,
            Target = character,
            Amount = character.ChaosValue,
        };
        DispatchRuntimeEvent(context);
        return context.Handled;
    }

    public static void NotifyReserveActionValueAdvanced(Character reserveCharacter, float actionValue)
    {
        if (reserveCharacter == null || actionValue <= 0f)
        {
            return;
        }

        DispatchRuntimeEvent(new TemporaryBattleModifierRuntimeContext
        {
            EventType = TemporaryBattleModifierRuntimeEventType.ReserveActionValueAdvanced,
            Target = reserveCharacter,
            FloatValue = actionValue,
        });
    }

    public static float GetPlayerSpeedMultiplier(Character character = null)
    {
        IReadOnlyList<TemporaryBattleModifierData> modifiers = GetEffectiveBattleModifiers();
        float multiplier = 1f;
        for (int i = 0; i < modifiers.Count; i++)
        {
            TemporaryBattleModifierData modifier = modifiers[i];
            if (modifier == null)
            {
                continue;
            }

            multiplier *= Mathf.Max(0.01f, modifier.playerSpeedMultiplier);
            if (TemporaryBattleModifierBehaviorRegistry.TryGetRuntimeBehavior(modifier, out ITemporaryBattleModifierBehavior behavior))
            {
                multiplier *= Mathf.Max(0.01f, behavior.GetPlayerSpeedMultiplier(modifier, character));
            }
        }

        return multiplier;
    }

    public static float GetPlayerDamageMultiplier(UnitCombatant attacker = null, UnitCombatant target = null, DamageType damageType = DamageType.Physical, bool isCriticalHit = false)
    {
        IReadOnlyList<TemporaryBattleModifierData> modifiers = GetEffectiveBattleModifiers();
        float multiplier = 1f;
        for (int i = 0; i < modifiers.Count; i++)
        {
            TemporaryBattleModifierData modifier = modifiers[i];
            if (modifier == null)
            {
                continue;
            }

            float baseMultiplier = damageType == DamageType.Magical
                ? modifier.playerDotDamageMultiplier
                : modifier.playerDirectDamageMultiplier;
            multiplier *= Mathf.Max(0.01f, baseMultiplier);
            if (TemporaryBattleModifierBehaviorRegistry.TryGetRuntimeBehavior(modifier, out ITemporaryBattleModifierBehavior behavior))
            {
                multiplier *= Mathf.Max(0.01f, behavior.GetPlayerDamageMultiplier(modifier, attacker, target, damageType, isCriticalHit));
            }
        }

        return multiplier;
    }

    public static float GetPlayerCritDamageBonus(UnitCombatant attacker = null)
    {
        IReadOnlyList<TemporaryBattleModifierData> modifiers = GetEffectiveBattleModifiers();
        float bonus = 0f;
        for (int i = 0; i < modifiers.Count; i++)
        {
            TemporaryBattleModifierData modifier = modifiers[i];
            if (modifier == null)
            {
                continue;
            }

            bonus += modifier.playerCritDamageBonus;
            if (TemporaryBattleModifierBehaviorRegistry.TryGetRuntimeBehavior(modifier, out ITemporaryBattleModifierBehavior behavior))
            {
                bonus += behavior.GetPlayerCritDamageBonus(modifier, attacker);
            }
        }

        return bonus;
    }

    public static float GetPlayerCritRateBonus(UnitCombatant attacker = null)
    {
        return SumBehaviorBonus(attacker, static (behavior, modifier, unit) => behavior.GetPlayerCritRateBonus(modifier, unit));
    }

    public static float GetPlayerHealingReceivedMultiplier(UnitCombatant target = null)
    {
        float multiplier = 1f;
        IReadOnlyList<TemporaryBattleModifierData> modifiers = GetEffectiveBattleModifiers();
        for (int i = 0; i < modifiers.Count; i++)
        {
            TemporaryBattleModifierData modifier = modifiers[i];
            if (modifier == null)
            {
                continue;
            }

            if (TemporaryBattleModifierBehaviorRegistry.TryGetRuntimeBehavior(modifier, out ITemporaryBattleModifierBehavior behavior))
            {
                multiplier *= Mathf.Max(0f, behavior.GetPlayerHealingReceivedMultiplier(modifier, target));
            }
        }

        return multiplier;
    }

    public static float GetPlayerShieldGainMultiplier(UnitCombatant target = null)
    {
        float multiplier = 1f;
        IReadOnlyList<TemporaryBattleModifierData> modifiers = GetEffectiveBattleModifiers();
        for (int i = 0; i < modifiers.Count; i++)
        {
            TemporaryBattleModifierData modifier = modifiers[i];
            if (modifier == null)
            {
                continue;
            }

            if (TemporaryBattleModifierBehaviorRegistry.TryGetRuntimeBehavior(modifier, out ITemporaryBattleModifierBehavior behavior))
            {
                multiplier *= Mathf.Max(0f, behavior.GetPlayerShieldGainMultiplier(modifier, target));
            }
        }

        return multiplier;
    }

    public static float GetPlayerMaxHealthMultiplier(Character character = null)
    {
        float multiplier = 1f;
        IReadOnlyList<TemporaryBattleModifierData> modifiers = GetEffectiveBattleModifiers();
        for (int i = 0; i < modifiers.Count; i++)
        {
            TemporaryBattleModifierData modifier = modifiers[i];
            if (modifier == null)
            {
                continue;
            }

            if (TemporaryBattleModifierBehaviorRegistry.TryGetRuntimeBehavior(modifier, out ITemporaryBattleModifierBehavior behavior))
            {
                multiplier *= Mathf.Max(0.01f, behavior.GetPlayerMaxHealthMultiplier(modifier, character));
            }
        }

        return multiplier;
    }

    public static float GetPlayerDefenseMultiplier(Character character = null)
    {
        float multiplier = 1f;
        IReadOnlyList<TemporaryBattleModifierData> modifiers = GetEffectiveBattleModifiers();
        for (int i = 0; i < modifiers.Count; i++)
        {
            TemporaryBattleModifierData modifier = modifiers[i];
            if (modifier == null)
            {
                continue;
            }

            if (TemporaryBattleModifierBehaviorRegistry.TryGetRuntimeBehavior(modifier, out ITemporaryBattleModifierBehavior behavior))
            {
                multiplier *= Mathf.Max(0.01f, behavior.GetPlayerDefenseMultiplier(modifier, character));
            }
        }

        return multiplier;
    }

    public static float GetCharacterTurnEndActionValue(float baseActionValue, Character character)
    {
        float multiplier = 1f;
        IReadOnlyList<TemporaryBattleModifierData> modifiers = GetEffectiveBattleModifiers();
        for (int i = 0; i < modifiers.Count; i++)
        {
            TemporaryBattleModifierData modifier = modifiers[i];
            if (modifier == null)
            {
                continue;
            }

            if (TemporaryBattleModifierBehaviorRegistry.TryGetRuntimeBehavior(modifier, out ITemporaryBattleModifierBehavior behavior))
            {
                multiplier *= Mathf.Max(0.01f, behavior.GetCharacterTurnEndActionValueMultiplier(modifier, character));
            }
        }

        return Mathf.Max(0f, baseActionValue * multiplier);
    }

    public static float GetEnemyTurnEndActionValue(float baseActionValue, Enemy enemy)
    {
        float multiplier = 1f;
        IReadOnlyList<TemporaryBattleModifierData> modifiers = GetEffectiveBattleModifiers();
        for (int i = 0; i < modifiers.Count; i++)
        {
            TemporaryBattleModifierData modifier = modifiers[i];
            if (modifier == null)
            {
                continue;
            }

            if (TemporaryBattleModifierBehaviorRegistry.TryGetRuntimeBehavior(modifier, out ITemporaryBattleModifierBehavior behavior))
            {
                multiplier *= Mathf.Max(0.01f, behavior.GetEnemyTurnEndActionValueMultiplier(modifier, enemy));
            }
        }

        return Mathf.Max(0f, baseActionValue * multiplier);
    }

    public static void AddPendingNextDamageBonus(Character character, float bonus)
    {
        if (character == null || bonus <= 0f)
        {
            return;
        }

        if (s_pendingNextDamageBonusByCharacter.TryGetValue(character, out float existingBonus))
        {
            s_pendingNextDamageBonusByCharacter[character] = existingBonus + bonus;
            return;
        }

        s_pendingNextDamageBonusByCharacter[character] = bonus;
    }

    public static float ConsumePendingNextDamageMultiplier(UnitCombatant attacker)
    {
        if (!(attacker is Character character) || !s_pendingNextDamageBonusByCharacter.TryGetValue(character, out float bonus) || bonus <= 0f)
        {
            return 1f;
        }

        s_pendingNextDamageBonusByCharacter.Remove(character);
        return 1f + bonus;
    }

    public static void AddTrackedCommandSpend(int moduleIndex, int amount)
    {
        if (moduleIndex < 0 || amount <= 0)
        {
            return;
        }

        if (s_commandPointsSpentByModule.TryGetValue(moduleIndex, out int currentAmount))
        {
            s_commandPointsSpentByModule[moduleIndex] = currentAmount + amount;
            return;
        }

        s_commandPointsSpentByModule[moduleIndex] = amount;
    }

    public static int GetTrackedCommandSpend(int moduleIndex)
    {
        return s_commandPointsSpentByModule.TryGetValue(moduleIndex, out int currentAmount) ? currentAmount : 0;
    }

    public static void SetTrackedCommandSpend(int moduleIndex, int amount)
    {
        if (moduleIndex < 0)
        {
            return;
        }

        s_commandPointsSpentByModule[moduleIndex] = Mathf.Max(0, amount);
    }

    public static bool TryConsumeEmergencyEvade(int moduleIndex)
    {
        if (moduleIndex < 0 || s_emergencyEvadeUsedModules.Contains(moduleIndex))
        {
            return false;
        }

        s_emergencyEvadeUsedModules.Add(moduleIndex);
        return true;
    }

    public static bool TryConsumeChaosImmunity(int moduleIndex)
    {
        if (moduleIndex < 0 || s_chaosImmunityUsedModules.Contains(moduleIndex))
        {
            return false;
        }

        s_chaosImmunityUsedModules.Add(moduleIndex);
        return true;
    }

    public static void AddReserveChargeProgress(int moduleIndex, Character character, float advanceValue, float threshold, int maxStacks)
    {
        if (moduleIndex < 0 || character == null || advanceValue <= 0f)
        {
            return;
        }

        Dictionary<Character, float> progressByCharacter = GetOrCreateReserveAdvanceProgress(moduleIndex);
        Dictionary<Character, int> stackByCharacter = GetOrCreateSwapChargeStacks(moduleIndex);

        float currentProgress = progressByCharacter.TryGetValue(character, out float storedProgress) ? storedProgress : 0f;
        int currentStacks = stackByCharacter.TryGetValue(character, out int storedStacks) ? storedStacks : 0;
        currentProgress += advanceValue;

        while (currentProgress >= threshold && currentStacks < maxStacks)
        {
            currentProgress -= threshold;
            currentStacks++;
        }

        progressByCharacter[character] = Mathf.Max(0f, currentProgress);
        stackByCharacter[character] = Mathf.Clamp(currentStacks, 0, maxStacks);
    }

    public static int ConsumeSwapChargeStacks(int moduleIndex, Character character)
    {
        if (moduleIndex < 0 || character == null || !s_swapChargeStacksByModule.TryGetValue(moduleIndex, out Dictionary<Character, int> stackByCharacter))
        {
            return 0;
        }

        if (!stackByCharacter.TryGetValue(character, out int currentStacks) || currentStacks <= 0)
        {
            return 0;
        }

        stackByCharacter[character] = 0;
        if (s_reserveAdvanceProgressByModule.TryGetValue(moduleIndex, out Dictionary<Character, float> progressByCharacter))
        {
            progressByCharacter[character] = 0f;
        }

        return currentStacks;
    }

    public static bool IsSupportCharacter(Character character)
    {
        if (character == null)
        {
            return false;
        }

        return character.characterType == CharacterType.DotSupport || character.characterType == CharacterType.DirectSupport;
    }

    public static bool IsNextCombatantEnemy()
    {
        return TurnManager.Instance != null && TurnManager.Instance.GetCurrentCombatant() is Enemy;
    }

    private static void DispatchRuntimeEvent(TemporaryBattleModifierRuntimeContext context)
    {
        if (context == null)
        {
            return;
        }

        IReadOnlyList<TemporaryBattleModifierData> modifiers = GetEffectiveBattleModifiers();
        Datas datas = Datas.Instance;
        if (datas == null)
        {
            return;
        }

        for (int i = 0; i < modifiers.Count; i++)
        {
            TemporaryBattleModifierData modifier = modifiers[i];
            if (modifier == null || !TemporaryBattleModifierBehaviorRegistry.TryGetRuntimeBehavior(modifier, out ITemporaryBattleModifierBehavior behavior))
            {
                continue;
            }

            behavior.HandleRuntimeEvent(datas, modifier, context);
        }
    }

    private static float SumBehaviorBonus(UnitCombatant attacker, Func<ITemporaryBattleModifierBehavior, TemporaryBattleModifierData, UnitCombatant, float> selector)
    {
        float bonus = 0f;
        IReadOnlyList<TemporaryBattleModifierData> modifiers = GetEffectiveBattleModifiers();
        for (int i = 0; i < modifiers.Count; i++)
        {
            TemporaryBattleModifierData modifier = modifiers[i];
            if (modifier == null || !TemporaryBattleModifierBehaviorRegistry.TryGetRuntimeBehavior(modifier, out ITemporaryBattleModifierBehavior behavior))
            {
                continue;
            }

            bonus += selector(behavior, modifier, attacker);
        }

        return bonus;
    }

    private static IReadOnlyList<TemporaryBattleModifierData> GetEffectiveBattleModifiers()
    {
        if (s_hasBattleModifierSnapshot)
        {
            return s_battleModifierSnapshot;
        }

        Datas datas = Datas.Instance;
        return datas != null ? datas.GetActiveBattleModifiers() : Array.Empty<TemporaryBattleModifierData>();
    }

    private static void RemoveActiveModuleModifier(Datas datas, int moduleIndex)
    {
        IReadOnlyList<TemporaryBattleModifierData> modifiers = datas.GetActiveBattleModifiers();
        for (int i = modifiers.Count - 1; i >= 0; i--)
        {
            TemporaryBattleModifierData modifier = modifiers[i];
            if (modifier != null && modifier.sourceModuleIndex == moduleIndex)
            {
                datas.RemoveActiveBattleModifierAt(i);
            }
        }
    }

    private static bool IsUnlimitedModuleModifier(TemporaryBattleModifierData modifier)
    {
        return modifier != null && modifier.sourceModuleIndex >= 0 && modifier.remainingBattles >= UnlimitedModuleBattleCount;
    }

    private static bool TryGetModuleIndex(GridModuleDefinition module, out int moduleIndex)
    {
        ModulePlacementController controller = ModulePlacementController.Instance;
        if (controller != null && controller.TryGetOwnedModuleIndex(module, out moduleIndex))
        {
            return true;
        }

        moduleIndex = -1;
        return false;
    }

    private static Dictionary<Character, float> GetOrCreateReserveAdvanceProgress(int moduleIndex)
    {
        if (!s_reserveAdvanceProgressByModule.TryGetValue(moduleIndex, out Dictionary<Character, float> progressByCharacter))
        {
            progressByCharacter = new Dictionary<Character, float>();
            s_reserveAdvanceProgressByModule[moduleIndex] = progressByCharacter;
        }

        return progressByCharacter;
    }

    private static Dictionary<Character, int> GetOrCreateSwapChargeStacks(int moduleIndex)
    {
        if (!s_swapChargeStacksByModule.TryGetValue(moduleIndex, out Dictionary<Character, int> stackByCharacter))
        {
            stackByCharacter = new Dictionary<Character, int>();
            s_swapChargeStacksByModule[moduleIndex] = stackByCharacter;
        }

        return stackByCharacter;
    }
}