using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class State : MonoBehaviour
{
    [Header("状态配置")]
    [Tooltip("默认持续回合数")]
    [Min(1)]
    [SerializeField] private int defaultTurns = 1;

    [Tooltip("剩余回合数（运行时）")]
    [SerializeField] private int remainingTurns;

    public Combatant Owner { get; private set; }
    public int RemainingTurns => remainingTurns;

    public void Mount(Combatant owner, int overrideTurns = -1)
    {
        Owner = owner;
        remainingTurns = overrideTurns > 0 ? overrideTurns : defaultTurns;
        OnStateApply();
    }

    public bool TickOnTurnStart()
    {
        OnOwnerTurnStart();
        remainingTurns--;
        return remainingTurns <= 0;
    }

    public void EndState()
    {
        OnStateEnd();
    }

    protected virtual void OnStateApply()
    {
    }

    protected virtual void OnOwnerTurnStart()
    {
    }

    protected virtual void OnStateEnd()
    {
    }
}
