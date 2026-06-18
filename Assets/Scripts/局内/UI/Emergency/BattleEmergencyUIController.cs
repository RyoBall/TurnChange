using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 驱动左下状态栏与右下换人键的紧急脉冲。运行时按 E（默认）可切换预览模式，强制显示全部光圈。
/// </summary>
[DefaultExecutionOrder(50)]
public class BattleEmergencyUIController : MonoBehaviour
{
    [Header("Thresholds")]
    [SerializeField, Range(0.01f, 1f)] private float hpThreshold = 0.4f;
    [SerializeField, Range(1, 5)] private int chaosThreshold = 4;

    [Header("Bindings")]
    [SerializeField] private CharacterStateUIManager characterStateUIManager;
    [SerializeField] private List<CharacterStateUIItem> characterStateItems = new List<CharacterStateUIItem>();
    [SerializeField] private UIEmergencyPulseEffect changeButtonPulse;

    [Header("Preview (Play Mode)")]
    [SerializeField] private bool enablePreviewHotkey = true;
    [SerializeField] private KeyCode previewToggleKey = KeyCode.E;
    [SerializeField] private bool showPreviewPanel = true;

    private readonly List<UIEmergencyPulseEffect> m_ChangeButtonPulses = new List<UIEmergencyPulseEffect>();
    private bool m_PreviewActive;
    private GUIStyle m_PanelStyle;
    private GUIStyle m_ButtonStyle;
    private GUIStyle m_LabelStyle;
    private bool m_StylesInitialized;

    public bool IsPreviewActive => m_PreviewActive;

    private void Awake()
    {
        ResolveBindings();
    }

    private void Start()
    {
        ResolveBindings();
        RefreshEmergencyVisuals();
    }

    private void OnEnable()
    {
        SubscribeCharacterEvents();
    }

    private void OnDisable()
    {
        UnsubscribeCharacterEvents();
        m_PreviewActive = false;
        SetAllEmergencyVisuals(false);
    }

    private void Update()
    {
        if (changeButtonPulse == null)
        {
            ResolveChangeButtonPulses();
        }

        HandlePreviewInput();

        if (m_PreviewActive)
        {
            ApplyPreviewVisuals();
            return;
        }

        RefreshEmergencyVisuals();
    }

    private void OnGUI()
    {
        if (!Application.isPlaying || !showPreviewPanel || !enablePreviewHotkey)
        {
            return;
        }

        InitStyles();

        const float panelWidth = 280f;
        const float panelHeight = 118f;
        Rect panelRect = new Rect(16f, Screen.height - panelHeight - 16f, panelWidth, panelHeight);
        GUI.Box(panelRect, GUIContent.none, m_PanelStyle);

        GUILayout.BeginArea(new Rect(panelRect.x + 12f, panelRect.y + 10f, panelWidth - 24f, panelHeight - 20f));
        GUILayout.Label("紧急光圈 · 预览", m_LabelStyle);
        GUILayout.Label($"状态：{(m_PreviewActive ? "预览中（忽略 HP/混沌）" : "跟随战斗状态")}", m_LabelStyle);
        GUILayout.Space(6f);

        string buttonLabel = m_PreviewActive ? "关闭预览" : "开启预览";
        if (GUILayout.Button($"{buttonLabel}  [{previewToggleKey}]", m_ButtonStyle))
        {
            TogglePreview();
        }

        GUILayout.EndArea();
    }

