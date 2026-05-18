using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class DamageCounter : MonoBehaviour
{
    public static int CountDamage(UnitCombatant attacker, UnitCombatant defender, SkillBase skill,  bool forceTrueDamage = false)
    {
        if (attacker == null || defender == null)
        {
            Debug.LogWarning("[DamageCounter] 无效的参数");
            return 0;
        }
        if (skill == null)
        {
            Debug.LogWarning("[DamageCounter] 技能参数为null，使用默认伤害计算");
            skill = new SkillBase { skillCoef = 1f, skillBase = 0 };
        }
        float skillCoef = skill.skillCoef;
        int skillBase = skill.skillBase;
        bool isTrueDamage = IsTrueDamage(attacker, false, forceTrueDamage);

        float criRand = Random.Range(0f, 1f);
        bool isCrit = criRand < attacker.critRate;
        float rand = Random.Range(0.85f, 1.15f);
        float defenseFactor = isTrueDamage ? 1f : (defender.K / (defender.K + defender.defense));
        float raw = (attacker.attack * skillCoef + skillBase)  * (isCrit ? attacker.critDamage : 1f) * defenseFactor * rand;
        raw *= attacker.GetOutgoingDamageMultiplier(false);
        raw *= defender.GetIncomingDamageMultiplier(false, isTrueDamage);
        return Mathf.RoundToInt(raw);
    }

    public static int CountDotDamage(State state,UnitCombatant attacker,UnitCombatant defender, bool forceTrueDamage = false)
    {
        if (state == null || attacker == null || defender == null)
        {
            Debug.LogWarning("[DamageCounter] 无效的参数");
            return 0;
        }

        float rand=Random.Range(0.85f, 1.15f);
        bool isTrueDamage = IsTrueDamage(attacker, true, forceTrueDamage);
        float defenseFactor = isTrueDamage ? 1f : (defender.K / (defender.K + defender.defense));
        float damage = state.atkT * state.skillCoefT * defenseFactor * rand;
        damage *= attacker.GetOutgoingDamageMultiplier(true);
        damage *= defender.GetIncomingDamageMultiplier(true, isTrueDamage);

        if (EnvironmentManager.Instance != null && EnvironmentManager.Instance.HasEnvironment(EnvironmentType.Gravity))
        {
            damage *= 2f;
        }

        return Mathf.RoundToInt(damage);
    }

    public static bool IsTrueDamage(UnitCombatant attacker, bool isDotDamage, bool forceTrueDamage = false)
    {
        return attacker != null && attacker.DealsTrueDamage(isDotDamage, forceTrueDamage);
    }
}
