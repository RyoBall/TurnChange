using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Enemy : Combatant
{
    public float selectedScale = 1.1f;
    public float selectAnimDuration = 0.12f;

    private Vector3 m_defaultScale;
    private Tween m_scaleTween;
    [Header("属性")]
    public int maxHP;
    public int currentHP;
    public float attack;
    private void Start()
    {
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

    public void TakeDamage(int damage)
    {
        // 这里可以添加受伤动画、音效等反馈。
        Debug.Log($"[Enemy] {gameObject.name} 受到 {damage} 点伤害");
        //展示伤害
        DamageTextPool.Instance.Get().ShowDamage(damage, transform.position);
        if(currentHP <= 0)
        Die();
    }
    public void Die()
    {
        EnemyManager.Instance?.UnregisterEnemy(this);
        Destroy(gameObject);
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
}
