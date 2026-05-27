using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public enum ChessBossUnitRole
{
    None,
    PromotionPawn,
    QueenBoss,
    SummonedPawn
}

[Serializable]
public class ChessBossPendingData
{
    public bool enabled;
    public string bossGroupId = "chess-boss";
    public ChessBossUnitRole unitRole = ChessBossUnitRole.None;
    public bool startHiddenUntilPhaseTwo;
    public EnemyRosterData summonPawnData;
    public int summonPawnLevel = 1;
    public Vector3 pawnAdvanceOffset = new Vector3(0f, 0f, -0.8f);
    public float pawnAdvanceDuration = 0.2f;
    public int pawnPromotionSteps = 5;
    public float summonedPawnHealRatio = 0.03f;
    public bool immuneToDaze = true;
    public bool immuneToTaunt = true;

    public ChessBossPendingData Clone()
    {
        return new ChessBossPendingData
        {
            enabled = enabled,
            bossGroupId = bossGroupId,
            unitRole = unitRole,
            startHiddenUntilPhaseTwo = startHiddenUntilPhaseTwo,
            summonPawnData = summonPawnData,
            summonPawnLevel = summonPawnLevel,
            pawnAdvanceOffset = pawnAdvanceOffset,
            pawnAdvanceDuration = pawnAdvanceDuration,
            pawnPromotionSteps = pawnPromotionSteps,
            summonedPawnHealRatio = summonedPawnHealRatio,
            immuneToDaze = immuneToDaze,
            immuneToTaunt = immuneToTaunt
        };
    }
}

public class ChessBossEnemy : Enemy
{
    [Header("棋局Boss默认配置")]
    [SerializeField] private ChessBossUnitRole chessRole = ChessBossUnitRole.None;
    [SerializeField] private string bossGroupId = "chess-boss";
    [SerializeField] private bool startHiddenUntilPhaseTwo;
    [SerializeField] private EnemyRosterData summonPawnData;
    [SerializeField, Min(1)] private int summonPawnLevel = 1;
    [SerializeField] private ChessBossEnemy summonPawnPrefabFallback;
    [SerializeField] private Transform[] chessSummonPoints;
    [SerializeField] private Vector3 pawnAdvanceOffset = new Vector3(0f, 0f, -0.8f);
    [SerializeField] private float pawnAdvanceDuration = 0.2f;
    [SerializeField, Min(1)] private int pawnPromotionSteps = 5;
    [SerializeField, Range(0f, 1f)] private float summonedPawnHealRatio = 0.03f;
    [SerializeField] private bool immuneToDaze = true;
    [SerializeField] private bool immuneToTaunt = true;

    private readonly HashSet<ChessBossEnemy> m_summonedChessPawns = new HashSet<ChessBossEnemy>();
    private ChessBossEnemy m_linkedQueen;
    private bool m_hasTriggeredPromotion;
    private bool m_hasUsedChessCoronation;
    private bool m_suppressLinkedQueenDeathNotify;
    private int m_pawnAdvanceCount;
    private int m_chessPrestigeStacks;
    private int m_pendingCastlingOpportunities;
    private int m_summonedPawnKillCounter;
    private int m_chessKingStandPosition = int.MinValue;
    private int m_chessRookStandPosition = int.MinValue;
    private int m_exhaustedTurnsRemaining;
    private int m_exhaustionAccumulatedDamage;
    private bool m_exhaustionWindowGrantedCastling;
    private bool m_exhaustionExpiresOnNextOwnTurn;
    private float m_pendingTurnEndDelayRatio;

    private bool IsChessBossUnit => chessRole != ChessBossUnitRole.None;
    public bool StartsHiddenUntilPhaseTwo => IsChessQueenBoss && startHiddenUntilPhaseTwo;
    public bool IsChessQueenBoss => chessRole == ChessBossUnitRole.QueenBoss;
    public bool IsChessPromotionPawn => chessRole == ChessBossUnitRole.PromotionPawn;
    public bool IsChessSummonedPawn => chessRole == ChessBossUnitRole.SummonedPawn;
    public int ChessPrestigeStacks => m_chessPrestigeStacks;
    public override bool ShouldRegisterAtBattleStart => !StartsHiddenUntilPhaseTwo;

    protected override void OnConfigureFromBattleSpawnData(BattleEnemySpawnData spawnData)
    {
        base.OnConfigureFromBattleSpawnData(spawnData);
        if (spawnData == null || spawnData.chessBossData == null || !spawnData.chessBossData.enabled)
        {
            return;
        }

        ApplyChessBossPendingData(spawnData.chessBossData);
    }

