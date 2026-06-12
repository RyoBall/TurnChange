using System.Collections;
using UnityEngine;

/// <summary>
/// Fight 场景运行时快速预览三种场域展开效果。
/// 快捷键：1/2/3 持续预览（展开后保持流动，手动退出），Q 收缩退出，R 强制停止。
/// 后处理扩散/收缩固定从屏幕中心 (0.5, 0.5)；可选 Transform 仅用于爆发粒子位置。
/// </summary>
public class FieldDomainEffectTester : MonoBehaviour
{
    [Header("开关")]
    [SerializeField] private bool enableQuickTest = true;
    [SerializeField] private bool showOnScreenPanel = true;
    [SerializeField] private bool logStatusToConsole = true;

    [Header("预览参数")]
    [SerializeField] private Transform burstOrigin;
    [Tooltip("勾选则 1/2/3 为一次性完整循环；不勾选则展开后持续预览直至 Q/R 退出。")]
    [SerializeField] private bool useOneShotPreviewCycle;
    [SerializeField] private float activeHoldDuration = 2f;
    [SerializeField] private bool useFirstFieldCharacterForBurst = true;

    [Header("References")]
    [SerializeField] private FieldDomainScreenEffectController effectController;

    private Coroutine m_PreviewCoroutine;
    private bool m_SustainedPreviewRunning;
    private GUIStyle m_PanelStyle;
    private GUIStyle m_ButtonStyle;
    private GUIStyle m_LabelStyle;
    private bool m_StylesInitialized;

    private void Awake()
    {
        if (effectController == null)
        {
            effectController = GetComponent<FieldDomainScreenEffectController>();
        }

        if (effectController == null)
        {
            effectController = FieldDomainScreenEffectController.Instance;
        }
    }

