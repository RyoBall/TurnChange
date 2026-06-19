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
    ChessQueenThroneAssault,     // [已废弃] 后袭王座 — 完整版皇后使用
    ChessQueenCoronation,        // [已废弃] 王权加冕 — 完整版皇后使用
    ChessQueenSingleBurst,       // 皇后单体爆发（预告关卡缩小版）
    // 龙Boss技能
    DragonDotSkill1,     // Dot龙技能一：龙息喷吐
    DragonDotSkill2,     // Dot龙技能二：净化
    DragonDotRage,       // Dot龙暴怒：无尽炼狱
    DragonDirectSkill1,  // 直伤龙技能一：龙爪
    DragonDirectSkill2,  // 直伤龙技能二：龙威
    DragonDirectRage,    // 直伤龙暴怒：死亡标记
    DragonChaosSkill1,   // 混沌龙技能一：混沌吐息
    DragonChaosSkill2,   // 混沌龙技能二：时间扭曲
    DragonChaosRage,     // 混沌龙暴怒：混沌风暴
    // 西洋剑客技能
    SwordsmanThrust,     // 易伤突刺
    SwordsmanDance,      // 剑舞连斩
    SwordsmanBlock,      // 铁壁格挡
    SwordsmanSteady,     // 稳固架势
    SwordsmanDisrupt,    // 扰敌步法
    SwordsmanShadow     // 迅影刺击
}
public enum targetType
{
    Character,
    Self,
    None
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
    [Header("目标类型")]
    [SerializeField] private targetType targetType;

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
        bool enteredSkillCamera = false;
        if(targetType == targetType.Character)
        {
            yield return CinemachineCameraManager.Instance?.TransitionIntoSkillCamera(ManagedCameraType.Help);
            enteredSkillCamera = true;
        }
        else if(targetType == targetType.Self)
        {
            yield return CinemachineCameraManager.Instance?.TransitionIntoSkillCamera(ManagedCameraType.Attack);
            enteredSkillCamera = true;
        }
        switch (enemySkillType)
        {
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
            case EnemySkillType.ChessQueenSingleBurst:
                yield return ChessQueenSingleBurst(self);
                break;
            // 龙Boss技能
            case EnemySkillType.DragonDotSkill1:
                yield return DragonDotSkill1(self);
                break;
            case EnemySkillType.DragonDotSkill2:
                yield return DragonDotSkill2(self);
                break;
            case EnemySkillType.DragonDotRage:
                yield return DragonDotRage(self);
                break;
            case EnemySkillType.DragonDirectSkill1:
                yield return DragonDirectSkill1(self);
                break;
            case EnemySkillType.DragonDirectSkill2:
                yield return DragonDirectSkill2(self);
                break;
            case EnemySkillType.DragonDirectRage:
                yield return DragonDirectRage(self);
                break;
            case EnemySkillType.DragonChaosSkill1:
                yield return DragonChaosSkill1(self);
                break;
            case EnemySkillType.DragonChaosSkill2:
                yield return DragonChaosSkill2(self);
                break;
            case EnemySkillType.DragonChaosRage:
                yield return DragonChaosRage(self);
                break;
            // 西洋剑客技能
            case EnemySkillType.SwordsmanThrust:
                yield return SwordsmanThrust(self);
                break;
            case EnemySkillType.SwordsmanDance:
                yield return SwordsmanDance(self);
                break;
            case EnemySkillType.SwordsmanBlock:
                yield return SwordsmanBlock(self);
                break;
            case EnemySkillType.SwordsmanSteady:
                yield return SwordsmanSteady(self);
                break;
            case EnemySkillType.SwordsmanDisrupt:
                yield return SwordsmanDisrupt(self);
                break;
            case EnemySkillType.SwordsmanShadow:
                yield return SwordsmanShadow(self);
                break;
        }
        StartCooldown();
        yield return new WaitForSeconds(0.5f); // 技能执行后的小间隔
        if (enteredSkillCamera)
        {
            yield return CinemachineCameraManager.Instance?.TransitionOutOfSkillCamera();
        }
    }

    // 1.护盾手 技能一：给当前HP最低的敌人（包括自己）加护盾
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
        target.AddState(StateType.Poison, self, 99, 2);
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
                ally.AddState(StateType.Poison, self,99, 2);
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
        // 预告关卡使用技能自身CD系统，不调用内部CD
        if (!queen.IsPreviewBoss)
        {
            queen.StartSummonCooldown();
        }
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
            yield return CinemachineCameraManager.Instance?.TransitionIntoSkillCamera(ManagedCameraType.Help);
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

    /// <summary>新技能：皇后单体爆发 — 对随机单体造成伤害，施加1点混沌值（预告关卡缩小版）</summary>
    private IEnumerator ChessQueenSingleBurst(Enemy self)
    {
        ChessQueenEnemy queen = self as ChessQueenEnemy;
        if (queen == null) yield break;

        Character target = CharacterManager.Instance.GetCharacterByRand();
        if (target == null) yield break;

        float prestigeBonus = queen.GetPrestigeDamageBonus();
        float totalCoef = skillCoef * prestigeBonus;

        NotifyDamageSkillUsed(queen, new List<UnitCombatant> { target });
        var damageInfo = DamageCounter.CountDamage(queen, target, totalCoef, skillBase, DamageType.Physical, true, false, false);
        target.TakeDamage(damageInfo);
        target.TryAddChaos(1);
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

    // ============ 龙Boss技能 ============

    // --- Dot龙 ---

    /// <summary>Dot龙技能一：龙息喷吐 — 全体直伤+龙息Dot</summary>
    private IEnumerator DragonDotSkill1(Enemy self)
    {
        DragonBossEnemy dragon = self as DragonBossEnemy;
        if (dragon == null) yield break;

        int breathStacks = dragon.ReinforceLevel >= 1 ? 3 : 2;
        float coef = dragon.ReinforceLevel >= 1 ? (extraData1 > 0f ? extraData1 : 1.5f) : skillCoef;
        float baseDmg = dragon.ReinforceLevel >= 1 ? (extraData2 > 0f ? extraData2 : 0f) : skillBase;

        var allies = new List<Character>(CharacterManager.Instance.fieldCharacters);
        NotifyDamageSkillUsed(self, allies);
        foreach (var ally in allies)
        {
            if (ally == null || ally.IsDead) continue;
            var damageInfo = DamageCounter.CountDamage(self, ally, coef, baseDmg, DamageType.Physical, true, false, false);
            ally.TakeDamage(damageInfo);
            ally.AddState(StateType.DragonBreath, self, 99, breathStacks);
        }
    }

    /// <summary>Dot龙技能二：净化 — 清除友方负面状态</summary>
    private IEnumerator DragonDotSkill2(Enemy self)
    {
        DragonBossEnemy dragon = self as DragonBossEnemy;
        if (dragon == null) yield break;

        int clearCount = dragon.ReinforceLevel >= 1 ? 2 : 1;
        if (EnemyManager.Instance == null) yield break;

        IReadOnlyList<Enemy> aliveEnemies = EnemyManager.Instance.AliveEnemies;
        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            Enemy enemy = aliveEnemies[i];
            if (enemy == null || enemy.IsDead) continue;

            int cleared = 0;
            for (int j = enemy.States.Count - 1; j >= 0 && cleared < clearCount; j--)
            {
                State state = enemy.States[j];
                if (state != null && state.isDebuff)
                {
                    enemy.RemoveState(state);
                    cleared++;
                }
            }
        }
        FloatingTipGenerator.Instance?.ShowTipAtObject(self.transform, $"{self.combatantName}净化友方负面状态");
    }

    /// <summary>Dot龙暴怒：无尽炼狱 — 全体施加不灭之焰</summary>
    private IEnumerator DragonDotRage(Enemy self)
    {
        BattleDialogEvents.Raise(BattleDialogEventType.DragonDotUltimate, enemy: self as Enemy);

        var allies = new List<Character>(CharacterManager.Instance.fieldCharacters);
        foreach (var ally in allies)
        {
            if (ally == null || ally.IsDead) continue;
            ally.AddState(StateType.EternalFlame, self, 99, 1);
        }
        FloatingTipGenerator.Instance?.ShowTipAtObject(self.transform, $"{self.combatantName}无尽炼狱！");
        yield break;
    }

    // --- 直伤龙 ---

    /// <summary>直伤龙技能一：龙爪 — 单体直伤</summary>
    private IEnumerator DragonDirectSkill1(Enemy self)
    {
        DragonBossEnemy dragon = self as DragonBossEnemy;
        if (dragon == null) yield break;

        var target = CharacterManager.Instance.GetCharacterByRand();
        if (target == null) yield break;

        float coef = dragon.ReinforceLevel >= 1 ? (extraData1 > 0f ? extraData1 : 2.5f) : skillCoef;
        float baseDmg = dragon.ReinforceLevel >= 1 ? (extraData2 > 0f ? extraData2 : 0f) : skillBase;

        NotifyDamageSkillUsed(self, new List<UnitCombatant> { target });
        var damageInfo = DamageCounter.CountDamage(self, target, coef, baseDmg, DamageType.Physical, true, false, false);
        target.TakeDamage(damageInfo);
    }

    /// <summary>直伤龙技能二：龙威 — 单体施加瞩目</summary>
    private IEnumerator DragonDirectSkill2(Enemy self)
    {
        DragonBossEnemy dragon = self as DragonBossEnemy;
        if (dragon == null) yield break;

        var target = CharacterManager.Instance.GetCharacterByRand();
        if (target == null) yield break;

        int attractStacks = dragon.ReinforceLevel >= 1 ? 2 : 1;
        target.AddState(StateType.Attract, self, 1, attractStacks);
        FloatingTipGenerator.Instance?.ShowTipAtObject(target.transform, $"瞩目 x{attractStacks}");
    }

    /// <summary>直伤龙暴怒：死亡标记 — 前置施加即死，蓄力后触发</summary>
    private IEnumerator DragonDirectRage(Enemy self)
    {
        DirectDragonEnemy dragon = self as DirectDragonEnemy;
        if (dragon == null) yield break;

        // 如果正在蓄力，执行即死
        if (dragon.IsChargingRage)
        {
            dragon.SetChargingRage(false);
            BattleDialogEvents.Raise(BattleDialogEventType.DragonInstantDeathTriggered, enemy: self as Enemy);
            yield return ExecuteInstantDeath();
            yield break;
        }

        // 否则施加即死状态并蓄力
        BattleDialogEvents.Raise(BattleDialogEventType.DragonInstantDeathWarning, enemy: self as Enemy);
        var target = CharacterManager.Instance.GetCharacterByRand();
        if (target == null) yield break;

        target.AddState(StateType.InstantDeath, self, 1,1);
        dragon.SetChargingRage(true);
        FloatingTipGenerator.Instance?.ShowTipAtObject(target.transform, "即死标记");
        FloatingTipGenerator.Instance?.ShowTipAtObject(self.transform, $"{self.combatantName}蓄力中...");
    }

    private IEnumerator ExecuteInstantDeath()
    {
        var allies = new List<Character>(CharacterManager.Instance.fieldCharacters);
        for (int i = allies.Count - 1; i >= 0; i--)
        {
            Character ally = allies[i];
            if (ally == null || ally.IsDead) continue;
            State instantDeath = ally.GetState(StateType.InstantDeath);
            if (instantDeath != null)
            {
                ally.TakeDamage(new UnitCombatant.DamageInfo(ally.maxHP, ally).AsTrueDamage());
                FloatingTipGenerator.Instance?.ShowTipAtObject(ally.transform, "即死触发！");
            }
        }
        yield break;
    }

    // --- 混沌龙 ---

    /// <summary>混沌龙技能一：混沌吐息 — 单体混沌+行动延后</summary>
    private IEnumerator DragonChaosSkill1(Enemy self)
    {
        DragonBossEnemy dragon = self as DragonBossEnemy;
        if (dragon == null) yield break;

        var target = CharacterManager.Instance.GetCharacterByRand();
        if (target == null) yield break;

        int chaosAmount = dragon.ReinforceLevel >= 1 ? 2 : 1;
        float delayRatio = dragon.ReinforceLevel >= 1 ? 0.5f : 0.2f;

        target.TryAddChaos(chaosAmount);
        target.ChangeActionValue(target.currentActionValue + target.BaseActionValue * delayRatio);
        FloatingTipGenerator.Instance?.ShowTipAtObject(target.transform, $"混沌+{chaosAmount} 行动延后");
    }

    /// <summary>混沌龙技能二：时间扭曲 — 自身与随机友方行动提前</summary>
    private IEnumerator DragonChaosSkill2(Enemy self)
    {
        DragonBossEnemy dragon = self as DragonBossEnemy;
        if (dragon == null) yield break;

        float advanceRatio = dragon.ReinforceLevel >= 1 ? 0.5f : 0.4f;

        // 自身行动提前
        self.ChangeActionValue(self.currentActionValue - self.BaseActionValue * advanceRatio);

        // 随机一名敌方单体行动提前
        var target = CharacterManager.Instance.GetCharacterByRand();
        if (target != null)
        {
            target.ChangeActionValue(target.currentActionValue - target.BaseActionValue * advanceRatio);
            FloatingTipGenerator.Instance?.ShowTipAtObject(target.transform, "行动提前");

            // 强化后：下次伤害提升20%
            if (dragon.ReinforceLevel >= 1)
            {
                target.AddState(StateType.DamageChange, self, 1, 1,true,0.2f);
            }
        }

        FloatingTipGenerator.Instance?.ShowTipAtObject(self.transform, "时间扭曲");
    }

    /// <summary>混沌龙暴怒：混沌风暴 — 全体3混沌，震慑者额外直伤</summary>
    private IEnumerator DragonChaosRage(Enemy self)
    {
        BattleDialogEvents.Raise(BattleDialogEventType.DragonChaosUltimate, enemy: self as Enemy);

        var allies = new List<Character>(CharacterManager.Instance.fieldCharacters);
        NotifyDamageSkillUsed(self, allies);

        foreach (var ally in allies)
        {
            if (ally == null || ally.IsDead) continue;
            ally.TryAddChaos(3);

            // 若已处于震慑，额外造成大量直伤
            if (ally.GetState(StateType.Daze) != null)
            {
                float extraCoef = extraData1 > 0f ? extraData1 : 2f;
                var damageInfo = DamageCounter.CountDamage(self, ally, extraCoef, skillBase, DamageType.Physical, true, false, false);
                ally.TakeDamage(damageInfo);
            }
        }
        FloatingTipGenerator.Instance?.ShowTipAtObject(self.transform, "混沌风暴！");
        yield break;
    }

    // ============ 西洋剑客技能 ============

    /// <summary>亮剑-易伤突刺：单体中等直伤+2层易伤</summary>
    private IEnumerator SwordsmanThrust(Enemy self)
    {
        var target = CharacterManager.Instance.GetCharacterByRand();
        if (target == null) yield break;

        NotifyDamageSkillUsed(self, new List<UnitCombatant> { target });
        var damageInfo = DamageCounter.CountDamage(self, target, skillCoef, skillBase, DamageType.Physical, true, false, false);
        target.TakeDamage(damageInfo);
        target.AddState(StateType.Vulnerable, self, 99, 2);
        yield break;
    }

    /// <summary>亮剑-剑舞连斩：随机单体3段微伤，每段1混沌</summary>
    private IEnumerator SwordsmanDance(Enemy self)
    {
        for (int i = 0; i < 3; i++)
        {
            var target = CharacterManager.Instance.GetCharacterByRand();
            if (target == null || target.IsDead) continue;

            float microCoef = extraData1 > 0f ? extraData1 : 0.3f;
            NotifyDamageSkillUsed(self, new List<UnitCombatant> { target });
            var damageInfo = DamageCounter.CountDamage(self, target, microCoef, 0f, DamageType.Physical, true, false, false);
            target.TakeDamage(damageInfo);
            target.TryAddChaos(1);
            yield return new WaitForSeconds(0.15f);
        }
        yield break;
    }

    /// <summary>防御-铁壁格挡：自身1层抵御，随机单体2混沌</summary>
    private IEnumerator SwordsmanBlock(Enemy self)
    {
        self.AddState(StateType.Resist, self, 99, 1);

        var target = CharacterManager.Instance.GetCharacterByRand();
        if (target != null)
        {
            target.TryAddChaos(2);
        }
        yield break;
    }

    /// <summary>防御-稳固架势：回复微量HP，清除1个负面</summary>
    private IEnumerator SwordsmanSteady(Enemy self)
    {
        float healRatio = extraData1 > 0f ? extraData1 : 0.05f;
        int healAmount = Mathf.RoundToInt(self.maxHP * healRatio);
        self.Heal(healAmount);

        // 清除自身1个负面状态
        for (int i = self.States.Count - 1; i >= 0; i--)
        {
            State state = self.States[i];
            if (state != null && state.isDebuff)
            {
                self.RemoveState(state);
                break;
            }
        }
        yield break;
    }

    /// <summary>游击-扰敌步法：全体微量伤害，自身下次行动提前50%</summary>
    private IEnumerator SwordsmanDisrupt(Enemy self)
    {
        float microCoef = extraData1 > 0f ? extraData1 : 0.2f;
        var allies = new List<Character>(CharacterManager.Instance.fieldCharacters);
        NotifyDamageSkillUsed(self, allies);
        foreach (var ally in allies)
        {
            if (ally == null || ally.IsDead) continue;
            var damageInfo = DamageCounter.CountDamage(self, ally, microCoef, 0f, DamageType.Physical, true, false, false);
            ally.TakeDamage(damageInfo);
        }

        // 自身行动提前50%
        self.ChangeActionValue(self.currentActionValue - self.BaseActionValue * 0.5f);
        yield break;
    }

    /// <summary>游击-迅影刺击：行动条最前单位低伤+行动延后30%（无视嘲讽）</summary>
    private IEnumerator SwordsmanShadow(Enemy self)
    {
        // 找到行动条最前的角色
        Character fastest = null;
        float lowestActionValue = float.MaxValue;
        for (int i = 0; i < CharacterManager.Instance.fieldCharacters.Count; i++)
        {
            Character c = CharacterManager.Instance.fieldCharacters[i];
            if (c == null || c.IsDead) continue;
            if (c.currentActionValue < lowestActionValue)
            {
                lowestActionValue = c.currentActionValue;
                fastest = c;
            }
        }

        if (fastest == null) yield break;

        float lowCoef = extraData1 > 0f ? extraData1 : 0.5f;
        NotifyDamageSkillUsed(self, new List<UnitCombatant> { fastest });
        var damageInfo = DamageCounter.CountDamage(self, fastest, lowCoef, 0f, DamageType.Physical, true, false, false);
        fastest.TakeDamage(damageInfo);

        // 行动延后30%
        fastest.ChangeActionValue(fastest.currentActionValue + fastest.BaseActionValue * 0.3f);
        yield break;
    }
}
