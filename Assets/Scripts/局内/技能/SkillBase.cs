using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class SkillBase : ScriptableObject
{
    public string skillName;

    [TextArea(2, 5)]
    public string description;

    public Sprite icon;
    [Header("伤害技能相关参数")]
    public int skillBase;
    public float skillCoef = 1f;

    public virtual IEnumerator Execute(UnitCombatant unitCombatant,List<Enemy> selectedEnemies)
    {
        //默认技能执行逻辑，子类可以重写以实现不同的效果
        yield break;
    }
    public virtual IEnumerator Execute(UnitCombatant unitCombatant)
    {
        //默认技能执行逻辑，子类可以重写以实现不同的效果
        
        yield break;
    }
}