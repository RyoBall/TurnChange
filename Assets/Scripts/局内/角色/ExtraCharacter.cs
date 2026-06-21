using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExtraCharacter : UnitCombatant
{
    public Character character;

    public override Sprite TurnImageSprite => character != null ? character.TurnImageSprite : null;

    public void Initialize(Character character)
    {
        this.character = character;
    }
    public override IEnumerator PerformTurn()
    {
        //执行角色的回合逻辑
        yield return character.PerformTurn();
        TurnManager.Instance.RemoveCombatant(this); // 换人后移除自己的回合
        yield return new WaitForSeconds(0.2f);
        Destroy(gameObject); // 销毁换人角色的对象
    }
}
