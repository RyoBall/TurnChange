using System.Text;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class EnvironmentTextDisplay : MonoBehaviour
{
    [Header("UI绑定")]
    [SerializeField] private TextMeshProUGUI textMesh;

    [Header("文本样式")]
    [SerializeField] private string title = "环境";
    [SerializeField] private string noEnvironmentText = "无";

    [Header("刷新")]
    [SerializeField, Min(0.02f)] private float refreshInterval = 0.1f;

    private readonly StringBuilder m_builder = new StringBuilder(128);
    private float m_nextRefreshTime;

    private void Awake()
    {
        if (textMesh == null)
        {
            textMesh = GetComponent<TextMeshProUGUI>();
        }
    }

    private void OnEnable()
    {
        m_nextRefreshTime = 0f;
        RefreshNow();
    }

    private void Update()
    {
        if (Time.time < m_nextRefreshTime)
        {
            return;
        }

        m_nextRefreshTime = Time.time + refreshInterval;
        RefreshNow();
    }

    public void RefreshNow()
    {
        if (textMesh == null)
        {
            return;
        }

        EnvironmentManager environmentManager = EnvironmentManager.Instance;
        if (environmentManager == null || environmentManager.ActiveEnvironments == null || environmentManager.ActiveEnvironments.Count <= 0)
        {
            textMesh.text = string.IsNullOrEmpty(title)
                ? noEnvironmentText
                : $"{title}\n{noEnvironmentText}";
            return;
        }

        m_builder.Clear();
        if (!string.IsNullOrEmpty(title))
        {
            m_builder.Append(title);
            m_builder.Append('\n');
        }

        bool hasAnyEnvironment = false;
        for (int i = 0; i < environmentManager.ActiveEnvironments.Count; i++)
        {
            BattleEnvironment environment = environmentManager.ActiveEnvironments[i];
            if (environment == null || !environment.IsApplied)
            {
                continue;
            }

            if (hasAnyEnvironment)
            {
                m_builder.Append('\n');
            }

            hasAnyEnvironment = true;
            m_builder.Append(environment.name);
            m_builder.Append(' ');
            m_builder.Append(environment.RemainingActionValue);
            m_builder.Append("AV");
        }

        if (!hasAnyEnvironment)
        {
            if (!string.IsNullOrEmpty(title))
            {
                m_builder.Append(noEnvironmentText);
            }
            else
            {
                m_builder.Append(noEnvironmentText);
            }
        }

        textMesh.text = m_builder.ToString();
    }
}
