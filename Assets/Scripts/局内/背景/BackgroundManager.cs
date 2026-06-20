using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class BackgroundManager : MonoBehaviour
{
    public static BackgroundManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        ResolveMaterials();
        RecordDefaultColors();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            KillAllTweens();
            Instance = null;
        }
    }

    [Header("变色参数")]
    [SerializeField] private Color m_defaultColor = Color.white;
    [SerializeField] private Color m_darkColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    [SerializeField] private float m_duration = 0.3f;
    [SerializeField] private Ease m_easeType = Ease.InOutQuad;

    [Header("额外材质来源")]
    [Tooltip("选择使用 SceneMaterialController 的哪个克隆材质作为额外同步变暗目标")]
    [SerializeField] private ExtraMaterialSource m_extraMaterialSource = ExtraMaterialSource.MaterialB;

    private enum ExtraMaterialSource
    {
        /// <summary>使用 ClonedMaterialA（背景）</summary>
        MaterialA,
        /// <summary>使用 ClonedMaterialB（地面）</summary>
        MaterialB
    }

    /// <summary>背景材质（来自 SceneMaterialController.ClonedMaterialA）</summary>
    private Material m_backgroundMaterial;

    /// <summary>额外同步变暗的材质</summary>
    private Material m_additionalMaterial;

    /// <summary>背景材质的默认颜色</summary>
    private Color m_backgroundDefaultColor = Color.white;

    /// <summary>额外材质的默认颜色</summary>
    private Color m_additionalDefaultColor = Color.white;

    /// <summary>当前变暗请求的优先级，0 表示未变暗</summary>
    private int m_darkPriority = 0;

    /// <summary>当前正在运行的变色 Tween</summary>
    private Tween m_colorTween;

    /// <summary>当前正在运行的额外材质变色 Tween</summary>
    private Tween m_additionalTween;

    /// <summary>
    /// 切换背景明暗。
    /// </summary>
    /// <param name="enter">true 变暗，false 恢复</param>
    /// <param name="priority">优先级，变暗时取较大值保留，恢复时低于当前优先级则忽略。默认 1。</param>
    public Tween ChangeBackground(bool enter, int priority = 1)
    {
        if (enter)
        {
            if (priority > m_darkPriority)
            {
                m_darkPriority = priority;
            }
            return PlayDarkenTween();
        }
        else
        {
            if (priority < m_darkPriority)
            {
                return null;
            }
            m_darkPriority = 0;
            return PlayRestoreTween();
        }
    }

    /// <summary>
    /// 播放变暗动画
    /// </summary>
    private Tween PlayDarkenTween()
    {
        KillAllTweens();
        m_colorTween = m_backgroundMaterial.DOColor(m_darkColor, m_duration).SetEase(m_easeType);

        if (m_additionalMaterial != null)
        {
            m_additionalTween = m_additionalMaterial.DOColor(m_darkColor, m_duration).SetEase(m_easeType);
        }

        return m_colorTween;
    }

    /// <summary>
    /// 播放恢复动画
    /// </summary>
    private Tween PlayRestoreTween()
    {
        KillAllTweens();
        m_colorTween = m_backgroundMaterial.DOColor(m_backgroundDefaultColor, m_duration).SetEase(m_easeType);

        if (m_additionalMaterial != null)
        {
            m_additionalTween = m_additionalMaterial.DOColor(m_additionalDefaultColor, m_duration).SetEase(m_easeType);
        }

        return m_colorTween;
    }

    /// <summary>
    /// 停止所有变色 Tween
    /// </summary>
    private void KillAllTweens()
    {
        if (m_colorTween != null && m_colorTween.IsActive())
        {
            m_colorTween.Kill();
        }
        m_colorTween = null;

        if (m_additionalTween != null && m_additionalTween.IsActive())
        {
            m_additionalTween.Kill();
        }
        m_additionalTween = null;
    }

    /// <summary>
    /// 从 SceneMaterialController 获取克隆后的材质实例
    /// </summary>
    private void ResolveMaterials()
    {
        SceneMaterialController controller = SceneMaterialController.Instance;
        if (controller == null)
        {
            Debug.LogWarning("[BackgroundManager] SceneMaterialController 未找到");
            return;
        }

        m_backgroundMaterial = controller.ClonedMaterialA;

        m_additionalMaterial = m_extraMaterialSource == ExtraMaterialSource.MaterialA
            ? controller.ClonedMaterialA
            : controller.ClonedMaterialB;
    }

    /// <summary>
    /// 记录所有材质的默认颜色，用于恢复时还原
    /// </summary>
    private void RecordDefaultColors()
    {
        if (m_backgroundMaterial != null)
        {
            m_backgroundDefaultColor = m_backgroundMaterial.color;
        }

        if (m_additionalMaterial != null)
        {
            m_additionalDefaultColor = m_additionalMaterial.color;
        }
    }
}
