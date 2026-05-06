using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SkillType
{
    Attack,
    RemoveSelf,
    InsertCharacter,
    AllAttack
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

        switch (skillType)
        {
            case SkillType.Attack:
                yield return ExecuteAttack(character, selectedEnemies);
                break;
            case SkillType.InsertCharacter:
                yield return ExecuteInsertCharacter(character, selectedEnemies);
                break;
            case SkillType.RemoveSelf:
                yield return ExecuteRemoveSelf(character, selectedEnemies);
                break;
            case SkillType.AllAttack:
                yield return ExecuteAllAttack(character);
                break;
        }
        
        character.EndTurn();
    }

    private IEnumerator ExecuteAttack(Character character, List<Enemy> selectedEnemies)
    {
        LogSkillTargets("Attack", selectedEnemies);
        foreach (var enemy in selectedEnemies)
        {
            if (enemy != null)
            {
                enemy.TakeDamage((int)character.attack);
            }
        }
        yield break;
    }
    private IEnumerator ExecuteRemoveSelf(Character character, List<Enemy> selectedEnemies)
    {
        LogSkillTargets("RemoveSelf", selectedEnemies);
        TurnManager.Instance.RemoveCombatant(character);
        yield return new WaitForSeconds(.5f);
    }
    private IEnumerator ExecuteInsertCharacter(Character character, List<Enemy> selectedEnemies)
    {
        LogSkillTargets("InsertCharacter", selectedEnemies);

        SkillManager.Instance.changeCharacter.GetComponent<Combatant>().currentActionValue=0;
        TurnManager.Instance.InsertCombatant(SkillManager.Instance.changeCharacter.GetComponent<Combatant>(),false);

        Debug.Log($"[SkillBase] 技能 {skillName} 已将 {SkillManager.Instance.changeCharacter.name} 插入行动轮次");
        yield return new WaitForSeconds(.5f);
    }
    private IEnumerator ExecuteAllAttack(Character character)
    {
        LogSkillTargets("AllAttack", null);

        var enemies = EnemyManager.Instance.AliveEnemies;
        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.TakeDamage((int)character.attack);
            }
        }
        yield break;
    }
    private void LogSkillTargets(string typeName, List<Enemy> selectedEnemies)//打印选择的目标
    {
        if (selectedEnemies == null || selectedEnemies.Count == 0)
        {
            Debug.Log($"[SkillBase] 执行 {skillName} ({typeName})，无需目标或未选择目标");
            return;
        }

        var names = new List<string>();
        for (int i = 0; i < selectedEnemies.Count; i++)
        {
            if (selectedEnemies[i] != null)
            {
                names.Add(selectedEnemies[i].name);
            }
        }

        Debug.Log($"[SkillBase] 执行 {skillName} ({typeName})，目标: {string.Join(", ", names)}");
    }
}