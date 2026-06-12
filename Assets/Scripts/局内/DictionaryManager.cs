
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;  // 仅在编辑器下使用

public class StateDictionaryManager : MonoBehaviour
{
    private static Dictionary<StateType, State> stateDict;

    // 静态构造 + 编辑器自动刷新
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        LoadAllStateEffects();
    }

#if UNITY_EDITOR
    [InitializeOnLoadMethod]  // 编辑器启动或脚本重编译时自动执行
    static void EditorInitialize()
    {
        LoadAllStateEffects();
        // 可选：监听资产变化，自动刷新字典
        EditorApplication.projectChanged -= LoadAllStateEffects;    
        EditorApplication.projectChanged += LoadAllStateEffects;
    }
#endif

    private static void LoadAllStateEffects()
    {
        stateDict = new Dictionary<StateType, State>();
        // 加载项目中所有指定类型的 ScriptableObject
        State[] allStates = Resources.LoadAll<State>("");
        
        // 如果你不想放在 Resources 文件夹，可以使用下面编辑器专用方法
        // 但为了运行时能读取，建议还是把状态资产放到 Resources 或 Addressables

        foreach (var State in allStates)
        {
            if (!stateDict.ContainsKey(State.stateType))
                stateDict.Add(State.stateType, State);
            else
                Debug.LogWarning($"重复的状态类型: {State.stateType}，请检查资源命名和配置");
        }
        
        Debug.Log($"状态字典加载完成，共 {stateDict.Count} 种状态");
    }
    public static State GetState(StateType stateType)
    {
        if (stateDict != null && stateDict.TryGetValue(stateType, out var data))
        {   
            var state = Instantiate(data); // 返回实例化对象，避免修改原始数据
            state.name = data.name; // 保持实例化对象的名字与原始数据一致，方便调试
            return state;
        }

        Debug.LogError($"未找到状态: {stateType}");
        return null;
    }
    public static string GetStateName(StateType stateType)
    {
        if (stateDict != null && stateDict.TryGetValue(stateType, out var data))
            return data.name;
        else if(stateType == StateType.None)
            return "";

        Debug.LogError($"未找到状态: {stateType}");
        return null;
    }
}
public class EnvironmentDictionaryManager : MonoBehaviour
{
    private static Dictionary<EnvironmentType, BattleEnvironment> environmentDict;

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

    private static void LoadAllEnvironments()
    {
        environmentDict = new Dictionary<EnvironmentType, BattleEnvironment>();
        BattleEnvironment[] allEnvironments = Resources.LoadAll<BattleEnvironment>(string.Empty);

        foreach (var environment in allEnvironments)
        {
            if (environment == null)
            {
                continue;
            }

            if (!environmentDict.ContainsKey(environment.environmentType))
            {
                environmentDict.Add(environment.environmentType, environment);
            }
            else
            {
                Debug.LogWarning($"重复的环境类型: {environment.environmentType}，请检查资源命名和配置");
            }
        }

        Debug.Log($"环境字典加载完成，共 {environmentDict.Count} 种环境");
    }

    public static BattleEnvironment GetEnvironment(EnvironmentType environmentType)
    {
        if (environmentDict == null)
        {
            LoadAllEnvironments();
        }

        if (environmentDict.TryGetValue(environmentType, out var environment))
        {
            var environmentInstance = Instantiate(environment);
            environmentInstance.name = environment.name;
            return environmentInstance;
        }

        Debug.LogError($"未找到环境: {environmentType}");
        return null;
    }
}
public class SkillDictionaryManager : MonoBehaviour
{
    private static Dictionary<CharacterSkillType, CharacterSkillBase> skillDict;

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

    private static void LoadAllSkills()
    {
        skillDict = new Dictionary<CharacterSkillType, CharacterSkillBase>();
        CharacterSkillBase[] allSkills = Resources.LoadAll<CharacterSkillBase>(string.Empty);

        foreach (var skill in allSkills)
        {
            if (skill == null)
            {
                continue;
            }

            if (!skillDict.ContainsKey(skill.skillType))
            {
                skillDict.Add(skill.skillType, skill);
            }
            else
            {
                Debug.LogWarning($"重复的技能类型: {skill.skillType}，请检查资源命名和配置");
            }
        }   

        Debug.Log($"技能字典加载完成，共 {skillDict.Count} 种技能");
    }

    public static CharacterSkillBase GetSkill(CharacterSkillType skillType)
    {
        if (skillDict == null)
        {
            LoadAllSkills();
        }

        if (skillDict.TryGetValue(skillType, out var skill))
        {
            return skill;
        }

        Debug.LogError($"未找到技能: {skillType}");
        return null;
    }
    public static string GetSkillName(CharacterSkillType skillType)
    {
        if (skillDict.TryGetValue(skillType, out var skill))
            return skill.skillName;
        Debug.LogError($"未找到技能: {skillType}");
        return null;
    }
}
public class EnemySkillDictionaryManager : MonoBehaviour
{
    private static Dictionary<EnemySkillType, EnemySkillBase> enemySkillDict;

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

    private static void LoadAllEnemySkills()
    {
        enemySkillDict = new Dictionary<EnemySkillType, EnemySkillBase>();
        EnemySkillBase[] allEnemySkills = Resources.LoadAll<EnemySkillBase>(string.Empty);

        foreach (var skill in allEnemySkills)
        {
            if (skill == null)
            {
                continue;
            }

            if (!enemySkillDict.ContainsKey(skill.enemySkillType))
            {
                enemySkillDict.Add(skill.enemySkillType, skill);
            }
            else
            {
                Debug.LogWarning($"重复的敌人技能类型: {skill.enemySkillType}，请检查资源命名和配置");
            }
        }

        Debug.Log($"敌人技能字典加载完成，共 {enemySkillDict.Count} 种敌人技能");
    }

    public static EnemySkillBase GetEnemySkill(EnemySkillType skillType)
    {
        if (enemySkillDict == null)
        {
            LoadAllEnemySkills();
        }

        if (enemySkillDict.TryGetValue(skillType, out var skill))
        {
            return skill;
        }

        Debug.LogError($"未找到敌人技能: {skillType}");
        return null;
    }
    public static string GetEnemySkillName(EnemySkillType skillType)
    {
        if (enemySkillDict.TryGetValue(skillType, out var skill))
            return skill.skillName;
        else if(skillType == EnemySkillType.NoneNone)
            return "";
        Debug.Log($"未找到敌人技能: {skillType}");
        return null;
    }
}
