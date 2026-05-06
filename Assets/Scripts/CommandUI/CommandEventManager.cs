using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CommandEventManager
{
    private static bool s_isExecutingSkill;

    public static void ExecuteSkill(Character character, SkillBase skill)
    {
        if (character == null)
        {
            Debug.LogWarning("[CommandEventManager] 当前没有可执行技能的角色");
            return;
        }

        if (skill == null)
        {
            Debug.LogWarning("[CommandEventManager] 当前按钮没有绑定技能");
            return;
        }

        if (s_isExecutingSkill)
        {
            Debug.LogWarning("[CommandEventManager] 正在执行技能，请等待当前技能结束");
            return;
        }

        Debug.Log($"[CommandEventManager] 执行技能: {skill.skillName}");
        character.StartCoroutine(ExecuteSkillCoroutine(character, skill));
    }

    private static IEnumerator ExecuteSkillCoroutine(Character character, SkillBase skill)
    {
        s_isExecutingSkill = true;
        yield return skill.Execute(character);
        s_isExecutingSkill = false;
    }
}
