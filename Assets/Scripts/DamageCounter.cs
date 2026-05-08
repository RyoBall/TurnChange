using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class DamageCounter : MonoBehaviour
{
    public static int CountDamage(UnitCombatant attacker, UnitCombatant defender, SkillBase skill,float buffMultiplier=1f)
    {
        float criRand=Random.Range(0f, 1f);
        bool isCrit = criRand < attacker.critRate;
        float rand=Random.Range(0.85f, 1.15f);
        return Mathf.RoundToInt((attacker.attack*skill.skillCoef+skill.skillBase)*buffMultiplier*(isCrit?attacker.critDamage:1f)*(defender.K/(defender.K+defender.defense))*rand);
    }
    public static int CountDotDamage(State state,UnitCombatant attacker,UnitCombatant defender)
    {
        float buffMultiplier=1f;
        foreach(var attackerState in attacker.States)
        {
            if(attackerState.isBuff)
            {
                buffMultiplier+=attackerState.GetBuffMultiplier();   
            }
        }
        float rand=Random.Range(0.85f, 1.15f);
        return Mathf.RoundToInt(state.atkT*state.skillCoefT*(defender.K/(defender.K+defender.defense))*rand*buffMultiplier);
    }
}
