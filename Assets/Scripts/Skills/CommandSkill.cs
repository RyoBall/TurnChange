using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CommandSkillType
{
    Change,
}
[CreateAssetMenu(fileName = "NewSkill", menuName = "技能/CommandSkill"), System.Serializable]
public class CommandSkillBase : SkillBase
{
    public CommandSkillType commandSkillType;
    public override IEnumerator Execute(UnitCombatant unitCombatant)
    {
        //指挥点技能的默认执行逻辑，子类可以重写以实现不同的效果
            switch (commandSkillType)
            {
                case CommandSkillType.Change:
                    yield return ExcuteChange();
                    //切换角色的逻辑
                    break;
            }
        yield break;
    }
    private IEnumerator ExcuteChange()
    {
        if(!Commander.GetInstance().UseCommandPoints(1))
        {
            FloatingTipGenerator.Instance.ShowDefaultTip("指挥点不足，无法使用技能");
            yield break;
        }
        SkillManager.Instance.changeCharacter.GetComponent<Combatant>().ChangeActionValue(0);
        TurnManager.Instance.InsertCombatant(Instantiate(SkillManager.Instance.changeCharacter.GetComponent<Combatant>()), false);
        yield return new WaitForSeconds(.5f);
    }
}
