using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class DamageCounter : MonoBehaviour
{
    public static UnitCombatant.DamageInfo CountDamage(UnitCombatant attacker, UnitCombatant defender, SkillBase skill, bool ifTrueDamage = false, float extraDamage = 0f)
    {
        if (attacker == null || defender == null)
        {
            Debug.LogWarning("[DamageCounter] 无效的参数");
            return new UnitCombatant.DamageInfo(0, attacker);
        }
        if (skill == null)
        {
            Debug.LogWarning("[DamageCounter] 技能参数为null，使用默认伤害计算");
            skill = new SkillBase { skillCoef = 1f, skillBase = 0 };
        }
        return CountDamage(attacker, defender, skill.skillCoef, skill.skillBase + extraDamage, ifTrueDamage);
    }

    public static UnitCombatant.DamageInfo CountDamage(
        UnitCombatant attacker,
        UnitCombatant defender,
        float attackCoefficient,
        float baseDamage = 0f,
        bool ifTrueDamage = false,
        bool canCrit = true,
        bool applyRandomVariance = true)
    {
        bool isCrit;
        return CountDamage(attacker, defender, attackCoefficient, baseDamage, ifTrueDamage, canCrit, applyRandomVariance, out isCrit);
    }

    public static UnitCombatant.DamageInfo CountDamage(
        UnitCombatant attacker,
        UnitCombatant defender,
        float attackCoefficient,
        float baseDamage,
        bool ifTrueDamage,
        bool canCrit,
        bool applyRandomVariance,
        out bool isCrit)
    {
        isCrit = false;
        if (attacker == null || defender == null)
        {
            Debug.LogWarning("[DamageCounter] 无效的参数");
            return new UnitCombatant.DamageInfo(0, attacker);
        }
    //计算系数
        EnvironmentManager environmentManager = EnvironmentManager.Instance;
        bool isTrueDamage = IsTrueDamage(attacker, false, ifTrueDamage);
        //根据环境作用修正暴击系数，并判断是否暴击
        float effectiveCritRate = attacker.critRate + (environmentManager != null ? environmentManager.GetCritRateBonus(attacker) : 0f);
        float effectiveCritDamage = attacker.critDamage + (environmentManager != null ? environmentManager.GetCritDamageBonus(attacker) : 0f);
        isCrit = canCrit && Random.value < Mathf.Clamp01(effectiveCritRate);
        //获取防御系数与随机系数
        float randomFactor = applyRandomVariance ? Random.Range(0.85f, 1.15f) : 1f;
        float defenseFactor = isTrueDamage ? 1f : (defender.K / (defender.K + defender.defense));
    
    //计算伤害
        //先计算基础伤害
        float raw = (attacker.attack * attackCoefficient + baseDamage) * randomFactor;
        //计算暴击影响
        raw *= isCrit ? effectiveCritDamage : 1f;
        //计算防御影响
        raw *= defenseFactor;
        //计算状态附加的增伤影响
        raw *= attacker.GetOutgoingDamageMultiplier(false);
        raw *= defender.GetIncomingDamageMultiplier(false, isTrueDamage);
        //计算环境增伤影响
        raw *= environmentManager != null ? environmentManager.GetIncomingDamageMultiplier(attacker, defender, false, isTrueDamage) : 1f;

        var damageInfo = new UnitCombatant.DamageInfo(Mathf.Max(0, Mathf.RoundToInt(raw)), attacker);
        if (isTrueDamage)
        {
            damageInfo = damageInfo.AsTrueDamage();
        }

        return damageInfo;
    }

    public static UnitCombatant.DamageInfo CountDotDamage(State state, UnitCombatant attacker, UnitCombatant defender, bool ifTrueDamage = false)
    {
        if (state == null || attacker == null || defender == null)
        {
            Debug.LogWarning("[DamageCounter] 无效的参数");
            return new UnitCombatant.DamageInfo(0, attacker).AsDot();
        }

        float rand = Random.Range(0.85f, 1.15f);
        bool isTrueDamage = IsTrueDamage(attacker, true, ifTrueDamage);
        float defenseFactor = isTrueDamage ? 1f : (defender.K / (defender.K + defender.defense));
        float damage = state.atkT * state.skillCoefT * defenseFactor * rand;
        damage *= attacker.GetOutgoingDamageMultiplier(true);
        damage *= defender.GetIncomingDamageMultiplier(true, isTrueDamage);
        damage *= EnvironmentManager.Instance != null ? EnvironmentManager.Instance.GetIncomingDamageMultiplier(attacker, defender, true, isTrueDamage) : 1f;

        var damageInfo = new UnitCombatant.DamageInfo(Mathf.RoundToInt(damage), attacker)
            .AsDot()
            .WithState(state.stateType);
        if (isTrueDamage)
        {
            damageInfo = damageInfo.AsTrueDamage();
        }

        return damageInfo;
    }

    public static bool IsTrueDamage(UnitCombatant attacker, bool isDotDamage, bool forceTrueDamage = false)
    {
        return attacker != null && attacker.DealsTrueDamage(isDotDamage, forceTrueDamage);
    }
}
