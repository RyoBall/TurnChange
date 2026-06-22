using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 直伤龙 — 单体高伤，施加瞩目，暴怒即死
/// </summary>
public class DirectDragonEnemy : DragonBossEnemy
{
    private bool m_isChargingRage;

    public bool IsChargingRage => m_isChargingRage;

    public void SetChargingRage(bool charging)
    {
        m_isChargingRage = charging;
    }

    protected override EnemySkillBase GetForcedSkillForTurn()
    {
        // 蓄力中优先于基类暴怒选技，确保下一回合触发即死（可忽略 CD）
        if (m_isChargingRage)
        {
            EnemySkillBase rageSkill = GetSkillInstance(EnemySkillType.DragonDirectRage);
            if (rageSkill != null)
            {
                return rageSkill;
            }
        }

        return base.GetForcedSkillForTurn();
    }

    public override bool ShouldBypassSkillCooldown(EnemySkillBase skill)
    {
        return m_isChargingRage
            && skill != null
            && skill.enemySkillType == EnemySkillType.DragonDirectRage;
    }

    public override bool CanUseEnemySkill(EnemySkillBase skill)
    {
        if (!base.CanUseEnemySkill(skill)) return false;

        switch (skill.enemySkillType)
        {
            case EnemySkillType.DragonDirectSkill1:
                return GetAliveFieldCharacters().Count > 0;
            case EnemySkillType.DragonDirectSkill2:
                return GetAliveFieldCharacters().Count > 0;
            case EnemySkillType.DragonDirectRage:
                return IsRaging && (m_isChargingRage || GetAliveFieldCharacters().Count > 0);
            default:
                return true;
        }
    }

    private List<Character> GetAliveFieldCharacters()
    {
        List<Character> alive = new List<Character>();
        if (CharacterManager.Instance == null) return alive;
        for (int i = 0; i < CharacterManager.Instance.fieldCharacters.Count; i++)
        {
            Character c = CharacterManager.Instance.fieldCharacters[i];
            if (c != null && !c.IsDead) alive.Add(c);
        }
        return alive;
    }
}