    private void Update()
    {
        if (!enableQuickTest || !Application.isPlaying)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            StartPreviewCycle(EnvironmentType.Gravity);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            StartPreviewCycle(EnvironmentType.DesperationField);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            StartPreviewCycle(EnvironmentType.MiracleField);
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            StartContractOnly();
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            StopPreview();
        }
    }

    private void OnGUI()
    {
        if (!enableQuickTest || !showOnScreenPanel || !Application.isPlaying)
        {
            return;
        }

        InitStyles();

        const float panelWidth = 320f;
        const float panelHeight = 280f;
        Rect panelRect = new Rect(16f, 16f, panelWidth, panelHeight);
        GUI.Box(panelRect, GUIContent.none, m_PanelStyle);

        GUILayout.BeginArea(new Rect(panelRect.x + 12f, panelRect.y + 10f, panelWidth - 24f, panelHeight - 20f));
        GUILayout.Label("场域效果快速测试", m_LabelStyle);
        GUILayout.Space(4f);
        GUILayout.Label(GetStatusText(), m_LabelStyle);
        GUILayout.Space(8f);

        string previewSuffix = useOneShotPreviewCycle ? "（一次循环）" : "（持续预览）";
        if (GUILayout.Button("1 · 重裁域场" + previewSuffix, m_ButtonStyle))
        {
            StartPreviewCycle(EnvironmentType.Gravity);
        }

        if (GUILayout.Button("2 · 绝境域场" + previewSuffix, m_ButtonStyle))
        {
            StartPreviewCycle(EnvironmentType.DesperationField);
        }

        if (GUILayout.Button("3 · 奇迹域场" + previewSuffix, m_ButtonStyle))
        {
            StartPreviewCycle(EnvironmentType.MiracleField);
        }

        GUILayout.Space(4f);

        using (new GUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Q · 收缩", m_ButtonStyle))
            {
                StartContractOnly();
            }

            if (GUILayout.Button("R · 退出预览", m_ButtonStyle))
            {
                StopPreview();
            }
        }

        GUILayout.Space(6f);
        string modeHint = useOneShotPreviewCycle
            ? "1/2/3 自动收缩  Q/R 退出"
            : "1/2/3 持续流动  Q/R 收缩退出";
        GUILayout.Label($"快捷键：{modeHint}\n后处理中心：屏幕正中", m_LabelStyle);
        GUILayout.EndArea();
    }

    [ContextMenu("Preview Verdict Field")]
    private void ContextPreviewVerdict()
    {
        StartPreviewCycle(EnvironmentType.Gravity);
    }

    [ContextMenu("Preview Desperation Field")]
    private void ContextPreviewDesperation()
    {
        StartPreviewCycle(EnvironmentType.DesperationField);
    }

    [ContextMenu("Preview Miracle Field")]
    private void ContextPreviewMiracle()
    {
        StartPreviewCycle(EnvironmentType.MiracleField);
    }

    [ContextMenu("Stop Preview")]
    private void ContextStopPreview()
    {
        StopPreview();
    }

    public void StartPreviewCycle(EnvironmentType environmentType)
    {
        if (!enableQuickTest || effectController == null)
        {
            Debug.LogWarning("[FieldDomainEffectTester] 未找到 FieldDomainScreenEffectController。");
            return;
        }

        Transform origin = ResolveBurstOrigin();

        if (m_PreviewCoroutine != null)
        {
            StopCoroutine(m_PreviewCoroutine);
        }

        if (logStatusToConsole)
        {
            string burstInfo = origin != null ? $"粒子中心={origin.name}" : "无爆发粒子";
            string mode = useOneShotPreviewCycle ? "一次循环" : "持续预览（Q/R 退出）";
            Debug.Log($"[FieldDomainEffectTester] 开始预览 {GetEnvironmentDisplayName(environmentType)}（{mode}），屏幕中心扩散，{burstInfo}");
        }

        m_SustainedPreviewRunning = !useOneShotPreviewCycle;
        m_PreviewCoroutine = useOneShotPreviewCycle
            ? StartCoroutine(RunOneShotPreviewCycle(environmentType, origin))
            : StartCoroutine(RunSustainedPreview(environmentType, origin));
    }

    public void StartContractOnly()
    {
        ExitPreview(playContract: true, forceStop: false);
    }

    public void StopPreview()
    {
        ExitPreview(playContract: true, forceStop: false);
    }

    public void ForceStopPreview()
    {
        ExitPreview(playContract: false, forceStop: true);
    }

    private void ExitPreview(bool playContract, bool forceStop)
    {
        m_SustainedPreviewRunning = false;

        if (m_PreviewCoroutine != null)
        {
            StopCoroutine(m_PreviewCoroutine);
            m_PreviewCoroutine = null;
        }

        if (effectController == null)
        {
            return;
        }

        if (forceStop)
        {
            effectController.ForceStop();
            if (logStatusToConsole)
            {
                Debug.Log("[FieldDomainEffectTester] 已强制停止场域效果。");
            }
            return;
        }

        if (!effectController.HasActiveFieldVisual)
        {
            if (logStatusToConsole)
            {
                Debug.Log("[FieldDomainEffectTester] 当前没有激活的场域效果可收缩。");
            }
            return;
        }

        m_PreviewCoroutine = StartCoroutine(RunContractAndClear());
    }

    private IEnumerator RunOneShotPreviewCycle(EnvironmentType environmentType, Transform origin)
    {
        yield return effectController.PlayPreviewCycle(environmentType, origin, activeHoldDuration, skipOpeningIntroWait: true);
        m_PreviewCoroutine = null;

        if (logStatusToConsole)
        {
            Debug.Log($"[FieldDomainEffectTester] 预览完成 {GetEnvironmentDisplayName(environmentType)}");
        }
    }

    private IEnumerator RunSustainedPreview(EnvironmentType environmentType, Transform origin)
    {
        yield return effectController.PlaySustainedPreview(environmentType, origin, skipOpeningIntroWait: true);

        if (!effectController.HasActiveFieldVisual)
        {
            m_SustainedPreviewRunning = false;
            m_PreviewCoroutine = null;
            yield break;
        }

        while (m_SustainedPreviewRunning
               && effectController.HasActiveFieldVisual
               && effectController.CurrentPhase == FieldDomainVisualPhase.Active)
        {
            yield return null;
        }

        m_SustainedPreviewRunning = false;
        m_PreviewCoroutine = null;

        if (logStatusToConsole && effectController.HasActiveFieldVisual)
        {
            Debug.Log($"[FieldDomainEffectTester] 持续预览结束 {GetEnvironmentDisplayName(environmentType)}");
        }
    }

    private IEnumerator RunContractAndClear()
    {
        yield return effectController.PlayContract(null);
        m_PreviewCoroutine = null;

        if (logStatusToConsole)
        {
            Debug.Log("[FieldDomainEffectTester] 预览已收缩退出。");
        }
    }

    private Transform ResolveBurstOrigin()
    {
        if (burstOrigin != null)
        {
            return burstOrigin;
        }

        if (!useFirstFieldCharacterForBurst)
        {
            return null;
        }

        return TryGetFirstFieldCharacterTransform();
    }

    private Transform TryGetFirstFieldCharacterTransform()
    {
        if (CharacterManager.Instance == null)
        {
            return null;
        }

        Character character = CharacterManager.Instance.GetFieldCharacterByStandPosition(0);
        if (character == null)
        {
            character = CharacterManager.Instance.GetFieldCharacterByStandPosition(1);
        }

        return character != null ? character.transform : null;
    }

    private string GetStatusText()
    {
        if (effectController == null)
        {
            return "状态：缺少 FieldDomainScreenEffectController";
        }

        string rendering = FieldDomainScreenEffectController.IsRendering ? "渲染中" : "未渲染";
        string phase = effectController.CurrentPhase.ToString();
        string type = effectController.HasActiveFieldVisual
            ? GetEnvironmentDisplayName(effectController.ActiveEnvironmentType)
            : "无";
        string previewMode = m_SustainedPreviewRunning ? "持续预览中" : (useOneShotPreviewCycle ? "一次循环" : "空闲");

        string materialStatus = effectController.HasValidEffectMaterial ? "OK" : "缺失/编译失败";

        return $"渲染:{rendering}  阶段:{phase}\n场域:{type}  半径:{effectController.CurrentRadius:F2}\n预览:{previewMode}\nShader材质:{materialStatus}\n后处理中心:屏幕正中 (0.5, 0.5)";
    }

    private static string GetEnvironmentDisplayName(EnvironmentType environmentType)
    {
        return environmentType switch
        {
            EnvironmentType.Gravity => "重裁域场",
            EnvironmentType.DesperationField => "绝境域场",
            EnvironmentType.MiracleField => "奇迹域场",
            _ => environmentType.ToString()
        };
    }

    private void InitStyles()
    {
        if (m_StylesInitialized)
        {
            return;
        }

        m_PanelStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeTexture(2, 2, new Color(0f, 0f, 0f, 0.72f)) }
        };

        m_ButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleCenter
        };

        m_LabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            normal = { textColor = Color.white },
            wordWrap = true
        };

        m_StylesInitialized = true;
    }

    private static Texture2D MakeTexture(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }

        Texture2D texture = new Texture2D(width, height);
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
}
