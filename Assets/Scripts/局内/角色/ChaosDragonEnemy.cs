using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 混沌龙 — 施加混沌值，行动条操控
/// 指挥点奖励：每次使用技能+1，暴怒技能+1
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

    public override IEnumerator PerformTurn()
    {
        yield return BeginTurnPreActions();
        if (!CanProceedWithTurn)
        {
            yield break;
        }

        // 执行行动
        EnemySkillBase selectedSkill = SelectSkillForTurn();
        if (selectedSkill == null)
        {
            FloatingTipGenerator.Instance?.ShowTipAtObject(transform, $"{combatantName}暂无可用技能");
            yield break;
        }

        yield return new WaitForSeconds(0.2f);
        FloatingTipGenerator.Instance?.ShowDefaultTip(selectedSkill.skillName);
        yield return new WaitForSeconds(0.5f);
        yield return new WaitForSeconds(0.5f);
        SkillExecuteManager.ExecuteSkill(this, selectedSkill);
        yield return new WaitUntil(() => !SkillExecuteManager.s_isExecutingSkill);

        // 指挥点奖励：混沌龙每次使用技能 +1
        bool isRageSkill = selectedSkill.enemySkillType == EnemySkillType.DragonChaosRage;
        if (isRageSkill)
        {
            NotifyRageSkillUsed();
        }
        else
        {
            NotifyChaosDragonSkillUsed();
        }

        yield return WaitForDeathEvents();
        InvokeOnEnemyActEvent();
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
