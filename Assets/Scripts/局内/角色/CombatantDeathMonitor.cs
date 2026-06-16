using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatantDeathMonitor : MonoBehaviour
{
    private static CombatantDeathMonitor instance;

    private readonly HashSet<UnitCombatant> registeredUnits = new HashSet<UnitCombatant>();
    private readonly HashSet<UnitCombatant> processedUnits = new HashSet<UnitCombatant>();
    private readonly HashSet<UnitCombatant> runningUnits = new HashSet<UnitCombatant>();

    public static CombatantDeathMonitor Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject obj = new GameObject("CombatantDeathMonitor");
                instance = obj.AddComponent<CombatantDeathMonitor>();
            }

            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    public static void Register(UnitCombatant unit)
    {
        if (unit == null)
        {
            return;
        }

        Instance.RegisterInternal(unit);
    }

    public static void Unregister(UnitCombatant unit)
    {
        if (instance == null || unit == null)
        {
            return;
        }

        instance.registeredUnits.Remove(unit);
        instance.processedUnits.Remove(unit);
        instance.runningUnits.Remove(unit);
    }

    public static IEnumerator CheckDeathsAndWait()
    {
        CombatantDeathMonitor monitor = Instance;
        while (true)
        {
            bool triggeredDeath = monitor.TriggerDeathsByHealth();
            monitor.StartPendingDeathHandlers();

            if (monitor.runningUnits.Count == 0)
            {
                if (!triggeredDeath)
                {
                    yield break;
                }

                continue;
            }

            while (monitor.runningUnits.Count > 0)
            {
                yield return null;
            }
        }
    }

    private void RegisterInternal(UnitCombatant unit)
    {
        registeredUnits.Add(unit);
        if (!unit.IsDead)
        {
            processedUnits.Remove(unit);
            runningUnits.Remove(unit);
        }
    }

    private bool TriggerDeathsByHealth()
    {
        bool triggeredDeath = false;
        List<UnitCombatant> snapshot = new List<UnitCombatant>(registeredUnits);
        for (int i = 0; i < snapshot.Count; i++)
        {
            UnitCombatant unit = snapshot[i];
            if (unit == null || unit.IsDead)
            {
                continue;
            }

            if (unit.currentHP > 0)
            {
                continue;
            }

            unit.Die();
            triggeredDeath = true;
        }

        return triggeredDeath;
    }

    private void StartPendingDeathHandlers()
    {
        List<UnitCombatant> snapshot = new List<UnitCombatant>(registeredUnits);
        for (int i = 0; i < snapshot.Count; i++)
        {
            UnitCombatant unit = snapshot[i];
            if (unit == null)
            {
                continue;
            }

            if (!unit.IsDead || processedUnits.Contains(unit) || runningUnits.Contains(unit))
            {
                continue;
            }

            runningUnits.Add(unit);
            StartCoroutine(RunDeathHandler(unit));
        }
    }

    private IEnumerator RunDeathHandler(UnitCombatant unit)
    {
        if (unit != null)
        {
            yield return unit.ExecuteDeathEvent();
        }

        runningUnits.Remove(unit);
        if (unit != null)
        {
            processedUnits.Add(unit);
        }
    }
}