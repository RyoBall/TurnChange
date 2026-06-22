using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExtraCharacter : UnitCombatant
{
    public Character character;

    public override Sprite TurnImageSprite => character != null ? character.TurnImageSprite : null;

    protected override void Awake()
    {
        // 不调用 base.Awake()，避免注册到 CombatantDeathMonitor。
        // ExtraCharacter 是临时回合插入节点，不需要参与死亡监控，
        // 且其默认 HP=0 会被死亡监控器误判为已死亡而提前移除。
    }

    protected override void OnDestroy()
    {
        // 不调用 base.OnDestroy()，因为 Awake 中跳过了 Register，
        // 无需 Unregister。
    }

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
