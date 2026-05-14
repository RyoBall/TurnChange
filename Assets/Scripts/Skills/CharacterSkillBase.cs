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
    EnemyAttack
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
        yield return new WaitForSeconds(.5f);
        FloatingTipGenerator.Instance.ShowTipAtObject(character.transform, $"{character.name}的入场技能触发，获得重力环境");
        EnvironmentManager.Instance.AddEnvironment(EnvironmentType.Gravity, 200);
        yield break;
    }
    private IEnumerator ExitSkillOne(Character character)
    {
        yield return new WaitForSeconds(.5f);
        FloatingTipGenerator.Instance.ShowTipAtObject(character.transform, $"{character.name}的离场技能触发，结算dot伤害");
        foreach (var enemy in EnemyManager.Instance.AliveEnemies)
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
                int damage = Mathf.RoundToInt(character.attack * 0.5f);
                enemy.TakeDamage(damage, character, false, false);
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

        foreach (var enemy in EnemyManager.Instance.AliveEnemies)
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

        foreach (var enemy in EnemyManager.Instance.AliveEnemies)
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

        foreach (var enemy in EnemyManager.Instance.AliveEnemies)
        {
            if (enemy == null)
            {
                continue;
            }

            int damage = Mathf.RoundToInt(character.attack * 0.4f);
            enemy.TakeDamage(damage, character, false, false);

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
        foreach (var state in target.States)
        {
            if (state != null && state.isDot)
            {
                state.DotTrigger();
            }
        }
        GlobalFeedbacks.Instance?.skillFeedback?.PlayFeedbacks();
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
    #endregion

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
        return pool[Random.Range(0, pool.Count)];
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