    public override void InitializeFromPendingLevelData(PendingBattleLevelData pendingData, IReadOnlyList<Enemy> spawnedEnemies)
    {
        base.InitializeFromPendingLevelData(pendingData, spawnedEnemies);
        if (!IsChessBossUnit || spawnedEnemies == null)
        {
            return;
        }

        if (!IsChessQueenBoss)
        {
            m_linkedQueen = FindQueenByGroup(spawnedEnemies);
        }
    }

    protected override void OnTurnStartBeforeStateSettlement()
    {
        base.OnTurnStartBeforeStateSettlement();
        AdvanceChessExhaustionOnOwnTurnStart();
    }

    public override float ConsumeTurnEndActionValue()
    {
        float nextActionValue = BaseActionValue * (1f + Mathf.Max(0f, m_pendingTurnEndDelayRatio));
        m_pendingTurnEndDelayRatio = 0f;
        return nextActionValue;
    }

    public override bool CanUseEnemySkill(EnemySkillBase skill)
    {
        if (!base.CanUseEnemySkill(skill))
        {
            return false;
        }

        switch (skill.enemySkillType)
        {
            case EnemySkillType.ChessPawnAction:
                return IsChessPromotionPawn || (IsChessSummonedPawn && GetChessQueenController() != null && !GetChessQueenController().IsDead);
            case EnemySkillType.ChessQueenChaosCharge:
                return IsChessQueenBoss && GetAliveFieldCharacters().Count > 0;
            case EnemySkillType.ChessQueenSummonPawn:
                return IsChessQueenBoss && ResolveSummonPawnPrefab() != null && summonPawnData != null;
            case EnemySkillType.ChessQueenThroneAssault:
                return IsChessQueenBoss && TryResolveChessTargets(out _, out _);
            case EnemySkillType.ChessQueenCoronation:
                return ShouldUseChessCoronation() && GetAliveFieldCharacters().Count > 0;
            default:
                return true;
        }
    }

    protected override EnemySkillBase GetForcedSkillForTurn()
    {
        EnemySkillBase coronationSkill = GetSkillInstance(EnemySkillType.ChessQueenCoronation);
        if (coronationSkill != null && coronationSkill.CanUse(this) && ShouldUseChessCoronation())
        {
            return coronationSkill;
        }

        return base.GetForcedSkillForTurn();
    }

    public override void Die()
    {
        bool wasChessQueen = IsChessQueenBoss;
        ChessBossEnemy queen = IsChessSummonedPawn ? GetChessQueenController() : null;
        base.Die();

        if (wasChessQueen)
        {
            CleanupLinkedChessUnitsOnDeath();
        }

        if (queen != null && !m_suppressLinkedQueenDeathNotify)
        {
            queen.NotifySummonedPawnKilled(this);
        }
    }

    public override void TakeDamage(DamageInfo damageInfo)
    {
        int previousHp = currentHP;
        int previousShield = currentShield;
        base.TakeDamage(damageInfo);

        if (!IsChessQueenBoss || m_exhaustedTurnsRemaining <= 0)
        {
            return;
        }

        int hpLoss = Mathf.Max(0, previousHp - currentHP);
        int shieldLoss = Mathf.Max(0, previousShield - currentShield);
        RegisterDamageDuringExhaustion(hpLoss + shieldLoss);
    }

    protected override bool CanReceiveState(StateType stateType, UnitCombatant giver)
    {
        bool isImmuneState = (stateType == StateType.Daze && immuneToDaze) || (stateType == StateType.Taunt && immuneToTaunt);
        if (!isImmuneState)
        {
            return base.CanReceiveState(stateType, giver);
        }

        FloatingTipGenerator.Instance?.ShowTipAtObject(transform, $"{combatantName}免疫{StateDictionaryManager.GetStateName(stateType)}");
        return false;
    }

