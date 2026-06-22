using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdditionalCharacter : UnitCombatant
{
    public Character character;

    public override Sprite TurnImageSprite => character != null ? character.TurnImageSprite : null;

    private SkillBase m_skillOverride;
    private List<Enemy> m_selectedEnemies;

    protected override void Awake()
    {
        // 不调用 base.Awake()，避免注册到 CombatantDeathMonitor。
        // AdditionalCharacter 是临时回合插入节点，不需要参与死亡监控，
        // 且其默认 HP=0 会被死亡监控器误判为已死亡而提前移除。
    }

    protected override void OnDestroy()
    {
        // 不调用 base.OnDestroy()，因为 Awake 中跳过了 Register，
        // 无需 Unregister。
    }

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
                yield return SkillExecuteManager.ExecuteSkillAsCoroutine(character, skillToExecute, m_selectedEnemies);
            }
            else
            {
                yield return SkillExecuteManager.ExecuteSkillAsCoroutine(character, skillToExecute);
            }
        }
        else
        {
            Debug.LogWarning($"[AdditionalCharacter] {character.combatantName} 追加回合缺少可用技能，additionalSkillType={character.additionalSkillType}");
        }

        TurnManager.Instance?.RemoveCombatant(this);
        yield return null;
        Destroy(gameObject);
    }
}
