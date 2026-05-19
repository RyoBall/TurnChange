using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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
        StartCooldown(character);
        if (shouldEndTurn)
        {
            character.EndTurn();
        }
    }
    #region 技能具体执行逻辑
    private IEnumerator EnterSkillOne(Character character)
    {
        FloatingTipGenerator.Instance.ShowTipAtObject(character.transform, $"{character.name}的入场技能触发，获得重力环境");
        EnvironmentManager.Instance.AddEnvironment(EnvironmentType.Gravity, 200);
        yield break;
    }
    private IEnumerator ExitSkillOne(Character character)
    {
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
    private IEnumerator ExecuteAllAttack(Character character)
    {
        var enemies = new List<Enemy>(EnemyManager.Instance.AliveEnemies);
        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                var damageInfo = DamageCounter.CountDamage(character, enemy, this);
                enemy.TakeDamage(damageInfo);
                bool hadSeqFlame = enemy.HasState(StateType.SeqFlame);
                State state = enemy.AddState(StateType.SeqFlame, character, 2, 1, 1);

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

            enemy.AddState(StateType.PersistentTorment, character, 300);
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

            var damageInfo = DamageCounter.CountDamage(character, enemy, this);
            enemy.TakeDamage(damageInfo);

            StateType dotType = PickRandomDotState(enemy);
            enemy.AddState(dotType, character, 3, 1, 0.5f);
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

        bool ifAttack = false;
        foreach (var state in target.States)
        {
            if (state != null && state.isDot)
            {
                ifAttack = true;
                state.DotTrigger();
            }
        }
        target.AddState(StateType.ElementalDetonation, character, 2);
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

        character.AddState(StateType.CounterCharge, character, 99, 1);
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

            ally.AddState(StateType.Resist, character, 99, 1);
            ally.ChangeActionValue(ally.currentActionValue - ally.BaseActionValue * 0.25f);
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
        
        target.ChangeActionValue(target.currentActionValue - target.currentActionValue * 0.5f);
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

        //斩杀
        float executeThreshold = IsBoss(target) ? 0.2f : 0.4f;
        if (target.currentHP <= Mathf.RoundToInt(target.maxHP * executeThreshold))
        {
            target.TakeDamage(new UnitCombatant.DamageInfo(target.currentHP + target.currentShield + 99999, character).AsTrueDamage());
            yield break;
        }
        //伤害计算
        float hpCoef = IsBoss(target) ? 0.10f : 0.20f;
        var damageInfo = DamageCounter.CountDamage(character, target, this.skillCoef,this.skillBase+Mathf.RoundToInt(target.maxHP * hpCoef), true,false,true);
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

        float coef = HasAnyDebuff(target) ? 2.0f : 1.5f;

        bool isCrit;
        var damageInfo = DamageCounter.CountDamage(character, target, coef, 0f, false, true, true, out isCrit);

        target.TakeDamage(damageInfo);
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
        yield break;
    }

    private IEnumerator SubDpsEnter(Character character)
    {
        if (character == null)
        {
            yield break;
        }

        EnvironmentManager.Instance?.AddEnvironment(EnvironmentType.DesperationField, 300);

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

        target.AddState(StateType.ArmorBreak, character, 99, 1);
        var damageInfo = DamageCounter.CountDamage(character, target, this);
        target.TakeDamage(damageInfo);
        yield break;
    }

    private IEnumerator SubDpsSkillTwo(Character character)
    {
        if (character == null)
        {
            yield break;
        }

        //不走常规伤害流程，直接根据当前HP和即将受到的伤害来判断是否触发斩杀效果
        int selfDamage = Mathf.RoundToInt(character.maxHP * 0.5f);
        if (character.currentHP <= selfDamage)
        {
            character.TakeDamage(new UnitCombatant.DamageInfo(character.currentHP - 1, character).AsTrueDamage());
        }
        else
        {
            character.TakeDamage(new UnitCombatant.DamageInfo(selfDamage, character).AsTrueDamage());
        }

        character.AddState(StateType.BloodSurgeHeal, character, 99, 2);
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

        Character target = CharacterManager.Instance.GetPendingSwapInCharacter(character);

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

    private Enemy GetPrimaryEnemy(List<Enemy> selectedEnemies)//获取第一个敌人
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