    public IEnumerator ExecuteChessPawnAction(EnemySkillBase skill)//棋子行动逻辑
    {
        ChessBossEnemy queen = GetChessQueenController();
        if (IsChessSummonedPawn && queen != null && !queen.IsDead)
        {
            float healRatio = skill != null && skill.extraData1 > 0f ? skill.extraData1 : summonedPawnHealRatio;
            int healAmount = Mathf.Max(1, Mathf.RoundToInt(queen.maxHP * healRatio));
            queen.Heal(healAmount);
            FloatingTipGenerator.Instance?.ShowTipAtObject(transform, $"{combatantName}为皇后回复{healAmount}");
        }

        if (IsChessPromotionPawn)
        {
            m_pawnAdvanceCount = Mathf.Min(pawnPromotionSteps, m_pawnAdvanceCount + 1);
            Vector3 targetPosition = transform.position + pawnAdvanceOffset;
            if (pawnAdvanceDuration > 0f)
            {
                yield return transform.DOMove(targetPosition, pawnAdvanceDuration).SetEase(Ease.InOutSine).WaitForCompletion();
            }
            else
            {
                transform.position = targetPosition;
            }

            FloatingTipGenerator.Instance?.ShowTipAtObject(transform, $"{combatantName}推进至第{m_pawnAdvanceCount}格");
            if (m_pawnAdvanceCount >= pawnPromotionSteps)
            {
                TryPromoteChessQueen();
            }
        }
    }

    public IEnumerator ExecuteChessQueenSummonPawn(EnemySkillBase skill)
    {
        if (!IsChessQueenBoss || summonPawnData == null)
        {
            yield break;
        }

        GameObject summonPrefab = ResolveSummonPawnPrefab();
        if (summonPrefab == null)
        {
            Debug.LogWarning($"[ChessBossEnemy] {combatantName} 缺少兵卒召唤 prefab");
            yield break;
        }

        ResolveChessSummonPose(out Vector3 summonPosition, out Quaternion summonRotation);
        GameObject spawnedObject = Instantiate(summonPrefab, summonPosition, summonRotation, transform.parent);
        Enemy pawn = spawnedObject.GetComponent<Enemy>();
        if (pawn == null)
        {
            Debug.LogError("[ChessBossEnemy] 召唤的兵卒 prefab 缺少 Enemy 组件", spawnedObject);
            Destroy(spawnedObject);
            yield break;
        }

        ChessBossEnemy chessPawn = pawn as ChessBossEnemy;
        if (chessPawn != null)
        {
            chessPawn.ConfigureAsSummonedPawn(this, summonPawnData, GetNextEnemyStandPosition(), summonPawnLevel);
        }
        else
        {
            pawn.ConfigureFromRosterData(summonPawnData, GetNextEnemyStandPosition(), summonPawnLevel);
        }

        pawn.ChangeActionValue(pawn.BaseActionValue, false);
        EnemyManager.Instance?.RegisterEnemy(pawn);
        TurnManager.Instance?.InsertCombatant(pawn);
        if (chessPawn != null)
        {
            m_summonedChessPawns.Add(chessPawn);
        }

        AddChessPrestige(1);
        FloatingTipGenerator.Instance?.ShowTipAtObject(transform, $"{combatantName}召唤兵卒");
        yield break;
    }

    public IEnumerator ExecuteChessQueenThroneAssault(EnemySkillBase skill)
    {
        if (!TryResolveChessTargets(out Character kingCharacter, out Character rookCharacter))
        {
            yield break;
        }

        m_chessKingStandPosition = kingCharacter.standPosition;
        m_chessRookStandPosition = rookCharacter.standPosition;
        FloatingTipGenerator.Instance?.ShowTipAtObject(kingCharacter.transform, "王棋位");
        FloatingTipGenerator.Instance?.ShowTipAtObject(rookCharacter.transform, "车棋位");

        State.NotifyDamageSkillUsed(this);
        DamageInfo damageInfo = DamageCounter.CountDamage(this, kingCharacter, skill.skillCoef, skill.skillBase, true, false, false);
        kingCharacter.TakeDamage(damageInfo);

        float delayRatio = skill != null && skill.extraData1 > 0f ? skill.extraData1 : 1f;
        float exhaustionThresholdRatio = skill != null && skill.extraData2 > 0f ? skill.extraData2 : 0.15f;
        QueueTurnEndActionDelay(delayRatio);
        ApplyChessExhaustion(exhaustionThresholdRatio);
        TryTriggerChessCastling();
        yield break;
    }

