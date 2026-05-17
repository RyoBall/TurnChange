using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }
    [Tooltip("是否自动开始回合循环")]
    public bool autoStart = true;

    [Tooltip("每个回合结束后等待的帧数")]
    public float turnDelay = 0.1f;

    private List<Combatant> combatants = new List<Combatant>();
    private bool turnLoopStarted;
    private bool isTurnInitialized = false;
    public bool IsTurnInitialized => isTurnInitialized;
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
            }
            extraCombatant.combatantName = character.combatantName;
            extraCombatant.ChangeActionValue(0f, false);
            InsertCombatant(extraCombatant, false);
        }
    }
    #region 回合开始的函数
    IEnumerator StartFight()
    {
        //在此处插入需要在回合进行前进行的事情

        //设置回合图片
        yield return StartCoroutine(SetTurnImages());
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
            combatant.ChangeActionValue(combatant.BaseActionValue, false);
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

        combatants = combatants.OrderBy(c => c.currentActionValue).ToList();
        foreach (var combatant in combatants)
        {
            turnOrder.AddLast(combatant);
        }
    }
    #endregion
    private IEnumerator RunTurnLoop()
    {
        while (true)
        {
            ////// 回合开始前
            // 直接读取链表头，链表头就是下一位行动者。
            var nextNode = turnOrder.First;
            if (nextNode == null)
            {
                yield break;
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

            State.TickAllStatesByActionValue(advanceValue);

            if (EnvironmentManager.Instance != null)
            {
                EnvironmentManager.Instance.TickEnvironments(advanceValue);
            }

            if (nextCombatant != null)
            {
                Debug.Log($"[TurnManager] 进入回合: {nextCombatant.name} (速度={nextCombatant.speed})");
            }

            ////// 回合开始。
            yield return StartCoroutine(nextCombatant.PerformTurn());
            ////// 回合结束
            // 回合结束后重新计算当前角色的下一次行动值。
            if (nextCombatant != null)
            {   
                nextCombatant.ChangeActionValue(nextCombatant.BaseActionValue);
                Debug.Log($"[TurnManager] 结束回合: {nextCombatant.name}，重置行动值到 {nextCombatant.BaseActionValue:F0}");
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
        InsertCombatantByActionValue(combatant,false);

        if (TurnImageManager.Instance != null)
        {
            TurnImageManager.Instance.Reorder();
        }
    }

    //从外部插入角色回合的接口,在触发时默认进行一次重排
    public void InsertCombatant(Combatant combatant, bool insertAtEnd = true)
    {
        InsertCombatantByActionValue(combatant, insertAtEnd);
        if (TurnImageManager.Instance != null)
        {
            TurnImageManager.Instance.Reorder();
        }
        //如果是新角色加入战斗，注册状态UI
        if (combatant is UnitCombatant unitCombatant)
        {
            UnitStateTextDisplay.Instance.RegisterUnit(unitCombatant);
        }
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
    }
    #region 工具
    private void InsertCombatantByActionValue(Combatant combatant, bool insertAtEnd = true)//插入角色回合，目前默认插入时会排在所有相同行动值角色之前
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
        while (node != null && (insertAtEnd ? node.Value.currentActionValue <= combatant.currentActionValue : node.Value.currentActionValue < combatant.currentActionValue))
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
    #endregion
    //获取当前回合的角色
    public Combatant GetCurrentCombatant()
    {
        return turnOrder.First?.Value;
    }
}
