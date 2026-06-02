using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 棋局皇后Boss
/// 阶段一隐藏，阶段二入场
/// 技能：混沌横冲、兵卒召唤、后袭王座、王权加冕
/// </summary>
public class ChessQueenEnemy : Enemy
{
    [Header("皇后配置")]
    [SerializeField] private string bossGroupId = "chess-boss";
    [SerializeField] private bool startHiddenUntilPhaseTwo = true;
    [SerializeField] internal EnemyRosterData summonPawnData;
    [SerializeField, Min(1)] internal int summonPawnLevel = 1;
    [SerializeField] internal ChessSummonedPawnEnemy summonPawnPrefabFallback;
    [SerializeField] private Transform[] chessSummonPoints;
    [SerializeField, Range(0f, 1f)] private float summonedPawnHealRatio = 0.03f;
    [SerializeField] private bool immuneToDaze = true;
    [SerializeField] private bool immuneToTaunt = true;
    [SerializeField] private SpriteRenderer chessSpriteRenderer;

    [Header("技能CD")]
    [SerializeField, Min(0)] private int summonPawnCooldown = 3;
    [SerializeField, Min(0)] private int throneAssaultCooldown = 4;

    // 内部状态
    private readonly HashSet<ChessSummonedPawnEnemy> m_summonedPawns = new HashSet<ChessSummonedPawnEnemy>();
    private int m_prestigeStacks;
    private bool m_hasUsedCoronation;
    private int m_summonedPawnKillCounter;
    private int m_summonSkillRemainingCooldown;
    private int m_throneAssaultRemainingCooldown;
    private bool m_isChargingThroneAssault; // 后袭王座蓄力标记

    // 属性
    public string BossGroupId => bossGroupId;
    public bool IsChessQueenBoss => true;
    public bool StartsHiddenUntilPhaseTwo => startHiddenUntilPhaseTwo;
    public override bool ShouldRegisterAtBattleStart => !StartsHiddenUntilPhaseTwo;
    public int PrestigeStacks => m_prestigeStacks;
    public float SummonedPawnHealRatio => summonedPawnHealRatio;
    public bool HasUsedCoronation => m_hasUsedCoronation;
    public bool IsChargingThroneAssault => m_isChargingThroneAssault;

    protected override void Start()
    {
        base.Start();
        // 皇后在 Start 中占用站位
        if (ChessStandPositionManager.Instance != null)
        {
            transform.position = ChessStandPositionManager.Instance.GetQueenStandPosition().position;
        }
    }

    public override void Die()
    {
        base.Die();
        CleanupLinkedPawnsOnDeath();
    }

    public override bool CanUseEnemySkill(EnemySkillBase skill)
    {
        if (!base.CanUseEnemySkill(skill))
        {
            return false;
        }

        switch (skill.enemySkillType)
        {
            case EnemySkillType.ChessQueenChaosCharge:
                return GetAliveFieldCharacters().Count > 0;
            case EnemySkillType.ChessQueenSummonPawn:
                return m_summonSkillRemainingCooldown <= 0 && summonPawnData != null;
            case EnemySkillType.ChessQueenThroneAssault:
                return m_isChargingThroneAssault || (m_throneAssaultRemainingCooldown <= 0 && GetAliveFieldCharacters().Count >= 2);
            case EnemySkillType.ChessQueenCoronation:
                return ShouldUseCoronation();
            default:
                return true;
        }
    }

    protected override EnemySkillBase GetForcedSkillForTurn()
    {
        // 如果正在蓄力，强制使用后袭王座
        if (m_isChargingThroneAssault)
        {
            EnemySkillBase throneSkill = GetSkillInstance(EnemySkillType.ChessQueenThroneAssault);
            if (throneSkill != null && throneSkill.CanUse(this))
            {
                return throneSkill;
            }
        }

        // 如果满足加冕条件，强制使用
        EnemySkillBase coronationSkill = GetSkillInstance(EnemySkillType.ChessQueenCoronation);
        if (coronationSkill != null && coronationSkill.CanUse(this) && ShouldUseCoronation())
        {
            return coronationSkill;
        }

        return base.GetForcedSkillForTurn();
    }

