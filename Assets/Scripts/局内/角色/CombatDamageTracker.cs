using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 局内战斗伤害统计：用于直伤龙即死等需按累计输出选目标的机制。
/// </summary>
public static class CombatDamageTracker
{
    private static readonly Dictionary<Character, int> s_damageDealtByCharacter = new Dictionary<Character, int>();

    public static void Reset()
    {
        s_damageDealtByCharacter.Clear();
    }

    public static void RecordDamageDealt(Character source, int damage)
    {
        if (source == null || source.IsDead || damage <= 0)
        {
            return;
        }

        if (!s_damageDealtByCharacter.TryGetValue(source, out int total))
        {
            total = 0;
        }

        s_damageDealtByCharacter[source] = total + damage;
    }

    public static int GetDamageDealt(Character source)
    {
        if (source == null)
        {
            return 0;
        }

        return s_damageDealtByCharacter.TryGetValue(source, out int total) ? total : 0;
    }

    /// <summary>累计伤害最高；并列时选速度步长（BaseActionValue）最小者。</summary>
    public static Character SelectHighestDamageDealer(IReadOnlyList<Character> candidates)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return null;
        }

        Character best = null;
        int bestDamage = int.MinValue;
        float bestSpeedStep = float.MaxValue;

        for (int i = 0; i < candidates.Count; i++)
        {
            Character candidate = candidates[i];
            if (candidate == null || candidate.IsDead)
            {
                continue;
            }

            int damage = GetDamageDealt(candidate);
            float speedStep = candidate.BaseActionValue;
            if (damage > bestDamage || (damage == bestDamage && speedStep < bestSpeedStep))
            {
                best = candidate;
                bestDamage = damage;
                bestSpeedStep = speedStep;
            }
        }

        return best;
    }
}
