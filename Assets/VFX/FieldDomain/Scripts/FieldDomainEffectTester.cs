using System.Collections;
using UnityEngine;

/// <summary>
/// Fight 场景运行时快速预览三种场域展开效果。
/// 快捷键：1/2/3 完整循环，Q 收缩，R 停止，C 切换扩散中心。
/// </summary>
public class FieldDomainEffectTester : MonoBehaviour
{
    [Header("开关")]
    [SerializeField] private bool enableQuickTest = true;
    [SerializeField] private bool showOnScreenPanel = true;
    [SerializeField] private bool logStatusToConsole = true;

    [Header("预览参数")]
    [SerializeField] private Transform testOrigin;
    [SerializeField] private float activeHoldDuration = 2f;
    [SerializeField] private bool useFirstFieldCharacterAsOrigin = true;
    [SerializeField] private bool useScreenCenterIfNoOrigin = true;
    [SerializeField] private float screenCenterDepth = 8f;

    [Header("References")]
    [SerializeField] private FieldDomainScreenEffectController effectController;
    [SerializeField] private Camera previewCamera;

    private Transform m_RuntimeOrigin;
    private Coroutine m_PreviewCoroutine;
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

        if (previewCamera == null)
        {
            previewCamera = Camera.main;
        }

        EnsureRuntimeOrigin();
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
        else if (Input.GetKeyDown(KeyCode.C))
        {
            CycleOriginMode();
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
        const float panelHeight = 300f;
        Rect panelRect = new Rect(16f, 16f, panelWidth, panelHeight);
        GUI.Box(panelRect, GUIContent.none, m_PanelStyle);

        GUILayout.BeginArea(new Rect(panelRect.x + 12f, panelRect.y + 10f, panelWidth - 24f, panelHeight - 20f));
        GUILayout.Label("场域效果快速测试", m_LabelStyle);
        GUILayout.Space(4f);
        GUILayout.Label(GetStatusText(), m_LabelStyle);
        GUILayout.Space(8f);

        if (GUILayout.Button("1 · 重裁域场（完整循环）", m_ButtonStyle))
        {
            StartPreviewCycle(EnvironmentType.Gravity);
        }

        if (GUILayout.Button("2 · 绝境域场（完整循环）", m_ButtonStyle))
        {
            StartPreviewCycle(EnvironmentType.DesperationField);
        }

        if (GUILayout.Button("3 · 奇迹域场（完整循环）", m_ButtonStyle))
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

            if (GUILayout.Button("R · 停止", m_ButtonStyle))
            {
                StopPreview();
            }
        }

        if (GUILayout.Button("C · 切换扩散中心", m_ButtonStyle))
        {
            CycleOriginMode();
        }

        GUILayout.Space(6f);
        GUILayout.Label("快捷键：1/2/3 预览  Q收缩  R停止  C换中心", m_LabelStyle);
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

        Transform origin = ResolveOrigin();
        if (origin == null)
        {
            Debug.LogWarning("[FieldDomainEffectTester] 无法确定扩散中心，请在 Inspector 指定 Test Origin。");
            return;
        }

        if (m_PreviewCoroutine != null)
        {
            StopCoroutine(m_PreviewCoroutine);
        }

        if (logStatusToConsole)
        {
            Debug.Log($"[FieldDomainEffectTester] 开始预览 {GetEnvironmentDisplayName(environmentType)}，中心={origin.name}，位置={origin.position}");
        }