    public override IEnumerator PerformTurn()
    {
        if (!IsBattleVisible || dead)
        {
            yield break;
        }

        // 随机选择技能（排除不能使用的）
        EnemySkillBase selectedSkill = SelectRandomAvailableSkill();
        if (selectedSkill != null)
        {
            switch (selectedSkill.enemySkillType)
            {
                case EnemySkillType.ChessQueenChaosCharge:
                case EnemySkillType.ChessQueenSummonPawn:
                    FloatingTipGenerator.Instance?.ShowDefaultTip($"{selectedSkill.skillName}");
                    break;
                case EnemySkillType.ChessQueenThroneAssault:
                    if (!m_isChargingThroneAssault)
                    {
                        FloatingTipGenerator.Instance?.ShowDefaultTip($"后袭王座:下个回合对王棋发动袭击");
                    }
                    else
                        FloatingTipGenerator.Instance?.ShowDefaultTip($"后袭王座");
                    break;
                case EnemySkillType.ChessQueenCoronation:
                    FloatingTipGenerator.Instance?.ShowDefaultTip($"王权加冕");
                    break;
            }
            yield return selectedSkill.Execute(this);
        }
    }

    /// <summary>进入阶段二</summary>
    public void EnterPhaseTwo(int prestigeStacks)
    {
        if (!IsChessQueenBoss || IsBattleVisible)
        {
            return;
        }
        // 直接消灭所有残余召唤兵卒
        List<Enemy> enemies = new List<Enemy>(EnemyManager.Instance.AliveEnemies);
        for (int i = 0; i < enemies.Count; i++)
        {
            ChessPawnEnemy pawn = enemies[i] as ChessPawnEnemy;
            if (pawn == null)
            {
                continue;
            }

            pawn.TakeDamage(new DamageInfo(pawn.currentHP).AsTrueDamage()); // 直接消灭所有残余兵卒
        }

        m_prestigeStacks = Mathf.Max(0, prestigeStacks);
        StartCoroutine(EnterPhaseTwoWithFadeIn());
    }

    private IEnumerator EnterPhaseTwoWithFadeIn()
    {
        SetBattleVisibility(true);
        if (chessSpriteRenderer != null)
        {
            Color c = chessSpriteRenderer.color;
            chessSpriteRenderer.color = new Color(c.r, c.g, c.b, 0f);
        }

        participateInTurnLoopAtStart = true;
        ChangeActionValue(BaseActionValue, false);
        EnemyManager.Instance?.RegisterEnemy(this);
        TurnManager.Instance?.InsertCombatant(this);
        FloatingTipGenerator.Instance?.ShowTipAtObject(transform, $"{combatantName}升变入场");
        ClearLinkedPromotionPawns();

        // 进入二阶段时标记王棋/车棋
        MarkChessKingAndRook();

        if (chessSpriteRenderer != null)
        {
            yield return chessSpriteRenderer.DOFade(1f, 0.5f).SetEase(Ease.OutQuad).WaitForCompletion();
        }
    }

    /// <summary>标记王棋（速度最低）与车棋（速度最高）</summary>
    private void MarkChessKingAndRook()
    {
        List<Character> aliveCharacters = GetAliveFieldCharacters();
        if (aliveCharacters.Count < 2) return;

        aliveCharacters.Sort((a, b) => a.speed.CompareTo(b.speed));
        Character kingCharacter = aliveCharacters[0];
        Character rookCharacter = aliveCharacters[1];

        kingCharacter.AddState(StateType.ChessKingMark, this, 99, 99);
        rookCharacter.AddState(StateType.ChessRookMark, this, 99, 99);
        FloatingTipGenerator.Instance?.ShowTipAtObject(kingCharacter.transform, "王棋位");
        FloatingTipGenerator.Instance?.ShowTipAtObject(rookCharacter.transform, "车棋位");
    }

    /// <summary>通知召唤兵卒被击杀</summary>
    public void NotifySummonedPawnKilled(ChessSummonedPawnEnemy pawn)
    {
        if (pawn == null || !m_summonedPawns.Remove(pawn))
        {
            return;
        }

        AddPrestige(-1);
        m_summonedPawnKillCounter++;
        if (m_summonedPawnKillCounter >= 2)
        {
            m_summonedPawnKillCounter -= 2;
            Commander.GetInstance().AddCastlingOpportunity(1, "兵卒击杀触发易位");
        }
    }

    /// <summary>设置蓄力标记</summary>
    public void SetChargingThroneAssault(bool charging)
    {
        m_isChargingThroneAssault = charging;
    }

