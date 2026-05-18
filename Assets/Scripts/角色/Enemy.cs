using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using MoreMountains.Feedbacks;
public class Enemy : UnitCombatant
{
    public string enemyID;
    public float selectedScale = 1.1f;
    public float selectAnimDuration = 0.12f;

    private Vector3 m_defaultScale;
    private Tween m_scaleTween;
    public List<EnemySkillType> skills = new List<EnemySkillType>();
    #region 自爆相关 因为项目比较小 所以先把自爆相关的状态和逻辑写在Enemy类里，后续如果需要的话再重构
    public bool hasStartExploded = false;
    #endregion
    private void Start()
    {
        LoadDataFromCSV();
        currentHP*=2;
        maxHP*=2;
        m_defaultScale = transform.localScale;
    }

    public override IEnumerator PerformTurn()
    {
        enterFeedback?.PlayFeedbacks();
        yield return new WaitForSeconds(0.5f);
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
        //执行行动前
        yield return new WaitForSeconds(0.5f);
        FloatingTipGenerator.Instance?.ShowDefaultTip($"单体攻击");
        yield return new WaitForSeconds(0.5f);//进入回合动画
        enterFeedback?.PlayFeedbacks();
        yield return new WaitForSeconds(0.5f);//进入回合动画
        yield return ActionCoroutine();
    }
    private IEnumerator ActionCoroutine()
    {
        //这里先写个随机攻击的逻辑，后续会替换成更复杂的AI
        int rand = Random.Range(0, skills.Count);
        EnemySkillType skillType = skills[rand];
        SkillExecuteManager.ExecuteSkill(this, EnemySkillDictionaryManager.GetEnemySkill(skillType));
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
        TransferElementalDetonationOnDeath();
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

    private void TransferElementalDetonationOnDeath()
    {
        State detonation = GetState(StateType.ElementalDetonation);
        if (detonation == null || EnemyManager.Instance == null)
        {
            return;
        }

        Enemy target = null;
        int minHp = int.MaxValue;
        foreach (var enemy in EnemyManager.Instance.AliveEnemies)
        {
            if (enemy == null || enemy == this)
            {
                continue;
            }

            if (enemy.currentHP < minHp)
            {
                minHp = enemy.currentHP;
                target = enemy;
            }
        }

        if (target == null)
        {
            return;
        }

        UnitCombatant giver = detonation.giver;
        if (giver == null)
        {
            giver = this;
        }

        target.AddState(StateType.ElementalDetonation, giver, 2, 1, detonation.skillCoefT);
    }
}
