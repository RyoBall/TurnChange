using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单个楼层的纹理配置条目
/// </summary>
[Serializable]
public class FloorTextureEntry
{
    [Tooltip("楼层ID，对应 Datas 中的 currentFloorIndex")]
    [SerializeField] private int floorId;

    [Tooltip("该楼层对应的第一张纹理（如背景）")]
    [SerializeField] private Sprite textureA;

    [Tooltip("该楼层对应的第二张纹理（如前景/叠加层）")]
    [SerializeField] private Sprite textureB;

    public int FloorId => floorId;
    public Sprite TextureA => textureA;
    public Sprite TextureB => textureB;
}

/// <summary>
/// 楼层纹理配置表 - 存储每个楼层ID对应的两张纹理
/// 放置于 Resources/ 下，通过 Resources.Load 自动获取
/// </summary>
[CreateAssetMenu(fileName = "FloorTextureConfig", menuName = "配置/楼层纹理配置")]
public class FloorTextureConfig : ScriptableObject
{
    private static FloorTextureConfig s_instance;

    public static FloorTextureConfig Instance
    {
        get
        {
            if (s_instance == null)
            {
                s_instance = Resources.Load<FloorTextureConfig>("FloorTextureConfig");
            }
            return s_instance;
        }
    }

    [Header("楼层纹理映射")]
    [Tooltip("每个楼层ID对应的两张纹理配置")]
    [SerializeField] private List<FloorTextureEntry> floorTextureEntries = new List<FloorTextureEntry>();

    /// <summary>
    /// 根据楼层ID获取纹理配置
    /// </summary>
    /// <param name="floorId">楼层ID</param>
    /// <param name="textureA">输出的第一张纹理</param>
    /// <param name="textureB">输出的第二张纹理</param>
    /// <returns>是否找到对应配置</returns>
    public bool TryGetTextures(int floorId, out Sprite textureA, out Sprite textureB)
    {
        textureA = null;
        textureB = null;

        if (floorTextureEntries == null)
        {
            return false;
        }

        for (int i = 0; i < floorTextureEntries.Count; i++)
        {
            FloorTextureEntry entry = floorTextureEntries[i];
            if (entry != null && entry.FloorId == floorId)
            {
                textureA = entry.TextureA;
                textureB = entry.TextureB;
                return true;
            }
        }

        return false;
    }
}
