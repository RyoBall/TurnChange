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

        FieldDomainScreenEffectController.Instance?.ForceStop();
    }

    public void AddEnvironment(
        EnvironmentType type,
        int durationActionValue = -1,
        UnitCombatant applier = null,
        float extraData1 = 0f,
        float extraData2 = 0f,
        float extraData3 = 0f,
        float extraData4 = 0f)
    {
        BattleEnvironment existing = GetActiveEnvironment(type);
        if (existing != null)
        {
            existing.RefreshDuration(durationActionValue, extraData1, extraData2, extraData3, extraData4);
            existing.SetApplier(applier);
            return;
        }

        BattleEnvironment environmentTemplate = EnvironmentDictionaryManager.GetEnvironmentTemplate(type);
        BattleEnvironment newEnvironment = environmentTemplate != null
            ? Instantiate(environmentTemplate)
            : ScriptableObject.CreateInstance<BattleEnvironment>();
        newEnvironment.name = environmentTemplate != null ? environmentTemplate.name : type.ToString();
        newEnvironment.environmentType = type;
        newEnvironment.ApplyEnvironment(applier, durationActionValue, extraData1, extraData2, extraData3, extraData4);
    }
    public void RemoveEnvironmentIfExist(EnvironmentType type)
    {
        BattleEnvironment existing = GetActiveEnvironment(type);
        if (existing != null)
        {
            existing.RemoveEnvironment();
        }
    }

    public void RegisterEnvironment(BattleEnvironment environment)//将环境添加到列表
    {
        if (environment == null || activeEnvironments.Contains(environment))
        {
            return;
        }

        activeEnvironments.Add(environment);

        if (FieldDomainScreenEffectController.Instance != null)
        {
            FieldDomainScreenEffectController.Instance.NotifyEnvironmentRegistered(
                environment.environmentType,
                environment.Applier);
        }
    }

    public void UnregisterEnvironment(BattleEnvironment environment)
    {
        if (environment == null)
        {
            return;
        }

        activeEnvironments.Remove(environment);

        if (FieldDomainScreenEffectController.Instance != null)
        {
            FieldDomainScreenEffectController.Instance.NotifyEnvironmentUnregistered(
                environment.environmentType,
                environment.Applier);
        }
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

    public bool HasEnvironment(EnvironmentType type)
    {
        return GetActiveEnvironment(type) != null;
    }

    public float GetCritRateBonus(UnitCombatant unit)
    {
        float bonus = 0f;
        for (int i = 0; i < activeEnvironments.Count; i++)
        {
            BattleEnvironment environment = activeEnvironments[i];
            if (environment == null || !environment.IsApplied)
            {
                continue;
            }

            bonus += environment.GetCritRateBonus(unit);
        }

        return bonus;
    }

    public float GetCritDamageBonus(UnitCombatant unit)
    {
        float bonus = 0f;
        for (int i = 0; i < activeEnvironments.Count; i++)
        {
            BattleEnvironment environment = activeEnvironments[i];
            if (environment == null || !environment.IsApplied)
            {
                continue;
            }

            bonus += environment.GetCritDamageBonus(unit);
        }

        return bonus;
    }

    public float GetIncomingDamageMultiplier(UnitCombatant attacker, UnitCombatant defender, bool isDotDamage, bool isTrueDamage)
    {
        float multiplier = 1f;
        for (int i = 0; i < activeEnvironments.Count; i++)
        {
            BattleEnvironment environment = activeEnvironments[i];
            if (environment == null || !environment.IsApplied)
            {
                continue;
            }

            multiplier *= environment.GetIncomingDamageMultiplier(attacker, defender, isDotDamage, isTrueDamage);
        }

        return multiplier;
    }

    public void NotifyCombatantActed(UnitCombatant combatant)
    {
        if (combatant == null)
        {
            return;
        }

        for (int i = 0; i < activeEnvironments.Count; i++)
        {
            BattleEnvironment environment = activeEnvironments[i];
            if (environment == null || !environment.IsApplied)
            {
                continue;
            }

            environment.OnCombatantActed(combatant);
        }
    }

    private BattleEnvironment GetActiveEnvironment(EnvironmentType type)
    {
        for (int i = 0; i < activeEnvironments.Count; i++)
        {
            BattleEnvironment environment = activeEnvironments[i];
            if (environment != null && environment.environmentType == type && environment.IsApplied)
            {
                return environment;
            }
        }

        return null;
    }
}