    public IEnumerator ExecuteChessQueenCoronation(EnemySkillBase skill)
    {
        if (!IsChessQueenBoss)
        {
            yield break;
        }

        float bonusPerPrestige = skill != null && skill.extraData1 > 0f ? skill.extraData1 : 0.2f;
        float totalSkillCoef = skill != null ? skill.skillCoef * (1f + m_chessPrestigeStacks * bonusPerPrestige) : 1f;
        int totalSkillBase = skill != null ? skill.skillBase : 0;

        State.NotifyDamageSkillUsed(this);
        List<Character> targets = GetAliveFieldCharacters();
        for (int i = 0; i < targets.Count; i++)
        {
            Character target = targets[i];
            if (target == null)
            {
                continue;
            }

            DamageInfo damageInfo = DamageCounter.CountDamage(this, target, totalSkillCoef, totalSkillBase, true, false, false);
            target.TakeDamage(damageInfo);
        }

        FloatingTipGenerator.Instance?.ShowTipAtObject(transform, $"{combatantName}消耗{m_chessPrestigeStacks}层威望");
        m_chessPrestigeStacks = 0;
        m_hasUsedChessCoronation = true;
        yield break;
    }

    public void EnterChessPhaseTwoFromPawns(int prestigeStacks)
    {
        if (!IsChessQueenBoss || IsBattleVisible)
        {
            return;
        }

        AddChessPrestige(prestigeStacks);
        SetBattleVisibility(true);
        participateInTurnLoopAtStart = true;
        ChangeActionValue(BaseActionValue, false);
        EnemyManager.Instance?.RegisterEnemy(this);
        TurnManager.Instance?.InsertCombatant(this);
        FloatingTipGenerator.Instance?.ShowTipAtObject(transform, $"{combatantName}升变入场");
        ClearLinkedPromotionPawns();
    }

    public void NotifySummonedPawnKilled(ChessBossEnemy pawn)
    {
        if (pawn == null)
        {
            return;
        }

        if (!m_summonedChessPawns.Remove(pawn))
        {
            return;
        }

        AddChessPrestige(-1);
        m_summonedPawnKillCounter++;
        if (m_summonedPawnKillCounter >= 2)
        {
            m_summonedPawnKillCounter -= 2;
            GainCastlingOpportunity(1, "兵卒击杀触发易位");
        }
    }

    public void ConfigureAsSummonedPawn(ChessBossEnemy queen, EnemyRosterData data, int standPosition, int level)
    {
        m_linkedQueen = queen;
        chessRole = ChessBossUnitRole.SummonedPawn;
        bossGroupId = queen != null ? queen.bossGroupId : bossGroupId;
        startHiddenUntilPhaseTwo = false;
        summonedPawnHealRatio = queen != null ? queen.summonedPawnHealRatio : summonedPawnHealRatio;
        ConfigureFromRosterData(data, standPosition, level);
    }

    private void ApplyChessBossPendingData(ChessBossPendingData pendingData)
    {
        if (pendingData == null || !pendingData.enabled)
        {
            return;
        }

        chessRole = pendingData.unitRole;
        bossGroupId = string.IsNullOrWhiteSpace(pendingData.bossGroupId) ? bossGroupId : pendingData.bossGroupId;
        startHiddenUntilPhaseTwo = pendingData.startHiddenUntilPhaseTwo;
        summonPawnData = pendingData.summonPawnData != null ? pendingData.summonPawnData : summonPawnData;
        summonPawnLevel = Mathf.Max(1, pendingData.summonPawnLevel);
        pawnAdvanceOffset = pendingData.pawnAdvanceOffset;
        pawnAdvanceDuration = Mathf.Max(0f, pendingData.pawnAdvanceDuration);
        pawnPromotionSteps = Mathf.Max(1, pendingData.pawnPromotionSteps);
        summonedPawnHealRatio = Mathf.Clamp01(pendingData.summonedPawnHealRatio);
        immuneToDaze = pendingData.immuneToDaze;
        immuneToTaunt = pendingData.immuneToTaunt;
    }

    private ChessBossEnemy GetChessQueenController()
    {
        if (IsChessQueenBoss)
        {
            return this;
        }

        return m_linkedQueen;
    }

    private ChessBossEnemy FindQueenByGroup(IReadOnlyList<Enemy> spawnedEnemies)
    {
        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            ChessBossEnemy chessEnemy = spawnedEnemies[i] as ChessBossEnemy;
            if (chessEnemy == null || !chessEnemy.IsChessQueenBoss)
            {
                continue;
            }

            if (string.Equals(chessEnemy.bossGroupId, bossGroupId, StringComparison.Ordinal))
            {
                return chessEnemy;
            }
        }

