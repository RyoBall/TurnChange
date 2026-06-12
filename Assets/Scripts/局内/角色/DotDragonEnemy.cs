using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dot龙 — 施加龙息Dot，净化负面状态
/// </summary>
public class DotDragonEnemy : DragonBossEnemy
{
    public override bool CanUseEnemySkill(EnemySkillBase skill)
    {
        if (!base.CanUseEnemySkill(skill)) return false;

        switch (skill.enemySkillType)
        {
            case EnemySkillType.DragonDotSkill1:
                return GetAliveFieldCharacters().Count > 0;
            case EnemySkillType.DragonDotSkill2:
                return HasAnyDebuffOnAllies();
            case EnemySkillType.DragonDotRage:
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

    private bool HasAnyDebuffOnAllies()
    {
        if (EnemyManager.Instance == null) return false;
        IReadOnlyList<Enemy> aliveEnemies = EnemyManager.Instance.AliveEnemies;
        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            Enemy enemy = aliveEnemies[i];
            if (enemy == null || enemy.IsDead) continue;
            for (int j = 0; j < enemy.States.Count; j++)
            {
                if (enemy.States[j] != null && enemy.States[j].isDebuff) return true;
            }
        }
        return false;
    }
}
