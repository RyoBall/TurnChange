using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SkillExecuteManager
{
    public static bool s_isExecutingSkill;//用于外部获取当前是否在执行技能
    

    public static void ExecuteSkill(Character character, SkillBase skill)
    {
        CoroutineHelper.GetHelper().StartCoroutine(ExecuteSkillCoroutine(character, skill));
    }

    private static IEnumerator ExecuteSkillCoroutine(Character character, SkillBase skill)
    {
        if(s_isExecutingSkill)
        {
            FloatingTipGenerator.Instance.ShowDefaultTip("正在执行技能，请稍后...");
            yield break;
        }
        s_isExecutingSkill = true;
        yield return skill.Execute(character);
        s_isExecutingSkill = false;
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