    /// <summary>开始召唤技能CD</summary>
    public void StartSummonCooldown()
    {
        m_summonSkillRemainingCooldown = summonPawnCooldown;
    }

    /// <summary>开始后袭王座CD</summary>
    public void StartThroneAssaultCooldown()
    {
        m_throneAssaultRemainingCooldown = throneAssaultCooldown;
    }

    /// <summary>Tick皇后技能CD</summary>
    public void TickQueenSkillCooldowns()
    {
        if (m_summonSkillRemainingCooldown > 0) m_summonSkillRemainingCooldown--;
        if (m_throneAssaultRemainingCooldown > 0) m_throneAssaultRemainingCooldown--;
    }

    /// <summary>添加威望层数</summary>
    public void AddPrestige(int delta)
    {
        m_prestigeStacks = Mathf.Max(0, m_prestigeStacks + delta);
    }

    /// <summary>消耗所有威望（加冕时调用）</summary>
    public int ConsumeAllPrestige()
    {
        int consumed = m_prestigeStacks;
        m_prestigeStacks = 0;
        m_hasUsedCoronation = true;
        return consumed;
    }

    /// <summary>延迟行动值</summary>
    public void DelayActionValue(float delayRatio)
    {
        m_pendingTurnEndDelayRatio += Mathf.Max(0f, delayRatio);
    }

    private float m_pendingTurnEndDelayRatio;

    public override float ConsumeTurnEndActionValue()
    {
        float nextActionValue = BaseActionValue * (1f + Mathf.Max(0f, m_pendingTurnEndDelayRatio));
        m_pendingTurnEndDelayRatio = 0f;
        return nextActionValue;
    }

    /// <summary>威望伤害减免（独立乘区）</summary>
    public float GetPrestigeDamageReduction()
    {
        return 1f - Mathf.Min(1f, m_prestigeStacks * 0.1f);
    }

    /// <summary>威望伤害加成</summary>
    public float GetPrestigeDamageBonus()
    {
        return 1f + m_prestigeStacks * 0.1f;
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

    private EnemySkillBase SelectRandomAvailableSkill()
    {
        // 收集所有可用技能
        List<EnemySkillBase> availableSkills = new List<EnemySkillBase>();
        EnemySkillType[] skillTypes = { EnemySkillType.ChessQueenChaosCharge, EnemySkillType.ChessQueenSummonPawn, EnemySkillType.ChessQueenThroneAssault };

        for (int i = 0; i < skillTypes.Length; i++)
        {
            EnemySkillBase skill = GetSkillInstance(skillTypes[i]);
            if (skill != null && skill.CanUse(this))
            {
                availableSkills.Add(skill);
            }
        }

        if (availableSkills.Count == 0)
        {
            // 所有技能都不可用，回退到默认
            return GetSkillInstance(EnemySkillType.ChessQueenChaosCharge);
        }

        return availableSkills[Random.Range(0, availableSkills.Count)];
    }

    private bool ShouldUseCoronation()
    {
        return !m_hasUsedCoronation && currentHP > 0 && currentHP <= Mathf.Max(1, Mathf.FloorToInt(maxHP / 3f));
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

    private void ClearLinkedPromotionPawns()
    {
        if (EnemyManager.Instance == null)
        {
            return;
        }

        List<Enemy> enemies = new List<Enemy>(EnemyManager.Instance.AliveEnemies);
        for (int i = 0; i < enemies.Count; i++)
        {
            ChessPawnEnemy pawn = enemies[i] as ChessPawnEnemy;
            if (pawn == null || !string.Equals(pawn.BossGroupId, bossGroupId, System.StringComparison.Ordinal))
            {
                continue;
            }

            pawn.gameObject.SetActive(false);
            if (LevelCharacterSpawner.Instance != null)
            {
                LevelCharacterSpawner.Instance.ReleaseEnemyStandPosition(pawn.standPosition);
            }
        }
    }

    private void CleanupLinkedPawnsOnDeath()
    {
        if (EnemyManager.Instance == null)
        {
            return;
        }

        List<Enemy> enemies = new List<Enemy>(EnemyManager.Instance.AliveEnemies);
        for (int i = 0; i < enemies.Count; i++)
        {
            ChessSummonedPawnEnemy pawn = enemies[i] as ChessSummonedPawnEnemy;
            if (pawn == null)
            {
                continue;
            }

            pawn.gameObject.SetActive(false);
        }
    }
}