        m_PreviewCoroutine = StartCoroutine(RunPreviewCycle(environmentType, origin));
    }

    public void StartContractOnly()
    {
        if (effectController == null)
        {
            return;
        }

        if (m_PreviewCoroutine != null)
        {
            StopCoroutine(m_PreviewCoroutine);
            m_PreviewCoroutine = null;
        }

        Transform origin = ResolveOrigin();
        m_PreviewCoroutine = StartCoroutine(RunContract(origin));
    }

    public void StopPreview()
    {
        if (m_PreviewCoroutine != null)
        {
            StopCoroutine(m_PreviewCoroutine);
            m_PreviewCoroutine = null;
        }

        effectController?.ForceStop();

        if (logStatusToConsole)
        {
            Debug.Log("[FieldDomainEffectTester] 已强制停止场域效果。");
        }
    }

    private IEnumerator RunPreviewCycle(EnvironmentType environmentType, Transform origin)
    {
        yield return effectController.PlayPreviewCycle(environmentType, origin, activeHoldDuration, skipOpeningIntroWait: true);
        m_PreviewCoroutine = null;

        if (logStatusToConsole)
        {
            Debug.Log($"[FieldDomainEffectTester] 预览完成 {GetEnvironmentDisplayName(environmentType)}");
        }
    }

    private IEnumerator RunContract(Transform origin)
    {
        if (!effectController.HasActiveFieldVisual)
        {
            if (logStatusToConsole)
            {
                Debug.Log("[FieldDomainEffectTester] 当前没有激活的场域效果可收缩。");
            }

            yield break;
        }

        yield return effectController.PlayContract(origin);
        m_PreviewCoroutine = null;
    }

    private Transform ResolveOrigin()
    {
        if (testOrigin != null)
        {
            return testOrigin;
        }

        if (useFirstFieldCharacterAsOrigin)
        {
            Transform characterOrigin = TryGetFirstFieldCharacterTransform();
            if (characterOrigin != null)
            {
                return characterOrigin;
            }
        }

        if (useScreenCenterIfNoOrigin)
        {
            EnsureRuntimeOrigin();
            UpdateScreenCenterOrigin();
            return m_RuntimeOrigin;
        }

        return null;
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

    private void EnsureRuntimeOrigin()
    {
        if (m_RuntimeOrigin != null)
        {
            return;
        }

        GameObject originObject = new GameObject("FieldDomainTestOrigin");
        originObject.hideFlags = HideFlags.HideAndDontSave;
        m_RuntimeOrigin = originObject.transform;
    }

    private void UpdateScreenCenterOrigin()
    {
        if (m_RuntimeOrigin == null)
        {
            return;
        }

        Camera camera = previewCamera != null ? previewCamera : Camera.main;
        if (camera == null)
        {
            m_RuntimeOrigin.position = Vector3.zero;
            return;
        }

        Vector3 viewportCenter = new Vector3(0.5f, 0.42f, screenCenterDepth);
        m_RuntimeOrigin.position = camera.ViewportToWorldPoint(viewportCenter);
    }

    private void CycleOriginMode()
    {
        if (testOrigin != null)
        {
            testOrigin = null;
            useFirstFieldCharacterAsOrigin = true;
            useScreenCenterIfNoOrigin = false;
        }
        else if (useFirstFieldCharacterAsOrigin)
        {
            useFirstFieldCharacterAsOrigin = false;
            useScreenCenterIfNoOrigin = true;
        }
        else
        {
            useScreenCenterIfNoOrigin = false;
            useFirstFieldCharacterAsOrigin = true;
        }

        Transform origin = ResolveOrigin();
        if (logStatusToConsole)
        {
            string mode = testOrigin != null
                ? "手动 Transform"
                : useFirstFieldCharacterAsOrigin
                    ? "首个在场角色"
                    : "屏幕中心";
            Debug.Log($"[FieldDomainEffectTester] 扩散中心模式：{mode}（当前={(origin != null ? origin.name : "无")}）");
        }
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

        string materialStatus = effectController.HasValidEffectMaterial ? "OK" : "缺失/编译失败";
        string hookStatus = GetComponent<FieldDomainCameraRenderHook>() != null ? "已启用" : "未挂载";

        string originMode = testOrigin != null
            ? "手动"
            : useFirstFieldCharacterAsOrigin
                ? "角色"
                : "屏幕中心";

        return $"渲染:{rendering}  阶段:{phase}\n场域:{type}  半径:{effectController.CurrentRadius:F2}\nShader材质:{materialStatus}  运行时Hook:{hookStatus}\n中心模式:{originMode}";
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

    private void OnDestroy()
    {
        if (m_RuntimeOrigin != null)
        {
            Destroy(m_RuntimeOrigin.gameObject);
            m_RuntimeOrigin = null;
        }
    }
}
