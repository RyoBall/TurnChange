using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SkillExecuteManager
{
    public static bool s_isExecutingSkill;//用于外部获取当前是否在执行技能

    public static void ExecuteSkill(Character character, SkillBase skill)
    {
        character.StartCoroutine(ExecuteSkillCoroutine(character, skill));
    }

    private static IEnumerator ExecuteSkillCoroutine(Character character, SkillBase skill)
    {
        yield return new WaitUntil(() => !s_isExecutingSkill);//确保在执行新技能前，之前的技能已经结束
        s_isExecutingSkill = true;
        yield return skill.Execute(character);
        s_isExecutingSkill = false;
    }
}
