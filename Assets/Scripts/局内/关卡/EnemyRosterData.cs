using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyRosterData", menuName = "Level/Enemy Roster Data")]
public class EnemyRosterData : ScriptableObject
{
    public string enemyName;
    public string enemyID;
    public Sprite enemySprite;
    public GameObject prefabOverride;

    public List<EnemySkillType> skills = new List<EnemySkillType>();

    public GameObject PrefabOverride => prefabOverride;
}
