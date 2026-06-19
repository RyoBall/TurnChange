using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Boss血条UI组件 — 绑定到单个Boss（UnitCombatant），显示其血量
/// 由 BossHealthBarManager 统一管理排列
/// 当Boss为剑客时，额外显示韧性点
/// </summary>
public class BossHealthBar : MonoBehaviour
{
    [Header("绑定")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("血条颜色")]
    [SerializeField] private Color fillColor = new Color(0.9f, 0.2f, 0.2f, 1f);

    [Header("差值显示")]
    [SerializeField] private Image diffImage;
    [SerializeField] private Color damageDiffColor = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color healDiffColor = new Color(0.4f, 1f, 0.4f, 1f);
    [SerializeField] private float decreaseSpeed = 0.5f;
    [SerializeField] private float increaseSpeed = 1.0f;

    [Header("韧性点显示（仅剑客）")]
    [SerializeField] private GameObject tenacityGroup;
    [SerializeField] private TMP_Text tenacityText;
    [SerializeField] private Color tenacityNormalColor = new Color(1f, 0.85f, 0.3f, 1f);
    [SerializeField] private Color tenacityZeroColor = new Color(1f, 0.3f, 0.3f, 1f);

    private UnitCombatant m_target;
    private ISwordsmanTenacityProvider m_tenacityProvider;
    private RectTransform m_rectTransform;
    private float m_targetFill = 0f;
    private float m_displayedFill = 0f;
    private float m_diffFill = 0f;
    private Color m_currentDiffColor;
    private bool m_initialized;

    private const float Epsilon = 0.0001f;

    public UnitCombatant Target => m_target;
    public RectTransform RectTransform
    {
        get
        {
            if (m_rectTransform == null)
            {
                m_rectTransform = GetComponent<RectTransform>();
            }
            return m_rectTransform;
        }
    }

    private void Awake()
    {
        m_rectTransform = GetComponent<RectTransform>();
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        SetupVisuals();
    }

    private void Update()
    {
        if (m_target == null)
        {
            return;
        }

        float nextFill = GetHealthPercent();
        if (Mathf.Abs(nextFill - m_targetFill) > Epsilon)
        {
            HandleFillChanged(nextFill);
        }

        AnimateFills();

        if (fillImage != null)
        {
            fillImage.fillAmount = m_displayedFill;
        }

        UpdateDiffOverlay();
        UpdateHpText();
    }

    /// <summary>绑定目标Boss</summary>
    public void Bind(UnitCombatant target)
    {
        m_target = target;
        m_targetFill = GetHealthPercent();
        m_displayedFill = m_targetFill;
        m_diffFill = m_targetFill;
        m_currentDiffColor = damageDiffColor;

        if (fillImage != null)
        {
            fillImage.fillAmount = m_displayedFill;
            fillImage.color = fillColor;
        }

        HideDiffOverlay();
        UpdateNameText();
        UpdateHpText();
        InitializeTenacityDisplay();
        m_initialized = true;
    }

    /// <summary>解绑目标（Boss死亡时调用）</summary>
    public void Unbind()
    {
        CleanupTenacityDisplay();
        m_target = null;
        m_initialized = false;
    }

    /// <summary>设置可见性</summary>
    public void SetVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void SetupVisuals()
    {
        if (fillImage != null && fillImage.type != Image.Type.Filled)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        }

        if (diffImage != null)
        {
            diffImage.type = Image.Type.Filled;
            diffImage.fillMethod = Image.FillMethod.Horizontal;
            diffImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            diffImage.raycastTarget = false;
            diffImage.gameObject.SetActive(false);
        }
    }

    private float GetHealthPercent()
    {
        if (m_target == null || m_target.maxHP <= 0)
        {
            return 0f;
        }

        return Mathf.Clamp01((float)m_target.currentHP / m_target.maxHP);
    }

    private void HandleFillChanged(float nextFill)
    {
        if (nextFill < m_displayedFill - Epsilon)
        {
            // 受伤：diff 留在原位置，displayed 跳到新值
            m_diffFill = Mathf.Max(m_diffFill, m_displayedFill);
            m_displayedFill = nextFill;
            m_currentDiffColor = damageDiffColor;
        }
        else if (nextFill > m_displayedFill + Epsilon)
        {
            // 回血：diff 跳到新值，displayed 追上去
            m_diffFill = nextFill;
            m_currentDiffColor = healDiffColor;
        }
        else
        {
            m_displayedFill = nextFill;
            m_diffFill = nextFill;
        }

        m_targetFill = nextFill;
    }

    private void AnimateFills()
    {
        if (m_displayedFill < m_targetFill - Epsilon)
        {
            m_displayedFill = Mathf.MoveTowards(m_displayedFill, m_targetFill, increaseSpeed * Time.deltaTime);
        }
        else if (m_displayedFill > m_targetFill + Epsilon)
        {
            m_displayedFill = m_targetFill;
        }

        if (m_diffFill > m_targetFill + Epsilon)
        {
            m_diffFill = Mathf.MoveTowards(m_diffFill, m_targetFill, decreaseSpeed * Time.deltaTime);
        }
        else if (m_diffFill < m_targetFill - Epsilon)
        {
            m_diffFill = m_targetFill;
        }
    }

    private void UpdateDiffOverlay()
    {
        if (diffImage == null)
        {
            return;
        }

        float diffAmount = m_diffFill - m_displayedFill;
        if (diffAmount > Epsilon)
        {
            diffImage.gameObject.SetActive(true);
            diffImage.fillAmount = m_diffFill;
            diffImage.color = m_currentDiffColor;
        }
        else
        {
            diffImage.gameObject.SetActive(false);
        }
    }

    private void HideDiffOverlay()
    {
        if (diffImage != null)
        {
            diffImage.gameObject.SetActive(false);
        }
    }

    private void UpdateNameText()
    {
        if (nameText != null && m_target != null)
        {
            nameText.text = m_target.combatantName;
        }
    }

    private void UpdateHpText()
    {
        if (hpText != null && m_target != null)
        {
            hpText.text = $"{m_target.currentHP} / {m_target.maxHP}";
        }
    }

    // ============ 韧性点显示（仅剑客） ============

    private void InitializeTenacityDisplay()
    {
        m_tenacityProvider = m_target as ISwordsmanTenacityProvider;
        bool isSwordsman = m_tenacityProvider != null;

        if (tenacityGroup != null)
        {
            tenacityGroup.SetActive(isSwordsman);
        }

        if (isSwordsman)
        {
            m_tenacityProvider.TenacityChanged += OnTenacityChanged;
            RefreshTenacityText();
        }
    }

    private void CleanupTenacityDisplay()
    {
        if (m_tenacityProvider != null)
        {
            m_tenacityProvider.TenacityChanged -= OnTenacityChanged;
            m_tenacityProvider = null;
        }
    }

    private void OnTenacityChanged()
    {
        RefreshTenacityText();
    }

    private void RefreshTenacityText()
    {
        if (tenacityText == null || m_tenacityProvider == null)
        {
            return;
        }

        int current = m_tenacityProvider.CurrentTenacity;
        int max = m_tenacityProvider.MaxTenacity;
        tenacityText.text = $"韧性 {current}/{max}";
        tenacityText.color = current <= 0 ? tenacityZeroColor : tenacityNormalColor;
    }
}
