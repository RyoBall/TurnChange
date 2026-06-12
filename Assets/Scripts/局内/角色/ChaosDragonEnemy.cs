using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 混沌龙 — 施加混沌值，行动条操控
/// </summary>
public class ChaosDragonEnemy : DragonBossEnemy
{
    public override bool CanUseEnemySkill(EnemySkillBase skill)
    {
        if (!base.CanUseEnemySkill(skill)) return false;

        switch (skill.enemySkillType)
        {
            case EnemySkillType.DragonChaosSkill1:
                return GetAliveFieldCharacters().Count > 0;
            case EnemySkillType.DragonChaosSkill2:
                return GetAliveFieldCharacters().Count > 0;
            case EnemySkillType.DragonChaosRage:
                return IsRaging && GetAliveFieldCharacters().Count > 0;
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
