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
    Exploder2,      // 群体自爆手技能二
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
        if (m_remainingCooldown > 0 && (owner == null || !owner.ShouldBypassSkillCooldown(this)))
        {
            return false;
        }

        if (owner == null) return true;
        if (!owner.CanUseEnemySkill(this)) return false;

        // 自爆兵技能一：启动后不可用；启动前始终可用
        if (enemySkillType == EnemySkillType.Exploder)
        {
            if (owner.explodeState == ExplodeType.hasStarted || owner.explodeState == ExplodeType.ReadyToBurst)
            {
                return false;
            }
            return true;
        }

        // 自爆兵技能二：启动后不可用；启动前以 extraData1 概率可用（默认0.5，即配合技能一实现75%/25%选择）
        if (enemySkillType == EnemySkillType.Exploder2)
        {
            if (owner.explodeState == ExplodeType.hasStarted || owner.explodeState == ExplodeType.ReadyToBurst)
            {
                return false;
            }
            float chance = extraData1 > 0f ? extraData1 : 0.5f;
            return Random.value < chance;
        }

        return true;
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
            case EnemySkillType.Exploder2:
                yield return ExploderBurstSkill(self);
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
        NotifyDragonRageSkillIfNeeded(self);
        StartCooldown();
        yield return new WaitForSeconds(0.5f); // 技能执行后的小间隔
        if (enteredSkillCamera)
        {
            yield return CinemachineCameraManager.Instance?.TransitionOutOfSkillCamera();
        }
    }

    private void NotifyDragonRageSkillIfNeeded(Enemy self)
    {
        if (self is DragonBossEnemy dragon && IsDragonRageSkillType(enemySkillType))
        {
            dragon.NotifyRageSkillUsed();
        }
    }

    private static bool IsDragonRageSkillType(EnemySkillType skillType)
    {
        return skillType == EnemySkillType.DragonDotRage
            || skillType == EnemySkillType.DragonDirectRage
            || skillType == EnemySkillType.DragonChaosRage;
    }

    // 1.护盾手 技能一：给当前HP绝对值最低的敌方单位施加护盾（护盾比例=extraData1，默认15%最大HP）
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
            float shieldRatio = extraData1 > 0f ? extraData1 : 0.15f;
            int shield = Mathf.RoundToInt(self.maxHP * shieldRatio);
            target.AddShield(shield);
        }
        yield break;
    }
    // 1.护盾手 技能二：对我方全体造成少量伤害，并使我方全体行动延后（延后比例=extraData1，默认40%）
    private IEnumerator ShieldSupport_2(Enemy self)
    {
        var allies = new List<Character>(CharacterManager.Instance.fieldCharacters);
        NotifyDamageSkillUsed(self, allies);
        float delayRatio = extraData1 > 0f ? extraData1 : 0.4f;
        foreach (var ally in allies)
        {
            if (ally == null) continue;
            var damageInfo = DamageCounter.CountDamage(self, ally, this, DamageType.Physical);
            ally.TakeDamage(damageInfo);
            ally.ChangeActionValue(ally.currentActionValue + ally.BaseActionValue * delayRatio);
        }
        yield break;
    }
    // 2.单体攻击手：随机对我方单体造成伤害，并施加混沌值（混沌层数=extraData1，默认2）
    private IEnumerator SingleAttack(Enemy self)
    {
        var target = CharacterManager.Instance.GetCharacterByRand(self);
        if (target == null) yield break;
        NotifyDamageSkillUsed(self, new List<UnitCombatant> { target });
        var damageInfo = DamageCounter.CountDamage(self, target, this, DamageType.Physical, true);
        target.TakeDamage(damageInfo);
        int chaosAmount = Mathf.Max(1, Mathf.RoundToInt(extraData1 > 0f ? extraData1 : 2f));
        target.TryAddChaos(chaosAmount);
        yield break;
    }
    // 3.负面手 技能一：对我方全体施加混沌值（混沌层数=extraData1，默认2）
    private IEnumerator Debuff_1(Enemy self)
    {
        var allies = new List<Character>(CharacterManager.Instance.fieldCharacters);
        int chaosAmount = Mathf.Max(1, Mathf.RoundToInt(extraData1 > 0f ? extraData1 : 2f));
        foreach (var ally in allies)
        {
            if (ally == null) continue;
            ally.TryAddChaos(chaosAmount);
        }
        yield break;
    }
    // 3.负面手 技能二：随机对我方单体造成伤害并施加瞩目（瞩目层数=extraData1，默认1）
    private IEnumerator Debuff_2(Enemy self)
    {
        var target = CharacterManager.Instance.GetCharacterByRand(self);
        if (target == null) yield break;
        // 造成伤害（使用 skillCoef 和 skillBase）
        NotifyDamageSkillUsed(self, new List<UnitCombatant> { target });
        var damageInfo = DamageCounter.CountDamage(self, target, this, DamageType.Physical, true);
        target.TakeDamage(damageInfo);
        int attractStacks = Mathf.Max(1, Mathf.RoundToInt(extraData1 > 0f ? extraData1 : 1f));
        target.AddState(StateType.Attract, self, 1, attractStacks);
        yield break;
    }
    // 4.群体自爆手 技能一：引爆倒计时 — 空过回合，提示启动失败
    private IEnumerator ExploderSkill(Enemy self)
    {
        if (self == null) yield break;

        // 已经启动或已爆炸，不再执行
        if (self.explodeState == ExplodeType.hasStarted || self.explodeState == ExplodeType.ReadyToBurst)
        {
            yield break;
        }

        FloatingTipGenerator.Instance?.ShowTipAtObject(self.transform, $"{self.combatantName}启动失败...", true);
    }

    private IEnumerator StartExploder(Enemy self)
    {
        if (self.explodeState == ExplodeType.hasStarted || self.explodeState == ExplodeType.ReadyToBurst)
        {
            yield break;
        }

        FloatingTipGenerator.Instance?.ShowTipAtObject(self.transform, $"{self.combatantName}启动自爆");
        // extraData2：自爆倒计时层数，默认2层
        int countdownStacks = Mathf.Max(1, Mathf.RoundToInt(extraData2 > 0f ? extraData2 : 2f));
        self.AddState(StateType.ExploderProcess, self, countdownStacks);
        yield break;
    }

    /// <summary>自爆兵技能二：致命自爆 — 启动自爆流程（施加倒计时状态），倒计时归零后自动爆炸</summary>
    private IEnumerator ExploderBurstSkill(Enemy self)
    {
        if (self == null) yield break;

        if (self.explodeState == ExplodeType.hasStarted || self.explodeState == ExplodeType.ReadyToBurst)
        {
            // 已启动或已就绪，不再重复启动
            yield break;
        }

        // 启动自爆流程
        yield return StartExploder(self);
    }

    private IEnumerator ExploderBurst(Enemy self)
    {
        var allies = new List<Character>(CharacterManager.Instance.fieldCharacters);
        NotifyDamageSkillUsed(self, allies);
        foreach (var ally in allies)
        {
            if (ally == null) continue;
            var damageInfo = DamageCounter.CountDamage(self, ally, skillCoef, (float)skillBase, DamageType.Physical, true, false, false);
            ally.TakeDamage(damageInfo);
            // 施加混沌值（混沌层数=extraData3，默认3）
            int chaosAmount = Mathf.Max(0, Mathf.RoundToInt(extraData3 > 0f ? extraData3 : 3f));
            if (chaosAmount > 0)
            {
                ally.TryAddChaos(chaosAmount);
            }
        }
        self.TakeDamage(new UnitCombatant.DamageInfo(self.maxHP, self).AsTrueDamage());
        yield break;
    }
    // 5.Dot施加手 技能一：随机对我方单体造成少量伤害并施加鸩毒（鸩毒层数=extraData1，默认2）
    private IEnumerator DotMaster_1(Enemy self)
    {
        var target = CharacterManager.Instance.GetCharacterByRand(self);
        if (target == null) yield break;
        // 造成伤害
        NotifyDamageSkillUsed(self, new List<UnitCombatant> { target });
        var damageInfo = DamageCounter.CountDamage(self, target, this, DamageType.Physical, true);
        target.TakeDamage(damageInfo);
        int poisonStacks = Mathf.Max(1, Mathf.RoundToInt(extraData1 > 0f ? extraData1 : 2f));
        target.AddState(StateType.Poison, self, 99, poisonStacks);
        yield break;
    }
    // 5.Dot施加手 技能二：对我方全体造成少量伤害，并独立判定鸩毒转化
    //   extraData1=判定概率系数（默认0.2），extraData2=削减比例（默认0.5），extraData3=失败额外施加层数（默认1）
    private IEnumerator DotMaster_2(Enemy self)
    {
        var allies = new List<Character>(CharacterManager.Instance.fieldCharacters);
        // 全体伤害
        NotifyDamageSkillUsed(self, allies);
        foreach (var ally in allies)
        {
            if (ally == null) continue;
            var damageInfo = DamageCounter.CountDamage(self, ally, this, DamageType.Physical, true);
            ally.TakeDamage(damageInfo);
        }
        // 独立判定鸩毒转化
        float chanceCoeff = extraData1 > 0f ? extraData1 : 0.2f;
        float reduceRatio = extraData2 > 0f ? extraData2 : 0.5f;
        int failExtraStacks = Mathf.Max(1, Mathf.RoundToInt(extraData3 > 0f ? extraData3 : 1f));
        foreach (var ally in allies)
        {
            if (ally == null) continue;
            State poison = ally.GetState(StateType.Poison);
            int poisonStacks = poison != null ? poison.Stacks : 0;
            float chance = chanceCoeff * poisonStacks;
            if (poisonStacks > 0 && Random.value < chance)
            {
                ally.TryAddChaos(1);
                int reduce = Mathf.FloorToInt(poisonStacks * reduceRatio);
                if (reduce > 0) poison.Stacks -= reduce;
            }
            else if (poisonStacks > 0)
            {
                ally.AddState(StateType.Poison, self, 99, failExtraStacks);
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

        // 初始兵卒：造成伤害后向前推进
        ChessPawnEnemy pawn = self as ChessPawnEnemy;
        if (pawn != null)
        {
            Character target = CharacterManager.Instance.GetCharacterByRand(self);
            if (target != null)
            {
                NotifyDamageSkillUsed(self, new List<UnitCombatant> { target });
                var damageInfo = DamageCounter.CountDamage(self, target, this, DamageType.Physical);
                target.TakeDamage(damageInfo);
            }

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

        foreach (var ally in allies)
        {
            if (ally == null || ally.IsDead) continue;
            var damageInfo = DamageCounter.CountDamage(self, ally, skillCoef, skillBase, DamageType.Physical, false, false, false);
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
        queen.RegisterSummonedPawn(pawn);
        pawn.ChangeActionValue(pawn.BaseActionValue, false);
        EnemyManager.Instance?.RegisterEnemy(pawn);
        TurnManager.Instance?.InsertCombatant(pawn);

        queen.AddPrestige(1);
        // CD 由 EnemySkillBase.Execute() 末尾的 StartCooldown() 统一处理
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

        // 蓄力前标记王棋/车棋
        List<Character> fieldCharacters = GetAliveFieldCharacters(queen);
        if (fieldCharacters.Count < 2)
        {
            FloatingTipGenerator.Instance?.ShowTipAtObject(queen.transform, "场上目标不足，无法蓄力");
            yield break;
        }

        queen.MarkChessKingAndRook();

        // 标记蓄力
        queen.SetChargingThroneAssault(true);
        FloatingTipGenerator.Instance?.ShowTipAtObject(queen.transform, $"{queen.combatantName}蓄力中...");
    }

    private IEnumerator ExecuteThroneAssaultStrike(ChessQueenEnemy queen)
    {
        queen.SetChargingThroneAssault(false);
        // CD 由 EnemySkillBase.Execute() 末尾的 StartCooldown() 统一处理

        // 找到有王棋状态的角色
        Character kingTarget = FindCharacterWithState(StateType.ChessKingMark);
        if (kingTarget == null)
        {
            // 王棋丢失，攻击落空
            BattleDialogEvents.Raise(BattleDialogEventType.ChessQueenThroneMissed, enemy: queen);
            FloatingTipGenerator.Instance?.ShowTipAtObject(queen.transform, "由于王棋丢失，皇后的攻击落空了");
            // 仍然施加力竭和行动延后（蓄力后的代价）
            queen.DelayActionValue(1f);
            queen.AddState(StateType.ChessExhaustion, queen, 1, 1);
            yield break;
        }

        NotifyDamageSkillUsed(queen, new List<UnitCombatant> { kingTarget });
        var damageInfo = DamageCounter.CountDamage(queen, kingTarget, skillCoef, skillBase, DamageType.Physical, false, false, false);
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

            var damageInfo = DamageCounter.CountDamage(queen, target, totalSkillCoef, skillBase, DamageType.Physical, false, false, false);
            target.TakeDamage(damageInfo);
        }

        FloatingTipGenerator.Instance?.ShowTipAtObject(queen.transform, $"{queen.combatantName}消耗{prestigeConsumed}层威望");
    }

    /// <summary>新技能：皇后单体爆发 — 对随机单体造成伤害，施加1点混沌值（预告关卡缩小版）</summary>
    private IEnumerator ChessQueenSingleBurst(Enemy self)
    {
        ChessQueenEnemy queen = self as ChessQueenEnemy;
        if (queen == null) yield break;

        Character target = CharacterManager.Instance.GetCharacterByRand(self);
        if (target == null) yield break;

        NotifyDamageSkillUsed(queen, new List<UnitCombatant> { target });
        var damageInfo = DamageCounter.CountDamage(queen, target, skillCoef, skillBase, DamageType.Physical, true, false, false);
        target.TakeDamage(damageInfo);
        int chaosAmount = ResolveExtraInt(extraData1, 1);
        target.TryAddChaos(chaosAmount);
    }

    // ============ 辅助方法 ============

    private static int ResolveExtraInt(float field, int defaultValue, int minValue = 1)
    {
        int resolved = field > 0f ? Mathf.RoundToInt(field) : defaultValue;
        return Mathf.Max(minValue, resolved);
    }

    private static float ResolveExtraFloat(float field, float defaultValue)
    {
        return field > 0f ? field : defaultValue;
    }

    private static int ResolveReinforcedInt(float baseField, float reinforcedField, int baseDefault, int reinforcedDefault, bool isReinforced)
    {
        float field = isReinforced ? (reinforcedField > 0f ? reinforcedField : baseField) : baseField;
        return ResolveExtraInt(field, isReinforced ? reinforcedDefault : baseDefault);
    }

    private static float ResolveReinforcedFloat(float baseField, float reinforcedField, float baseDefault, float reinforcedDefault, bool isReinforced)
    {
        float field = isReinforced ? (reinforcedField > 0f ? reinforcedField : baseField) : baseField;
        return ResolveExtraFloat(field, isReinforced ? reinforcedDefault : baseDefault);
    }

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
        return queen != null ? queen.level : 1;
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

        bool isReinforced = dragon.ReinforceLevel >= 1;
        int breathStacks = ResolveReinforcedInt(extraData3, extraData4, 1, 2, isReinforced);
        float coef = isReinforced ? (extraData1 > 0f ? extraData1 : 1.5f) : skillCoef;
        float baseDmg = isReinforced ? (extraData2 > 0f ? extraData2 : 0f) : skillBase;

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

        BattleDialogEvents.Raise(BattleDialogEventType.DragonDotPurify, enemy: self);

        int clearCount = ResolveReinforcedInt(extraData3, extraData4, 1, 2, dragon.ReinforceLevel >= 1);
        if (EnemyManager.Instance == null) yield break;

        IReadOnlyList<Enemy> aliveEnemies = EnemyManager.Instance.AliveEnemies;
        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            Enemy enemy = aliveEnemies[i];
            if (enemy == null || enemy.IsDead) continue;

            int cleared = 0;
            List<StateType> debuffTypes = CollectRandomDebuffTypes(enemy.States, clearCount);
            for (int t = 0; t < debuffTypes.Count; t++)
            {
                RemoveDebuffType(enemy, debuffTypes[t]);
                cleared++;
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
            int flameStacks = ResolveExtraInt(extraData3, 1);
            ally.AddState(StateType.EternalFlame, self, 99, flameStacks);
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

        var target = CharacterManager.Instance.GetCharacterByRand(self);
        if (target == null) yield break;

        bool isReinforced = dragon.ReinforceLevel >= 1;
        float coef = isReinforced ? (extraData1 > 0f ? extraData1 : 2.5f) : skillCoef;
        float baseDmg = isReinforced ? (extraData2 > 0f ? extraData2 : 0f) : skillBase;

        NotifyDamageSkillUsed(self, new List<UnitCombatant> { target });
        var damageInfo = DamageCounter.CountDamage(self, target, coef, baseDmg, DamageType.Physical, true, false, false);
        target.TakeDamage(damageInfo);

        if (isReinforced && extraData3 > 0f)
        {
            int attractStacks = ResolveExtraInt(extraData3, 1);
            target.AddState(StateType.Attract, self, 1, attractStacks);
        }
    }

    /// <summary>直伤龙技能二：龙威 — 单体施加瞩目</summary>
    private IEnumerator DragonDirectSkill2(Enemy self)
    {
        DragonBossEnemy dragon = self as DragonBossEnemy;
        if (dragon == null) yield break;

        var target = CharacterManager.Instance.GetCharacterByRand(self);
        if (target == null) yield break;

        int attractStacks = ResolveReinforcedInt(extraData3, extraData4, 1, 2, dragon.ReinforceLevel >= 1);
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
        List<Character> candidates = new List<Character>(CharacterManager.Instance.fieldCharacters);
        Character target = CombatDamageTracker.SelectHighestDamageDealer(candidates);
        if (target == null)
        {
            target = CharacterManager.Instance.GetCharacterByRand(self);
        }
        if (target == null) yield break;

        target.AddState(StateType.InstantDeath, self, 1, 1);
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
            if (ally.GetState(StateType.InstantDeath) == null) continue;

            ApplyInstantDeathTo(ally);
        }

        yield break;
    }

    private static void ApplyInstantDeathTo(Character ally)
    {
        State instantDeath = ally.GetState(StateType.InstantDeath);
        if (instantDeath != null)
        {
            ally.RemoveState(instantDeath);
        }

        int lethalDamage = ally.currentHP + ally.currentShield;
        if (lethalDamage <= 0)
        {
            return;
        }

        ally.TakeDamage(new UnitCombatant.DamageInfo(lethalDamage, ally)
            .AsTrueDamage()
            .BypassingShield()
            .WithState(StateType.InstantDeath));
        FloatingTipGenerator.Instance?.ShowTipAtObject(ally.transform, "即死触发！");
    }

    // --- 混沌龙 ---

    /// <summary>混沌龙技能一：混沌吐息 — 单体混沌+行动延后</summary>
    private IEnumerator DragonChaosSkill1(Enemy self)
    {
        DragonBossEnemy dragon = self as DragonBossEnemy;
        if (dragon == null) yield break;

        var target = CharacterManager.Instance.GetCharacterByRand(self);
        if (target == null) yield break;

        bool isReinforced = dragon.ReinforceLevel >= 1;
        int chaosAmount = ResolveReinforcedInt(extraData3, extraData4, 1, 2, isReinforced);
        float speedStepPenalty = ResolveReinforcedFloat(extraData1, extraData2, 15f, 30f, isReinforced);

        target.TryAddChaos(chaosAmount);
        target.ChangeActionValue(target.currentActionValue + speedStepPenalty);

        if (isReinforced && skillCoef > 0f)
        {
            float chaosCoef = skillCoef * target.ChaosValue;
            if (chaosCoef > 0f)
            {
                NotifyDamageSkillUsed(self, new List<UnitCombatant> { target });
                var damageInfo = DamageCounter.CountDamage(self, target, chaosCoef, skillBase, DamageType.Physical, false, false, false);
                target.TakeDamage(damageInfo);
            }
        }

        FloatingTipGenerator.Instance?.ShowTipAtObject(target.transform, $"混沌+{chaosAmount} 速度步长+{speedStepPenalty}");
    }

    /// <summary>混沌龙技能二：时间扭曲 — 自身与随机友方行动提前</summary>
    private IEnumerator DragonChaosSkill2(Enemy self)
    {
        DragonBossEnemy dragon = self as DragonBossEnemy;
        if (dragon == null) yield break;

        BattleDialogEvents.Raise(BattleDialogEventType.DragonChaosLeap, enemy: self);

        bool isReinforced = dragon.ReinforceLevel >= 1;
        float advanceRatio = ResolveReinforcedFloat(extraData1, extraData3, 0.4f, 0.5f, isReinforced);

        // 自身行动提前
        self.ChangeActionValue(self.currentActionValue - self.BaseActionValue * advanceRatio);

        // 随机一名敌方单体行动提前
        var target = CharacterManager.Instance.GetCharacterByRand(self);
        if (target != null)
        {
            target.ChangeActionValue(target.currentActionValue - target.BaseActionValue * advanceRatio);
            FloatingTipGenerator.Instance?.ShowTipAtObject(target.transform, "行动提前");

            // 强化后：目标下次输出提升
            if (isReinforced)
            {
                float damageBoost = ResolveExtraFloat(extraData2, 0.2f);
                target.AddState(StateType.DamageChange, self, 1, 1, true, damageBoost);
            }
        }

        FloatingTipGenerator.Instance?.ShowTipAtObject(self.transform, "时间扭曲");
    }

    /// <summary>混沌龙暴怒：混沌风暴 — 全体加混沌，混沌值达到5的角色额外直伤</summary>
    private IEnumerator DragonChaosRage(Enemy self)
    {
        BattleDialogEvents.Raise(BattleDialogEventType.DragonChaosUltimate, enemy: self as Enemy);

        var allies = new List<Character>(CharacterManager.Instance.fieldCharacters);
        NotifyDamageSkillUsed(self, allies);
        int chaosAmount = ResolveExtraInt(extraData2, 3);

        foreach (var ally in allies)
        {
            if (ally == null || ally.IsDead) continue;
            ally.TryAddChaos(chaosAmount);

            if (ally.ChaosValue >= ally.MaxChaosValueConst)
            {
                float extraCoef = extraData1 > 0f ? extraData1 : 2f;
                var damageInfo = DamageCounter.CountDamage(self, ally, extraCoef, skillBase, DamageType.Physical, false, false, false);
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
        var target = CharacterManager.Instance.GetCharacterByRand(self);
        if (target == null) yield break;

        NotifyDamageSkillUsed(self, new List<UnitCombatant> { target });
        var damageInfo = DamageCounter.CountDamage(self, target, skillCoef, skillBase, DamageType.Physical, true, false, false);
        target.TakeDamage(damageInfo);
        int vulnerableStacks = ResolveExtraInt(extraData1, 2);
        target.AddState(StateType.Vulnerable, self, 99, vulnerableStacks);
        yield break;
    }

    /// <summary>亮剑-剑舞连斩：随机单体多段微伤，每段施加混沌</summary>
    private IEnumerator SwordsmanDance(Enemy self)
    {
        int hitCount = ResolveExtraInt(extraData1, 3);
        int chaosPerHit = ResolveExtraInt(extraData2, 1);
        for (int i = 0; i < hitCount; i++)
        {
            var target = CharacterManager.Instance.GetCharacterByRand(self);
            if (target == null || target.IsDead) continue;

            NotifyDamageSkillUsed(self, new List<UnitCombatant> { target });
            var damageInfo = DamageCounter.CountDamage(self, target, skillCoef, skillBase, DamageType.Physical, true, false, false);
            target.TakeDamage(damageInfo);
            target.TryAddChaos(chaosPerHit);
            yield return new WaitForSeconds(0.15f);
        }
        yield break;
    }

    /// <summary>防御-铁壁格挡：自身1层抵御，随机单体2混沌</summary>
    private IEnumerator SwordsmanBlock(Enemy self)
    {
        int resistStacks = ResolveExtraInt(extraData1, 1);
        self.AddState(StateType.Resist, self, 99, resistStacks);

        var target = CharacterManager.Instance.GetCharacterByRand(self);
        if (target != null)
        {
            int chaosAmount = ResolveExtraInt(extraData2, 2);
            target.TryAddChaos(chaosAmount);
        }
        yield break;
    }

    /// <summary>防御-稳固架势：回复微量HP，清除负面状态</summary>
    private IEnumerator SwordsmanSteady(Enemy self)
    {
        float healRatio = ResolveExtraFloat(extraData1, 0.05f);
        int healAmount = Mathf.RoundToInt(self.maxHP * healRatio);
        self.Heal(healAmount);

        int clearCount = ResolveExtraInt(extraData2, 1);
        RemoveRandomDebuffTypes(self, clearCount);
        yield break;
    }

    /// <summary>游击-扰敌步法：全体微量伤害，自身下次行动提前</summary>
    private IEnumerator SwordsmanDisrupt(Enemy self)
    {
        var allies = new List<Character>(CharacterManager.Instance.fieldCharacters);
        NotifyDamageSkillUsed(self, allies);
        foreach (var ally in allies)
        {
            if (ally == null || ally.IsDead) continue;
            var damageInfo = DamageCounter.CountDamage(self, ally, this, DamageType.Physical);
            ally.TakeDamage(damageInfo);
        }

        float advanceRatio = ResolveExtraFloat(extraData2, 0.5f);
        self.ChangeActionValue(self.currentActionValue - self.BaseActionValue * advanceRatio);
        yield break;
    }

    /// <summary>游击-迅影刺击：行动条最前单位低伤+行动延后（无视嘲讽）</summary>
    private IEnumerator SwordsmanShadow(Enemy self)
    {
        Character fastest = TurnManager.Instance != null
            ? TurnManager.Instance.GetNextActingFieldCharacter()
            : null;
        if (fastest == null)
        {
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
        }

        if (fastest == null) yield break;

        NotifyDamageSkillUsed(self, new List<UnitCombatant> { fastest });
        var damageInfo = DamageCounter.CountDamage(self, fastest, this, DamageType.Physical);
        fastest.TakeDamage(damageInfo);

        float delayRatio = ResolveExtraFloat(extraData2, 0.3f);
        float delayAmount = fastest.BaseActionValue * delayRatio;
        fastest.ChangeActionValue(fastest.currentActionValue + delayAmount);
        yield break;
    }

    private static List<StateType> CollectRandomDebuffTypes(IReadOnlyList<State> states, int pickCount)
    {
        List<StateType> availableTypes = new List<StateType>();
        if (states == null || pickCount <= 0)
        {
            return availableTypes;
        }

        for (int i = 0; i < states.Count; i++)
        {
            State state = states[i];
            if (state == null || !state.isDebuff)
            {
                continue;
            }

            if (!availableTypes.Contains(state.stateType))
            {
                availableTypes.Add(state.stateType);
            }
        }

        for (int i = availableTypes.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            StateType temp = availableTypes[i];
            availableTypes[i] = availableTypes[swapIndex];
            availableTypes[swapIndex] = temp;
        }

        if (availableTypes.Count > pickCount)
        {
            availableTypes.RemoveRange(pickCount, availableTypes.Count - pickCount);
        }

        return availableTypes;
    }

    private static void RemoveDebuffType(UnitCombatant target, StateType debuffType)
    {
        if (target == null)
        {
            return;
        }

        for (int i = target.States.Count - 1; i >= 0; i--)
        {
            State state = target.States[i];
            if (state != null && state.isDebuff && state.stateType == debuffType)
            {
                target.RemoveState(state);
            }
        }
    }

    private static void RemoveRandomDebuffTypes(UnitCombatant target, int removeTypeCount)
    {
        if (target == null || removeTypeCount <= 0)
        {
            return;
        }

        List<StateType> debuffTypes = CollectRandomDebuffTypes(target.States, removeTypeCount);
        for (int i = 0; i < debuffTypes.Count; i++)
        {
            RemoveDebuffType(target, debuffTypes[i]);
        }
    }
}
