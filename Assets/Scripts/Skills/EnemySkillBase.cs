using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemySkillType
{
    NormalAttack,
    ShieldSupport_1, // 护盾手技能一
    ShieldSupport_2, // 护盾手技能二
    SingleAttack,    // 单体攻击手
    Debuff_1,        // 负面手技能一
    Debuff_2,        // 负面手技能二
    Exploder_1,      // 群体自爆手技能一
    Exploder_2,      // 群体自爆手技能二
    DotMaster_1,     // 持续伤害Dot施加手技能一
    DotMaster_2      // 持续伤害Dot施加手技能二
}
[CreateAssetMenu(fileName = "NewEnemySkill", menuName = "技能/EnemySkill"), System.Serializable]
public class EnemySkillBase : SkillBase
{
    public EnemySkillType enemySkillType;

    public override IEnumerator Execute(UnitCombatant unitCombatant)
    {
        if (unitCombatant == null) yield break;
        Enemy self = unitCombatant as Enemy;
        if (self == null) yield break;



        switch (enemySkillType)
        {
            case EnemySkillType.NormalAttack:
                // 可补充普通攻击逻辑
                break;
            case EnemySkillType.ShieldSupport_1:
                yield return ShieldSupport_1(self);
                break;
            case EnemySkillType.ShieldSupport_2:
                yield return ShieldSupport_2(self);
                break;
            case EnemySkillType.SingleAttack:
                yield return SingleAttack(self);
                break;
            case EnemySkillType.Debuff_1:
                yield return Debuff_1(self);
                break;
            case EnemySkillType.Debuff_2:
                yield return Debuff_2(self);
                break;
            case EnemySkillType.Exploder_1:
                yield return Exploder_1(self);
                break;
            case EnemySkillType.Exploder_2:
                yield return Exploder_2(self);
                break;
            case EnemySkillType.DotMaster_1:
                yield return DotMaster_1(self);
                break;
            case EnemySkillType.DotMaster_2:
                yield return DotMaster_2(self);
                break;
        }
    }

    // 1.护盾手 技能一
    private IEnumerator ShieldSupport_1(Enemy self)
    {
        Enemy target = null;
        foreach(Enemy enemy in EnemyManager.Instance.AliveEnemies)
        {
            if(target==null || target.currentHP > enemy.currentHP)
            {
                target = enemy;
            }
        }
        if (target != null)
        {
            int shield = Mathf.RoundToInt(self.maxHP * 0.1f);
            target.AddShield(shield);
            FloatingTipGenerator.Instance?.ShowTipAtObject(target.transform, $"获得护盾 {shield}");
        }
        yield return new WaitForSeconds(0.5f);
    }
    // 1.护盾手 技能二
    private IEnumerator ShieldSupport_2(Enemy self)
    {
        foreach (var ally in CharacterManager.Instance.fieldCharacters)
        {
            if (ally == null) continue;
            int damage = Mathf.RoundToInt(self.attack * 0.3f);
            ally.TakeDamage(damage, self, false, true);
            ally.ChangeActionValue(ally.currentActionValue + ally.BaseActionValue * 0.2f);
            FloatingTipGenerator.Instance?.ShowTipAtObject(ally.transform, $"受到延后与伤害");
        }
        yield return new WaitForSeconds(0.5f);
    }
    // 2.单体攻击手
    private IEnumerator SingleAttack(Enemy self)
    {
        var target = CharacterManager.Instance.GetCharacterByRand();
        if (target == null) yield break;
        int damage = Mathf.RoundToInt(self.attack * 0.8f);
        target.TakeDamage(damage, self, false, true);
        target.TryAddChaos(1);
        FloatingTipGenerator.Instance?.ShowTipAtObject(target.transform, $"直伤:{damage}");
        yield return new WaitForSeconds(0.5f);
    }
    // 3.负面手 技能一
    private IEnumerator Debuff_1(Enemy self)
    {
        foreach (var ally in CharacterManager.Instance.fieldCharacters)
        {
            if (ally == null) continue;
            ally.TryAddChaos(1);
        }
        yield return new WaitForSeconds(0.5f);
    }
    // 3.负面手 技能二
    private IEnumerator Debuff_2(Enemy self)
    {
        var target = CharacterManager.Instance.GetCharacterByRand();
        if (target == null) yield break;
        target.AddState(StateType.Attract, self, 1);
        FloatingTipGenerator.Instance?.ShowTipAtObject(target.transform, "瞩目+1");
        yield return new WaitForSeconds(0.5f);
    }
    // 4.群体自爆手 技能一
    private IEnumerator Exploder_1(Enemy self)
    {
        if(self.hasStartExploded) yield break;
        yield return new WaitForSeconds(0.5f);
    }
    // 4.群体自爆手 技能二（倒计时为零时自爆）
    private IEnumerator Exploder_2(Enemy self)
    {
        if (self.hasStartExploded) yield break;
        foreach (var ally in CharacterManager.Instance.fieldCharacters)
        {
            if (ally == null) continue;
            int damage = Mathf.RoundToInt(self.attack * 0.6f);
            ally.TakeDamage(damage, self, false, true);
            ally.TryAddChaos(1);
            FloatingTipGenerator.Instance?.ShowTipAtObject(ally.transform, $"自爆伤害:{damage}");
        }
        yield return new WaitForSeconds(0.5f);
    }
    // 5.Dot施加手 技能一
    private IEnumerator DotMaster_1(Enemy self)
    {
        var targets = CharacterManager.Instance.fieldCharacters;
        if (targets == null || targets.Count == 0) yield break;
        var target = targets[Random.Range(0, targets.Count)];
        if (target == null) yield break;
        target.AddState(StateType.Poison, self, 2, 2);
        FloatingTipGenerator.Instance?.ShowTipAtObject(target.transform, "鸩毒+2");
        yield return new WaitForSeconds(0.5f);
    }
    // 5.Dot施加手 技能二
    private IEnumerator DotMaster_2(Enemy self)
    {
        foreach (var ally in CharacterManager.Instance.fieldCharacters)
        {
            if (ally == null) continue;
            State poison = ally.GetState(StateType.Poison);
            int poisonStacks = poison != null ? poison.Stacks : 0;
            float chance = 0.2f * poisonStacks;
            if (poisonStacks > 0 && Random.value < chance)
            {
                ally.TryAddChaos(1);
                int reduce = Mathf.FloorToInt(poisonStacks * 0.5f);
                if (reduce > 0) poison.Stacks -= reduce;
                FloatingTipGenerator.Instance?.ShowTipAtObject(ally.transform, $"鸩毒-{reduce}");
            }
            else if (poisonStacks > 0)
            {
                ally.AddState(StateType.Poison, self, 1);
                FloatingTipGenerator.Instance?.ShowTipAtObject(ally.transform, "鸩毒+1");
            }
        }
        yield return new WaitForSeconds(0.5f);
    }
}
