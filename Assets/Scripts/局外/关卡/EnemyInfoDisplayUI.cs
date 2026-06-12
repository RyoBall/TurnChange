using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 单个敌人信息显示组件，挂载在敌人信息预制体上
/// </summary>
public class EnemyInfoDisplayUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameLevelText;
    [SerializeField] private Image enemyImage;

    /// <summary>
    /// 接收LevelEnemyEntry参数，显示敌人等级、名称和图标
    /// </summary>
    public void SetEnemyData(LevelEnemyEntry entry)
    {
        if (entry == null || entry.enemyData == null) return;

        if (nameLevelText != null)
        {
            string displayName = entry.enemyData.enemyName ?? entry.enemyData.enemyID;
            nameLevelText.text = $"Lv.{entry.level} {displayName}";
        }

        if (enemyImage != null)
        {
            enemyImage.sprite = entry.enemyData.enemySprite;
            enemyImage.enabled = entry.enemyData.enemySprite != null;
        }
    }
}
