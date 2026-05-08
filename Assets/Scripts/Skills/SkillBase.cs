using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SkillType
{
    Change,
    AllAttack,
    StrengthenSelf,
    EnterSkillOne,
    ExitSkillOne
}

[CreateAssetMenu(fileName = "NewSkill", menuName = "TurnChange/Skill"), System.Serializable]
public class SkillBase : ScriptableObject
{
    public string skillName;

    [TextArea(2, 5)]
    public string description;

    public Sprite icon;
    public SkillType skillType;

    [Header("目标选择设置")]
    public bool requiresEnemyTarget = false;
    [Min(1)]
    public int enemyTargetCount = 1;
    [Header("是否结束回合")]
    public bool endTurnAfterUse = true;
    [Header("伤害技能相关参数")]
    public int skillBase;
    public float skillCoef = 1f;

    public IEnumerator Execute(Character character)
    {
        if (character == null)
        {
            Debug.LogWarning($"[SkillBase] 角色为空，无法执行技能 {skillName}");
            yield break;
        }

        List<Enemy> selectedEnemies = null;
        if (requiresEnemyTarget)
        {
            if (SkillManager.Instance == null)
            {
                Debug.LogWarning("[SkillBase] 缺少 SkillManager，无法进入选敌流程");
                character.EndTurn();
                yield break;
            }

            selectedEnemies = new List<Enemy>();
            yield return SkillManager.Instance.SelectEnemiesCoroutine(enemyTargetCount, selectedEnemies);
        }

        FloatingTipGenerator.Instance.ShowDefaultTip($"{skillName}");

        switch (skillType)
        {
            case SkillType.Change:
                yield return ExcuteChange(character, selectedEnemies);
                break;
            case SkillType.AllAttack:
                yield return ExecuteAllAttack(character);
                break;
            case SkillType.EnterSkillOne:
                yield return EnterSkillOne(null, character);
                break;
            case SkillType.ExitSkillOne:
                yield return ExitSkillOne(character, null);
                break;
        }

        if (endTurnAfterUse)
        {
            character.EndTurn();
        }
    }
    #region 技能具体执行逻辑
    private IEnumerator StrengthenSelf(Character character, List<Enemy> selectedEnemies)
    {
        State state = character.AddState(StateType.StrengthenSelf, character, skillCoef, 2);
        yield break;
    }
    private IEnumerator EnterSkillOne(Character oldCharacter, Character newCharacter)
    {
        yield return new WaitForSeconds(.5f);
        FloatingTipGenerator.Instance.ShowDefaultTip($"{newCharacter.name}的入场技能触发，获得重力环境");
        EnvironmentManager.Instance.AddEnvironment(EnvironmentType.Gravity, 200);
        yield break;
    }
    private IEnumerator ExitSkillOne(Character oldCharacter, Character newCharacter)
    {
        yield return new WaitForSeconds(.5f);
        FloatingTipGenerator.Instance.ShowDefaultTip($"{oldCharacter.name}的离场技能触发，结算dot伤害");
        foreach (var enemy in EnemyManager.Instance.AliveEnemies)
        {
            if (enemy != null)
            {
                foreach (var state in enemy.States)
                {
                    if (state.isDot)
                    {
                        state.DotTrigger(1.2f);
                    }
                }
            }
        }
        yield break;
    }
    private IEnumerator ExcuteChange(Character character, List<Enemy> selectedEnemies)
    {
        SkillManager.Instance.changeCharacter.GetComponent<Combatant>().currentActionValue = 0;
        TurnManager.Instance.InsertCombatant(SkillManager.Instance.changeCharacter.GetComponent<Combatant>(), false);
        yield return new WaitForSeconds(.5f);
    }
    private IEnumerator ExecuteAllAttack(Character character)
    {
        var enemies = new List<Enemy>(EnemyManager.Instance.AliveEnemies);
        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.TakeDamage((int)character.attack);
                State state=enemy.AddState(StateType.Burn, character, skillCoef,1);
                state.ChangeRemainingTurns(2);
            }
        }
        yield break;
    }
    #endregion
    private int damageCount(UnitCombatant attacker)
    {
        return Mathf.RoundToInt(skillBase + skillCoef * skillBase);
    }
}