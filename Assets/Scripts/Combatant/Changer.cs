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
        Debug.Log("[Changer] 换人完成，结束回合");
        TurnManager.Instance.RemoveCombatant(this); // 换人后移除自己的回合
        Debug.Log("[Changer] 已从回合循环中移除");
        yield return new WaitForSeconds(0.5f); // 等待换人动画等效果结束
    }   
}
