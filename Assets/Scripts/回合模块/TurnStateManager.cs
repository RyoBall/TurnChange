using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TurnState
{
    InCharacterTurn,
    InSkillUsing,
    OutCharacterTurn,
}
public class TurnStateManager : MonoBehaviour
{
    public static TurnStateManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    public TurnState currentState = TurnState.InCharacterTurn;
    public Coroutine ChangeState(TurnState newState,Character character=null)
    {
        return StartCoroutine(ChangeStateIE(newState, character));
    }
    private IEnumerator ChangeStateIE(TurnState newState,Character character=null)
    {
        yield return ExitState(currentState);
        yield return EnterState(newState, character);
        currentState = newState;
        //这里可以添加一些状态切换时的全局逻辑，比如UI更新等
    }
    private Coroutine EnterState(TurnState state,Character character=null)
    {
        switch (state)
        {
            case TurnState.InCharacterTurn:
                return CommandButtonManager.Instance.FadeInButtons(character);
                //进入角色回合的逻辑
            case TurnState.InSkillUsing:
                break;
            case TurnState.OutCharacterTurn:
                break;
                //进入角色回合结束的逻辑
        }
        return null;
    }
    private Coroutine ExitState(TurnState state)
    {
        switch (state)
        {
            case TurnState.InCharacterTurn:
                return CommandButtonManager.Instance.FadeOutButtons();
                //退出角色回合的逻辑
            case TurnState.InSkillUsing:
                break;
                //退出技能使用的逻辑
            case TurnState.OutCharacterTurn:
                break;
                //退出角色回合结束的逻辑
        }
        return null;
    }
}
