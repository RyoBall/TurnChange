using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using MoreMountains.Feedbacks;
public enum ExplodeType
{
    None,
    Normal,
    hasStarted,
    ReadyToBurst
}
public class Enemy : UnitCombatant
{
    public string enemyID;
    public float selectedScale = 1.1f;
    public float selectAnimDuration = 0.12f;

    private Vector3 m_defaultScale;
    private Tween m_scaleTween;
    public List<EnemySkillType> skills = new List<EnemySkillType>();
    private List<EnemySkillBase> m_skillInstances = new List<EnemySkillBase>();
    private Dictionary<EnemySkillType, EnemySkillBase> m_skillInstanceMap = new Dictionary<EnemySkillType, EnemySkillBase>();
    #region 自爆相关 因为项目比较小 所以先把自爆相关的状态和逻辑写在Enemy类里，后续如果需要的话再重构
    public ExplodeType explodeState = ExplodeType.None;
    #endregion
    private void Start()
    {
        InitializeSkill();
        LoadDataFromCSV();
        currentHP *= 2;
        maxHP *= 2;
        m_defaultScale = transform.localScale;
    }

    public override IEnumerator PerformTurn()
    {
        enterFeedback?.PlayFeedbacks();
        yield return new WaitForSeconds(0.2f);
        yield return ProcessStatesOnTurnStart();
        //如果死亡了就直接结束回合
        if (dead)
        {
            yield break;
        }
        if (!CanActThisTurn())
        {
            FloatingTipGenerator.Instance?.ShowTipAtObject(transform, $"无法行动");
            yield break;
        }
        //执行行动
        yield return ActionCoroutine();
    }
    private IEnumerator ActionCoroutine()
    {
        //这里先写个随机攻击的逻辑，后续会替换成更复杂的AI
        if (m_skillInstances == null || m_skillInstances.Count == 0)
        {
            InitializeSkill();
        }

        if (m_skillInstances == null || m_skillInstances.Count == 0)
        {
            yield break;
        }

        int rand = Random.Range(0, m_skillInstances.Count);
        EnemySkillBase skill = m_skillInstances[rand];
        if (skill == null)
        {
            yield break;
        }

        yield return new WaitForSeconds(0.2f);
        FloatingTipGenerator.Instance?.ShowDefaultTip(skill.skillName);
        yield return new WaitForSeconds(0.5f);//进入回合动画
        enterFeedback?.PlayFeedbacks();
        yield return new WaitForSeconds(0.5f);
        SkillExecuteManager.ExecuteSkill(this, skill);
        yield return WaitForDeathEvents();
        yield break;
    }
    public override void Die()
    {
        base.Die();
        EnemyManager.Instance?.UnregisterEnemy(this);
    }

    protected override IEnumerator OnDeathEvent()
    {
        yield return base.OnDeathEvent();
        gameObject.SetActive(false);
    }
    #region 选敌相关
    private void OnMouseDown()
    {
        // OnMouseDown 默认响应鼠标左键，这里把点击事件转发给选敌系统。
        SkillManager.Instance?.OnEnemyClicked(this);
        mouseExitFeedback?.PlayFeedbacks();
    }
    private void OnMouseEnter()
    {
        if (SkillManager.Instance.IsSelectingEnemies)
        {
            mouseExitFeedback?.StopFeedbacks();
            mouseEnterFeedback?.PlayFeedbacks();
        }
    }
    private void OnMouseExit()
    {
        if (SkillManager.Instance.IsSelectingEnemies)
        {
            mouseEnterFeedback?.StopFeedbacks();
            mouseExitFeedback?.PlayFeedbacks();
        }
    }

    public void SetSelectedVisual(bool selected)//被选中可视化函数
    {
        if (m_scaleTween != null)
        {
            m_scaleTween.Kill();
            m_scaleTween = null;
        }

        var targetScale = selected ? m_defaultScale * selectedScale : m_defaultScale;
        m_scaleTween = transform.DOScale(targetScale, selectAnimDuration).SetEase(Ease.OutQuad);
    }
    #endregion

    #region 读取数据
    public void LoadDataFromCSV()
    {
        if (string.IsNullOrEmpty(enemyID))
        {
            Debug.Log("EnemyID is null or empty for " + gameObject.name);
            return;
        }

        var levelDataDict = LevelDataContainer.EnemyLevelData[enemyID];
        if (levelDataDict == null || !levelDataDict.ContainsKey(level))
        {
            Debug.LogError($"未找到敌人数据: {enemyID} 等级: {level}");
            return;
        }
        var levelData = levelDataDict[level];
        maxHP = levelData.maxHP;
        currentHP = maxHP;
        attack = levelData.attack;
        defense = levelData.defense;
        speed = levelData.speed;
        K = levelData.K;
    }
    #endregion

    public void InitializeSkill()
    {
        CleanupSkillInstances();
        m_skillInstances.Clear();
        m_skillInstanceMap.Clear();

        if (skills == null)
        {
            return;
        }

        foreach (var skillType in skills)
        {
            EnemySkillBase skill = CreateSkillInstance(skillType);
            if (skill == null)
            {
                continue;
            }

            m_skillInstances.Add(skill);
            m_skillInstanceMap[skillType] = skill;
        }
    }

    public EnemySkillBase GetSkillInstance(EnemySkillType skillType)
    {
        if (m_skillInstanceMap == null || m_skillInstanceMap.Count == 0)
        {
            InitializeSkill();
        }

        m_skillInstanceMap.TryGetValue(skillType, out EnemySkillBase skill);
        return skill;
    }

    private EnemySkillBase CreateSkillInstance(EnemySkillType skillType)
    {
        EnemySkillBase template = EnemySkillDictionaryManager.GetEnemySkill(skillType);
        if (template == null)
        {
            return null;
        }

        EnemySkillBase instance = Instantiate(template);
        instance.name = template.name;
        return instance;
    }

    private void CleanupSkillInstances()
    {
        if (m_skillInstances == null)
        {
            return;
        }

        for (int i = 0; i < m_skillInstances.Count; i++)
        {
            if (m_skillInstances[i] == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(m_skillInstances[i]);
            }
            else
            {
                DestroyImmediate(m_skillInstances[i]);
            }
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        CleanupSkillInstances();
    }
}
