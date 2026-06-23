using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
public enum SkillTargetType
{
    Enemy,
    Ally,
    Other
}
public enum CharacterSkillType
{
    None,
    EnterSkillOne,
    ExitSkillOne,
    AllAttack,
    PursuitPunish,
    PursuitPunishAdditional,
    EnterSkillTwo,
    ExitSkillTwo,
    DebuffSpreadAttack,
    ElementDetonate,
    EnterSkillThree,
    ExitSkillThree,
    TauntPull,
    ShieldAndAdvance,
    EnemyAttack,
    MainDpsEnter,
    MainDpsExit,
    MainDpsSkillOne,
    MainDpsSkillOneAdditional,
    MainDpsSkillTwo,
    SubDpsEnter,
    SubDpsExit,
    SubDpsSkillOne,
    SubDpsSkillTwo,
    HealerEnter,
    HealerExit,
    HealerSkillOne,
    HealerSkillTwo
}
public enum CharacterSkillTag
{
    Attack,
    DotAttack,
    Buff,
    Debuff
}
[CreateAssetMenu(fileName = "NewSkill", menuName = "技能/CharacterSkill"), System.Serializable]
public class CharacterSkillBase : SkillBase
{
    private const float BossMaxHpDamageCoef = 0.02f;
    private const float BossExecuteThreshold = 0.12f;

    public SkillTargetType skillTargetType;
    public CharacterSkillType skillType;
    [Header("额外参数")]
    public float extraData1;
    public float extraData2;
    public float extraData3;
    public float extraData4;
    [Header("关键词")]
    public List<string> words = new List<string>();
    [Header("标签")]
    public List<string> tags = new List<string>();
    [Header("目标选择设置")]
    public bool requiresEnemyTarget = false;
    [Min(1)]
    public int enemyTargetCount = 1;
    public bool requiresAllyTarget = false;
    [Min(1)]
    public int allyTargetCount = 1;
    [Header("是否结束回合")]
    public bool endTurnAfterUse = true;
    [Header("冷却回合")]
    [Min(0)]
    public int cooldownTurns = 0;

    private readonly Dictionary<Character, int> m_runtimeCooldown = new Dictionary<Character, int>();

    private static void NotifyDamageSkillUsed(UnitCombatant unitCombatant, IReadOnlyList<UnitCombatant> damagedUnits)
    {
        State.NotifyDamageSkillUsed(unitCombatant, damagedUnits);
    }

    private DamageType GetCurrentSkillDamageType()
    {
        switch (skillType)
        {
            case CharacterSkillType.EnterSkillOne:
            case CharacterSkillType.ExitSkillOne:
            case CharacterSkillType.AllAttack:
            case CharacterSkillType.PursuitPunish:
            case CharacterSkillType.PursuitPunishAdditional:
            case CharacterSkillType.EnterSkillTwo:
            case CharacterSkillType.ExitSkillTwo:
            case CharacterSkillType.DebuffSpreadAttack:
            case CharacterSkillType.ElementDetonate:
                return DamageType.Magical;
            default:
                return DamageType.Physical;
        }
    }

    public override IEnumerator Execute(UnitCombatant unitCombatant)
    {
        yield return ExecuteInternal(unitCombatant, null, false);
    }

    public override IEnumerator Execute(UnitCombatant unitCombatant, List<Enemy> selectedEnemies)
    {
        yield return ExecuteInternal(unitCombatant, selectedEnemies, true);
    }