    [ContextMenu("Toggle Emergency Pulse Preview")]
    private void ContextTogglePreview()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[BattleEmergencyUIController] 预览仅在 Play 模式下可用。");
            return;
        }

        TogglePreview();
    }

    public void TogglePreview()
    {
        m_PreviewActive = !m_PreviewActive;
        if (m_PreviewActive)
        {
            ResolveBindings();
            ApplyPreviewVisuals();
            Debug.Log("[BattleEmergencyUIController] 紧急光圈预览已开启。");
        }
        else
        {
            RefreshEmergencyVisuals();
            Debug.Log("[BattleEmergencyUIController] 紧急光圈预览已关闭。");
        }
    }

    private void HandlePreviewInput()
    {
        if (!enablePreviewHotkey || !Application.isPlaying)
        {
            return;
        }

        if (Input.GetKeyDown(previewToggleKey))
        {
            TogglePreview();
        }
    }

    private void ApplyPreviewVisuals()
    {
        ResolveBindings();
        SetAllEmergencyVisuals(true);
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

    private void ResolveBindings()
    {
        if (characterStateUIManager == null)
        {
            characterStateUIManager = FindFirstObjectByType<CharacterStateUIManager>();
        }

        if (characterStateUIManager != null)
        {
            characterStateItems.Clear();
            characterStateItems.AddRange(characterStateUIManager.CharacterUIs);
        }

        ResolveChangeButtonPulses();
    }

    private void ResolveChangeButtonPulses()
    {
        m_ChangeButtonPulses.Clear();

        foreach (List<CommandButton> buttonList in EnumerateCommandButtonLists())
        {
            CollectPulsesFromButtons(buttonList);
        }

        CommandButton[] buttons = FindObjectsByType<CommandButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (CommandButton button in buttons)
        {
            if (button == null || !button.IsChangeSkillButton)
            {
                continue;
            }

            UIEmergencyPulseEffect pulse = button.GetComponent<UIEmergencyPulseEffect>();
            if (pulse == null || m_ChangeButtonPulses.Contains(pulse))
            {
                continue;
            }

            pulse.RebuildIfNeeded();
            if (!pulse.HasValidShape)
            {
                continue;
            }

            m_ChangeButtonPulses.Add(pulse);
        }

        changeButtonPulse = SelectPrimaryChangeButtonPulse();
    }

    private void CollectPulsesFromButtons(IEnumerable<CommandButton> buttons)
    {
        if (buttons == null)
        {
            return;
        }

        foreach (CommandButton button in buttons)
        {
            if (button == null || !button.IsChangeSkillButton)
            {
                continue;
            }

            UIEmergencyPulseEffect pulse = button.GetComponent<UIEmergencyPulseEffect>();
            if (pulse == null || m_ChangeButtonPulses.Contains(pulse))
            {
                continue;
            }

            pulse.RebuildIfNeeded();
            if (!pulse.HasValidShape)
            {
                continue;
            }

            m_ChangeButtonPulses.Add(pulse);
        }
    }

    private UIEmergencyPulseEffect SelectPrimaryChangeButtonPulse()
    {
        foreach (UIEmergencyPulseEffect pulse in m_ChangeButtonPulses)
        {
            CommandButton button = pulse.GetComponent<CommandButton>();
            if (button != null && button.IsChangeSkillButton)
            {
                return pulse;
            }
        }

        return m_ChangeButtonPulses.Count > 0 ? m_ChangeButtonPulses[0] : null;
    }

    private static IEnumerable<List<CommandButton>> EnumerateCommandButtonLists()
    {
        if (CommandSkillManager.Instance != null && CommandSkillManager.Instance.commandButtons != null)
        {
            yield return CommandSkillManager.Instance.commandButtons;
        }
    }

    private IEnumerable<CharacterStateUIItem> GetActiveCharacterStateItems()
    {
        if (characterStateItems != null && characterStateItems.Count > 0)
        {
            foreach (CharacterStateUIItem item in characterStateItems)
            {
                if (item != null)
                {
                    yield return item;
                }
            }

            yield break;
        }

        CharacterStateUIItem[] foundItems = FindObjectsByType<CharacterStateUIItem>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (CharacterStateUIItem item in foundItems)
        {
            if (item != null)
            {
                yield return item;
            }
        }
    }

    private void SubscribeCharacterEvents()
    {
        CharacterManager characterManager = CharacterManager.Instance;
        if (characterManager == null)
        {
            return;
        }

        characterManager.OnFieldCharacterSwapped -= HandleFieldCharacterSwapped;
        characterManager.OnFieldCharactersReordered -= HandleFieldCharactersReordered;
        characterManager.OnFieldCharacterSwapped += HandleFieldCharacterSwapped;
        characterManager.OnFieldCharactersReordered += HandleFieldCharactersReordered;
    }

    private void UnsubscribeCharacterEvents()
    {
        CharacterManager characterManager = CharacterManager.Instance;
        if (characterManager == null)
        {
            return;
        }

        characterManager.OnFieldCharacterSwapped -= HandleFieldCharacterSwapped;
        characterManager.OnFieldCharactersReordered -= HandleFieldCharactersReordered;
    }

    private void HandleFieldCharacterSwapped(Character oldCharacter, Character newCharacter)
    {
        RefreshEmergencyVisuals();
    }

    private void HandleFieldCharactersReordered()
    {
        RefreshEmergencyVisuals();
    }

    private void RefreshEmergencyVisuals()
    {
        CharacterManager characterManager = CharacterManager.Instance;
        if (characterManager == null)
        {
            SetAllEmergencyVisuals(false);
            return;
        }

        bool anyEmergency = false;
        foreach (CharacterStateUIItem item in GetActiveCharacterStateItems())
        {
            bool isEmergency = IsEmergency(item.CurrentCharacter);
            item.SetEmergencyActive(isEmergency);
            anyEmergency |= isEmergency;
        }

        SetChangeButtonPulsesActive(anyEmergency);
    }

    private void SetAllEmergencyVisuals(bool active)
    {
        foreach (CharacterStateUIItem item in GetActiveCharacterStateItems())
        {
            item.SetEmergencyActive(active);
        }

        SetChangeButtonPulsesActive(active);
    }

    private void SetChangeButtonPulsesActive(bool active)
    {
        if (changeButtonPulse == null)
        {
            ResolveChangeButtonPulses();
        }

        changeButtonPulse = SelectPrimaryChangeButtonPulse();
        changeButtonPulse?.SetEmergencyActive(active);
    }

    private bool IsEmergency(Character character)
    {
        if (character == null || character.IsDead || character.maxHP <= 0)
        {
            return false;
        }

        float hpRatio = character.currentHP / (float)character.maxHP;
        return hpRatio <= hpThreshold || character.ChaosValue >= chaosThreshold;
    }
}
