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

    // 棋局Boss 王车易位机会系统
    private int m_castlingOpportunities;
    private const int MaxCastlingOpportunities = 2;

    public int CastlingOpportunities => m_castlingOpportunities;

    public void AddCastlingOpportunity(int amount, string tipText = null)
    {
        if (amount <= 0)
        {
            return;
        }

        int before = m_castlingOpportunities;
        m_castlingOpportunities = Mathf.Clamp(m_castlingOpportunities + amount, 0, MaxCastlingOpportunities);
        if (m_castlingOpportunities > before && !string.IsNullOrEmpty(tipText))
        {
            FloatingTipGenerator.Instance?.ShowDefaultTip(tipText);
        }
    }

    public bool TryConsumeCastlingOpportunity()
    {
        if (m_castlingOpportunities <= 0)
        {
            return false;
        }

        m_castlingOpportunities--;
        return true;
    }

    public void ResetCastlingOpportunities()
    {
        m_castlingOpportunities = 0;
    }

    public bool UseCommandPoints(int amount)
    {
        if (amount > 0 && amount <= commandPoints)
        {
            commandPoints -= amount;
            TemporaryBattleModifierRuntimeManager.NotifyCommandPointsSpent(amount);
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
        return RecoverCommandPointsInternal(amount, tipText);
    }

    public void NotifyEnemyKilled()
    {
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
            RecoverCommandPointsInternal(GuaranteeRecoveryAmount, $"指挥点+{GuaranteeRecoveryAmount}");
        }
    }

    private bool RecoverCommandPointsInternal(int amount, string tipText)
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
        FloatingTipGenerator.Instance?.ShowDefaultTip(string.IsNullOrEmpty(tipText)
            ? $"指挥点+{actualRecovered}"
            : tipText);
        return true;
    }
}
