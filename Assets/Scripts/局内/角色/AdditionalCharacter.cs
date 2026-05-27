using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdditionalCharacter : Combatant
{
    public Character character;
    private SkillBase m_skillOverride;
    private List<Enemy> m_selectedEnemies;

    public void Initialize(Character character)
    {
        this.character = character;
        m_skillOverride = null;
        m_selectedEnemies = null;
    }

    public void Initialize(Character character, SkillBase skillOverride, List<Enemy> selectedEnemies)
    {
        this.character = character;
        m_skillOverride = skillOverride;
        m_selectedEnemies = selectedEnemies != null ? new List<Enemy>(selectedEnemies) : null;
    }

    public override IEnumerator PerformTurn()
    {
        SkillBase skillToExecute = m_skillOverride != null ? m_skillOverride : character != null ? character.additionalSkill : null;
        if (character != null && skillToExecute != null)
        {
            if (m_selectedEnemies != null)
            {
                SkillExecuteManager.ExecuteSkill(character, skillToExecute, m_selectedEnemies, true);
            }
            else
            {
                SkillExecuteManager.ExecuteSkill(character, skillToExecute, true);
            }

            yield return new WaitUntil(() => !SkillExecuteManager.s_isExecutingSkill);
        }

        TurnManager.Instance?.RemoveCombatant(this);
        Destroy(gameObject);
    }
}
