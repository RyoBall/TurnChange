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

    /// <summary>
    /// 直接返回技能执行协程，调用方通过 yield return 等待技能完成。
    /// 用于 AdditionalCharacter 等需要在 PerformTurn 中同步等待技能完成的场景。
    /// </summary>
    public static IEnumerator ExecuteSkillAsCoroutine(UnitCombatant unit, SkillBase skill)
    {
        return ExecuteSkillCoroutine(unit, skill, false);
    }

    /// <summary>
    /// 直接返回技能执行协程，支持嵌套执行（外层技能仍持有执行锁时传入 ifCouldInsert=true）。
    /// </summary>
    public static IEnumerator ExecuteSkillAsCoroutine(UnitCombatant unit, SkillBase skill, bool ifCouldInsert)
    {
        return ExecuteSkillCoroutine(unit, skill, ifCouldInsert);
    }

    /// <summary>
    /// 直接返回技能执行协程（带预选目标），调用方通过 yield return 等待技能完成。
    /// </summary>
    public static IEnumerator ExecuteSkillAsCoroutine(UnitCombatant unit, SkillBase skill, List<Enemy> selectedEnemies)
    {
        return ExecuteSkillCoroutine(unit, skill, selectedEnemies, false);
    }

    private static IEnumerator ExecuteSkillCoroutine(UnitCombatant unit, SkillBase skill,bool ifCouldInsert=false)
    {
        if (s_isExecutingSkill&&!ifCouldInsert)
        {
            FloatingTipGenerator.Instance.ShowDefaultTip("正在执行技能，请稍后...");
            yield break;
        }

        // ifCouldInsert 仅允许嵌套执行，不得释放外层技能持有的执行锁
        bool acquiredLock = !s_isExecutingSkill;
        if (acquiredLock)
        {
            s_isExecutingSkill = true;
        }

        yield return skill.Execute(unit);
        yield return UnitCombatant.WaitForPendingDeaths();

        if (acquiredLock)
        {
            s_isExecutingSkill = false;
        }

        OnSkillExecuted?.Invoke(unit, skill);
    }

    private static IEnumerator ExecuteSkillCoroutine(UnitCombatant unit, SkillBase skill, List<Enemy> selectedEnemies, bool ifCouldInsert = false)
    {
        if (s_isExecutingSkill && !ifCouldInsert)
        {
            FloatingTipGenerator.Instance.ShowDefaultTip("正在执行技能，请稍后...");
            yield break;
        }

        bool acquiredLock = !s_isExecutingSkill;
        if (acquiredLock)
        {
            s_isExecutingSkill = true;
        }

        yield return skill.Execute(unit, selectedEnemies);
        yield return UnitCombatant.WaitForPendingDeaths();

        if (acquiredLock)
        {
            s_isExecutingSkill = false;
        }

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
        }
        return Instance;
    }
}