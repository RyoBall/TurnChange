using System.Collections;
using System.Collections.Generic;
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

    [Header("状态列表")]
    [SerializeField] private List<State> states = new List<State>();

    public IReadOnlyList<State> States => states;

    public float BaseActionValue => 10000f / Mathf.Max(1, speed);

    public State AddState(State stateTemplate, int overrideTurns = -1)
    {
        if (stateTemplate == null)
        {
            Debug.LogWarning($"[Combatant] {name} 挂载状态失败：状态模板为空");
            return null;
        }

        State stateInstance = gameObject.AddComponent(stateTemplate.GetType()) as State;
        if (stateInstance == null)
        {
            Debug.LogWarning($"[Combatant] {name} 挂载状态失败：无法添加组件 {stateTemplate.GetType().Name}");
            return null;
        }

        states.Add(stateInstance);
        stateInstance.Mount(this, overrideTurns);
        return stateInstance;
    }

    public bool RemoveState(State state)
    {
        if (state == null)
        {
            return false;
        }

        if (!states.Remove(state))
        {
            return false;
        }

        state.EndState();
        Destroy(state);
        return true;
    }

    public void ProcessStatesOnTurnStart()
    {
        for (int i = states.Count - 1; i >= 0; i--)
        {
            State state = states[i];
            if (state == null)
            {
                states.RemoveAt(i);
                continue;
            }

            bool shouldEnd = state.TickOnTurnStart();
            if (shouldEnd)
            {
                states.RemoveAt(i);
                state.EndState();
                Destroy(state);
            }
        }
    }

    public virtual IEnumerator PerformTurn()
    {
        yield break;
    }
}
