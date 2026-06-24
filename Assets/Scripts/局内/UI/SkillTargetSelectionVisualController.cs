using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 选目标/切人阶段的场景暗淡视觉：背景变暗，非目标单位与战斗 UI 变暗，目标类型全体单位与提示文本保持亮度。
/// </summary>
public interface ISkillTargetSelectionVisual
{
    void ShowEnemyTargetSelection();
    void ShowAllyTargetSelection();
    void ShowSwapReserveSelection();
    void Hide();
    void ClearSelectionHoverVisuals();
}

[DisallowMultipleComponent]
public class SkillTargetSelectionVisualController : MonoBehaviour, ISkillTargetSelectionVisual
{
    public static SkillTargetSelectionVisualController Instance { get; private set; }

    private const int SelectionDimPriority = 3;

    [Header("暗淡参数")]
    [SerializeField] private float m_dimMultiplier = 0.3f;
    [SerializeField] private float m_duration = 0.3f;
    [SerializeField] private Ease m_ease = Ease.InOutQuad;

    [Header("引用")]
    [SerializeField] private TMP_Text m_targetPromptText;
    [SerializeField] private List<CanvasGroup> m_battleUiGroupsToDim = new List<CanvasGroup>();

    private enum SelectionVisualMode
    {
        None,
        EnemyTarget,
        AllyTarget,
        SwapReserve
    }

    private struct SpriteDimSnapshot
    {
        public SpriteRenderer Renderer;
        public Color OriginalColor;
    }

    private struct CanvasDimSnapshot
    {
        public CanvasGroup Group;
        public float OriginalAlpha;
    }

    private SelectionVisualMode m_currentMode = SelectionVisualMode.None;
    private readonly List<SpriteDimSnapshot> m_spriteSnapshots = new List<SpriteDimSnapshot>();
    private readonly List<CanvasDimSnapshot> m_canvasSnapshots = new List<CanvasDimSnapshot>();
    private readonly List<Tween> m_activeTweens = new List<Tween>();
    private bool m_staticBattleUiGroupsResolved;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        SkillManager skillManager = GetComponent<SkillManager>();
        if (skillManager != null && m_targetPromptText == null)
        {
            m_targetPromptText = skillManager.targetPromptText;
        }

