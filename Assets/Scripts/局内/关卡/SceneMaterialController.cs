using UnityEngine;

/// <summary>
/// 场景材质控制器 - 单例，控制两个物体的主纹理
/// 在 Awake 中克隆材质，在进入战斗场景时优先使用关卡配置纹理，否则按楼层ID回退
/// </summary>
[DisallowMultipleComponent]
public class SceneMaterialController : MonoBehaviour
{
    public static SceneMaterialController Instance { get; private set; }

    [Header("材质目标物体")]
    [Tooltip("第一个需要控制材质的物体（如背景）")]
    [SerializeField] private Renderer targetRendererA;

    [Tooltip("第二个需要控制材质的物体（如前景）")]
    [SerializeField] private Renderer targetRendererB;

    [Header("楼层纹理配置")]
    [Tooltip("存储每个楼层ID对应的纹理配置列表")]
    [SerializeField] private FloorTextureConfig floorTextureConfig;

    /// <summary>克隆后的材质A</summary>
    private Material m_clonedMaterialA;

    /// <summary>克隆后的材质B</summary>
    private Material m_clonedMaterialB;

    /// <summary>克隆后的材质A（只读）</summary>
    public Material ClonedMaterialA => m_clonedMaterialA;

    /// <summary>克隆后的材质B（只读）</summary>
    public Material ClonedMaterialB => m_clonedMaterialB;

    /// <summary>是否已初始化材质</summary>
    private bool m_materialsInitialized;

    private void Awake()
    {
        if (!TryClaimSingleton())
        {
            return;
        }

        CloneMaterials();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        DestroyClonedMaterials();
    }

    private void Start()
    {
        ApplyFloorTextures();
    }

    /// <summary>
    /// 尝试获取单例，若已存在则销毁自身
    /// </summary>
    private bool TryClaimSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return false;
        }

        Instance = this;
        return true;
    }

    /// <summary>
    /// 将两个目标物体的材质替换为实例化后的克隆材质
    /// </summary>
    private void CloneMaterials()
    {
        if (targetRendererA != null && targetRendererA.sharedMaterial != null)
        {
            m_clonedMaterialA = new Material(targetRendererA.sharedMaterial);
            targetRendererA.material = m_clonedMaterialA;
        }

        if (targetRendererB != null && targetRendererB.sharedMaterial != null)
        {
            m_clonedMaterialB = new Material(targetRendererB.sharedMaterial);
            targetRendererB.material = m_clonedMaterialB;
        }

        m_materialsInitialized = true;
    }

    /// <summary>
    /// 销毁克隆出的材质实例
    /// </summary>
    private void DestroyClonedMaterials()
    {
        if (m_clonedMaterialA != null)
        {
            Destroy(m_clonedMaterialA);
            m_clonedMaterialA = null;
        }

        if (m_clonedMaterialB != null)
        {
            Destroy(m_clonedMaterialB);
            m_clonedMaterialB = null;
        }
    }

    /// <summary>
    /// 优先使用关卡配置纹理，缺失时按楼层ID回退，并填充到材质中
    /// 在每次进入战斗场景时调用
    /// </summary>
    public void ApplyFloorTextures()
    {
        if (!m_materialsInitialized)
        {
            CloneMaterials();
        }

        Sprite textureA = GetLevelSceneTextureA();
        Sprite textureB = GetLevelSceneTextureB();

        if (textureA == null || textureB == null)
        {
            int floorId = GetCurrentFloorId();
            FloorTextureConfig config = ResolveFloorTextureConfig();
            if (config != null && config.TryGetTextures(floorId, out Sprite floorTextureA, out Sprite floorTextureB))
            {
                if (textureA == null)
                {
                    textureA = floorTextureA;
                }

                if (textureB == null)
                {
                    textureB = floorTextureB;
                }
            }
        }

        if (textureA == null && textureB == null)
        {
            Debug.LogWarning("[SceneMaterialController] 未找到关卡或楼层纹理配置");
            return;
        }

        ApplyTextureToMaterial(m_clonedMaterialA, textureA);
        ApplyTextureToMaterial(m_clonedMaterialB, textureB);
    }

    private Sprite GetLevelSceneTextureA()
    {
        return LevelSetupManager.Instance != null
            ? LevelSetupManager.Instance.LevelSceneTextureA
            : null;
    }

    private Sprite GetLevelSceneTextureB()
    {
        return LevelSetupManager.Instance != null
            ? LevelSetupManager.Instance.LevelSceneTextureB
            : null;
    }

    private FloorTextureConfig ResolveFloorTextureConfig()
    {
        if (floorTextureConfig == null)
        {
            floorTextureConfig = FloorTextureConfig.Instance;
        }

        if (floorTextureConfig == null)
        {
            Debug.LogWarning("[SceneMaterialController] 未找到 FloorTextureConfig 配置");
        }

        return floorTextureConfig;
    }

    /// <summary>
    /// 获取当前楼层ID
    /// </summary>
    private int GetCurrentFloorId()
    {
        if (Datas.Instance != null)
        {
            return Datas.Instance.GetCurrentFloorIndex();
        }

        return 0;
    }

    /// <summary>
    /// 将 Sprite 的纹理填充到材质的主纹理中
    /// </summary>
    private void ApplyTextureToMaterial(Material targetMaterial, Sprite sprite)
    {
        if (targetMaterial == null || sprite == null)
        {
            return;
        }

        targetMaterial.mainTexture = sprite.texture;
    }
}