        return null;
    }

    private bool ShouldUseChessCoronation()
    {
        return IsChessQueenBoss && !m_hasUsedChessCoronation && currentHP > 0 && currentHP <= Mathf.Max(1, Mathf.FloorToInt(maxHP / 3f));
    }

    private List<Character> GetAliveFieldCharacters()
    {
        List<Character> aliveCharacters = new List<Character>();
        if (CharacterManager.Instance == null)
        {
            return aliveCharacters;
        }

        for (int i = 0; i < CharacterManager.Instance.fieldCharacters.Count; i++)
        {
            Character character = CharacterManager.Instance.fieldCharacters[i];
            if (character == null || character.IsDead)
            {
                continue;
            }

            aliveCharacters.Add(character);
        }

        return aliveCharacters;
    }

    private bool TryResolveChessTargets(out Character kingCharacter, out Character rookCharacter)
    {
        List<Character> aliveCharacters = GetAliveFieldCharacters();
        kingCharacter = null;
        rookCharacter = null;
        if (aliveCharacters.Count < 2)
        {
            return false;
        }

        kingCharacter = aliveCharacters
            .OrderBy(character => character.speed)
            .ThenBy(character => character.standPosition)
            .FirstOrDefault();
        Character resolvedKingCharacter = kingCharacter;
        rookCharacter = aliveCharacters
            .Where(character => character != resolvedKingCharacter)
            .OrderByDescending(character => character.speed)
            .ThenBy(character => character.standPosition)
            .FirstOrDefault();
        return kingCharacter != null && rookCharacter != null;
    }

    private void QueueTurnEndActionDelay(float delayRatio)
    {
        m_pendingTurnEndDelayRatio += Mathf.Max(0f, delayRatio);
    }

    private void ApplyChessExhaustion(float thresholdRatio)
    {
        m_exhaustedTurnsRemaining = 1;
        m_exhaustionAccumulatedDamage = 0;
        m_exhaustionWindowGrantedCastling = false;
        m_exhaustionExpiresOnNextOwnTurn = true;
        int threshold = Mathf.Max(1, Mathf.CeilToInt(maxHP * Mathf.Max(0f, thresholdRatio)));
        FloatingTipGenerator.Instance?.ShowTipAtObject(transform, $"{combatantName}进入力竭,阈值{threshold}");
    }

    private void AdvanceChessExhaustionOnOwnTurnStart()
    {
        if (!m_exhaustionExpiresOnNextOwnTurn)
        {
            return;
        }

        m_exhaustionExpiresOnNextOwnTurn = false;
        m_exhaustedTurnsRemaining = Mathf.Max(0, m_exhaustedTurnsRemaining - 1);
        if (m_exhaustedTurnsRemaining <= 0)
        {
            m_exhaustionAccumulatedDamage = 0;
            m_exhaustionWindowGrantedCastling = false;
        }
    }

    private void RegisterDamageDuringExhaustion(int damage)
    {
        if (damage <= 0 || m_exhaustedTurnsRemaining <= 0 || m_exhaustionWindowGrantedCastling)
        {
            return;
        }

        m_exhaustionAccumulatedDamage += damage;
        int threshold = Mathf.Max(1, Mathf.CeilToInt(maxHP * 0.15f));
        if (m_exhaustionAccumulatedDamage >= threshold)
        {
            m_exhaustionWindowGrantedCastling = true;
            GainCastlingOpportunity(1, "力竭破绽暴露");
        }
    }

    private void GainCastlingOpportunity(int amount, string tipText)
    {
        if (amount <= 0)
        {
            return;
        }

        int before = m_pendingCastlingOpportunities;
        m_pendingCastlingOpportunities = Mathf.Clamp(m_pendingCastlingOpportunities + amount, 0, 2);
        if (m_pendingCastlingOpportunities <= before)
        {
            return;
        }

        FloatingTipGenerator.Instance?.ShowTipAtObject(transform, tipText);
        TryTriggerChessCastling();
    }

    private bool TryTriggerChessCastling()
    {
        if (m_pendingCastlingOpportunities <= 0 || CharacterManager.Instance == null)
        {
            return false;
        }

        Character kingCharacter = CharacterManager.Instance.GetFieldCharacterByStandPosition(m_chessKingStandPosition);
        Character rookCharacter = CharacterManager.Instance.GetFieldCharacterByStandPosition(m_chessRookStandPosition);
        if (kingCharacter == null || rookCharacter == null || kingCharacter == rookCharacter)
        {
            return false;
        }

        if (!CharacterManager.Instance.SwapFieldCharacters(kingCharacter, rookCharacter))
        {
            return false;
        }

        m_pendingCastlingOpportunities--;
        rookCharacter.AddState(StateType.Resist, this, 99, 1);
        FloatingTipGenerator.Instance?.ShowTipAtObject(transform, "王车易位");
        return true;
    }

    private void AddChessPrestige(int delta)
    {
        m_chessPrestigeStacks = Mathf.Max(0, m_chessPrestigeStacks + delta);
    }

    private void TryPromoteChessQueen()
    {
        if (m_hasTriggeredPromotion)
        {
            return;
        }

        ChessBossEnemy queen = GetChessQueenController();
        if (queen == null)
        {
            return;
        }

        m_hasTriggeredPromotion = true;
        queen.EnterChessPhaseTwoFromPawns(CountAlivePromotionPawns(queen));
    }

    private int CountAlivePromotionPawns(ChessBossEnemy queen)
    {
        if (EnemyManager.Instance == null || queen == null)
        {
            return 0;
        }

        int aliveCount = 0;
        IReadOnlyList<Enemy> aliveEnemies = EnemyManager.Instance.AliveEnemies;
        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            ChessBossEnemy enemy = aliveEnemies[i] as ChessBossEnemy;
            if (enemy != null && enemy.IsChessPromotionPawn && enemy.GetChessQueenController() == queen && !enemy.IsDead)
            {
                aliveCount++;
            }
        }

        return aliveCount;
    }

    private void ClearLinkedPromotionPawns()
    {
        if (EnemyManager.Instance == null)
        {
            return;
        }

        List<Enemy> enemies = new List<Enemy>(EnemyManager.Instance.AliveEnemies);
        for (int i = 0; i < enemies.Count; i++)
        {
            ChessBossEnemy enemy = enemies[i] as ChessBossEnemy;
            if (enemy == null || enemy == this || !enemy.IsChessPromotionPawn || enemy.GetChessQueenController() != this)
            {
                continue;
            }

            enemy.RemoveFromBattleSilently();
        }
    }

    private void CleanupLinkedChessUnitsOnDeath()
    {
        if (EnemyManager.Instance == null)
        {
            return;
        }

        List<Enemy> enemies = new List<Enemy>(EnemyManager.Instance.AliveEnemies);
        for (int i = 0; i < enemies.Count; i++)
        {
            ChessBossEnemy enemy = enemies[i] as ChessBossEnemy;
            if (enemy == null || enemy == this || enemy.GetChessQueenController() != this)
            {
                continue;
            }

            enemy.RemoveFromBattleSilently();
        }
    }

    private void RemoveFromBattleSilently()
    {
        if (IsDead)
        {
            SetBattleVisibility(false);
            gameObject.SetActive(false);
            return;
        }

        m_suppressLinkedQueenDeathNotify = true;
        dead = true;
        TurnManager.Instance?.RemoveCombatant(this);
        EnemyManager.Instance?.UnregisterEnemy(this);
        SetBattleVisibility(false);
        gameObject.SetActive(false);
    }

    private int GetNextEnemyStandPosition()
    {
        int maxStandPosition = standPosition;
        if (EnemyManager.Instance != null)
        {
            IReadOnlyList<Enemy> enemies = EnemyManager.Instance.AliveEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null)
                {
                    maxStandPosition = Mathf.Max(maxStandPosition, enemies[i].standPosition);
                }
            }
        }

        return maxStandPosition + 1;
    }

    private void ResolveChessSummonPose(out Vector3 position, out Quaternion rotation)
    {
        if (ChessPieceSpawnPointManager.Instance != null
            && ChessPieceSpawnPointManager.Instance.TryGetNextAvailableSpawnPose(out position, out rotation))
        {
            return;
        }

        if (chessSummonPoints != null)
        {
            for (int i = 0; i < chessSummonPoints.Length; i++)
            {
                if (chessSummonPoints[i] != null)
                {
                    position = chessSummonPoints[i].position;
                    rotation = chessSummonPoints[i].rotation;
                    return;
                }
            }
        }

        float offset = 1f + m_summonedChessPawns.Count * 0.6f;
        position = transform.position + new Vector3(-offset, 0f, 0f);
        rotation = transform.rotation;
    }

    private GameObject ResolveSummonPawnPrefab()
    {
        if (summonPawnData != null && summonPawnData.PrefabOverride != null)
        {
            return summonPawnData.PrefabOverride;
        }

        return summonPawnPrefabFallback != null ? summonPawnPrefabFallback.gameObject : null;
    }
}