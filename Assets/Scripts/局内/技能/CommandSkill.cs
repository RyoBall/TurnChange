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
        yield return UnitCombatant.WaitForPendingDeaths();
        yield break;
    }
    private IEnumerator ExcuteChange()
    {
        if (TurnManager.Instance != null && TurnManager.Instance.HasChangerTurn())
        {
            FloatingTipGenerator.Instance.ShowDefaultTip("换人回合已存在");
            yield break;
        }

        if (Commander.GetInstance().CommandPoints < 1)
        {
            FloatingTipGenerator.Instance.ShowDefaultTip("指挥点不足，无法使用技能");
            yield break;
        }

        CharacterManager.Instance?.BeginCommandPointSwap();
        SkillManager.Instance.changeCharacter.GetComponent<Combatant>().ChangeActionValue(0);
        var changer = Instantiate(SkillManager.Instance.changeCharacter.GetComponent<Combatant>());
        changer.standPosition = 0;
        TurnManager.Instance.InsertCombatant(changer);
        yield return new WaitForSeconds(.5f);
    }
}
