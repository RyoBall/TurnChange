using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class DamageCounter : MonoBehaviour
{
    public static int CountDamage(UnitCombatant attacker, UnitCombatant defender, SkillBase skill, float buffMultiplier = 1f)
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

        float criRand = Random.Range(0f, 1f);
        bool isCrit = criRand < attacker.critRate;
        float rand = Random.Range(0.85f, 1.15f);
        float raw = (attacker.attack * skillCoef + skillBase) * buffMultiplier * (isCrit ? attacker.critDamage : 1f) * (defender.K / (defender.K + defender.defense)) * rand;
        raw *= attacker.GetOutgoingDamageMultiplier(false);
        raw *= defender.GetIncomingDamageMultiplier(false, false);
        return Mathf.RoundToInt(raw);
    }
    public static int CountDotDamage(State state,UnitCombatant attacker,UnitCombatant defender)
    {
        if (state == null || attacker == null || defender == null)
        {
            Debug.LogWarning("[DamageCounter] 无效的参数");
            return 0;
        }

        float buffMultiplier=1f;
        float rand=Random.Range(0.85f, 1.15f);
        float damage = state.atkT * state.skillCoefT * (defender.K / (defender.K + defender.defense)) * rand * buffMultiplier;
        damage *= attacker.GetOutgoingDamageMultiplier(true);
        damage *= defender.GetIncomingDamageMultiplier(true, false);

        if (EnvironmentManager.Instance != null && EnvironmentManager.Instance.HasEnvironment(EnvironmentType.Gravity))
        {
            damage *= 2f;
        }

        return Mathf.RoundToInt(damage);
    }
}
