using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }
    /// <summary>战斗正式开始时的静态事件（相机 intro 完成、回合图片初始化后触发）</summary>
    public static event System.Action BattleStarted;

    public event System.Action OnTurnOrderChanged;
    [Tooltip("是否自动开始回合循环")]
    public bool autoStart = true;

    [Tooltip("每个回合结束后等待的帧数")]
    public float turnDelay = 0.1f;

    private List<Combatant> combatants = new List<Combatant>();
    private bool turnLoopStarted;
    private bool isTurnInitialized = false;
    private bool turnLoopPaused;
    public bool IsTurnInitialized => isTurnInitialized;
    public bool IsTurnLoopPaused => turnLoopPaused;
    // 回合顺序由链表维护，链表头永远表示下一个行动的角色。
    [SerializeField] private readonly LinkedList<Combatant> turnOrder = new LinkedList<Combatant>();

    public IEnumerable<Combatant> CurrentTurnOrder => turnOrder;
    public GameObject ExtraTurnPrefab;//额外回合的预制体

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        isTurnInitialized = false;
    }
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    private void Start()
    {

    }
    void RegisterStateUI()
    {
        foreach (var combatant in combatants)
        {
            if (combatant is UnitCombatant unitCombatant)
            {
                UnitStateTextDisplay.Instance.RegisterUnit(unitCombatant);
            }
        }
    }
    public void ExtraTurnInsert(Character character)//插入额外回合的接口
    {
        if (ExtraTurnPrefab != null)
        {
            ExtraCharacter extraCombatant = Instantiate(ExtraTurnPrefab, transform).GetComponent<ExtraCharacter>();
            if (extraCombatant != null)
            {
                extraCombatant.Initialize(character);
                extraCombatant.standPosition = character.standPosition;
            }
            extraCombatant.combatantName = character.combatantName;
            extraCombatant.ChangeActionValue(0f, false);
            InsertCombatant(extraCombatant);
        }
    }
    public void AdditionalTurnInsert(Character character)//插入追加回合的接口
    {
        AdditionalTurnInsert(character, null, null);
    }

    public void AdditionalTurnInsert(Character character, SkillBase skillOverride, List<Enemy> selectedEnemies)//插入带指定技能和目标的追加回合接口
    {
        if (character == null)
        {
            return;
        }

        GameObject additionalTurnObject = new GameObject($"AdditionalTurn_{character.combatantName}");
        additionalTurnObject.transform.SetParent(transform);

        AdditionalCharacter additionalCombatant = additionalTurnObject.AddComponent<AdditionalCharacter>();
        if (skillOverride != null || selectedEnemies != null)
        {
            additionalCombatant.Initialize(character, skillOverride, selectedEnemies);
        }
        else
        {
            additionalCombatant.Initialize(character);
        }

        additionalCombatant.combatantName = character.combatantName;
        additionalCombatant.standPosition = -1;
        additionalCombatant.ChangeActionValue(0f, false);
        InsertCombatant(additionalCombatant);
    }
    #region 回合开始的函数
    IEnumerator StartFight()
    {
        yield return new WaitUntil(() => CinemachineCameraManager.Instance == null || CinemachineCameraManager.Instance.HasCompletedOpeningIntro);
        //设置回合图片
        yield return StartCoroutine(SetTurnImages());
        BattleStarted?.Invoke();
        // 如果有教程系统，等待教程完成后再继续
        if (TutorialController.Instance != null)
        {
            yield return new WaitUntil(() => TutorialController.Instance == null || !TutorialController.Instance.IsTutorialActive);
        }
        TemporaryBattleModifierRuntimeManager.NotifyBattleStarted();
        yield return StartCoroutine(TriggerOpeningEnterSkills());
        //回合开始
        isTurnInitialized = true;
        yield return StartCoroutine(RunTurnLoop());
    }
    IEnumerator SetTurnImages()//初始化回合图片
    {
        if (TurnImageManager.Instance != null)
        {
            yield return StartCoroutine(TurnImageManager.Instance.InitializeTurnImages());
        }
    }

    IEnumerator TriggerOpeningEnterSkills()
    {
        var openingCharacters = combatants
            .OfType<Character>()
            .Where(character => character != null && character.participateInTurnLoopAtStart)
            .OrderBy(character => character.standPosition)
            .Take(2)
            .ToList();

        for (int i = 0; i < openingCharacters.Count; i++)
        {
            Character character = openingCharacters[i];
            CharacterSkillBase enterSkill = character.GetEnterSkillInstance();
            if (enterSkill == null)
            {
                continue;
            }

            SkillExecuteManager.ExecuteSkill(character, enterSkill);
            yield return new WaitUntil(() => !SkillExecuteManager.s_isExecutingSkill);
            yield return new WaitForSeconds(0.5f);
        }
    }

    public void InitializeTurnOrder(List<Character> fieldCharacters, List<Enemy> fieldEnemies)
    {
        combatants.Clear();

        if (fieldCharacters != null)
        {
            for (int i = 0; i < fieldCharacters.Count; i++)
            {
                Character character = fieldCharacters[i];
                if (character == null || !character.participateInTurnLoopAtStart)
                {
                    continue;
                }

                combatants.Add(character);
            }
        }

        if (fieldEnemies != null)
        {
            for (int i = 0; i < fieldEnemies.Count; i++)
            {
                Enemy enemy = fieldEnemies[i];
                if (enemy == null || !enemy.participateInTurnLoopAtStart)
                {
                    continue;
                }

                combatants.Add(enemy);
            }
        }

        foreach (var combatant in combatants)
        {
            combatant.ChangeActionValue(combatant.ConsumeTurnEndActionValue(), false);
        }

        BuildTurnOrder();
        RegisterStateUI();

        if (autoStart && !turnLoopStarted)
        {
            turnLoopStarted = true;
            StartCoroutine(StartFight());
        }
    }
    private void BuildTurnOrder()////构建回合顺序
    {
        turnOrder.Clear();

        combatants = combatants
            .OrderBy(c => c.currentActionValue)
            .ThenBy(c => c.standPosition)
            .ToList();
        foreach (var combatant in combatants)
        {
            turnOrder.AddLast(combatant);
        }

        NotifyTurnOrderChanged();
    }
    #endregion
    private IEnumerator RunTurnLoop()
    {
        while (true)
        {
            if (turnLoopPaused)
            {
                yield return null;
                continue;
            }

            ////// 回合开始前
            // 直接读取链表头，链表头就是下一位行动者。
            var nextNode = turnOrder.First;
            if (nextNode == null)
            {
                yield break;
            }
            //需要检测当前场上角色是否全部死亡，如果全部死亡则不继续回合循环
            bool allCharactersDead = true;
            foreach (var cha in CharacterManager.Instance.fieldCharacters)
            {
                if (cha != null && !cha.IsDead)
                {
                    allCharactersDead = false;
                    break;
                }
            }
            if (allCharactersDead)
            {
                //填充在场角色全部死亡的逻辑
                SkillExecuteManager.ExecuteSkill(null, Commander.GetInstance().changeSkill);
            }
            //读取当前行动者行动值
            var nextCombatant = nextNode.Value;
            // 用当前行动者的行动值推进整张时间轴。
            float advanceValue = nextCombatant.currentActionValue;
            var combatantsSnapshot = turnOrder.ToList();
            foreach (var combatant in combatantsSnapshot)
            {
                combatant.ChangeActionValue(Mathf.Max(0f, combatant.currentActionValue - advanceValue), false);
            }
            //推进换人技能的冷却
            if (CharacterManager.Instance != null)
            {
                for (int i = 0; i < CharacterManager.Instance.reserveCharacters.Count; i++)
                {
                    Character reserveCharacter = CharacterManager.Instance.reserveCharacters[i];
                    reserveCharacter?.ReduceSwitchCooldown(advanceValue);
                    if (reserveCharacter != null)
                    {
                        TemporaryBattleModifierRuntimeManager.NotifyReserveActionValueAdvanced(reserveCharacter, advanceValue);
                    }
                }
            }
            //推进状态持续时间
            State.TickAllStatesByActionValue(advanceValue);
            //推进指挥点回复时间
            Commander.GetInstance().NotifyActionValueAdvanced(advanceValue);
            //推进环境持续时间
            if (EnvironmentManager.Instance != null)
            {
                EnvironmentManager.Instance.TickEnvironments(advanceValue);
            }
            ////// 回合开始。
            yield return StartCoroutine(nextCombatant.PerformTurn());
            ////// 回合结束
            //检查是否全部敌人死亡
            if (LevelSetupManager.Instance != null)
            {
                yield return LevelSetupManager.Instance.ResolveBattleProgressAfterTurn();
                if (LevelSetupManager.Instance.IsBattleResolved)
                {
                    yield break;
                }
            }
            //结算回合状态
            if (EnvironmentManager.Instance != null && nextCombatant is UnitCombatant actedUnit)
            {
                actedUnit.ProcessStatesOnTurnEnd();
                EnvironmentManager.Instance.NotifyCombatantActed(actedUnit);
            }

            // 回合结束后重新计算当前角色的下一次行动值。
            if (nextCombatant != null && nextCombatant is UnitCombatant unitCombatant && !unitCombatant.IsDead)
            {
                float nextActionValue = nextCombatant.ConsumeTurnEndActionValue();
                nextCombatant.ChangeActionValue(nextActionValue);
                Debug.Log($"[TurnManager] 结束回合: {nextCombatant.name}，重置行动值到 {nextActionValue:F0}");
            }
            else
            {
                Debug.Log($"[TurnManager] 角色死亡");
            }


            //回合转换延迟
            if (turnDelay > 0f)
            {
                yield return new WaitForSeconds(turnDelay);
            }
            else
            {
                yield return null;
            }

        }
    }

    public Combatant GetCurrentTurnCombatant()//获取当前回合角色
    {
        return turnOrder.First?.Value;
    }

    public void NotifyCombatantActionValueChanged(Combatant combatant)//重置单个角色的回合位置
    {
        if (combatant == null || !turnOrder.Contains(combatant))
        {
            return;
        }

        RemoveCombatantFromTurnOrder(combatant);
        InsertCombatantByActionValue(combatant);

        if (TurnImageManager.Instance != null)
        {
            TurnImageManager.Instance.Reorder();
        }

        NotifyTurnOrderChanged();
    }

    //从外部插入角色回合的接口,在触发时默认进行一次重排
    public void InsertCombatant(Combatant combatant)
    {
        if (combatant is Changer && HasChangerTurn())
        {
            return;
        }

        InsertCombatantByActionValue(combatant);
        if (TurnImageManager.Instance != null)
        {
            TurnImageManager.Instance.Reorder();
        }
        //如果是新角色加入战斗，注册状态UI
        if (combatant is UnitCombatant unitCombatant)
        {
            UnitStateTextDisplay.Instance.RegisterUnit(unitCombatant);
        }

        NotifyTurnOrderChanged();
    }
    public void RemoveCombatant(Combatant combatant)
    {
        RemoveCombatantFromTurnOrder(combatant);
        if (TurnImageManager.Instance != null)
        {
            TurnImageManager.Instance.Reorder();
        }
        //如果是角色离开战斗，注销状态UI
        if (combatant is UnitCombatant unitCombatant)
        {
            UnitStateTextDisplay.Instance.UnregisterUnit(unitCombatant);
        }

        NotifyTurnOrderChanged();
    }
    #region 工具
    private void InsertCombatantByActionValue(Combatant combatant)//插入角色回合，目前默认插入时会排在所有相同行动值角色之前
    {
        if (combatant == null)
        {
            return;
        }

        foreach (var c in turnOrder)
        {
            if (c == combatant)
            {
                return;
            }
        }

        if (turnOrder.Count == 0)
        {
            turnOrder.AddFirst(combatant);
            return;
        }

        var node = turnOrder.First.Next;
        while (node != null && !ShouldInsertBefore(combatant, node.Value))
        {
            node = node.Next;
        }

        if (node == null)
        {
            turnOrder.AddLast(combatant);
        }
        else
        {
            turnOrder.AddBefore(node, combatant);
        }
    }

    private bool ShouldInsertBefore(Combatant candidate, Combatant existing)
    {
        if (candidate.currentActionValue < existing.currentActionValue)
        {
            return true;
        }

        if (candidate.currentActionValue > existing.currentActionValue)
        {
            return false;
        }

        if (candidate.standPosition < existing.standPosition)
        {
            return true;
        }

        if (candidate.standPosition > existing.standPosition)
        {
            return false;
        }
        //如果行动值和站位都相同，默认插在最前方
        return true;
    }
    private void RemoveCombatantFromTurnOrder(Combatant combatant)//移除角色回合
    {
        if (combatant == null)
        {
            Debug.LogWarning("[TurnManager] 尝试移除 null 角色");
            return;
        }

        var node = turnOrder.First;
        while (node != null)
        {
            if (node.Value == combatant)
            {
                turnOrder.Remove(node);
                Debug.Log($"[TurnManager] 移除角色: {combatant.name}");
                return;
            }

            node = node.Next;
        }
    }

    public bool HasChangerTurn()
    {
        foreach (var combatant in turnOrder)
        {
            if (combatant is Changer)
            {
                return true;
            }
        }

        return false;
    }

    private void NotifyTurnOrderChanged()
    {
        OnTurnOrderChanged?.Invoke();
    }
    #endregion
    //获取当前回合的角色
    public Combatant GetCurrentCombatant()
    {
        return turnOrder.First?.Value;
    }
}