    private IEnumerator ExecuteInternal(UnitCombatant unitCombatant, List<Enemy> selectedEnemies, bool skipEnemySelection)
    {
        if (unitCombatant == null)
        {
            Debug.LogWarning($"[SkillBase] 角色为空，无法执行技能 {skillName}");
            yield break;
        }
        var character = unitCombatant as Character;
        int cd = GetRemainingCooldown(character);
        if (cd > 0)
        {
            FloatingTipGenerator.Instance?.ShowDefaultTip($"{skillName}冷却中: {cd}回合");
            yield break;
        }

        if (requiresEnemyTarget && !skipEnemySelection)
        {
            if (SkillManager.Instance == null)
            {
                Debug.LogWarning("[SkillBase] 缺少 SkillManager，无法进入选敌流程");
                character.EndTurn();
                yield break;
            }

            selectedEnemies = new List<Enemy>();
            yield return SkillManager.Instance.SelectEnemiesCoroutine(enemyTargetCount, selectedEnemies);
            if (selectedEnemies == null || selectedEnemies.Count <= 0)
            {
                Debug.Log("[SkillBase] 未选择敌人，技能取消");
                yield break;
            }
        }
        bool requiresRuntimeAllyTarget = RequiresRuntimeAllyTarget();

        List<Character> selectedCharacters = null;
        if (requiresRuntimeAllyTarget)
        {
            if (SkillManager.Instance == null)
            {
                Debug.LogWarning("[SkillBase] 缺少 SkillManager，无法进入选友流程");
                character.EndTurn();
                yield break;
            }

            selectedCharacters = new List<Character>();
            yield return SkillManager.Instance.SelectCharactersCoroutine(allyTargetCount, selectedCharacters);
            if (selectedCharacters == null || selectedCharacters.Count <= 0)
            {
                Debug.Log("[SkillBase] 未选择队友，技能取消");
                yield break;
            }
        }

        bool shouldEndTurn = ResolveShouldEndTurn();
        //技能镜头过渡
        CinemachineCameraManager cameraManager = CinemachineCameraManager.Instance;
        bool useSkillCameraTransition = cameraManager != null;
        if (useSkillCameraTransition && shouldEndTurn)
        {
            TurnStateManager.Instance.ChangeState(TurnState.OutCharacterTurn, unitCombatant as Character);
            if (skillTargetType == SkillTargetType.Enemy)
            {
                yield return cameraManager.TransitionIntoSkillCamera(ManagedCameraType.Attack);
            }
            else if (skillTargetType == SkillTargetType.Ally)
            {
                yield return cameraManager.TransitionIntoSkillCamera(ManagedCameraType.Help);
            }
            FloatingTipGenerator.Instance.ShowDefaultTip(SkillDictionaryManager.GetSkillName(skillType));
            //动画占位
            character.PlayATKAnimation();
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            FloatingTipGenerator.Instance.ShowDefaultTip(SkillDictionaryManager.GetSkillName(skillType));
            yield return new WaitForSeconds(0.2f);
            character.PlayATKAnimation();
            yield return new WaitForSeconds(0.5f);
            character.EndATKAnimation();
        }

        switch (skillType)
        {
            case CharacterSkillType.AllAttack:
                yield return ExecuteAllAttack(character);
                break;
            case CharacterSkillType.PursuitPunish:
                yield return PursuitPunish(character);
                break;
            case CharacterSkillType.PursuitPunishAdditional:
                yield return PursuitPunishAdditional(character);
                break;
            case CharacterSkillType.EnterSkillOne:
                yield return EnterSkillOne(character);
                break;
            case CharacterSkillType.EnterSkillTwo:
                yield return EnterSkillTwo(character);
                break;
            case CharacterSkillType.DebuffSpreadAttack:
                yield return DebuffSpreadAttack(character);
                break;
            case CharacterSkillType.ElementDetonate:
                yield return ElementDetonate(character, selectedEnemies);
                break;
            case CharacterSkillType.EnterSkillThree:
                yield return EnterSkillThree(character);
                break;
            case CharacterSkillType.TauntPull:
                yield return TauntPull(character, selectedEnemies);
                break;
            case CharacterSkillType.ShieldAndAdvance:
                yield return ShieldAndAdvance(character, selectedCharacters);
                break;
            case CharacterSkillType.MainDpsEnter:
                yield return MainDpsEnter(character);
                break;
            case CharacterSkillType.MainDpsSkillOne:
                yield return MainDpsSkillOne(character, selectedEnemies);
                break;
            case CharacterSkillType.MainDpsSkillOneAdditional:
                yield return MainDpsSkillOneAdditional(character, selectedEnemies);
                break;
            case CharacterSkillType.MainDpsSkillTwo:
                shouldEndTurn = false;
                yield return MainDpsSkillTwo(character);
                break;
            case CharacterSkillType.SubDpsEnter:
                yield return SubDpsEnter(character);
                break;
            case CharacterSkillType.SubDpsSkillOne:
                yield return SubDpsSkillOne(character, selectedEnemies);
                break;
            case CharacterSkillType.SubDpsSkillTwo:
                shouldEndTurn = false;
                yield return SubDpsSkillTwo(character);
                break;
            case CharacterSkillType.HealerEnter:
                yield return HealerEnter(character);
                break;
            case CharacterSkillType.HealerSkillOne:
                yield return HealerSkillOne(character, selectedCharacters);
                break;
            case CharacterSkillType.HealerSkillTwo:
                yield return HealerSkillTwo(character, selectedCharacters);
                break;
        }
        //受击动画占位
        if (useSkillCameraTransition && shouldEndTurn)
        {
            yield return new WaitForSeconds(0.5f);
            character.EndATKAnimation();
            yield return cameraManager.TransitionOutOfSkillCamera();
        }

        StartCooldown(character);
        if (shouldEndTurn)
        {
            character.EndTurn();
        }
    }
    #region 技能具体执行逻辑
    private IEnumerator EnterSkillOne(Character character)
    {
        int durationActionValue = Mathf.RoundToInt(extraData1 > 0f ? extraData1 : 100f);
        float verdictDotMultiplier = extraData2;
        DamageType damageType = GetCurrentSkillDamageType();
        FloatingTipGenerator.Instance.ShowTipAtObject(character.transform, $"{character.name}释放重裁域场");
        if (FieldDomainScreenEffectController.Instance != null)
        {
            yield return FieldDomainScreenEffectController.Instance.PlayExpand(EnvironmentType.Gravity, character.transform);
        }
        EnvironmentManager.Instance?.AddEnvironment(EnvironmentType.Gravity, durationActionValue, character, verdictDotMultiplier);
        foreach (var enemy in EnemyManager.Instance.AliveEnemies)
        {
            int debuffStack = 0;
            foreach (var state in enemy.States)
            {
                if (state != null && state.isDebuff)
                {
                    debuffStack++;
                }
            }
            if (debuffStack > 0)
            {
                enemy.TakeDamage(DamageCounter.CountDamage(character, enemy, 0, debuffStack * extraData3, damageType, false, true, true));
                enemy.AddState(StateType.Weakened, character, 99, 1);
            }
            else
            {
                enemy.AddState(StateType.Weakened, character, 99, 2);
            }
        }
        yield break;
    }
    private IEnumerator ExitSkillOne(Character character)
    {
        float dotTriggerMultiplier = extraData1;
        var enemies = new List<Enemy>(EnemyManager.Instance.AliveEnemies);
        NotifyDamageSkillUsed(character, enemies);
        FloatingTipGenerator.Instance.ShowTipAtObject(character.transform, $"{character.name}的离场技能触发，结算dot伤害");
        State.RunBatchedDotEvent(character, () =>
        {
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    foreach (var state in enemy.States)
                    {
                        if (state.isDot)
                        {
                            state.DotTrigger(dotTriggerMultiplier);
                        }
                    }
                }
            }
        });
        yield break;
    }
    private IEnumerator ExecuteAllAttack(Character character)
    {
        int dotDuration = Mathf.RoundToInt(extraData1);
        float refreshMultiplier = extraData2;
        DamageType damageType = GetCurrentSkillDamageType();
        var enemies = new List<Enemy>(EnemyManager.Instance.AliveEnemies);
        NotifyDamageSkillUsed(character, enemies);
        State.RunBatchedDotEvent(character, () =>
        {
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    var damageInfo = DamageCounter.CountDamage(character, enemy, this, damageType);
                    enemy.TakeDamage(damageInfo);
                    bool hadSeqFlame = enemy.HasState(StateType.SeqFlame);
                    State state = enemy.AddState(StateType.SeqFlame, character, dotDuration, 1);

                    if (hadSeqFlame)
                    {
                        state.DotTrigger(refreshMultiplier);
                    }
                }
            }
        });
        GlobalFeedbacks.Instance?.skillFeedback?.PlayFeedbacks();
        yield break;
    }
    private IEnumerator PursuitPunish(Character character)
    {
        if (character == null)
        {
            yield break;
        }

        int duration = Mathf.RoundToInt(extraData1);
        character.AddState(StateType.PursuitPunish, character, duration, 1);
        yield break;
    }

    private IEnumerator PursuitPunishAdditional(Character character)
    {
        if (character == null || EnemyManager.Instance == null)
        {
            yield break;
        }

        DamageType damageType = GetCurrentSkillDamageType();

        List<Enemy> markedEnemies = new List<Enemy>();
        IReadOnlyList<Enemy> aliveEnemies = EnemyManager.Instance.AliveEnemies;
        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            Enemy enemy = aliveEnemies[i];
            if (enemy == null || enemy.IsDead)
            {
                continue;
            }

            if (!enemy.HasState(StateType.PunishMark))
            {
                continue;
            }

            markedEnemies.Add(enemy);
        }

        if (markedEnemies.Count <= 0)
        {
            yield break;
        }

        NotifyDamageSkillUsed(character, markedEnemies);
        for (int i = 0; i < markedEnemies.Count; i++)
        {
            Enemy enemy = markedEnemies[i];
            if (enemy == null)
            {
                continue;
            }

            var damageInfo = DamageCounter.CountDamage(character, enemy, extraData1, 0f, damageType, false, false, false)
                .WithState(StateType.PursuitPunish);
            enemy.TakeDamage(damageInfo);
            State punishMark = enemy.GetState(StateType.PunishMark);
            if (punishMark != null)
            {
                enemy.RemoveState(punishMark);
            }
        }

        yield break;
    }
    private IEnumerator EnterSkillTwo(Character character)
    {
        if (character == null || EnemyManager.Instance == null)
        {
            yield break;
        }

        const int fallbackDurationActionValue = 300;
        int durationActionValue = Mathf.RoundToInt(extraData1 > 0f ? extraData1 : fallbackDurationActionValue);
        var enemies = new List<Enemy>(EnemyManager.Instance.AliveEnemies);
        foreach (var enemy in enemies)
        {
            if (enemy == null)
            {
                continue;
            }

            State tormentState = enemy.AddState(StateType.PersistentTorment, character, durationActionValue, 1);
            enemy.AddState(PickRandomDotState(enemy), character, 0, 1);
            TryApplyPersistentTormentDaze(tormentState);
        }

        yield break;
    }

    private IEnumerator ExitSkillTwo(Character character)
    {
        if (EnemyManager.Instance == null)
        {
            yield break;
        }

        int extendTurns = Mathf.RoundToInt(extraData1);

        var enemies = new List<Enemy>(EnemyManager.Instance.AliveEnemies);
        foreach (var enemy in enemies)
        {
            if (enemy == null)
            {
                continue;
            }

            foreach (var state in enemy.States)
            {
                if (state == null || !state.isDebuff || state.DurationType != StateDurationType.Turn)
                {
                    continue;
                }

                state.ExtendTurns(extendTurns);
            }
        }

        yield break;
    }

    private IEnumerator DebuffSpreadAttack(Character character)
    {
        if (EnemyManager.Instance == null)
        {
            yield break;
        }

        int dotDuration = Mathf.RoundToInt(extraData2);
        float refreshMultiplier = extraData3;
        DamageType damageType = GetCurrentSkillDamageType();
        var enemies = new List<Enemy>(EnemyManager.Instance.AliveEnemies);
        NotifyDamageSkillUsed(character, enemies);
        State.RunBatchedDotEvent(character, () =>
        {
            foreach (var enemy in enemies)
            {
                if (enemy == null)
                {
                    continue;
                }

                var damageInfo = DamageCounter.CountDamage(character, enemy, this, damageType);
                enemy.TakeDamage(damageInfo);

                StateType dotType = PickRandomDotState(enemy);
                bool hadDot = enemy.HasState(dotType);
                State state = enemy.AddState(dotType, character, dotDuration, 1);
                if (hadDot && state != null)
                {
                    state.DotTrigger(refreshMultiplier);
                }
            }
        });
        GlobalFeedbacks.Instance?.skillFeedback?.PlayFeedbacks();
        yield break;
    }

    private IEnumerator ElementDetonate(Character character, List<Enemy> selectedEnemies)
    {
        if (selectedEnemies == null || selectedEnemies.Count <= 0)
        {
            yield break;
        }

        Enemy target = selectedEnemies[0];
        if (target == null)
        {
            yield break;
        }

        NotifyDamageSkillUsed(character, new List<UnitCombatant> { target });
        bool ifAttack = false;
        State.RunBatchedDotEvent(character, () =>
        {
            // 遍历前拷贝列表，防止 DotTrigger 内 EndState 修改原集合导致枚举异常
            var statesSnapshot = new List<State>(target.States);
            foreach (var state in statesSnapshot)
            {
                if (state != null && state.isDot)
                {
                    ifAttack = true;
                    state.DotTrigger();
                }
            }
        });
        target.AddState(StateType.ElementalDetonation, character, Mathf.RoundToInt(extraData1));
        if (ifAttack)
        {
            GlobalFeedbacks.Instance?.skillFeedback?.PlayFeedbacks();
        }
        yield break;
    }

    private IEnumerator EnterSkillThree(Character character)
    {
        if (character == null)
        {
            yield break;
        }
        //叠层
        character.AddState(StateType.CounterCharge, character, 99, 1);
        if (extraData1 > 0f)
        {
            State chargeState = character.GetState(StateType.Charge);
            if (chargeState != null)
            {
                chargeState.ChangeStackCount(Mathf.RoundToInt(extraData1));
            }
        }

        if (CharacterManager.Instance == null)
        {
            yield break;
        }

        const float defaultAdvanceRatio = 0.5f;
        float advanceRatio = extraData2 > 0f ? extraData2 : defaultAdvanceRatio;
        int resistStacks = Mathf.Max(1, Mathf.RoundToInt(extraData3 > 0f ? extraData3 : 1f));

        //拉条与抵御
        foreach (var ally in CharacterManager.Instance.fieldCharacters)
        {
            if (ally == null)
            {
                continue;
            }

            ally.AddState(StateType.Resist, character, 99, resistStacks);
            ally.ChangeActionValue(ally.currentActionValue - ally.BaseActionValue * advanceRatio);
        }

        yield break;
    }

    private IEnumerator ExitSkillThree(Character character)
    {
        if (CharacterManager.Instance == null)
        {
            yield break;
        }

        int resistStacks = Mathf.Max(1, Mathf.RoundToInt(extraData1));
        float advanceRatio = extraData2;

        foreach (var ally in CharacterManager.Instance.fieldCharacters)
        {
            if (ally == null)
            {
                continue;
            }

            ally.AddState(StateType.Resist, character, 99, resistStacks);
            ally.ChangeActionValue(ally.currentActionValue - ally.BaseActionValue * advanceRatio);
        }

        yield break;
    }

    private IEnumerator TauntPull(Character character, List<Enemy> selectedEnemies)
    {
        Enemy target = GetPrimaryEnemy(selectedEnemies);
        if (character == null || target == null)
        {
            yield break;
        }

        int tauntDuration = Mathf.Max(1, Mathf.RoundToInt(extraData3));
        float pullRatio = extraData1 > 0f ? extraData1 : 1f;
        float weakenedOutputRatio = extraData2 > 0f && extraData2 < 1f ? 1f - extraData2 : 0.4f;

        target.AddState(StateType.Taunt, character, tauntDuration, 1);
        State weakenedState = target.AddState(StateType.ActionWeakened, character, 1, 1);
        ApplyStateBaseExtraData(weakenedState, extra1: weakenedOutputRatio);
        target.ChangeActionValue(target.currentActionValue - target.BaseActionValue * pullRatio);
        NotifyDamageSkillUsed(character, new List<UnitCombatant> { target });
        var damageInfo = DamageCounter.CountDamage(character, target, skillCoef, skillBase, DamageType.Physical, false, true, true);
        target.TakeDamage(damageInfo);
        yield break;
    }

    private IEnumerator ShieldAndAdvance(Character character, List<Character> selectedCharacters)
    {
        if (selectedCharacters == null || selectedCharacters.Count <= 0)
        {
            yield break;
        }

        Character target = selectedCharacters[0];
        if (target == null)
        {
            yield break;
        }

        float shieldHpCoef = extraData1;
        float advanceRatio = extraData2;
        float nextActionDamageBoost = extraData3 > 0f ? 1f + extraData3 : 1.2f;
        float shieldAttackCoef = extraData4;
        int shield = Mathf.RoundToInt(character.maxHP * shieldHpCoef + shieldAttackCoef *2);
        target.AddShield(shield);
        State boost = target.AddState(StateType.DamageChange, character, 1, 1);
        ApplyStateBaseExtraData(boost, extra1: nextActionDamageBoost);

        target.ChangeActionValue(target.currentActionValue - target.BaseActionValue * advanceRatio);
        yield break;
    }

    private IEnumerator MainDpsEnter(Character character)
    {
        if (character == null)
        {
            yield break;
        }

        int feastDuration = Mathf.RoundToInt(extraData1 > 0f ? extraData1 : 200f);
        State berserkFeast = character.AddState(StateType.BerserkFeast, character, feastDuration, 1);
        ApplyBerserkFeastCritBonus(berserkFeast, extraData2);
        character.AddState(StateType.BurningBlood, character, 99, 1);
        Enemy target = EnemyManager.Instance.GetLowestHPRatioEnemy();
        if (target == null)
            yield break;
        NotifyDamageSkillUsed(character, new List<UnitCombatant> { target });
        float normalExecuteThreshold = extraData4;
        float executeThreshold = IsBoss(target) ? BossExecuteThreshold : normalExecuteThreshold;
        if (target.currentHP <= Mathf.RoundToInt(target.maxHP * executeThreshold))
        {
            target.TakeDamage(new UnitCombatant.DamageInfo(target.currentHP + target.currentShield + 99999, character).AsTrueDamage());
            yield break;
        }
        //伤害计算（首领额外生命系数见 BossMaxHpDamageCoef）
        float normalHpCoef = extraData3 > 0f ? extraData3 : 0.06f;
        float hpCoef = IsBoss(target) ? BossMaxHpDamageCoef : normalHpCoef;
        var damageInfo = DamageCounter.CountDamage(character, target, this.skillCoef, this.skillBase + Mathf.RoundToInt(target.maxHP * hpCoef), DamageType.Physical, true, false, true);
        target.TakeDamage(damageInfo);
        yield break;
    }

    private IEnumerator MainDpsExit(Character character, List<Enemy> selectedEnemies)
    {
        if (character == null)
        {
            yield break;
        }

        RemoveStateIfExists(character, StateType.BerserkFeast);
        RemoveStateIfExists(character, StateType.BurningBlood);

        Enemy target = GetPrimaryEnemy(selectedEnemies);
        if (target == null)
        {
            yield break;
        }

        NotifyDamageSkillUsed(character, new List<UnitCombatant> { target });

        //斩杀
        float normalExecuteThreshold = extraData1;
        float executeThreshold = IsBoss(target) ? BossExecuteThreshold : normalExecuteThreshold;
        if (target.currentHP <= Mathf.RoundToInt(target.maxHP * executeThreshold))
        {
            target.TakeDamage(new UnitCombatant.DamageInfo(target.currentHP + target.currentShield + 99999, character).AsTrueDamage());
            yield break;
        }
        //伤害计算（首领额外生命系数见 BossMaxHpDamageCoef）
        float normalHpCoef = extraData3 > 0f ? extraData3 : 0.06f;
        float hpCoef = IsBoss(target) ? BossMaxHpDamageCoef : normalHpCoef;
        var damageInfo = DamageCounter.CountDamage(character, target, this.skillCoef, this.skillBase + Mathf.RoundToInt(target.maxHP * hpCoef), DamageType.Physical, true, false, true);
        target.TakeDamage(damageInfo);
        yield break;
    }

    private IEnumerator MainDpsSkillOne(Character character, List<Enemy> selectedEnemies)
    {
        yield return ExecuteMainDpsSkillOne(character, selectedEnemies, 1f, true);
    }

    private IEnumerator MainDpsSkillOneAdditional(Character character, List<Enemy> selectedEnemies)
    {
        yield return ExecuteMainDpsSkillOne(character, selectedEnemies, 0.5f, false);
    }

    private IEnumerator ExecuteMainDpsSkillOne(Character character, List<Enemy> selectedEnemies, float damageScale, bool insertAdditionalTurnOnCrit)
    {
        Enemy target = GetPrimaryEnemy(selectedEnemies);
        if (character == null || target == null)
        {
            yield break;
        }

        NotifyDamageSkillUsed(character, new List<UnitCombatant> { target });

        float normalCoef = skillCoef;
        // extraData1（CSV Extra_Data_1）：目标有减益时替换使用的 SkillCoef
        float debuffCoef = extraData1 > 0f ? extraData1 : normalCoef;
        float coef = (HasAnyDebuff(target) ? debuffCoef : normalCoef) * damageScale;
        int scaledSkillBase = Mathf.RoundToInt(skillBase * damageScale);

        bool isCrit;
        var damageInfo = DamageCounter.CountDamage(character, target, coef, scaledSkillBase, DamageType.Physical, false, true, true, out isCrit);

        target.TakeDamage(damageInfo);
        State berserkState = character.GetState(StateType.BerserkFeast);
        if (insertAdditionalTurnOnCrit && berserkState != null && isCrit && target.currentHP > 0)
        {
            CharacterSkillBase additionalSkill = SkillDictionaryManager.GetSkill(CharacterSkillType.MainDpsSkillOneAdditional);
            if (additionalSkill != null)
            {
                TurnManager.Instance?.AdditionalTurnInsert(character, additionalSkill, new List<Enemy> { target });
            }
        }

        GlobalFeedbacks.Instance?.skillFeedback?.PlayFeedbacks();
        yield break;
    }

    private IEnumerator MainDpsSkillTwo(Character character)
    {
        if (character == null)
        {
            yield break;
        }

        State deadlyArmorState = character.AddState(StateType.DeadlyArmor, character, 1, 1);
        if (deadlyArmorState != null)
        {
            if (extraData1 > 0f)
            {
                deadlyArmorState.baseExtraData2 = extraData1;
            }

            if (extraData2 > 0f)
            {
                deadlyArmorState.baseExtraData1 = extraData2;
            }
        }
        yield break;
    }

    private IEnumerator SubDpsEnter(Character character)
    {
        if (character == null)
        {
            yield break;
        }

        int durationActionValue = Mathf.RoundToInt(extraData1);
        if (FieldDomainScreenEffectController.Instance != null)
        {
            yield return FieldDomainScreenEffectController.Instance.PlayExpand(EnvironmentType.DesperationField, character.transform);
        }
        EnvironmentManager.Instance?.AddEnvironment(EnvironmentType.DesperationField, durationActionValue, character);
        character.AddState(StateType.CritRhythm, character, 99, 1);

        Enemy lowestHpEnemy = GetLowestHealthRatioEnemy();
        if (lowestHpEnemy != null)
        {
            NotifyDamageSkillUsed(character, new List<UnitCombatant> { lowestHpEnemy });
            var damageInfo = DamageCounter.CountDamage(character, lowestHpEnemy, skillCoef, skillBase, DamageType.Physical, false, true, true);
            lowestHpEnemy.TakeDamage(damageInfo);
            lowestHpEnemy.AddState(StateType.DesperationMark, character, durationActionValue, 1);
        }

        yield break;
    }

    private IEnumerator SubDpsExit(Character character)
    {
        if (character == null || EnemyManager.Instance == null)
        {
            yield break;
        }

        float recorded = 0f;
        State bloodBath = character.GetState(StateType.BloodBath);
        if (bloodBath != null)
        {
            recorded = Mathf.Max(0f, bloodBath.atkT);
            character.RemoveState(bloodBath);
        }

        float highThresholdRatio = extraData1;
        float midThresholdRatio = extraData2;
        int dazeDuration = Mathf.Max(1, Mathf.RoundToInt(extraData3));
        int vulnerableStacks = Mathf.Max(1, Mathf.RoundToInt(extraData4));
        var enemies = new List<Enemy>(EnemyManager.Instance.AliveEnemies);
        if (recorded >= character.maxHP * highThresholdRatio)
        {
            foreach (var enemy in enemies)
            {
                if (enemy == null)
                {
                    continue;
                }

                enemy.AddState(StateType.Daze, character, dazeDuration, 1);
            }
            yield break;
        }

        if (recorded >= character.maxHP * midThresholdRatio)
        {
            foreach (var enemy in enemies)
            {
                if (enemy == null)
                {
                    continue;
                }

                enemy.AddState(StateType.Vulnerable, character, 99, vulnerableStacks);
            }
            yield break;
        }

        NotifyDamageSkillUsed(character, enemies);
        foreach (var enemy in enemies)
        {
            if (enemy == null)
            {
                continue;
            }
            var damageInfo = DamageCounter.CountDamage(character, enemy, skillCoef, skillBase, DamageType.Physical, false, true, true);
            enemy.TakeDamage(damageInfo);
        }

        yield break;
    }

    private IEnumerator SubDpsSkillOne(Character character, List<Enemy> selectedEnemies)
    {
        Enemy target = GetPrimaryEnemy(selectedEnemies);
        if (character == null || target == null)
        {
            yield break;
        }

        NotifyDamageSkillUsed(character, new List<UnitCombatant> { target });
        int armorBreakStacks = Mathf.Max(1, Mathf.RoundToInt(extraData1));
        target.AddState(StateType.ArmorBreak, character, 99, armorBreakStacks);
        var damageInfo = DamageCounter.CountDamage(character, target, skillCoef, skillBase, DamageType.Physical, false, true, true);
        target.TakeDamage(damageInfo);
        yield break;
    }

    private IEnumerator SubDpsSkillTwo(Character character)
    {
        if (character == null)
        {
            yield break;
        }

        NotifyDamageSkillUsed(character, new List<UnitCombatant> { character });
        int selfDamage = Mathf.RoundToInt(character.maxHP * extraData1);
        if (character.currentHP <= selfDamage)
        {
            character.TakeDamage(new UnitCombatant.DamageInfo(character.currentHP - 1, character).AsTrueDamage());
        }
        else
        {
            character.TakeDamage(new UnitCombatant.DamageInfo(selfDamage, character).AsTrueDamage());
        }

        int healTriggerCount = Mathf.Max(1, Mathf.RoundToInt(extraData3));
        State bloodSurgeHeal = character.AddState(StateType.BloodSurgeHeal, character, 99, healTriggerCount);
        if (bloodSurgeHeal != null)
        {
            if (extraData2 > 0f)
            {
                bloodSurgeHeal.baseExtraData2 = extraData2;
            }

            if (extraData4 > 0f)
            {
                bloodSurgeHeal.baseExtraData1 = extraData4;
            }
        }
        FloatingTipGenerator.Instance?.ShowTipAtObject(character.transform, $"{character.name}进入浴血反哺");
        yield break;
    }

    private IEnumerator HealerEnter(Character character)
    {
        if (character == null)
        {
            yield break;
        }

        Character target = CharacterManager.Instance?.GetAnotherFieldCharacter(character);
        if (target == null)
        {
            yield break;
        }

        List<Character> allies = GetAllLivingAllies();

        int debuffClearCount = Mathf.Max(1, Mathf.RoundToInt(extraData1 > 0f ? extraData1 : 3f));
        float missingHpHealRatio = extraData2 > 0f ? extraData2 : 0.4f;

        RemoveDebuffs(target, debuffClearCount);

        target.AddState(StateType.RestorationSurge, character, 1, 1);

        HealAlliesByMissingHp(allies, missingHpHealRatio);
        yield break;
    }

    /* private IEnumerator HealerExit(Character character)
     {
         if (character == null || CharacterManager.Instance == null)
         {
             yield break;
         }

         Character target = CharacterManager.Instance.GetPendingSwapInCharacter(character);

         if (target == null)
         {
             yield break;
         }

         float highHpThreshold = extraData1;
         float midHpThreshold = extraData2;
         int weakGiftActions = Mathf.Max(1, Mathf.RoundToInt(extraData3));
         int sharedGiftActions = Mathf.Max(1, Mathf.RoundToInt(extraData4));
         float hpRatio = character.maxHP > 0 ? (float)character.currentHP / character.maxHP : 0f;
         if (hpRatio >= highHpThreshold)
         {
             target.AddState(StateType.GiftWeak, character, 99, weakGiftActions);
         }
         else if (hpRatio >= midHpThreshold)
         {
             target.AddState(StateType.GiftMid, character, 99, sharedGiftActions);
         }
         else
         {
             target.AddState(StateType.GiftStrong, character, 99, sharedGiftActions);
         }

         yield break;
     }*/

    private IEnumerator HealerSkillOne(Character character, List<Character> selectedCharacters)
    {
        if (character == null)
        {
            yield break;
        }

        Character target = GetOtherAlly(selectedCharacters, character);
        if (character == null || target == null)
        {
            yield break;
        }

        float maxHpHealRatio = extraData1 > 0f ? extraData1 : 0.1f;
        int healingSpringDuration = Mathf.Max(1, Mathf.RoundToInt(extraData2 > 0f ? extraData2 : 1f));

        HealAlliesByMaxHp(GetAllLivingAllies(), maxHpHealRatio);
        target.AddState(StateType.HealingSpring, character, healingSpringDuration, 1);
        yield break;
    }

    private IEnumerator HealerSkillTwo(Character character, List<Character> selectedCharacters)
    {
        if (character == null)
        {
            yield break;
        }

        int durationActionValue = Mathf.RoundToInt(extraData1 > 0f ? extraData1 : 100f);
        if (FieldDomainScreenEffectController.Instance != null)
        {
            yield return FieldDomainScreenEffectController.Instance.PlayExpand(EnvironmentType.MiracleField, character.transform);
        }
        EnvironmentManager.Instance?.AddEnvironment(EnvironmentType.MiracleField, durationActionValue, character);

        if (TurnStateManager.Instance != null)
        {
            yield return TurnStateManager.Instance.ChangeState(TurnState.OutCharacterTurn, character);
        }
        if (CharacterManager.Instance != null)
        {
            yield return CharacterManager.Instance.SelectAndSwapCoroutine(character);
        }

        character.EndTurn();
        yield break;
    }
    #endregion

    private bool RequiresRuntimeAllyTarget()
    {
        switch (skillType)
        {
            case CharacterSkillType.HealerSkillTwo:
                return false;
            default:
                return requiresAllyTarget;
        }
    }

    private bool ResolveShouldEndTurn()
    {
        switch (skillType)
        {
            case CharacterSkillType.HealerSkillOne:
                return true;
            case CharacterSkillType.HealerSkillTwo:
                return false;
            case CharacterSkillType.MainDpsSkillOneAdditional:
            case CharacterSkillType.PursuitPunishAdditional:
                return false;
            default:
                return endTurnAfterUse;
        }
    }

    private Enemy GetPrimaryEnemy(List<Enemy> selectedEnemies)//获取第一个敌人
    {
        if (selectedEnemies == null || selectedEnemies.Count <= 0)
        {
            return null;
        }

        return selectedEnemies[0];
    }

    private IEnumerator HandleMainDpsSwapOut(Character character)
    {
        if (character == null)
        {
            yield break;
        }

        RemoveStateIfExists(character, StateType.BerserkFeast);
        RemoveStateIfExists(character, StateType.BurningBlood);

        Enemy target = GetLowestHealthRatioEnemy();
        if (target == null)
        {
            yield break;
        }

        NotifyDamageSkillUsed(character, new List<UnitCombatant> { target });

        float executeThreshold = IsBoss(target) ? 0.2f : 0.4f;
        if (target.currentHP <= Mathf.RoundToInt(target.maxHP * executeThreshold))
        {
            target.TakeDamage(new UnitCombatant.DamageInfo(target.currentHP + target.currentShield + 99999, character).AsTrueDamage());
        }
        else
        {
            float maxHpRatio = IsBoss(target) ? 0.1f : 0.2f;
            int damage = Mathf.RoundToInt(target.maxHP * maxHpRatio + character.attack * 1.6f);
            target.TakeDamage(new UnitCombatant.DamageInfo(damage, character).AsTrueDamage());
        }

        GlobalFeedbacks.Instance?.skillFeedback?.PlayFeedbacks();
        yield return UnitCombatant.WaitForPendingDeaths();
    }

    private Character GetPrimaryAlly(List<Character> selectedCharacters, Character fallback)
    {
        if (selectedCharacters != null && selectedCharacters.Count > 0 && selectedCharacters[0] != null)
        {
            return selectedCharacters[0];
        }

        if (CharacterManager.Instance == null)
        {
            return null;
        }

        foreach (var ally in CharacterManager.Instance.fieldCharacters)
        {
            if (ally != null && ally != fallback)
            {
                return ally;
            }
        }

        return null;
    }

    private Character GetOtherAlly(List<Character> selectedCharacters, Character fallback)
    {
        if (selectedCharacters != null)
        {
            for (int i = 0; i < selectedCharacters.Count; i++)
            {
                Character selectedCharacter = selectedCharacters[i];
                if (selectedCharacter != null && selectedCharacter != fallback)
                {
                    return selectedCharacter;
                }
            }
        }

        return CharacterManager.Instance?.GetAnotherFieldCharacter(fallback);
    }

    private List<Character> GetAllLivingAllies()
    {
        List<Character> allies = new List<Character>();
        if (CharacterManager.Instance == null)
        {
            return allies;
        }

        IReadOnlyList<Character> source = CharacterManager.Instance.allCharacters != null
            && CharacterManager.Instance.allCharacters.Count > 0
            ? CharacterManager.Instance.allCharacters
            : CharacterManager.Instance.fieldCharacters;

        for (int i = 0; i < source.Count; i++)
        {
            Character ally = source[i];
            if (ally == null || ally.IsDead)
            {
                continue;
            }

            allies.Add(ally);
        }

        return allies;
    }
    private void HealAlliesByMissingHp(List<Character> allies, float ratio)
    {
        for (int i = 0; i < allies.Count; i++)
        {
            Character ally = allies[i];
            if (ally == null)
            {
                continue;
            }

            int missingHp = Mathf.Max(0, ally.maxHP - ally.currentHP);
            if (missingHp <= 0)
            {
                continue;
            }

            ally.Heal(Mathf.RoundToInt(missingHp * ratio));
        }
    }

    private void HealAlliesByMaxHp(List<Character> allies, float ratio)
    {
        for (int i = 0; i < allies.Count; i++)
        {
            Character ally = allies[i];
            if (ally == null || ally.maxHP <= 0)
            {
                continue;
            }

            ally.Heal(Mathf.RoundToInt(ally.maxHP * ratio));
        }
    }

    private void RemoveDebuffs(Character target, int removeCount)
    {
        if (target == null || removeCount <= 0)
        {
            return;
        }

        List<State> states = new List<State>(target.States);
        int removedCount = 0;
        for (int i = 0; i < states.Count && removedCount < removeCount; i++)
        {
            State state = states[i];
            if (state == null || !state.isDebuff)
            {
                continue;
            }

            if (target.RemoveState(state))
            {
                removedCount++;
            }
        }
    }

    private bool HasAnyDebuff(UnitCombatant target)
    {
        if (target == null)
        {
            return false;
        }

        foreach (var state in target.States)
        {
            if (state != null && state.isDebuff)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsBoss(Enemy enemy)
    {
        return enemy != null && enemy.IsBossForSkillRules();
    }

    private Enemy GetLowestHealthRatioEnemy()
    {
        if (EnemyManager.Instance == null)
        {
            return null;
        }

        Enemy bestTarget = null;
        float lowestHpRatio = float.MaxValue;
        for (int i = 0; i < EnemyManager.Instance.AliveEnemies.Count; i++)
        {
            Enemy enemy = EnemyManager.Instance.AliveEnemies[i];
            if (enemy == null || enemy.maxHP <= 0)
            {
                continue;
            }

            float hpRatio = (float)enemy.currentHP / enemy.maxHP;
            if (bestTarget == null || hpRatio < lowestHpRatio)
            {
                bestTarget = enemy;
                lowestHpRatio = hpRatio;
            }
        }

        return bestTarget;
    }

    private void TryApplyPersistentTormentDaze(State tormentState)
    {
        if (tormentState == null || tormentState.owner == null)
        {
            return;
        }

        int maxStacks = Mathf.Max(1, tormentState.MaxStacks);
        float chancePerStack = tormentState.baseExtraData2;
        int stunDuration = Mathf.RoundToInt(tormentState.baseExtraData3);
        int validLayer = Mathf.Clamp(tormentState.StackCount, 0, maxStacks);
        float chance = Mathf.Min(maxStacks * chancePerStack, validLayer * chancePerStack);
        if (chance <= 0f || Random.value > chance)
        {
            return;
        }

        tormentState.owner.AddState(StateType.Daze, tormentState.giver != null ? tormentState.giver : tormentState.owner, stunDuration, 1);
        FloatingTipGenerator.Instance?.ShowTipAtObject(tormentState.owner.transform, $"{tormentState.owner.name}受到震慑");
    }


    private void RemoveStateIfExists(UnitCombatant target, StateType stateType)
    {
        if (target == null)
        {
            return;
        }

        State state = target.GetState(stateType);
        if (state != null)
        {
            target.RemoveState(state);
        }
    }

    private StateType PickRandomDotState(Enemy enemy)
    {
        List<StateType> candidates = new List<StateType> { StateType.Ice, StateType.Corrosion, StateType.Wind };
        List<StateType> missing = new List<StateType>();
        for (int i = 0; i < candidates.Count; i++)
        {
            if (!enemy.HasState(candidates[i]))
            {
                missing.Add(candidates[i]);
            }
        }

        List<StateType> pool = missing.Count > 0 ? missing : candidates;
        return pool[UnityEngine.Random.Range(0, pool.Count)];
    }

    private static void ApplyStateBaseExtraData(State state, float? extra1 = null, float? extra2 = null, float? extra3 = null, float? extra4 = null)
    {
        if (state == null)
        {
            return;
        }

        if (extra1.HasValue)
        {
            state.baseExtraData1 = extra1.Value;
        }

        if (extra2.HasValue)
        {
            state.baseExtraData2 = extra2.Value;
        }

        if (extra3.HasValue)
        {
            state.baseExtraData3 = extra3.Value;
        }

        if (extra4.HasValue)
        {
            state.baseExtraData4 = extra4.Value;
        }
    }

    private static void ApplyBerserkFeastCritBonus(State berserkFeast, float critBonus)
    {
        if (berserkFeast == null || berserkFeast.owner == null || critBonus <= 0f)
        {
            return;
        }

        berserkFeast.owner.critRate = Mathf.Max(0f, berserkFeast.owner.critRate - berserkFeast.baseExtraData2);
        berserkFeast.baseExtraData2 = critBonus;
        berserkFeast.owner.critRate += critBonus;
    }

    public int GetRemainingCooldown(Character owner)
    {
        if (owner == null || !m_runtimeCooldown.TryGetValue(owner, out int remain))
        {
            return 0;
        }

        return Mathf.Max(0, remain);
    }

    public void TickCooldown(Character owner)
    {
        if (owner == null)
        {
            return;
        }

        if (!m_runtimeCooldown.TryGetValue(owner, out int remain) || remain <= 0)
        {
            return;
        }

        m_runtimeCooldown[owner] = remain - 1;
    }

    private void StartCooldown(Character owner)
    {
        if (owner == null)
        {
            return;
        }

        int actualCooldown = cooldownTurns;

        if (actualCooldown <= 0)
        {
            return;
        }

        m_runtimeCooldown[owner] = actualCooldown;
    }
}