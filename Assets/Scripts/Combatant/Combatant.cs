using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Combatant : MonoBehaviour
{
    [Tooltip("角色速度，值越大意味着越快")]
    public int speed = 100;
    [Tooltip("是否参与 TurnManager 的行动循环")]
    public bool participateInTurnLoop = true;
    public string combatantName;

    [HideInInspector]
    public float currentActionValue;

    public float BaseActionValue => 10000f / Mathf.Max(1, speed);

    public virtual IEnumerator PerformTurn()
    {
        yield break;
    }
}
