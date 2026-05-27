using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Commander : MonoBehaviour
{
    private const int DefaultCommandPoints = 1;
    private const int DefaultMaxCommandPoints = 5;
    private const int KillRecoveryAmount = 2;
    private const int GuaranteeRecoveryAmount = 1;
    private const float GuaranteeActionValueThreshold = 100f;

    private static Commander Instance ;
    public static Commander GetInstance()
    {
        if (Instance == null)
        {
            GameObject obj = new GameObject("Commander");
            Instance = obj.AddComponent<Commander>();
            DontDestroyOnLoad(obj);
        }
        return Instance;
    }
    private int commandPoints = DefaultCommandPoints;
    public int CommandPoints
    {
        get { return commandPoints; }
        private set{;}
    }
    private int maxCommandPoints = DefaultMaxCommandPoints;
    private float actionValueSinceLastRecovery;
    public int MaxCommandPoints => maxCommandPoints;

    public bool UseCommandPoints(int amount)
    {
        if (amount > 0 && amount <= commandPoints)
        {
            commandPoints -= amount;
            return true;
            //这里可以添加一些使用指示点后的逻辑，比如UI更新等
        }
        else
        {
            return false;
        }
    }

    public bool RecoverCommandPoints(int amount, string tipText = null)
    {
        return RecoverCommandPointsInternal(amount, true, tipText);
    }

    public void NotifyEnemyKilled(UnitCombatant source, UnitCombatant target)
    {
        return; // 目前击杀回点功能关闭，后续可以根据需要重新启用
        if (!(source is Character) || !(target is Enemy))
        {
            return;
        }

        RecoverCommandPoints(KillRecoveryAmount, $"击杀回点+{KillRecoveryAmount}");
    }

    public void NotifyActionValueAdvanced(float actionValue)
    {
        if (actionValue <= 0f)
        {
            return;
        }

        actionValueSinceLastRecovery += actionValue;
        while (actionValueSinceLastRecovery >= GuaranteeActionValueThreshold)
        {
            actionValueSinceLastRecovery -= GuaranteeActionValueThreshold;
            RecoverCommandPointsInternal(GuaranteeRecoveryAmount, false, $"指挥点+{GuaranteeRecoveryAmount}");
        }
    }

    private bool RecoverCommandPointsInternal(int amount, bool resetGuaranteeCounter, string tipText)
    {
        if (amount <= 0)
        {
            return false;
        }

        int before = commandPoints;
        commandPoints = Mathf.Clamp(commandPoints + amount, 0, maxCommandPoints);
        int actualRecovered = commandPoints - before;
        if (actualRecovered <= 0)
        {
            return false;
        }

        if (resetGuaranteeCounter)
        {
            actionValueSinceLastRecovery = 0f;
        }

        FloatingTipGenerator.Instance?.ShowDefaultTip(string.IsNullOrEmpty(tipText)
            ? $"指挥点+{actualRecovered}"
            : tipText);
        return true;
    }
}
