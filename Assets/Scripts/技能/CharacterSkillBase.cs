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
    Change,
    EnterSkillOne,
    ExitSkillOne,
    AllAttack,
    PursuitPunish,
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

[CreateAssetMenu(fileName = "NewSkill", menuName = "技能/CharacterSkill"), System.Serializable]
public class CharacterSkillBase : SkillBase
{
    public SkillTargetType skillTargetType;
    public CharacterSkillType skillType;
    [Header("额外参数")]
    public float extraData1;
    public float extraData2;
    public float extraData3;
    public float extraData4;

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

    private static void NotifyDamageSkillUsed(UnitCombatant unitCombatant)
    {
        State.NotifyCombatEvent(unitCombatant, StateCombatEventType.DamageSkillUsed);
    }

    public override IEnumerator Execute(UnitCombatant unitCombatant)
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

        List<Enemy> selectedEnemies = null;
        if (requiresEnemyTarget)
        {
            if (SkillManager.Instance == null)
            {
                Debug.LogWarning("[SkillBase] 缺少 SkillManager，无法进入选敌流程");
                character.EndTurn();
                yield break;
            }

            selectedEnemies = new List<Enemy>();
            yield return SkillManager.Instance.SelectEnemiesCoroutine(enemyTargetCount, selectedEnemies);
        }

        List<Character> selectedCharacters = null;
        if (requiresAllyTarget)
        {
            if (SkillManager.Instance == null)
            {
                Debug.LogWarning("[SkillBase] 缺少 SkillManager，无法进入选友流程");
                character.EndTurn();
                yield break;
            }

            selectedCharacters = new List<Character>();
            yield return SkillManager.Instance.SelectCharactersCoroutine(allyTargetCount, selectedCharacters);
        }

        bool shouldEndTurn = endTurnAfterUse;
        //技能镜头过渡
        CinemachineCameraManager cameraManager = CinemachineCameraManager.Instance;
        bool useSkillCameraTransition = cameraManager != null;
        if (useSkillCameraTransition && shouldEndTurn)
        {
            //使按钮取消
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
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            FloatingTipGenerator.Instance.ShowDefaultTip(SkillDictionaryManager.GetSkillName(skillType));
        }

        switch (skillType)
        {
            case CharacterSkillType.AllAttack:
                yield return ExecuteAllAttack(character);
                break;
            case CharacterSkillType.PursuitPunish:
                yield return PursuitPunish(character);
                break;
            case CharacterSkillType.EnterSkillOne:
                yield return EnterSkillOne(character);
                break;
            case CharacterSkillType.ExitSkillOne:
                yield return ExitSkillOne(character);
                break;
            case CharacterSkillType.EnterSkillTwo:
                yield return EnterSkillTwo(character);
                break;
            case CharacterSkillType.ExitSkillTwo:
                yield return ExitSkillTwo(character);
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
            case CharacterSkillType.ExitSkillThree:
                yield return ExitSkillThree(character);
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
            case CharacterSkillType.MainDpsExit:
                yield return MainDpsExit(character, selectedEnemies);
                break;
            case CharacterSkillType.MainDpsSkillOne:
                yield return MainDpsSkillOne(character, selectedEnemies);
                break;
            case CharacterSkillType.MainDpsSkillTwo:
                shouldEndTurn = false;
                yield return MainDpsSkillTwo(character);
                break;
            case CharacterSkillType.SubDpsEnter:
                yield return SubDpsEnter(character);
                break;
            case CharacterSkillType.SubDpsExit:
                yield return SubDpsExit(character);
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
            case CharacterSkillType.HealerExit:
                yield return HealerExit(character);
                break;
            case CharacterSkillType.HealerSkillOne:
                shouldEndTurn = false;
                yield return HealerSkillOne(character, selectedCharacters);
                break;
            case CharacterSkillType.HealerSkillTwo:
                yield return HealerSkillTwo(character, selectedCharacters);
                break;
        }
        //受击动画占位
        yield return new WaitForSeconds(0.5f);

