using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : Combatant
{
    public List<SkillBase> skills = new List<SkillBase>();

    bool endTurn = false;
    [Header("属性")]
    public int maxHP;
    public int currentHP;
    public float attack;
    public override IEnumerator PerformTurn()
    {
        endTurn=false;
        //展示攻击逻辑
        yield return CommandButtonManager.Instance.FadeInButtons(this);
        yield return new WaitUntil(() => endTurn);
        //结束玩家回合的内容
        yield return CommandButtonManager.Instance.FadeOutButtons();
    }
    public void EndTurn()
    {
        endTurn = true;
    }
}
