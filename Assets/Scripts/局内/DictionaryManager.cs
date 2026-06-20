
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;  // 仅在编辑器下使用

public class StateDictionaryManager : MonoBehaviour
{
    private static Dictionary<StateType, State> s_stateDict;
    private static bool s_initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        LoadAllStateEffects();
    }

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void EditorInitialize()
    {
        LoadAllStateEffects();
        EditorApplication.projectChanged -= LoadAllStateEffects;
        EditorApplication.projectChanged += LoadAllStateEffects;
    }
#endif

    private static void EnsureInitialized()
    {
        if (!s_initialized)
        {
            LoadAllStateEffects();
        }
    }

    private static void LoadAllStateEffects()
    {
        s_stateDict = new Dictionary<StateType, State>();
        State[] allStates = Resources.LoadAll<State>("");

        foreach (var state in allStates)
        {
            if (state == null)
            {
                continue;
            }

            if (!s_stateDict.ContainsKey(state.stateType))
            {
                s_stateDict.Add(state.stateType, state);
            }
            else
            {
                Debug.LogWarning($"重复的状态类型: {state.stateType}，请检查资源命名和配置");
            }
        }

        s_initialized = true;
        Debug.Log($"状态字典加载完成，共 {s_stateDict.Count} 种状态");
    }

    /// <summary>获取状态模板的只读引用（不 Instantiate，用于读取配置数据）</summary>
    public static State GetStateTemplate(StateType stateType)
    {
        EnsureInitialized();

        if (s_stateDict != null && s_stateDict.TryGetValue(stateType, out var data))
        {
            return data;
        }

        Debug.LogError($"未找到状态: {stateType}");
        return null;
    }

    /// <summary>获取状态的运行时实例（已 Instantiate，可安全修改）</summary>
    public static State GetState(StateType stateType)
    {
        State template = GetStateTemplate(stateType);
        if (template == null)
        {
            return null;
        }

        var state = Instantiate(template);
        state.name = template.name;
        return state;
    }

    public static string GetStateName(StateType stateType)
    {
        State template = GetStateTemplate(stateType);
        if (template != null)
        {
            return template.name;
        }

        if (stateType == StateType.None)
        {
            return "";
        }

        Debug.LogError($"未找到状态: {stateType}");
        return null;
    }
}
public class EnvironmentDictionaryManager : MonoBehaviour
{
    private static Dictionary<EnvironmentType, BattleEnvironment> s_environmentDict;
    private static bool s_initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        LoadAllEnvironments();
    }

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void EditorInitialize()
    {
        LoadAllEnvironments();
        EditorApplication.projectChanged -= LoadAllEnvironments;
        EditorApplication.projectChanged += LoadAllEnvironments;
    }
#endif

    private static void EnsureInitialized()
    {
        if (!s_initialized)
        {
            LoadAllEnvironments();
        }
    }

    private static void LoadAllEnvironments()
    {
        s_environmentDict = new Dictionary<EnvironmentType, BattleEnvironment>();
        BattleEnvironment[] allEnvironments = Resources.LoadAll<BattleEnvironment>(string.Empty);

        foreach (var environment in allEnvironments)
        {
            if (environment == null)
            {
                continue;
            }

            if (!s_environmentDict.ContainsKey(environment.environmentType))
            {
                s_environmentDict.Add(environment.environmentType, environment);
            }
            else
            {
                Debug.LogWarning($"重复的环境类型: {environment.environmentType}，请检查资源命名和配置");
            }
        }

        s_initialized = true;
        Debug.Log($"环境字典加载完成，共 {s_environmentDict.Count} 种环境");
    }

    /// <summary>获取环境模板的只读引用（不 Instantiate，用于读取配置数据）</summary>
    public static BattleEnvironment GetEnvironmentTemplate(EnvironmentType environmentType)
    {
        EnsureInitialized();

        if (s_environmentDict != null && s_environmentDict.TryGetValue(environmentType, out var environment))
        {
            return environment;
        }

        Debug.LogError($"未找到环境: {environmentType}");
        return null;
    }

    /// <summary>获取环境的运行时实例（已 Instantiate，可安全修改）</summary>
    public static BattleEnvironment GetEnvironment(EnvironmentType environmentType)
    {
        BattleEnvironment template = GetEnvironmentTemplate(environmentType);
        if (template == null)
        {
            return null;
        }

        var environmentInstance = Instantiate(template);
        environmentInstance.name = template.name;
        return environmentInstance;
    }
}
public class SkillDictionaryManager : MonoBehaviour
{
    private static Dictionary<CharacterSkillType, CharacterSkillBase> s_skillDict;
    private static bool s_initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        LoadAllSkills();
    }

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void EditorInitialize()
    {
        LoadAllSkills();
        EditorApplication.projectChanged -= LoadAllSkills;
        EditorApplication.projectChanged += LoadAllSkills;
    }
