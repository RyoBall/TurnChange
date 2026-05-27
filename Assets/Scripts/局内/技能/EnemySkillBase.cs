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
    Exploder,      // 群体自爆手技能一
    NoneNone,      // 群体自爆手技能二
    DotMaster_1,     // 持续伤害Dot施加手技能一
    DotMaster_2,      // 持续伤害Dot施加手技能二
    ChessPawnAction,
    ChessQueenChaosCharge,
    ChessQueenSummonPawn,
    ChessQueenThroneAssault,
    ChessQueenCoronation
}
[CreateAssetMenu(fileName = "NewEnemySkill", menuName = "技能/EnemySkill"), System.Serializable]
public class EnemySkillBase : SkillBase
{
    public EnemySkillType enemySkillType;
    [Header("额外参数")]
    public float extraData1;
    public float extraData2;
    public float extraData3;
    public float extraData4;
    [Header("冷却回合")]
    [Min(0)]
    public int cooldownTurns;

    private int m_remainingCooldown;

    public int RemainingCooldown => m_remainingCooldown;

    private static void NotifyDamageSkillUsed(UnitCombatant unitCombatant)
    {
        State.NotifyDamageSkillUsed(unitCombatant);
    }

    public bool CanUse(Enemy owner)
    {
        return m_remainingCooldown <= 0 && (owner == null || owner.CanUseEnemySkill(this));
    }

    public void TickCooldown()
    {
        if (m_remainingCooldown > 0)
        {
            m_remainingCooldown--;
        }
    }

    private void StartCooldown()
    {
        if (cooldownTurns > 0)
        {
            m_remainingCooldown = Mathf.Max(m_remainingCooldown, cooldownTurns);
        }
    }

