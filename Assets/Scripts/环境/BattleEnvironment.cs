using UnityEngine;

public enum EnvironmentType
{
    Gravity,
    // 可以根据需要添加更多环境类型
}
[CreateAssetMenu(fileName = "Environment", menuName = "环境/新环境")]
public class BattleEnvironment:ScriptableObject
{
    [Header("环境配置")]
    public EnvironmentType environmentType;
    [Tooltip("默认持续行动值")]
    [Min(1)]
    [SerializeField] private int defaultDurationActionValue = 1000;

    [Tooltip("剩余持续行动值（运行时）")]
    private int remainingActionValue;

    private bool isApplied;

    public int RemainingActionValue => remainingActionValue;
    public bool IsApplied => isApplied;
    public void ApplyEnvironment(int overrideActionValue = -1)
    {
        if (isApplied)
        {
            return;
        }

        remainingActionValue = overrideActionValue > 0 ? overrideActionValue : defaultDurationActionValue;
        isApplied = true;

        if (EnvironmentManager.Instance != null)
        {
            EnvironmentManager.Instance.RegisterEnvironment(this);
        }

        OnEnvironmentApply();
    }

    public bool TickByActionValue(int actionValueCost)
    {
        if (!isApplied)
        {
            return false;
        }

        remainingActionValue -= Mathf.Max(0, actionValueCost);
        if (remainingActionValue > 0)
        {
            return false;
        }

        RemoveEnvironment();
        return true;
    }

    public void RemoveEnvironment()
    {
        if (!isApplied)
        {
            return;
        }

        isApplied = false;

        if (EnvironmentManager.Instance != null)
        {
            EnvironmentManager.Instance.UnregisterEnvironment(this);
        }

        OnEnvironmentRemove();
    }

    protected virtual void OnEnvironmentApply()
    {
        switch(environmentType)
        {
            case EnvironmentType.Gravity:
                
                break;
        }  
    }

    protected virtual void OnEnvironmentRemove()
    {
    }

    private void OnDestroy()
    {
        if (isApplied && EnvironmentManager.Instance != null)
        {
            EnvironmentManager.Instance.UnregisterEnvironment(this);
        }
    }
}