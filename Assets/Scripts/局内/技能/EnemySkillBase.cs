using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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

    private static void NotifyDamageSkillUsed(UnitCombatant unitCombatant, IReadOnlyList<UnitCombatant> damagedUnits)
    {
        State.NotifyDamageSkillUsed(unitCombatant, damagedUnits);
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
            if (target != null && target.currentHP > enemy.currentHP)
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
        var allies = new List<Character>(CharacterManager.Instance.fieldCharacters);
        NotifyDamageSkillUsed(self, allies);
        foreach (var ally in allies)
        {
            if (ally == null) continue;
            var damageInfo = DamageCounter.CountDamage(self, ally, this, DamageType.Physical);
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
        NotifyDamageSkillUsed(self, new List<UnitCombatant> { target });
        var damageInfo = DamageCounter.CountDamage(self, target, this, DamageType.Physical, true);
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
        var allies = new List<Character>(CharacterManager.Instance.fieldCharacters);
        NotifyDamageSkillUsed(self, allies);
        foreach (var ally in allies)
        {
            if (ally == null) continue;
            var damageInfo = DamageCounter.CountDamage(self, ally, 0.6f, 0f, DamageType.Physical, true, false, false);
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

    // ============ 棋局Boss技能 ============

    /// <summary>棋子行动：初始兵卒推进，召唤兵卒回血</summary>
    private IEnumerator ChessPawnAction(Enemy self)
    {
        // 召唤兵卒：为皇后回血
        ChessSummonedPawnEnemy summonedPawn = self as ChessSummonedPawnEnemy;
        if (summonedPawn != null)
        {
            summonedPawn.HealQueen(extraData1);
            yield break;
        }

        // 初始兵卒：向前推进
        ChessPawnEnemy pawn = self as ChessPawnEnemy;
        if (pawn != null)
        {
            yield return pawn.AdvancePawn();
        }
    }

    /// <summary>技能一：混沌横冲 — 对全体造成伤害+混沌</summary>
    private IEnumerator ChessQueenChaosCharge(Enemy self)
    {
        ChessQueenEnemy queen = self as ChessQueenEnemy;
        if (queen == null) yield break;

        int chaosAmount = Mathf.Max(1, Mathf.RoundToInt(extraData1 > 0f ? extraData1 : 2f));
        var allies = new List<Character>(CharacterManager.Instance.fieldCharacters);
        NotifyDamageSkillUsed(self, allies);

        float prestigeBonus = queen.GetPrestigeDamageBonus();
        float totalCoef = skillCoef * prestigeBonus;

        foreach (var ally in allies)
        {
            if (ally == null || ally.IsDead) continue;
            var damageInfo = DamageCounter.CountDamage(self, ally, totalCoef, skillBase, DamageType.Physical, true, false, false);
            ally.TakeDamage(damageInfo);
            ally.TryAddChaos(chaosAmount);
        }
    }

    /// <summary>技能二：兵卒召唤 — 召唤一个兵卒，皇后获得1层威望</summary>
    private IEnumerator ChessQueenSummonPawn(Enemy self)
    {
        ChessQueenEnemy queen = self as ChessQueenEnemy;
        if (queen == null) yield break;

        // 获取召唤位置
        LevelCharacterSpawner spawner = LevelCharacterSpawner.Instance;
        if (spawner == null)
        {
            Debug.LogWarning("[EnemySkillBase] LevelCharacterSpawner 不可用");
            yield break;
        }

        if (!spawner.TryGetRandomAvailableEnemyStandPosition(out int standPosition))
        {
            Debug.LogWarning("[EnemySkillBase] 没有可用的敌人生成位置");
            yield break;
        }

        // 获取生成prefab
        EnemyRosterData rosterData = GetSummonPawnRosterData(queen);
        if (rosterData == null) yield break;

        GameObject summonPrefab = rosterData.PrefabOverride;
        if (summonPrefab == null)
        {
            ChessSummonedPawnEnemy fallback = GetSummonPawnPrefabFallback(queen);
            if (fallback != null) summonPrefab = fallback.gameObject;
        }

        if (summonPrefab == null)
        {
            Debug.LogWarning($"[EnemySkillBase] {queen.combatantName} 缺少兵卒召唤 prefab");
            yield break;
        }

        // 生成兵卒
        Vector3 spawnPos = queen.transform.position + new Vector3(-1f, 0f, 0f);
        Quaternion spawnRot = queen.transform.rotation;

        GameObject spawnedObject = Object.Instantiate(summonPrefab, spawnPos, spawnRot, queen.transform.parent);
        ChessSummonedPawnEnemy pawn = spawnedObject.GetComponent<ChessSummonedPawnEnemy>();
        if (pawn == null)
        {
            Debug.LogError("[EnemySkillBase] 召唤的兵卒 prefab 缺少 ChessSummonedPawnEnemy 组件", spawnedObject);
            Object.Destroy(spawnedObject);
            spawner.ReleaseEnemyStandPosition(standPosition);
            yield break;
        }

        pawn.ConfigureAsSummonedPawn(queen, rosterData, standPosition, GetSummonPawnLevel(queen));
        pawn.ChangeActionValue(pawn.BaseActionValue, false);
        EnemyManager.Instance?.RegisterEnemy(pawn);
        TurnManager.Instance?.InsertCombatant(pawn);

        queen.AddPrestige(1);
        queen.StartSummonCooldown();
        FloatingTipGenerator.Instance?.ShowTipAtObject(queen.transform, $"{queen.combatantName}召唤兵卒");
    }

    /// <summary>技能三：后袭王座 — 蓄力后造成伤害+力竭</summary>
    private IEnumerator ChessQueenThroneAssault(Enemy self)
    {
        ChessQueenEnemy queen = self as ChessQueenEnemy;
        if (queen == null) yield break;

        // 如果正在蓄力，执行蓄力攻击
        if (queen.IsChargingThroneAssault)
        {
            yield return ExecuteThroneAssaultStrike(queen);
            yield break;
        }

        // 否则标记蓄力（王棋/车棋已在进入二阶段时标记）
        queen.SetChargingThroneAssault(true);
        FloatingTipGenerator.Instance?.ShowTipAtObject(queen.transform, $"{queen.combatantName}蓄力中...");
    }

    private IEnumerator ExecuteThroneAssaultStrike(ChessQueenEnemy queen)
    {
        queen.SetChargingThroneAssault(false);
        queen.StartThroneAssaultCooldown();

        // 找到有王棋状态的角色
        Character kingTarget = FindCharacterWithState(StateType.ChessKingMark);
        if (kingTarget == null)
        {
            // 如果没有王棋，随机选一个
            List<Character> alive = GetAliveFieldCharacters(queen);
            if (alive.Count == 0) yield break;
            kingTarget = alive[0];
        }

        float prestigeBonus = queen.GetPrestigeDamageBonus();
        float totalCoef = skillCoef * prestigeBonus;

        NotifyDamageSkillUsed(queen, new List<UnitCombatant> { kingTarget });
        var damageInfo = DamageCounter.CountDamage(queen, kingTarget, totalCoef, skillBase, DamageType.Physical, true, false, false);
        kingTarget.TakeDamage(damageInfo);
        Debug.Log($"[EnemySkillBase] {queen.combatantName}对{kingTarget.combatantName}造成了{damageInfo.Damage}点伤害");
        // 行动延后100%
        queen.DelayActionValue(1f);

        // 给自己施加力竭
        queen.AddState(StateType.ChessExhaustion, queen, 1, 1);
    }

    /// <summary>技能四：王权加冕 — 消耗威望，对全体造成极大伤害</summary>
    private IEnumerator ChessQueenCoronation(Enemy self)
    {
        ChessQueenEnemy queen = self as ChessQueenEnemy;
        if (queen == null) yield break;

        int prestigeConsumed = queen.ConsumeAllPrestige();
        float bonusPerPrestige = extraData1 > 0f ? extraData1 : 0.2f;
        float totalSkillCoef = skillCoef * (1f + prestigeConsumed * bonusPerPrestige);

        List<Character> targets = GetAliveFieldCharacters(queen);
        NotifyDamageSkillUsed(queen, targets);
        for (int i = 0; i < targets.Count; i++)
        {
            Character target = targets[i];
            if (target == null) continue;

            var damageInfo = DamageCounter.CountDamage(queen, target, totalSkillCoef, skillBase, DamageType.Physical, true, false, false);
            target.TakeDamage(damageInfo);
        }

        FloatingTipGenerator.Instance?.ShowTipAtObject(queen.transform, $"{queen.combatantName}消耗{prestigeConsumed}层威望");
    }

    // ============ 辅助方法 ============

    private List<Character> GetAliveFieldCharacters(ChessQueenEnemy queen)
    {
        List<Character> aliveCharacters = new List<Character>();
        if (CharacterManager.Instance == null) return aliveCharacters;

        for (int i = 0; i < CharacterManager.Instance.fieldCharacters.Count; i++)
        {
            Character character = CharacterManager.Instance.fieldCharacters[i];
            if (character != null && !character.IsDead)
                aliveCharacters.Add(character);
        }
        return aliveCharacters;
    }

    private Character FindCharacterWithState(StateType stateType)
    {
        if (CharacterManager.Instance == null) return null;

        for (int i = 0; i < CharacterManager.Instance.fieldCharacters.Count; i++)
        {
            Character character = CharacterManager.Instance.fieldCharacters[i];
            if (character == null || character.IsDead) continue;
            if (character.GetState(stateType) != null) return character;
        }
        return null;
    }

    private void TryTriggerCastling(ChessQueenEnemy queen)
    {
        if (!Commander.GetInstance().TryConsumeCastlingOpportunity()) return;

        Character kingCharacter = FindCharacterWithState(StateType.ChessKingMark);
        Character rookCharacter = FindCharacterWithState(StateType.ChessRookMark);
        if (kingCharacter == null || rookCharacter == null || kingCharacter == rookCharacter) return;

        if (CharacterManager.Instance.SwapFieldCharacters(kingCharacter, rookCharacter))
        {
            rookCharacter.AddState(StateType.Resist, queen, 99, 1);
            FloatingTipGenerator.Instance?.ShowTipAtObject(queen.transform, "王车易位");
        }
    }

    private static EnemyRosterData GetSummonPawnRosterData(ChessQueenEnemy queen)
    {
        return queen != null ? queen.summonPawnData : null;
    }

    private static int GetSummonPawnLevel(ChessQueenEnemy queen)
    {
        return queen != null ? queen.summonPawnLevel : 1;
    }

    private static ChessSummonedPawnEnemy GetSummonPawnPrefabFallback(ChessQueenEnemy queen)
    {
        return queen != null ? queen.summonPawnPrefabFallback : null;
    }
}
