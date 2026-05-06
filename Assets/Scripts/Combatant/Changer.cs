using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Changer : Combatant
{
    public override IEnumerator PerformTurn()
    {
        if (CharacterManager.Instance == null)
        {
            Debug.LogWarning("[Changer] 场景中缺少 CharacterManager，无法执行换人");
            yield break;
        }

        yield return StartCoroutine(CharacterManager.Instance.SelectAndSwapCoroutine());
        TurnManager.Instance.RemoveCombatant(this); // 换人后自己回合结束，重新插入轮次末尾
        yield return new WaitForSeconds(0.5f); // 等待换人动画等效果结束
    }   
}
