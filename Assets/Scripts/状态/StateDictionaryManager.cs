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
        if (stateDict.TryGetValue(stateType, out var data))
            return data;
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
            return environment;
        }

        Debug.LogError($"未找到环境: {environmentType}");
        return null;
    }
}