        if (m_targetPromptText == null && CharacterManager.Instance != null)
        {
            m_targetPromptText = CharacterManager.Instance.promptText;
        }
    }

    private void Start()
    {
        EnsureStaticBattleUiGroupsResolved();
    }

    private void OnDestroy()
    {
        KillActiveTweens();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ShowEnemyTargetSelection()
    {
        ApplyMode(SelectionVisualMode.EnemyTarget);
    }

    public void ShowAllyTargetSelection()
    {
        ApplyMode(SelectionVisualMode.AllyTarget);
    }

    public void ShowSwapReserveSelection()
    {
        ApplyMode(SelectionVisualMode.SwapReserve);
    }

    public void Hide()
    {
        if (m_currentMode == SelectionVisualMode.None)
        {
            return;
        }

        RestoreVisualState();
        m_currentMode = SelectionVisualMode.None;
    }

    public void ClearSelectionHoverVisuals()
    {
        TurnImageManager.Instance?.ClearCombatantHoverHighlight();

        if (EnemyManager.Instance != null)
        {
            IReadOnlyList<Enemy> enemies = EnemyManager.Instance.AliveEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                enemies[i]?.ClearTargetSelectionHoverVisual();
            }
        }

        CharacterManager characterManager = CharacterManager.Instance;
        if (characterManager == null)
        {
            return;
        }

        ClearCharacterHoverList(characterManager.fieldCharacters);
        ClearCharacterHoverList(characterManager.reserveCharacters);
    }

    private static void ClearCharacterHoverList(IReadOnlyList<Character> characters)
    {
        if (characters == null)
        {
            return;
        }

        for (int i = 0; i < characters.Count; i++)
        {
            characters[i]?.ClearTargetSelectionHoverVisual();
        }
    }

    private void ApplyMode(SelectionVisualMode mode)
    {
        if (m_currentMode == mode)
        {
            return;
        }

        if (m_currentMode != SelectionVisualMode.None)
        {
            RestoreVisualStateImmediate();
        }

        m_currentMode = mode;
        KillActiveTweens();
        m_spriteSnapshots.Clear();
        m_canvasSnapshots.Clear();

        SkillDescription.Instance?.HideDescription();
        BackgroundManager.Instance?.ChangeBackground(true, SelectionDimPriority);
        EnsurePromptVisible();
        if (m_currentMode == SelectionVisualMode.SwapReserve)
        {
            EnsureReserveButtonsVisible();
        }

        DimNonTargetUnits();
        DimBattleUiGroups();
        DimCommandButtons();
    }

    /// <summary>
    /// 选敌：全部敌人保持亮度；选友：全部场上我方保持亮度。
    /// </summary>
    private void DimNonTargetUnits()
    {
        if (m_currentMode == SelectionVisualMode.EnemyTarget)
        {
            DimAllCharacters();
            return;
        }

        if (m_currentMode == SelectionVisualMode.AllyTarget)
        {
            DimAllEnemies();
            DimReserveCharacters();
            return;
        }

        if (m_currentMode == SelectionVisualMode.SwapReserve)
        {
            DimAllEnemies();
        }
    }

    private void DimAllCharacters()
    {
        CharacterManager manager = CharacterManager.Instance;
        if (manager == null)
        {
            return;
        }

        DimCharacterList(manager.fieldCharacters);
        DimCharacterList(manager.reserveCharacters);
    }

    private void DimReserveCharacters()
    {
        CharacterManager manager = CharacterManager.Instance;
        if (manager == null)
        {
            return;
        }

        DimCharacterList(manager.reserveCharacters);
    }

    private void DimCharacterList(IReadOnlyList<Character> characters)
    {
        if (characters == null)
        {
            return;
        }

        for (int i = 0; i < characters.Count; i++)
        {
            DimUnitVisuals(characters[i]);
        }
    }

    private void DimAllEnemies()
    {
        EnemyManager manager = EnemyManager.Instance;
        if (manager == null)
        {
            return;
        }

        IReadOnlyList<Enemy> enemies = manager.AliveEnemies;
        for (int i = 0; i < enemies.Count; i++)
        {
            DimUnitVisuals(enemies[i]);
        }
    }

    private void DimUnitVisuals(UnitCombatant unit)
    {
        if (unit == null)
        {
            return;
        }

        SpriteRenderer[] renderers = unit.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            Color originalColor = renderer.color;
            m_spriteSnapshots.Add(new SpriteDimSnapshot
            {
                Renderer = renderer,
                OriginalColor = originalColor
            });
            m_activeTweens.Add(renderer
                .DOColor(MultiplyRgb(originalColor, m_dimMultiplier), m_duration)
                .SetEase(m_ease));
        }

        CanvasGroup[] canvasGroups = unit.GetComponentsInChildren<CanvasGroup>(true);
        for (int i = 0; i < canvasGroups.Length; i++)
        {
            CanvasGroup canvasGroup = canvasGroups[i];
            if (canvasGroup == null
                || ShouldSkipCommandButtonCanvasGroup(canvasGroup)
                || ShouldSkipReserveButtonCanvasGroup(canvasGroup))
            {
                continue;
            }

            TryDimCanvasGroup(canvasGroup);
        }
    }

    private bool TryDimCanvasGroup(CanvasGroup canvasGroup)
    {
        if (canvasGroup == null
            || IsPromptCanvasGroup(canvasGroup)
            || IsCommandButtonCanvasGroup(canvasGroup)
            || ShouldSkipReserveButtonCanvasGroup(canvasGroup))
        {
            return false;
        }

        for (int i = 0; i < m_canvasSnapshots.Count; i++)
        {
            if (m_canvasSnapshots[i].Group == canvasGroup)
            {
                return false;
            }
        }

        m_canvasSnapshots.Add(new CanvasDimSnapshot
        {
            Group = canvasGroup,
            OriginalAlpha = canvasGroup.alpha
        });
        m_activeTweens.Add(canvasGroup.DOFade(m_dimMultiplier, m_duration).SetEase(m_ease));
        return true;
    }

    /// <summary>
    /// 技能按钮挂在角色 Canvas 下；选敌时会暗淡我方角色，需跳过按钮 CanvasGroup，
    /// 仅由 DimBattleUiGroups 统一变暗（与选友技能行为一致）。
    /// </summary>
    private static bool ShouldSkipCommandButtonCanvasGroup(CanvasGroup canvasGroup)
    {
        if (canvasGroup == null)
        {
            return false;
        }

        if (canvasGroup.GetComponent<CommandButton>() != null)
        {
            return true;
        }

        CommandButtonManager buttonManager = CommandButtonManager.Instance;
        if (buttonManager == null || buttonManager.buttonContainer == null)
        {
            return false;
        }

        Transform groupTransform = canvasGroup.transform;
        Transform buttonContainer = buttonManager.buttonContainer;
        return groupTransform == buttonContainer || groupTransform.IsChildOf(buttonContainer);
    }

    private static bool ShouldSkipReserveButtonCanvasGroup(CanvasGroup canvasGroup)
    {
        if (canvasGroup == null)
        {
            return false;
        }

        CharacterManager characterManager = CharacterManager.Instance;
        if (characterManager == null || characterManager.reserveButtonContainer == null)
        {
            return false;
        }

        Transform groupTransform = canvasGroup.transform;
        Transform reserveContainer = characterManager.reserveButtonContainer;
        return groupTransform == reserveContainer || groupTransform.IsChildOf(reserveContainer);
    }

    private void DimBattleUiGroups()
    {
        EnsureStaticBattleUiGroupsResolved();

        for (int i = 0; i < m_battleUiGroupsToDim.Count; i++)
        {
            CanvasGroup canvasGroup = m_battleUiGroupsToDim[i];
            if (canvasGroup == null)
            {
                continue;
            }

            TryDimCanvasGroup(canvasGroup);
        }
    }

    /// <summary>
    /// 选友时按钮挂在未变暗的场上角色 Canvas 下，需每次选目标时单独变暗（与选敌一致）。
    /// </summary>
    private void DimCommandButtons()
    {
        CommandButtonManager buttonManager = CommandButtonManager.Instance;
        if (buttonManager == null || buttonManager.commandButtons == null)
        {
            return;
        }

        for (int i = 0; i < buttonManager.commandButtons.Count; i++)
        {
            CommandButton button = buttonManager.commandButtons[i];
            if (button == null || !button.HasSkill)
            {
                continue;
            }

            CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                continue;
            }

            TryDimCanvasGroup(canvasGroup);
        }
    }

    private static bool IsCommandButtonCanvasGroup(CanvasGroup canvasGroup)
    {
        return ShouldSkipCommandButtonCanvasGroup(canvasGroup);
    }

    private void EnsurePromptVisible()
    {
        if (m_targetPromptText == null)
        {
            return;
        }

        CanvasGroup promptGroup = m_targetPromptText.GetComponentInParent<CanvasGroup>();
        if (promptGroup != null)
        {
            promptGroup.alpha = 1f;
        }
    }

    private void EnsureReserveButtonsVisible()
    {
        CharacterManager characterManager = CharacterManager.Instance;
        if (characterManager == null || characterManager.reserveButtonContainer == null)
        {
            return;
        }

        CanvasGroup containerGroup = characterManager.reserveButtonContainer.GetComponent<CanvasGroup>();
        if (containerGroup != null)
        {
            containerGroup.alpha = 1f;
        }

        CanvasGroup[] canvasGroups = characterManager.reserveButtonContainer.GetComponentsInChildren<CanvasGroup>(true);
        for (int i = 0; i < canvasGroups.Length; i++)
        {
            if (canvasGroups[i] != null)
            {
                canvasGroups[i].alpha = 1f;
            }
        }
    }

    private bool IsPromptCanvasGroup(CanvasGroup canvasGroup)
    {
        if (m_targetPromptText == null || canvasGroup == null)
        {
            return false;
        }

        return m_targetPromptText.transform.IsChildOf(canvasGroup.transform)
            || m_targetPromptText.transform == canvasGroup.transform;
    }

    private void EnsureStaticBattleUiGroupsResolved()
    {
        if (m_staticBattleUiGroupsResolved)
        {
            return;
        }

        TryAddCanvasGroup(TurnImageManager.Instance != null ? TurnImageManager.Instance.turnImageContainer : null);

        BossHealthBarManager bossBarManager = BossHealthBarManager.Instance;
        if (bossBarManager != null)
        {
            CanvasGroup[] bossBarGroups = bossBarManager.GetComponentsInChildren<CanvasGroup>(true);
            for (int i = 0; i < bossBarGroups.Length; i++)
            {
                TryAddCanvasGroup(bossBarGroups[i]);
            }
        }

        m_staticBattleUiGroupsResolved = true;
    }

    private void TryAddCanvasGroup(CanvasGroup canvasGroup)
    {
        if (canvasGroup == null || m_battleUiGroupsToDim.Contains(canvasGroup))
        {
            return;
        }

        m_battleUiGroupsToDim.Add(canvasGroup);
    }

    private void TryAddCanvasGroup(RectTransform rectTransform)
    {
        if (rectTransform == null)
        {
            return;
        }

        TryAddCanvasGroup(rectTransform.GetComponent<CanvasGroup>());
    }

    private void RestoreVisualState()
    {
        KillActiveTweens();
        RestoreSnapshots();
        BackgroundManager.Instance?.ChangeBackground(false, SelectionDimPriority);
        m_spriteSnapshots.Clear();
        m_canvasSnapshots.Clear();
    }

    private void RestoreVisualStateImmediate()
    {
        KillActiveTweens();
        RestoreSnapshots();
        m_spriteSnapshots.Clear();
        m_canvasSnapshots.Clear();
    }

    private void RestoreSnapshots()
    {
        for (int i = 0; i < m_spriteSnapshots.Count; i++)
        {
            SpriteDimSnapshot snapshot = m_spriteSnapshots[i];
            if (snapshot.Renderer != null)
            {
                snapshot.Renderer.color = snapshot.OriginalColor;
            }
        }

        for (int i = 0; i < m_canvasSnapshots.Count; i++)
        {
            CanvasDimSnapshot snapshot = m_canvasSnapshots[i];
            if (snapshot.Group != null)
            {
                snapshot.Group.alpha = snapshot.OriginalAlpha;
            }
        }
    }

    private void KillActiveTweens()
    {
        for (int i = 0; i < m_activeTweens.Count; i++)
        {
            Tween tween = m_activeTweens[i];
            if (tween != null && tween.IsActive())
            {
                tween.Kill();
            }
        }

        m_activeTweens.Clear();
    }

    private static Color MultiplyRgb(Color color, float multiplier)
    {
        return new Color(color.r * multiplier, color.g * multiplier, color.b * multiplier, color.a);
    }
}
