using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Enemy : UnitCombatant
{
    public string enemyID;
    public float selectedScale = 1.1f;
    public float selectAnimDuration = 0.12f;

    private Vector3 m_defaultScale;
    private Tween m_scaleTween;
    private void Start()
    {
        LoadDataFromCSV();
        m_defaultScale = transform.localScale;
        EnemyManager.Instance?.RegisterEnemy(this);
    }

    private void OnDestroy()
    {
        EnemyManager.Instance?.UnregisterEnemy(this);
    }

    public override IEnumerator PerformTurn()
    {
        ProcessStatesOnTurnStart();
        yield break;
    }

    public override void Die()
    {
        EnemyManager.Instance?.UnregisterEnemy(this);
        base.Die();
    }
    #region 选敌相关
    private void OnMouseDown()
    {
        // OnMouseDown 默认响应鼠标左键，这里把点击事件转发给选敌系统。
        SkillManager.Instance?.OnEnemyClicked(this);
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
        if(levelDataDict == null || !levelDataDict.ContainsKey(level))
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
}
