using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public CharacterSkillType skillType;
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

        FloatingTipGenerator.Instance.ShowDefaultTip($"{skillName}");

        bool shouldEndTurn = endTurnAfterUse;

        switch (skillType)
        {
            case CharacterSkillType.Change:
                yield return ExcuteChange(character, selectedEnemies);
                break;
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
                yield return HealerEnter(character, selectedCharacters);
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

        StartCooldown(character);
        if (shouldEndTurn)
        {
            HandleBurningBloodOnTurnEnd(character);
            character.EndTurn();
        }
    }
    #region 技能具体执行逻辑
    private IEnumerator EnterSkillOne(Character character)
    {
        yield return new WaitForSeconds(.5f);
        FloatingTipGenerator.Instance.ShowTipAtObject(character.transform, $"{character.name}的入场技能触发，获得重力环境");
        EnvironmentManager.Instance.AddEnvironment(EnvironmentType.Gravity, 200);
        yield break;
    }
    private IEnumerator ExitSkillOne(Character character)
    {
        yield return new WaitForSeconds(.5f);
        FloatingTipGenerator.Instance.ShowTipAtObject(character.transform, $"{character.name}的离场技能触发，结算dot伤害");
        var enemies = new List<Enemy>(EnemyManager.Instance.AliveEnemies);
        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                foreach (var state in enemy.States)
                {
                    if (state.isDot)
                    {
                        state.DotTrigger(1.2f);
                    }
                }
            }
        }
        yield break;
    }
    private IEnumerator ExcuteChange(Character character, List<Enemy> selectedEnemies)
    {
        SkillManager.Instance.changeCharacter.GetComponent<Combatant>().ChangeActionValue(0);
        TurnManager.Instance.InsertCombatant(SkillManager.Instance.changeCharacter.GetComponent<Combatant>(), false);
        yield return new WaitForSeconds(.5f);
    }
    private IEnumerator ExecuteAllAttack(Character character)
    {
        var enemies = new List<Enemy>(EnemyManager.Instance.AliveEnemies);
        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                int damage = DamageCounter.CountDamage(character,enemy,this);
                enemy.TakeDamage(new UnitCombatant.DamageInfo(damage, character));
                bool hadSeqFlame = enemy.HasState(StateType.SeqFlame);
                State state=enemy.AddState(StateType.SeqFlame, character, 2,1);

                if (hadSeqFlame)
                {
                    state.DotTrigger(0.6f);
                }
            }
        }
        GlobalFeedbacks.Instance?.skillFeedback?.PlayFeedbacks();
        yield break;
    }
    private IEnumerator PursuitPunish(Character character)
    {
        if (character == null)
        {
            yield break;
        }

        character.AddState(StateType.PursuitPunish, character, 2);
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

            enemy.AddState(StateType.PersistentTorment, character,300);
        }

        yield break;
    }

    private IEnumerator ExitSkillTwo(Character character)
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

            foreach (var state in enemy.States)
            {
                if (state == null || !state.isDebuff || state.DurationType != StateDurationType.Turn)
                {
                    continue;
                }

                state.ExtendTurns(1);
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

        var enemies = new List<Enemy>(EnemyManager.Instance.AliveEnemies);
        foreach (var enemy in enemies)
        {
            if (enemy == null)
            {
                continue;
            }

            int damage = DamageCounter.CountDamage(character,enemy,this);
            enemy.TakeDamage(new UnitCombatant.DamageInfo(damage, character));

            StateType dotType = PickRandomDotState(enemy);
            enemy.AddState(dotType, character,3);
        }
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

        target.AddState(StateType.ElementalDetonation, character, 2);
        bool ifAttack=false;
        foreach (var state in target.States)
        {
            if (state != null && state.isDot)
            {
                ifAttack = true;
                state.DotTrigger();
            }
        }
        if(ifAttack)
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

        character.AddState(StateType.CounterCharge, character,99,1);
        yield break;
    }

    private IEnumerator ExitSkillThree(Character character)
    {
        if (CharacterManager.Instance == null)
        {
            yield break;
        }

        foreach (var ally in CharacterManager.Instance.fieldCharacters)
        {
            if (ally == null)
            {
                continue;
            }

            ally.AddState(StateType.Resist, character, 99,1);
            ally.ChangeActionValue(ally.currentActionValue-ally.BaseActionValue * 0.25f);
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

        target.AddState(StateType.Taunt, character, 2);
        target.ChangeActionValue(0f);
        TurnManager.Instance?.NotifyCombatantActionValueChanged(target);
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

        int shield = Mathf.RoundToInt(target.maxHP * 0.4f + 2f * character.attack);
        target.AddShield(shield);
        //缺少增加伤害的buff
        target.ChangeActionValue(target.currentActionValue-target.BaseActionValue * 0.5f);
        yield break;
    }
    private IEnumerator EnemyAttack(Character character)
    {
        if (EnemyManager.Instance == null || CharacterManager.Instance == null)
        {
            yield break;
        }

                

        yield break;
    }

    private IEnumerator MainDpsEnter(Character character)
    {
        if (character == null)
        {
            yield break;
        }

        character.AddState(StateType.BerserkFeast, character, 200);
        character.AddState(StateType.BurningBlood, character, 99);
        FloatingTipGenerator.Instance?.ShowTipAtObject(character.transform, $"{character.name}进入狂暴盛宴");
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

        float executeThreshold = IsBoss(target) ? 0.2f : 0.4f;
        if (target.currentHP <= Mathf.RoundToInt(target.maxHP * executeThreshold))
        {
            target.TakeDamage(new UnitCombatant.DamageInfo(target.currentHP + target.currentShield + 99999, character).AsTrueDamage());
            FloatingTipGenerator.Instance?.ShowTipAtObject(target.transform, $"{target.name}被斩杀");
            yield break;
        }

        float hpCoef = IsBoss(target) ? 0.10f : 0.20f;
        int trueDamage = Mathf.RoundToInt(target.maxHP * hpCoef + character.attack * 1.6f);
        trueDamage = Mathf.RoundToInt(trueDamage * target.GetIncomingDamageMultiplier(false, true));
        target.TakeDamage(new UnitCombatant.DamageInfo(trueDamage, character).AsTrueDamage());
        yield break;
    }

    private IEnumerator MainDpsSkillOne(Character character, List<Enemy> selectedEnemies)
    {
        Enemy target = GetPrimaryEnemy(selectedEnemies);
        if (character == null || target == null)
        {
            yield break;
        }

        float coef = HasAnyDebuff(target) ? 2.0f : 1.5f;
        bool hasDeadlyArmor = character.HasState(StateType.DeadlyArmor);

        bool isCrit;
        int damage = CalculateDirectDamage(character, target, coef, 0f, true, hasDeadlyArmor, 0f, out isCrit);
        var damageInfo = new UnitCombatant.DamageInfo(damage, character);
        if (hasDeadlyArmor)
        {
            damageInfo = damageInfo.AsTrueDamage();
        }

        target.TakeDamage(damageInfo);

        if (isCrit)
        {
            TryDoCritPursuit(character, target, coef * 0.5f, hasDeadlyArmor);
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

        character.AddState(StateType.DeadlyArmor, character, 1, 1);
        FloatingTipGenerator.Instance?.ShowTipAtObject(character.transform, $"{character.name}获得致命穿甲");
        yield break;
    }

    private IEnumerator SubDpsEnter(Character character)
    {
        if (character == null)
        {
            yield break;
        }

        if (CharacterManager.Instance != null)
        {
            foreach (var ally in CharacterManager.Instance.fieldCharacters)
            {
                if (ally == null)
                {
                    continue;
                }

                //ally.AddState(StateType.DesperationField, character, 300);
            }
        }

        character.AddState(StateType.BloodBath, character, 300);
        FloatingTipGenerator.Instance?.ShowTipAtObject(character.transform, $"{character.name}展开绝境域场");
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

        var enemies = new List<Enemy>(EnemyManager.Instance.AliveEnemies);
        if (recorded >= character.maxHP * 2f)
        {
            foreach (var enemy in enemies)
            {
                if (enemy == null)
                {
                    continue;
                }

                enemy.AddState(StateType.Daze, character, 1, 1);
            }
            yield break;
        }

        if (recorded >= character.maxHP)
        {
            foreach (var enemy in enemies)
            {
                if (enemy == null)
                {
                    continue;
                }

                enemy.AddState(StateType.Vulnerable, character, 99, 2);
            }
            yield break;
        }

        float ratio = character.maxHP > 0 ? recorded / character.maxHP : 0f;
        float coef = Mathf.Max(0f, ratio * 0.5f);
        foreach (var enemy in enemies)
        {
            if (enemy == null)
            {
                continue;
            }

            bool isCrit;
            int damage = CalculateDirectDamage(character, enemy, coef, 0f, true, false, 0f, out isCrit);
            enemy.TakeDamage(new UnitCombatant.DamageInfo(damage, character));
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

        target.AddState(StateType.ArmorBreak, character, 99, 1);
        bool isCrit;
        int damage = CalculateDirectDamage(character, target, 1.5f, 0f, true, false, 0f, out isCrit);
        target.TakeDamage(new UnitCombatant.DamageInfo(damage, character));
        yield break;
    }

    private IEnumerator SubDpsSkillTwo(Character character)
    {
        if (character == null)
        {
            yield break;
        }

        int selfDamage = Mathf.RoundToInt(character.maxHP * 0.5f);
        if (character.currentHP <= selfDamage)
        {
            character.currentHP = Mathf.Max(1, character.currentHP);
        }
        else
        {
            character.TakeDamage(new UnitCombatant.DamageInfo(selfDamage, character).AsTrueDamage());
        }

        character.AddState(StateType.BloodSurgeHeal, character, 99, 2);
        FloatingTipGenerator.Instance?.ShowTipAtObject(character.transform, $"{character.name}进入浴血反哺");
        yield break;
    }

    private IEnumerator HealerEnter(Character character, List<Character> selectedCharacters)
    {
        Character target = GetPrimaryAlly(selectedCharacters, character);
        if (character == null || target == null)
        {
            yield break;
        }

        target.AddState(StateType.BloodContract, character, 250);
        target.AddState(StateType.CriticalGuard, character, 250);
        yield break;
    }

    private IEnumerator HealerExit(Character character)
    {
        if (character == null || CharacterManager.Instance == null)
        {
            yield break;
        }

        Character target = null;
        foreach (var ally in CharacterManager.Instance.fieldCharacters)
        {
            if (ally != null && ally != character)
            {
                target = ally;
                break;
            }
        }

        if (target == null)
        {
            yield break;
        }

        float hpRatio = character.maxHP > 0 ? (float)character.currentHP / character.maxHP : 0f;
        if (hpRatio >= 0.7f)
        {
            target.AddState(StateType.GiftWeak, character, 99, 3);
        }
        else if (hpRatio >= 0.3f)
        {
            target.AddState(StateType.GiftMid, character, 99, 2);
        }
        else
        {
            target.AddState(StateType.GiftStrong, character, 99, 2);
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

        int healAmount = Mathf.RoundToInt(character.maxHP * 0.25f);
        if (target.HasState(StateType.BurningBlood) || target.HasState(StateType.BloodBath))
        {
            healAmount = Mathf.RoundToInt(healAmount * 1.3f);
        }

        target.Heal(healAmount);
        target.GetState(StateType.BloodBath)?.AddRecordedValue(healAmount);

        int selfDamage = Mathf.RoundToInt(healAmount * 0.6f);
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

        int selfDamage = Mathf.RoundToInt(character.maxHP * 0.2f);
        character.TakeDamage(new UnitCombatant.DamageInfo(selfDamage, character).AsTrueDamage());

        target.AddState(StateType.BloodGift, character, 1, 1);
        target.ChangeActionValue(Mathf.Max(0f, target.currentActionValue - target.BaseActionValue * 0.5f));
        yield break;
    }
    #endregion

    private Enemy GetPrimaryEnemy(List<Enemy> selectedEnemies)
    {
        if (selectedEnemies == null || selectedEnemies.Count <= 0)
        {
            return null;
        }

        return selectedEnemies[0];
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

    private int CalculateDirectDamage(
        UnitCombatant attacker,
        UnitCombatant defender,
        float atkCoef,
        float baseDamage,
        bool canCrit,
        bool asTrueDamage,
        float extraDamageMultiplier,
        out bool isCrit)
    {
        isCrit = false;
        if (attacker == null || defender == null)
        {
            return 0;
        }

        float random = UnityEngine.Random.Range(0.85f, 1.15f);
        isCrit = canCrit && UnityEngine.Random.value < attacker.critRate;

        float critMul = isCrit ? attacker.critDamage : 1f;
        float outgoing = attacker.GetOutgoingDamageMultiplier(false);
        float incoming = defender.GetIncomingDamageMultiplier(false, asTrueDamage);

        float raw = (attacker.attack * atkCoef + baseDamage) * random * critMul;
        raw *= outgoing;
        raw *= incoming;

        if (!asTrueDamage)
        {
            float defenseFactor = defender.K / (defender.K + Mathf.Max(0f, defender.defense));
            raw *= defenseFactor;
        }

        if (attacker.HasState(StateType.BerserkFeast))
        {
            float missingHpRatio = attacker.maxHP > 0 ? (float)(attacker.maxHP - attacker.currentHP) / attacker.maxHP : 0f;
            raw *= 1f + Mathf.Clamp01(missingHpRatio) * 0.4f;
        }

        if (extraDamageMultiplier > 0f)
        {
            raw *= extraDamageMultiplier;
        }

        return Mathf.Max(0, Mathf.RoundToInt(raw));
    }

    private void TryDoCritPursuit(Character attacker, Enemy target, float atkCoef, bool asTrueDamage)
    {
        if (attacker == null || target == null || target.currentHP <= 0)
        {
            return;
        }

        int safety = 0;
        while (target.currentHP > 0 && safety < 50)
        {
            safety++;
            bool isCrit;
            int damage = CalculateDirectDamage(attacker, target, atkCoef, 0f, true, asTrueDamage, 1f, out isCrit);
            var damageInfo = new UnitCombatant.DamageInfo(damage, attacker);
            if (asTrueDamage)
            {
                damageInfo = damageInfo.AsTrueDamage();
            }

            target.TakeDamage(damageInfo);

            if (!isCrit)
            {
                break;
            }
        }
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

    private void HandleBurningBloodOnTurnEnd(Character character)
    {
        if (character == null || !character.HasState(StateType.BurningBlood))
        {
            return;
        }

        if (BurningBloodStateBehavior.ConsumeKillFlag(character))
        {
            return;
        }

        int selfDamage = Mathf.RoundToInt(character.maxHP * 0.25f);
        character.TakeDamage(new UnitCombatant.DamageInfo(selfDamage, character).AsTrueDamage());
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