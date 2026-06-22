using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 设置界面诊断浮层：Tab 显隐，C 清空；在设置按钮/面板打开时写入诊断文本。
/// 挂载在通用物体 Prefab 上，随 DontDestroyOnLoad 跨场景保留。
/// </summary>
[DisallowMultipleComponent]
public class SettingsPanelDebugOverlay : MonoBehaviour
{
    private const int MaxLineCount = 80;

    public static SettingsPanelDebugOverlay Instance { get; private set; }

    [SerializeField] private TMP_Text m_debugText;
    [SerializeField] private GameObject m_overlayRoot;

    private readonly StringBuilder m_buffer = new StringBuilder(4096);
    private bool m_isVisible;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureOverlayHidden();
        AppendBootSnapshot();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            SetVisible(!m_isVisible);
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            ClearText();
        }
    }

    public static void LogSettingsDiagnostics(string stage, SettingsPanelView openerFallback = null)
    {
        if (Instance == null)
        {
            return;
        }

        Instance.AppendSettingsDiagnostics(stage, openerFallback);
        Instance.SetVisible(true);
    }

    public static void NotifyPanelAwake(SettingsPanelView panel, bool claimedSingleton)
    {
        if (Instance == null || panel == null)
        {
            return;
        }

        Instance.AppendLine($"[Panel.Awake] {DescribePanel(panel)} claimedSingleton={claimedSingleton}");
        Instance.RefreshText();
    }

    private void AppendBootSnapshot()
    {
        AppendLine("=== Boot ===");
        AppendLine($"scene={SceneManager.GetActiveScene().name}");
        AppendLine($"overlay={gameObject.name} @ {DescribeScene(gameObject)}");
        AppendLine("Tab=显隐  C=清空  打开设置时会自动写入诊断");
        RefreshText();
    }

    private void AppendSettingsDiagnostics(string stage, SettingsPanelView openerFallback)
    {
        AppendLine(string.Empty);
        AppendLine($"=== [{stage}] t={Time.unscaledTime:F2} scene={SceneManager.GetActiveScene().name} ===");

        SettingsPanelView instancePanel = SettingsPanelView.Instance;
        AppendLine($"Instance: {DescribePanel(instancePanel)}");
        AppendLine($"OpenerFallback: {DescribePanel(openerFallback)}");

        SettingsPanelView[] allPanels = FindAllSettingsPanels();
        AppendLine($"FindObjectsByType<SettingsPanelView>: {allPanels.Length}");
        for (int i = 0; i < allPanels.Length; i++)
        {
            AppendLine($"  [{i}] {DescribePanel(allPanels[i])}");
        }

        AppendLine($"Datas.Instance: {(Datas.Instance != null ? "OK" : "NULL")}");
        AppendLine($"ScreenTransition.Instance: {(ScreenTransition.Instance != null ? "OK" : "NULL")}");
        if (ScreenTransition.Instance != null)
        {
            Canvas overlayCanvas = ScreenTransition.Instance.OverlayCanvas;
            AppendLine($"  OverlayCanvas: {(overlayCanvas != null ? DescribeObject(overlayCanvas.gameObject) : "NULL")}");
        }

        AppendDontDestroyCanvasDiagnostics();
        AppendResolvedOpenTarget(instancePanel, openerFallback);
        TrimLines();
        RefreshText();
    }

    private void AppendDontDestroyCanvasDiagnostics()
    {
        DontDestroyCanvas[] canvases = Object.FindObjectsByType<DontDestroyCanvas>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        AppendLine($"DontDestroyCanvas count: {canvases.Length}");
        for (int i = 0; i < canvases.Length; i++)
        {
            AppendLine($"  [{i}] {DescribeObject(canvases[i].gameObject)}");
        }
    }

    private void AppendResolvedOpenTarget(SettingsPanelView instancePanel, SettingsPanelView openerFallback)
    {
        SettingsPanelView resolved = instancePanel != null ? instancePanel : openerFallback;
        if (resolved == null)
        {
            AppendLine("结论: 无可用 SettingsPanel → Opener 会打 Warning，面板无法打开");
            return;
        }

        if (!resolved.gameObject.activeInHierarchy)
        {
            AppendLine("结论: 目标 Panel 存在但未激活 (activeInHierarchy=false)");
            return;
        }

        if (resolved.gameObject.scene.name != "DontDestroyOnLoad"
            && SceneManager.GetActiveScene().name != resolved.gameObject.scene.name)
        {
            AppendLine("结论: 目标 Panel 绑定在其它场景，切场景后可能被销毁");
            return;
        }

        AppendLine($"结论: 将调用 {resolved.name}.Open() @ {DescribeScene(resolved.gameObject)}");
    }

    private static SettingsPanelView[] FindAllSettingsPanels()
    {
        return Object.FindObjectsByType<SettingsPanelView>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
    }

    private static string DescribePanel(SettingsPanelView panel)
    {
        if (panel == null)
        {
            return "NULL";
        }

        return DescribeObject(panel.gameObject);
    }

    private static string DescribeObject(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return "NULL";
        }

        string hierarchy = BuildHierarchyPath(gameObject.transform);
        return $"{gameObject.name} active={gameObject.activeInHierarchy} scene={DescribeScene(gameObject)} path={hierarchy}";
    }

    private static string DescribeScene(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return "NULL";
        }

        Scene scene = gameObject.scene;
        return scene.IsValid() ? scene.name : "invalid";
    }

    private static string BuildHierarchyPath(Transform transform)
    {
        if (transform == null)
        {
            return string.Empty;
        }

        var path = new StringBuilder(transform.name);
        Transform current = transform.parent;
        while (current != null)
        {
            path.Insert(0, '/');
            path.Insert(0, current.name);
            current = current.parent;
        }

        return path.ToString();
    }

    private void AppendLine(string line)
    {
        m_buffer.AppendLine(line);
    }

    private void ClearText()
    {
        m_buffer.Length = 0;
        RefreshText();
    }

    private void TrimLines()
    {
        string text = m_buffer.ToString();
        string[] lines = text.Split('\n');
        if (lines.Length <= MaxLineCount)
        {
            return;
        }

        m_buffer.Length = 0;
        int start = lines.Length - MaxLineCount;
        for (int i = start; i < lines.Length; i++)
        {
            m_buffer.AppendLine(lines[i].TrimEnd('\r'));
        }
    }

    private void RefreshText()
    {
        if (m_debugText != null)
        {
            m_debugText.text = m_buffer.ToString();
        }
    }

    private void SetVisible(bool visible)
    {
        m_isVisible = visible;
        if (m_overlayRoot != null)
        {
            m_overlayRoot.SetActive(visible);
            return;
        }

        if (m_debugText != null)
        {
            m_debugText.gameObject.SetActive(visible);
        }
    }

    private void EnsureOverlayHidden()
    {
        SetVisible(false);
    }
}
