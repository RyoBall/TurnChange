using UnityEngine;

/// <summary>
/// 模块单元格渐变渲染的共享配置（ScriptableObject 单例）。
/// 放置于 Resources/配置可编程物体/ 下，通过 Resources.Load 自动加载。
/// 背包预览、商店预览、网格放置面板、光标跟随预览统一通过 ModuleCellConfig.Instance 获取配置。
///
/// 使用方法：
/// 1. 在 Project 窗口中右键 → Create → 局外 → Module Cell Config 创建资产
/// 2. 将其放入 Resources/配置可编程物体/ 目录下
/// 3. 其他脚本直接通过 ModuleCellConfig.Instance 获取配置，无需拖拽引用
/// </summary>
[CreateAssetMenu(fileName = "ModuleCellConfig", menuName = "局外/Module Cell Config", order = 100)]
public class ModuleCellConfig : ScriptableObject
{
    private const string ResourcePath = "配置可编程物体/ModuleCellConfig";

    private static ModuleCellConfig s_instance;

    public static ModuleCellConfig Instance
    {
        get
        {
            if (s_instance == null)
            {
                s_instance = Resources.Load<ModuleCellConfig>(ResourcePath);
            }
            return s_instance;
        }
    }

    private const string EmptyCellSpriteResourcePath = "Art/新的/方块底";

    [Header("背包网格空单元格")]
    [Tooltip("网格背包底色 Sprite。为空时尝试从 Resources/Art/新的/方块底 加载。")]
    [SerializeField] private Sprite m_emptyCellSprite;

    [Header("单元格预制体（可选）")]
    [Tooltip("如果不为空，创建单元格时会 Instantiate 此预制体；否则动态 new GameObject。")]
    [SerializeField] private GameObject m_cellPrefab;

    [Header("渐变 Shader")]
    [Tooltip("ModuleGradient shader 引用。为空时回退到纯色渲染。")]
    [SerializeField] private Shader m_gradientShader;

    [Tooltip("渐变角度（度）。")]
    [SerializeField] private float m_gradientAngle = 45f;

    public GameObject CellPrefab => m_cellPrefab;
    public Shader GradientShader => m_gradientShader;
    public float GradientAngle => m_gradientAngle;

    public Sprite EmptyCellSprite
    {
        get
        {
            if (m_emptyCellSprite != null)
            {
                return m_emptyCellSprite;
            }

            Sprite[] sprites = Resources.LoadAll<Sprite>(EmptyCellSpriteResourcePath);
            return sprites != null && sprites.Length > 0 ? sprites[0] : null;
        }
    }
}
