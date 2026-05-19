using UnityEngine;

public enum EnvironmentType
{
    Gravity,
    Cutdown,
    DesperationField
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
    private UnitCombatant applier;
    [System.NonSerialized] private IEnvironmentBehavior m_behavior;

    public int RemainingActionValue => remainingActionValue;
    public bool IsApplied => isApplied;
    public UnitCombatant Applier => applier;

    private IEnvironmentBehavior Behavior
    {
        get
        {
            if (m_behavior == null)
            {
                m_behavior = EnvironmentBehaviorFactory.Create(environmentType);
                m_behavior.Initialize(this);
            }

            return m_behavior;
        }
    }

    private void OnEnable()
    {
        m_behavior = null;
    }

    public void ApplyEnvironment(UnitCombatant source = null, int overrideActionValue = -1)
    {
        if (isApplied)
        {
            return;
        }

        applier = source;
        remainingActionValue = overrideActionValue > 0 ? overrideActionValue : defaultDurationActionValue;
        isApplied = true;

        if (EnvironmentManager.Instance != null)
        {
            EnvironmentManager.Instance.RegisterEnvironment(this);
        }

        OnEnvironmentApply();
    }

    public void RefreshDuration(int overrideActionValue = -1)
    {
        remainingActionValue = overrideActionValue > 0 ? overrideActionValue : defaultDurationActionValue;
    }

    public void SetApplier(UnitCombatant source)
    {
        applier = source;
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
        Behavior.OnEnvironmentApply();
    }

    protected virtual void OnEnvironmentRemove()
    {
        Behavior.OnEnvironmentRemove();
    }

    public float GetCritRateBonus(UnitCombatant unit)
    {
        if (!isApplied || unit == null)
        {
            return 0f;
        }

        return Behavior.GetCritRateBonus(unit);
    }

    public float GetCritDamageBonus(UnitCombatant unit)
    {
        if (!isApplied || unit == null)
        {
            return 0f;
        }

        return Behavior.GetCritDamageBonus(unit);
    }

    public float GetIncomingDamageMultiplier(UnitCombatant attacker, UnitCombatant defender, bool isDotDamage, bool isTrueDamage)
    {
        if (!isApplied || defender == null)
        {
            return 1f;
        }

        return Behavior.GetIncomingDamageMultiplier(attacker, defender, isDotDamage, isTrueDamage);
    }

    public void OnCombatantActed(UnitCombatant combatant)
    {
        if (!isApplied || combatant == null)
        {
            return;
        }

        Behavior.OnCombatantActed(combatant);
    }

    private void OnDestroy()
    {
        if (isApplied && EnvironmentManager.Instance != null)
        {
            EnvironmentManager.Instance.UnregisterEnvironment(this);
        }
    }
}

public interface IEnvironmentBehavior
{
    void Initialize(BattleEnvironment environment);
    void OnEnvironmentApply();
    void OnEnvironmentRemove();
    float GetCritRateBonus(UnitCombatant unit);
    float GetCritDamageBonus(UnitCombatant unit);
    float GetIncomingDamageMultiplier(UnitCombatant attacker, UnitCombatant defender, bool isDotDamage, bool isTrueDamage);
    void OnCombatantActed(UnitCombatant combatant);
}

public abstract class EnvironmentBehaviorBase : IEnvironmentBehavior
{
    protected BattleEnvironment environment;

    public virtual void Initialize(BattleEnvironment environment)
    {
        this.environment = environment;
    }

    public virtual void OnEnvironmentApply() { }
    public virtual void OnEnvironmentRemove() { }
    public virtual float GetCritRateBonus(UnitCombatant unit) { return 0f; }
    public virtual float GetCritDamageBonus(UnitCombatant unit) { return 0f; }
    public virtual float GetIncomingDamageMultiplier(UnitCombatant attacker, UnitCombatant defender, bool isDotDamage, bool isTrueDamage) { return 1f; }
    public virtual void OnCombatantActed(UnitCombatant combatant) { }
}

public static class EnvironmentBehaviorFactory
{
    public static IEnvironmentBehavior Create(EnvironmentType environmentType)
    {
        switch (environmentType)
        {
            case EnvironmentType.Gravity:
                return new GravityEnvironmentBehavior();
            case EnvironmentType.Cutdown:
                return new CutdownEnvironmentBehavior();
            case EnvironmentType.DesperationField:
                return new DesperationFieldEnvironmentBehavior();
            default:
                return new DefaultEnvironmentBehavior();
        }
    }
}

public class DefaultEnvironmentBehavior : EnvironmentBehaviorBase
{
}

public class DesperationFieldEnvironmentBehavior : EnvironmentBehaviorBase
{
    public override float GetCritRateBonus(UnitCombatant unit)
    {
        return GetMissingHpRatio(unit) * 0.5f;
    }

    public override float GetCritDamageBonus(UnitCombatant unit)
    {
        return GetMissingHpRatio(unit) * 0.6f;
    }

    private float GetMissingHpRatio(UnitCombatant unit)
    {
        if (!(unit is Character) || unit.maxHP <= 0)
        {
            return 0f;
        }

        return Mathf.Clamp01((float)(unit.maxHP - unit.currentHP) / unit.maxHP);
    }
}

public class GravityEnvironmentBehavior : EnvironmentBehaviorBase
{
    public override float GetIncomingDamageMultiplier(UnitCombatant attacker, UnitCombatant defender, bool isDotDamage, bool isTrueDamage)
    {
        if (!isDotDamage || !(defender is Enemy))
        {
            return 1f;
        }

        return 2f;
    }
}

public class CutdownEnvironmentBehavior : EnvironmentBehaviorBase
{
    private float m_currentDotBonus = 0.45f;

    public override void OnEnvironmentApply()
    {
        m_currentDotBonus = 0.45f;
    }

    public override float GetIncomingDamageMultiplier(UnitCombatant attacker, UnitCombatant defender, bool isDotDamage, bool isTrueDamage)
    {
        if (!(defender is Enemy))
        {
            return 1f;
        }

        if (!isDotDamage)
        {
            return 0.7f;
        }

        return 1f + m_currentDotBonus;
    }

    public override void OnCombatantActed(UnitCombatant combatant)
    {
        if (combatant == null || combatant != environment.Applier)
        {
            return;
        }

        m_currentDotBonus = Mathf.Max(0f, m_currentDotBonus - 0.15f);
    }
}