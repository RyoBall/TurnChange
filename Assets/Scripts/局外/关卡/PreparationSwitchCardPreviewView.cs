using UnityEngine;

/// <summary>
/// 备战页中央切人卡预览：复用 Fight 场景换人按钮 Prefab，仅展示不交互。
/// </summary>
[DisallowMultipleComponent]
public class PreparationSwitchCardPreviewView : MonoBehaviour
{
    private const string SwitchCardPrefabResourcePath = "Prefabs/换人按钮";

    [SerializeField] private RectTransform m_previewRoot;
    [SerializeField] private GameObject m_switchCardPrefab;
    [SerializeField] private float m_previewScale = 1.5f;

    private EnterCharacterButton m_cardInstance;
    private GameObject m_cardObject;

    private void Awake()
    {
        ApplyPreviewScale();
        EnsureCardInstance();
        Hide();
    }

    private void ApplyPreviewScale()
    {
        if (m_previewRoot != null)
        {
            m_previewRoot.localScale = Vector3.one * m_previewScale;
        }
    }

    public void Show(CharacterRosterData rosterData)
    {
        if (rosterData == null)
        {
            Hide();
            return;
        }

        EnsureCardInstance();
        if (m_cardInstance == null)
        {
            Debug.LogWarning("[PreparationSwitchCardPreviewView] 切人卡预览实例未创建，请检查 Prefab 引用。", this);
            return;
        }

        transform.SetAsLastSibling();

        int teamLevel = Datas.Instance != null ? Datas.Instance.GetTeamLevel() : 1;
        m_cardInstance.InitializePreview(rosterData, teamLevel);
        m_cardInstance.ResetScaleImmediate();

        if (m_cardObject != null)
        {
            m_cardObject.SetActive(true);
        }
    }

    public void Hide()
    {
        if (m_cardInstance != null)
        {
            m_cardInstance.ResetScaleImmediate();
        }

        if (m_cardObject != null)
        {
            m_cardObject.SetActive(false);
        }
    }

    private void EnsureCardInstance()
    {
        if (m_cardInstance != null)
        {
            return;
        }

        if (m_previewRoot == null)
        {
            Debug.LogWarning("[PreparationSwitchCardPreviewView] 未配置 PreviewRoot。", this);
            return;
        }

        GameObject prefab = ResolveSwitchCardPrefab();
        if (prefab == null)
        {
            Debug.LogWarning("[PreparationSwitchCardPreviewView] 未找到切人卡 Prefab。", this);
            return;
        }

        m_cardObject = Instantiate(prefab, m_previewRoot);
        m_cardInstance = m_cardObject.GetComponent<EnterCharacterButton>();
        if (m_cardInstance == null)
        {
            Debug.LogWarning("[PreparationSwitchCardPreviewView] 切人卡 Prefab 缺少 EnterCharacterButton 组件。", prefab);
            return;
        }

        CanvasGroup canvasGroup = m_cardObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = m_cardObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    private GameObject ResolveSwitchCardPrefab()
    {
        if (m_switchCardPrefab != null)
        {
            return m_switchCardPrefab;
        }

        return Resources.Load<GameObject>(SwitchCardPrefabResourcePath);
    }
}
