using System.Collections.Generic;
using UnityEngine;

public class EnvironmentManager : MonoBehaviour
{
    public static EnvironmentManager Instance { get; private set; }

    [Header("当前环境")]
    [SerializeField] private List<BattleEnvironment> activeEnvironments = new List<BattleEnvironment>();

    public IReadOnlyList<BattleEnvironment> ActiveEnvironments => activeEnvironments;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    public void AddEnvironment(EnvironmentType type, int durationActionValue = -1)
    {
        BattleEnvironment newEnvironment = ScriptableObject.CreateInstance<BattleEnvironment>();
        newEnvironment.environmentType = type;
        newEnvironment.ApplyEnvironment(durationActionValue);
    }

    public void RegisterEnvironment(BattleEnvironment environment)
    {
        if (environment == null || activeEnvironments.Contains(environment))
        {
            return;
        }

        activeEnvironments.Add(environment);
    }

    public void UnregisterEnvironment(BattleEnvironment environment)
    {
        if (environment == null)
        {
            return;
        }

        activeEnvironments.Remove(environment);
    }

    public void TickEnvironments(float passedActionValue)
    {
        int actionValueCost = Mathf.Max(0, Mathf.CeilToInt(passedActionValue));
        if (actionValueCost <= 0)
        {
            return;
        }

        for (int i = activeEnvironments.Count - 1; i >= 0; i--)
        {
            BattleEnvironment environment = activeEnvironments[i];
            if (environment == null)
            {
                activeEnvironments.RemoveAt(i);
                continue;
            }

            environment.TickByActionValue(actionValueCost);
        }
    }
}