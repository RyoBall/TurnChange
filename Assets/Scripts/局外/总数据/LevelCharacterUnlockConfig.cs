using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 可编程物体，配置关卡通关后解锁的角色。
/// 包含两个等长列表，CharacterType 与 LevelId 按索引一一对应，
/// 表示当指定关卡（LevelId）被通关后，对应角色（CharacterType）会被添加到 Datas 的角色列表中。
/// </summary>
[CreateAssetMenu(fileName = "LevelCharacterUnlockConfig", menuName = "关卡数据/关卡角色解锁配置")]
public class LevelCharacterUnlockConfig : ScriptableObject
{
    [SerializeField] private List<CharacterType> characterTypes = new List<CharacterType>();
    [SerializeField] private List<string> levelIds = new List<string>();

    /// <summary>
    /// 根据关卡 ID 查找对应的角色类型列表（一个关卡可能解锁多个角色）
    /// </summary>
    public IReadOnlyList<CharacterType> GetCharacterTypesForLevel(string levelId)
    {
        if (string.IsNullOrWhiteSpace(levelId))
        {
            return Array.Empty<CharacterType>();
        }

        var result = new List<CharacterType>();
        for (int i = 0; i < levelIds.Count && i < characterTypes.Count; i++)
        {
            if (string.Equals(levelIds[i], levelId, StringComparison.Ordinal))
            {
                result.Add(characterTypes[i]);
            }
        }

        return result;
    }

    /// <summary>
    /// 获取所有配置的配对数量
    /// </summary>
    public int Count => Math.Min(characterTypes.Count, levelIds.Count);
}
