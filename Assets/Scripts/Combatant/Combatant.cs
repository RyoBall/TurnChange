using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class Combatant : MonoBehaviour
{
    [Tooltip("角色速度，值越大意味着越快")]
    public int speed = 100;
    [Tooltip("是否在游戏开始时参与 TurnManager 的行动循环")]
    public bool participateInTurnLoopAtStart = true;
    public string combatantName;

    public float currentActionValue{get; private set;}
    public float BaseActionValue => 10000f / Mathf.Max(1, speed);
    public void ChangeActionValue(float delta,bool ifChangePos=true)
    {
        currentActionValue = Mathf.Max(0, delta);
        if(ifChangePos)
        TurnManager.Instance?.NotifyCombatantActionValueChanged(this);
    }

    public virtual IEnumerator PerformTurn()
    {
        yield break;
    }
}