    public override IEnumerator Execute(UnitCombatant unitCombatant)
    {
        if (unitCombatant == null) yield break;
        Enemy self = unitCombatant as Enemy;
        if (self == null) yield break;
        if (!CanUse(self)) yield break;

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
            case EnemySkillType.Exploder:
                yield return ExploderSkill(self);
                break;
            case EnemySkillType.DotMaster_1:
                yield return DotMaster_1(self);
                break;
            case EnemySkillType.DotMaster_2:
                yield return DotMaster_2(self);
                break;
            case EnemySkillType.ChessPawnAction:
                yield return ChessPawnAction(self);
                break;
            case EnemySkillType.ChessQueenChaosCharge:
                yield return ChessQueenChaosCharge(self);
                break;
            case EnemySkillType.ChessQueenSummonPawn:
                yield return ChessQueenSummonPawn(self);
                break;
            case EnemySkillType.ChessQueenThroneAssault:
                yield return ChessQueenThroneAssault(self);
                break;
            case EnemySkillType.ChessQueenCoronation:
                yield return ChessQueenCoronation(self);
                break;
        }
        StartCooldown();
        yield return new WaitForSeconds(0.5f); // 技能执行后的小间隔
    }

    // 1.护盾手 技能一
    private IEnumerator ShieldSupport_1(Enemy self)
    {
        Enemy target = null;
        foreach (Enemy enemy in EnemyManager.Instance.AliveEnemies)
        {
            if (target == null || target.currentHP > enemy.currentHP)
            {
                target = enemy;
            }
        }
        if (target != null)
        {
            int shield = Mathf.RoundToInt(self.maxHP * 0.1f);
            target.AddShield(shield);
        }
        yield break;
    }
    // 1.护盾手 技能二
    private IEnumerator ShieldSupport_2(Enemy self)
    {
        NotifyDamageSkillUsed(self);
        var allies = new List<Character>(CharacterManager.Instance.fieldCharacters);
        foreach (var ally in allies)
        {
            if (ally == null) continue;
            var damageInfo = DamageCounter.CountDamage(self, ally, this);
            ally.TakeDamage(damageInfo);
            ally.ChangeActionValue(ally.currentActionValue + ally.BaseActionValue * 0.2f);
        }
        yield break;
    }
    // 2.单体攻击手
    private IEnumerator SingleAttack(Enemy self)
    {
        var target = CharacterManager.Instance.GetCharacterByRand();
        if (target == null) yield break;
        NotifyDamageSkillUsed(self);
        var damageInfo = DamageCounter.CountDamage(self, target, this, true);
        target.TakeDamage(damageInfo);
        target.TryAddChaos(1);
        yield break;
    }
    // 3.负面手 技能一
    private IEnumerator Debuff_1(Enemy self)
    {
        var allies = new List<Character>(CharacterManager.Instance.fieldCharacters);
        foreach (var ally in allies)
        {
            if (ally == null) continue;
            ally.TryAddChaos(1);
        }
        yield break;
    }
    // 3.负面手 技能二
    private IEnumerator Debuff_2(Enemy self)
    {
        var target = CharacterManager.Instance.GetCharacterByRand();
        if (target == null) yield break;
        target.AddState(StateType.Attract, self, 1);
        yield break;
    }
    // 4.群体自爆手
    private IEnumerator ExploderSkill(Enemy self)
    {
        if (self == null)
        {
            yield break;
        }

        if (self.explodeState == ExplodeType.Normal || self.explodeState == ExplodeType.None)
        {
            if (Random.value < 0.5f)
            {
                yield return StartExploder(self);
                yield break;
            }
            FloatingTipGenerator.Instance?.ShowTipAtObject(self.transform, $"{self.combatantName}启动失败...",true);
        }
        else if (self.explodeState == ExplodeType.hasStarted)
        {
            yield break;
        }
        else if (self.explodeState == ExplodeType.ReadyToBurst)
        {
            yield return ExploderBurst(self);
        }
    }

    private IEnumerator StartExploder(Enemy self)
    {
        if (self.explodeState == ExplodeType.hasStarted || self.explodeState == ExplodeType.ReadyToBurst)
        {
            yield break;
        }

        FloatingTipGenerator.Instance?.ShowTipAtObject(self.transform, $"{self.combatantName}启动自爆");
        self.AddState(StateType.ExploderProcess, self, 2);
        yield break;
    }

    private IEnumerator ExploderBurst(Enemy self)
    {
        NotifyDamageSkillUsed(self);
        var allies = new List<Character>(CharacterManager.Instance.fieldCharacters);
        foreach (var ally in allies)
        {
            if (ally == null) continue;
            var damageInfo = DamageCounter.CountDamage(self, ally, 0.6f, 0f, true, false, false);
            ally.TakeDamage(damageInfo);
            ally.TryAddChaos(1);
        }
        self.TakeDamage(new UnitCombatant.DamageInfo(self.maxHP,self).AsTrueDamage());
        yield break;
    }
    // 5.Dot施加手 技能一
    private IEnumerator DotMaster_1(Enemy self)
    {
        var target = CharacterManager.Instance.GetCharacterByRand();
        if (target == null) yield break;
        target.AddState(StateType.Poison, self, 99, 2, 0.2f);
        yield break;
    }
    // 5.Dot施加手 技能二
    private IEnumerator DotMaster_2(Enemy self)
    {
        var allies = new List<Character>(CharacterManager.Instance.fieldCharacters);
        foreach (var ally in allies)
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
            }
            else if (poisonStacks > 0)
            {
                ally.AddState(StateType.Poison, self,99, 2,0.2f);
            }
        }
        yield break;
    }

    private IEnumerator ChessPawnAction(Enemy self)
    {
        ChessBossEnemy chessBoss = self as ChessBossEnemy;
        if (chessBoss != null)
        {
            yield return chessBoss.ExecuteChessPawnAction(this);
        }
    }

    private IEnumerator ChessQueenChaosCharge(Enemy self)
    {
        int chaosAmount = Mathf.Max(1, Mathf.RoundToInt(extraData1 > 0f ? extraData1 : 2f));
        NotifyDamageSkillUsed(self);
        var allies = new List<Character>(CharacterManager.Instance.fieldCharacters);
        foreach (var ally in allies)
        {
            if (ally == null || ally.IsDead)
            {
                continue;
            }

            var damageInfo = DamageCounter.CountDamage(self, ally, skillCoef, skillBase, true, false, false);
            ally.TakeDamage(damageInfo);
            ally.TryAddChaos(chaosAmount);
        }

        yield break;
    }

    private IEnumerator ChessQueenSummonPawn(Enemy self)
    {
        ChessBossEnemy chessBoss = self as ChessBossEnemy;
        if (chessBoss != null)
        {
            yield return chessBoss.ExecuteChessQueenSummonPawn(this);
        }
    }

    private IEnumerator ChessQueenThroneAssault(Enemy self)
    {
        ChessBossEnemy chessBoss = self as ChessBossEnemy;
        if (chessBoss != null)
        {
            yield return chessBoss.ExecuteChessQueenThroneAssault(this);
        }
    }

    private IEnumerator ChessQueenCoronation(Enemy self)
    {
        ChessBossEnemy chessBoss = self as ChessBossEnemy;
        if (chessBoss != null)
        {
            yield return chessBoss.ExecuteChessQueenCoronation(this);
        }
    }
}
