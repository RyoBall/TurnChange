using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SkillExecuteManager
{
    public static bool s_isExecutingSkill;//用于外部获取当前是否在执行技能
    public static event System.Action<UnitCombatant, SkillBase> OnSkillExecuted;


    public static void ExecuteSkill(UnitCombatant unit, SkillBase skill,bool ifCouldInsert=false)
    {
        CoroutineHelper.GetHelper().StartCoroutine(ExecuteSkillCoroutine(unit, skill,ifCouldInsert));
    }

    public static void ExecuteSkill(UnitCombatant unit, SkillBase skill, List<Enemy> selectedEnemies, bool ifCouldInsert = false)
    {
        CoroutineHelper.GetHelper().StartCoroutine(ExecuteSkillCoroutine(unit, skill, selectedEnemies, ifCouldInsert));
    }

    private static IEnumerator ExecuteSkillCoroutine(UnitCombatant unit, SkillBase skill,bool ifCouldInsert=false)
    {
        if (s_isExecutingSkill&&!ifCouldInsert)
        {
            FloatingTipGenerator.Instance.ShowDefaultTip("正在执行技能，请稍后...");
            yield break;
        }
        s_isExecutingSkill = true;
        yield return skill.Execute(unit);
        yield return UnitCombatant.WaitForPendingDeaths();
        s_isExecutingSkill = false;
        OnSkillExecuted?.Invoke(unit, skill);
    }

    private static IEnumerator ExecuteSkillCoroutine(UnitCombatant unit, SkillBase skill, List<Enemy> selectedEnemies, bool ifCouldInsert = false)
    {
        if (s_isExecutingSkill && !ifCouldInsert)
        {
            FloatingTipGenerator.Instance.ShowDefaultTip("正在执行技能，请稍后...");
            yield break;
        }

        s_isExecutingSkill = true;
        yield return skill.Execute(unit, selectedEnemies);
        yield return UnitCombatant.WaitForPendingDeaths();
        s_isExecutingSkill = false;
        OnSkillExecuted?.Invoke(unit, skill);
    }
}
public class CoroutineHelper : MonoBehaviour
{
    //这个类的唯一作用是提供一个MonoBehaviour实例来运行协程
    private static CoroutineHelper Instance;
    public static CoroutineHelper GetHelper()
    {
        if (Instance == null)
        {
            GameObject obj = new GameObject("CoroutineHelper");
            Instance = obj.AddComponent<CoroutineHelper>();
            DontDestroyOnLoad(obj);
        }
        return Instance;
    }
}