#endif

    private static void EnsureInitialized()
    {
        if (!s_initialized)
        {
            LoadAllSkills();
        }
    }

    private static void LoadAllSkills()
    {
        s_skillDict = new Dictionary<CharacterSkillType, CharacterSkillBase>();
        CharacterSkillBase[] allSkills = Resources.LoadAll<CharacterSkillBase>(string.Empty);

        foreach (var skill in allSkills)
        {
            if (skill == null)
            {
                continue;
            }

            if (!s_skillDict.ContainsKey(skill.skillType))
            {
                s_skillDict.Add(skill.skillType, skill);
            }
            else
            {
                Debug.LogWarning($"重复的技能类型: {skill.skillType}，请检查资源命名和配置");
            }
        }

        s_initialized = true;
        Debug.Log($"技能字典加载完成，共 {s_skillDict.Count} 种技能");
    }

    /// <summary>获取技能模板的只读引用（不 Instantiate，用于读取配置数据或 UI 展示）</summary>
    public static CharacterSkillBase GetSkillTemplate(CharacterSkillType skillType)
    {
        EnsureInitialized();

        if (s_skillDict != null && s_skillDict.TryGetValue(skillType, out var skill))
        {
            return skill;
        }

        Debug.LogError($"未找到技能: {skillType}");
        return null;
    }

    /// <summary>获取技能的运行时实例（已 Instantiate，可安全修改）</summary>
    public static CharacterSkillBase GetSkill(CharacterSkillType skillType)
    {
        CharacterSkillBase template = GetSkillTemplate(skillType);
        if (template == null)
        {
            return null;
        }

        var instance = Instantiate(template);
        instance.name = template.name;
        return instance;
    }

    public static string GetSkillName(CharacterSkillType skillType)
    {
        CharacterSkillBase template = GetSkillTemplate(skillType);
        if (template != null)
        {
            return template.skillName;
        }

        Debug.LogError($"未找到技能: {skillType}");
        return null;
    }
}
public class EnemySkillDictionaryManager : MonoBehaviour
{
    private static Dictionary<EnemySkillType, EnemySkillBase> s_enemySkillDict;
    private static bool s_initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        LoadAllEnemySkills();
    }

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void EditorInitialize()
    {
        LoadAllEnemySkills();
        EditorApplication.projectChanged -= LoadAllEnemySkills;
        EditorApplication.projectChanged += LoadAllEnemySkills;
    }
#endif

    private static void EnsureInitialized()
    {
        if (!s_initialized)
        {
            LoadAllEnemySkills();
        }
    }

    private static void LoadAllEnemySkills()
    {
        s_enemySkillDict = new Dictionary<EnemySkillType, EnemySkillBase>();
        EnemySkillBase[] allEnemySkills = Resources.LoadAll<EnemySkillBase>(string.Empty);

        foreach (var skill in allEnemySkills)
        {
            if (skill == null)
            {
                continue;
            }

            if (!s_enemySkillDict.ContainsKey(skill.enemySkillType))
            {
                s_enemySkillDict.Add(skill.enemySkillType, skill);
            }
            else
            {
                // 同一 EnemySkillType 存在多个变体（如普通版/强化版），保留系数更高的版本
                EnemySkillBase existing = s_enemySkillDict[skill.enemySkillType];
                if (skill.skillCoef > existing.skillCoef || skill.skillBase > existing.skillBase)
                {
                    s_enemySkillDict[skill.enemySkillType] = skill;
                    Debug.Log($"敌人技能 {skill.enemySkillType} 替换为更高数值版本: coef={skill.skillCoef} base={skill.skillBase}");
                }
                else
                {
                    Debug.LogWarning($"敌人技能 {skill.enemySkillType} 存在多个变体，保留数值更高的版本");
                }
            }
        }

        s_initialized = true;
        Debug.Log($"敌人技能字典加载完成，共 {s_enemySkillDict.Count} 种敌人技能");
    }

    /// <summary>获取敌人技能模板的只读引用（不 Instantiate，用于读取配置数据）</summary>
    public static EnemySkillBase GetEnemySkillTemplate(EnemySkillType skillType)
    {
        EnsureInitialized();

        if (s_enemySkillDict != null && s_enemySkillDict.TryGetValue(skillType, out var skill))
        {
            return skill;
        }

        Debug.LogError($"未找到敌人技能: {skillType}");
        return null;
    }

    /// <summary>获取敌人技能的运行时实例（已 Instantiate，可安全修改）</summary>
    public static EnemySkillBase GetEnemySkill(EnemySkillType skillType)
    {
        EnemySkillBase template = GetEnemySkillTemplate(skillType);
        if (template == null)
        {
            return null;
        }

        var instance = Instantiate(template);
        instance.name = template.name;
        return instance;
    }

    public static string GetEnemySkillName(EnemySkillType skillType)
    {
        EnemySkillBase template = GetEnemySkillTemplate(skillType);
        if (template != null)
        {
            return template.skillName;
        }

        if (skillType == EnemySkillType.Exploder2)
        {
            return "";
        }

        Debug.Log($"未找到敌人技能: {skillType}");
        return null;
    }
}
