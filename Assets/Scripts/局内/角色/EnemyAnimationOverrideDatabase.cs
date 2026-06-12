using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemyAnimationOverrideEntry
{
    public string enemyID;
    public List<AnimationClipOverrideEntry> clipOverrides = new List<AnimationClipOverrideEntry>();
}

[CreateAssetMenu(fileName = "EnemyAnimationOverrideDatabase", menuName = "Enemy/Animation Override Database")]
public class EnemyAnimationOverrideDatabase : AnimationOverrideDatabaseBase
{
    [SerializeField] private List<EnemyAnimationOverrideEntry> enemyOverrides = new List<EnemyAnimationOverrideEntry>();

    public bool TryGetEnemyOverrides(string targetEnemyID, out List<AnimationClipOverrideEntry> clipOverrides)
    {
        clipOverrides = null;
        if (string.IsNullOrWhiteSpace(targetEnemyID))
        {
            return false;
        }

        for (int i = 0; i < enemyOverrides.Count; i++)
        {
            EnemyAnimationOverrideEntry enemyEntry = enemyOverrides[i];
            if (enemyEntry == null)
            {
                continue;
            }

            if (!string.Equals(enemyEntry.enemyID, targetEnemyID, StringComparison.Ordinal))
            {
                continue;
            }

            clipOverrides = enemyEntry.clipOverrides;
            return clipOverrides != null && clipOverrides.Count > 0;
        }

        return false;
    }

#if UNITY_EDITOR
    protected override IEnumerable<List<AnimationClipOverrideEntry>> EnumerateOverrideLists()
    {
        for (int i = 0; i < enemyOverrides.Count; i++)
        {
            EnemyAnimationOverrideEntry enemyEntry = enemyOverrides[i];
            if (enemyEntry == null)
            {
                continue;
            }

            yield return enemyEntry.clipOverrides;
        }
    }
#endif
}