        if (useSkillCameraTransition && shouldEndTurn)
        {
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
        int duration = Mathf.RoundToInt(extraData1);
        float verdictDotMultiplier = extraData2;
        float cutdownDirectMultiplier = extraData3;
        float cutdownDotBonus = extraData4;
        FloatingTipGenerator.Instance.ShowTipAtObject(character.transform, $"{character.name}的入场技能触发，获得重力环境");
        EnvironmentManager.Instance.AddEnvironment(EnvironmentType.Gravity, duration, character, verdictDotMultiplier);
        EnvironmentManager.Instance.AddEnvironment(EnvironmentType.Cutdown, duration, character, cutdownDirectMultiplier, cutdownDotBonus);
        yield break;
    }
    private IEnumerator ExitSkillOne(Character character)
    {
        float dotTriggerMultiplier = extraData1;
        NotifyDamageSkillUsed(character);
        FloatingTipGenerator.Instance.ShowTipAtObject(character.transform, $"{character.name}的离场技能触发，结算dot伤害");
        var enemies = new List<Enemy>(EnemyManager.Instance.AliveEnemies);
        State.RunDotTriggerEvent(character, () =>
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
        NotifyDamageSkillUsed(character);
        var enemies = new List<Enemy>(EnemyManager.Instance.AliveEnemies);
        State.RunDotTriggerEvent(character, () =>
        {
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    var damageInfo = DamageCounter.CountDamage(character, enemy, this);
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
    private IEnumerator EnterSkillTwo(Character character)
    {
        if (EnemyManager.Instance == null)
        {
            yield break;
        }

        var enemies = new List<Enemy>(EnemyManager.Instance.AliveEnemies);
        foreach (var enemy in enemies)
        {
            if (enemy == null)
            {
                continue;
            }

            int duration = Mathf.RoundToInt(extraData1);
            enemy.AddState(StateType.PersistentTorment, character, duration, 1);
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
        float dotSkillCoef = extraData1;
        float refreshMultiplier = extraData3;
        NotifyDamageSkillUsed(character);

        var enemies = new List<Enemy>(EnemyManager.Instance.AliveEnemies);
        State.RunDotTriggerEvent(character, () =>
        {
            foreach (var enemy in enemies)
            {
                if (enemy == null)
                {
                    continue;
                }

                var damageInfo = DamageCounter.CountDamage(character, enemy, this);
                enemy.TakeDamage(damageInfo);

                StateType dotType = PickRandomDotState(enemy);
                bool hadDot = enemy.HasState(dotType);
                State state = enemy.AddState(dotType, character, dotDuration, 1, dotSkillCoef);
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

        NotifyDamageSkillUsed(character);
        bool ifAttack = false;
        State.RunDotTriggerEvent(character, () =>
        {
            foreach (var state in target.States)
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

        character.AddState(StateType.CounterCharge, character, 99, 1, extraData1);
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
        if (selectedEnemies == null || selectedEnemies.Count <= 0)
        {
            yield break;
        }

        Enemy target = selectedEnemies[0];
        if (target == null)
        {
            yield break;
        }
        //读取参数：extraData1 进度条推进比例，extraData2 伤害减免比例，extraData3 嘲讽持续回合数
        float advanceRatio = extraData1;
        float damageReduction = extraData2;
        int tauntDuration = Mathf.Max(1, Mathf.RoundToInt(extraData3));
        float outgoingMultiplier = Mathf.Max(0f, 1f - damageReduction);

        target.AddState(StateType.Taunt, character, tauntDuration, 1);
        if (damageReduction > 0f)
        {
            target.AddState(StateType.ActionWeakened, character, 1, 1, outgoingMultiplier);
        }
        target.ChangeActionValue(Mathf.Max(0f, target.currentActionValue - target.currentActionValue * advanceRatio));
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
        float nextActionDamageBoost = extraData3;
        float shieldAttackCoef = extraData4;
        int shield = Mathf.RoundToInt(target.maxHP * shieldHpCoef + character.attack * shieldAttackCoef);
        target.AddShield(shield);
        if (nextActionDamageBoost > 0f)
        {
            target.AddState(StateType.NextActionDamageBoost, character, 1, 1, 1f + nextActionDamageBoost);
        }

        target.ChangeActionValue(target.currentActionValue - target.currentActionValue * advanceRatio);
        yield break;
    }

    private IEnumerator MainDpsEnter(Character character)
    {
        if (character == null)
        {
            yield break;
        }

        int berserkDuration = Mathf.RoundToInt(extraData1);
        State berserkState = character.AddState(StateType.BerserkFeast, character, berserkDuration, 1, extraData2);
        if (berserkState != null && extraData3 > 0f)
        {
            berserkState.baseExtraData1 = extraData3;
        }

        character.AddState(StateType.BurningBlood, character, 99, 1, extraData4);
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

        NotifyDamageSkillUsed(character);

        //斩杀
        float normalExecuteThreshold = extraData1;
        float bossExecuteThreshold = extraData2;
        float executeThreshold = IsBoss(target) ? bossExecuteThreshold : normalExecuteThreshold;
        if (target.currentHP <= Mathf.RoundToInt(target.maxHP * executeThreshold))
        {
            target.TakeDamage(new UnitCombatant.DamageInfo(target.currentHP + target.currentShield + 99999, character).AsTrueDamage());
            yield break;
        }
        //伤害计算
        float normalHpCoef = extraData3;
        float bossHpCoef = extraData4;
        float hpCoef = IsBoss(target) ? bossHpCoef : normalHpCoef;
        var damageInfo = DamageCounter.CountDamage(character, target, this.skillCoef, this.skillBase + Mathf.RoundToInt(target.maxHP * hpCoef), true, false, true);
        target.TakeDamage(damageInfo);
        yield break;
    }

    private IEnumerator MainDpsSkillOne(Character character, List<Enemy> selectedEnemies)
    {
        Enemy target = GetPrimaryEnemy(selectedEnemies);
        if (character == null || target == null)
        {
            yield break;
        }

        NotifyDamageSkillUsed(character);

        float normalCoef = skillCoef;
        float debuffCoef = extraData1;
        float coef = HasAnyDebuff(target) ? debuffCoef : normalCoef;

        bool isCrit;
        var damageInfo = DamageCounter.CountDamage(character, target, coef, skillBase, false, true, true, out isCrit);

        target.TakeDamage(damageInfo);
        //在这里添加额外伤害的逻辑，基于暴击与否以及目标身上的状态
        State berserkState = character.GetState(StateType.BerserkFeast);
        if (berserkState != null && isCrit && target.currentHP > 0)
        {
            float chainRatio = Mathf.Clamp01(berserkState.baseExtraData1);
            if (chainRatio > 0f)
            {
                yield return ExecuteBerserkChainAttack(character, target, coef, skillBase, chainRatio);
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

        State deadlyArmorState = character.AddState(StateType.DeadlyArmor, character, 1, 1, extraData1);
        if (deadlyArmorState != null && extraData2 > 0f)
        {
            deadlyArmorState.baseExtraData1 = extraData2;
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
        EnvironmentManager.Instance?.AddEnvironment(EnvironmentType.DesperationField, durationActionValue, character);
        character.AddState(StateType.BloodBath, character, durationActionValue);
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

        NotifyDamageSkillUsed(character);
        foreach (var enemy in enemies)
        {
            if (enemy == null)
            {
                continue;
            }
            var damageInfo = DamageCounter.CountDamage(character, enemy, skillCoef, skillBase, false, true, true);
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

        NotifyDamageSkillUsed(character);
        int armorBreakStacks = Mathf.Max(1, Mathf.RoundToInt(extraData1));
        target.AddState(StateType.ArmorBreak, character, 99, armorBreakStacks);
        var damageInfo = DamageCounter.CountDamage(character, target, skillCoef, skillBase, false, true, true);
        target.TakeDamage(damageInfo);
        yield break;
    }

    private IEnumerator SubDpsSkillTwo(Character character)
    {
        if (character == null)
        {
            yield break;
        }

        NotifyDamageSkillUsed(character);
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
        State bloodSurgeHeal = character.AddState(StateType.BloodSurgeHeal, character, 99, healTriggerCount, extraData2);
        if (bloodSurgeHeal != null && extraData4 > 0f)
        {
            bloodSurgeHeal.baseExtraData1 = extraData4;
        }
        FloatingTipGenerator.Instance?.ShowTipAtObject(character.transform, $"{character.name}进入浴血反哺");
        yield break;
    }

    private IEnumerator HealerEnter(Character character)
    {
        Character target = CharacterManager.Instance?.GetAnotherFieldCharacter(character);
        if (character == null || target == null)
        {
            yield break;
        }

        int durationActionValue = Mathf.RoundToInt(extraData1);
        target.AddState(StateType.BloodContract, character, durationActionValue);
        target.AddState(StateType.CriticalGuard, character, durationActionValue);
        yield break;
    }

    private IEnumerator HealerExit(Character character)
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
    }

    private IEnumerator HealerSkillOne(Character character, List<Character> selectedCharacters)
    {
        Character target = GetPrimaryAlly(selectedCharacters, character);
        if (character == null || target == null)
        {
            yield break;
        }

        NotifyDamageSkillUsed(character);
        int healAmount = Mathf.RoundToInt(character.maxHP * extraData1);
        if (target.HasState(StateType.BurningBlood) || target.HasState(StateType.BloodBath))
        {
            healAmount = Mathf.RoundToInt(healAmount * (1f + extraData3));
        }

        target.Heal(healAmount);

        int selfDamage = Mathf.RoundToInt(healAmount * extraData2);
        character.TakeDamage(new UnitCombatant.DamageInfo(selfDamage, character).AsTrueDamage());
        yield break;
    }

    private IEnumerator HealerSkillTwo(Character character, List<Character> selectedCharacters)
    {
        Character target = GetPrimaryAlly(selectedCharacters, character);
        if (character == null || target == null)
        {
            yield break;
        }

        NotifyDamageSkillUsed(character);
        int selfDamage = Mathf.RoundToInt(character.maxHP * extraData1);
        character.TakeDamage(new UnitCombatant.DamageInfo(selfDamage, character).AsTrueDamage());

        int bloodGiftDuration = Mathf.Max(1, Mathf.RoundToInt(extraData3));
        target.AddState(StateType.BloodGift, character, bloodGiftDuration, 1);
        target.ChangeActionValue(Mathf.Max(0f, target.currentActionValue - target.currentActionValue * extraData2));
        yield break;
    }
    #endregion

    private Enemy GetPrimaryEnemy(List<Enemy> selectedEnemies)//获取第一个敌人
    {
        if (selectedEnemies == null || selectedEnemies.Count <= 0)
        {
            return null;
        }

        return selectedEnemies[0];
    }

    private IEnumerator ExecuteBerserkChainAttack(Character character, Enemy target, float baseCoef, int baseSkillBase, float chainRatio)
    {
        const int maxChainCount = 12;
        bool previousHitCrit = true;
        int chainCount = 0;

        while (previousHitCrit && target != null && target.currentHP > 0 && chainCount < maxChainCount)
        {
            bool isCrit;
            var chainDamage = DamageCounter.CountDamage(
                character,
                target,
                baseCoef * chainRatio,
                baseSkillBase * chainRatio,
                false,
                true,
                true,
                out isCrit);

            target.TakeDamage(chainDamage);
            previousHitCrit = isCrit;
            chainCount++;
            yield return new WaitForSeconds(0.1f);
        }
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
        if (enemy == null)
        {
            return false;
        }

        return !string.IsNullOrEmpty(enemy.enemyID)
            && enemy.enemyID.IndexOf("boss", System.StringComparison.OrdinalIgnoreCase) >= 0;
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