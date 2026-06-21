# AGENTS.md

This file provides guidance to AI coding agents (GitHub Copilot, Claude, etc.) when working with code in this repository.

## Core Principles

1. **尽可能复用已有的代码。** 在实现新功能前，先搜索项目中是否已有类似的实现、工具方法、UI 组件或数据类。优先扩展现有代码而非重复造轮子。如果现有代码不完全满足需求，优先考虑重构扩展现有代码，而非新建文件。

2. **接口优先。** 所有对外暴露的功能必须定义接口，其他模块只允许使用接口类型引用。接口定义放在实现类的同一文件中（类定义之前）。变量必须 `private`，数据访问通过接口的只读属性，数据修改通过接口的有业务含义的方法。

3. **所有改动不能影响已有函数的对外契约。** 修改现有代码时，函数名、参数类型和数量、返回值类型、对外部状态的影响必须保持不变。如需修改对外契约，必须先列出所有调用方并逐个确认。

## Quick Reference

- **Engine:** Unity 2022.3 LTS (URP)
- **Language:** C#, no asmdef files — all game code compiles into `Assembly-CSharp`
- **Entry Points:** `Assets/Scenes/Main.unity` (lobby) → `Assets/Scenes/Fight.unity` (battle)
- **No automated test suite** for gameplay code

> For detailed architecture, directory map, and system descriptions, see [CLAUDE.md](./CLAUDE.md).
> For out-of-battle (局外) panel switching, data persistence, module placement, and scene transition patterns, see [局外系统开发规范](.github/instructions/局外系统开发规范.instructions.md).

## Critical Conventions

### Naming
- **Classes/Methods/Variables:** English, PascalCase
- **Private fields:** `m_` prefix (e.g., `m_initialized`)
- **Static private fields:** `s_` prefix (e.g., `s_isExecutingSkill`)
- **Comments & UI strings:** Chinese
- **Directory names:** Chinese (局内, 局外, 回合模块, 角色, 技能, 状态, etc.)
- **Enum names:** Chinese pinyin/English mix (e.g., `CharacterSkillType.PursuitPunish`, `StateType.Daze`)

### SerializeField & Scene References
- **All variables must be `private`.** Use `[SerializeField] private` for Inspector-assigned references.
- **Never use `FindObjectOfType` or `GameObject.Find`.** Always use `[SerializeField]` for scene references.
- **`Instantiate` and `Destroy`** should only appear in dedicated factory/manager scripts.

### Unity Lifecycle Rules
- **Awake:** Only component caching (`GetComponent`), singleton setup (`Instance = this`), and calling init functions. No business logic.
- **Start:** Only external data access (e.g., `Datas.Instance`), event subscriptions, and first-render triggers (`RefreshViews()`).
- **Update:** Only input forwarding and state polling. No business logic.
- **OnDestroy:** Only unregister events and destroy self-created objects.
- Lifecycle methods >5 lines must be split into named private step functions.
- No `Time.timeScale`, `Resources.Load`, or `DontDestroyOnLoad` directly in lifecycle methods — encapsulate in dedicated methods.

### Singleton Pattern
All managers use this exact pattern — follow it when creating new managers:

```csharp
public class XxxManager : MonoBehaviour
{
    public static XxxManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
```

**Exception:** `GameManager` (`Assets/Scripts/局内/GameManager.cs`) is missing the duplicate check — do NOT copy its pattern.

### ScriptableObject Data Pipeline
All game data (skills, states, characters, enemies, grid modules) are ScriptableObjects created via CSV import:
```
Assets/Data/*.csv → CSVReader → Importer (MenuItem) → ScriptableObject → Resources/
```
- SOs are loaded at startup by `DictionaryManager` into static dictionaries
- **Always `Instantiate()`** when retrieving from dictionary to avoid mutating source assets
- Editor importers are in `Assets/Editor/` (e.g., `CharacterDataImporter.cs`, `StateImporter.cs`)
- Importers use `[InitializeOnLoadMethod]` + `EditorApplication.projectChanged` for **auto-incremental import** on CSV changes
- `AppConfig` SO at `Resources/AppConfig.asset` holds CSV source paths and asset output paths. Access via `Config.Instance`.

### Event System
Use C# native `event Action` / `event Action<T>` — no custom event bus:
```csharp
public event Action CharacterRosterChanged;
// Raise: CharacterRosterChanged?.Invoke();
```

Both instance events and static events are used. Static events are common for cross-system communication (e.g., `ChangePanelButton.PanelSwitched`, `Character.OnCharacterEnterTurn`).

### Panel Switching (局外)
Panels use a **decentralized event pattern** — no central PanelManager:
- `ChangePanelButton` raises `PanelSwitched` static event when opening a panel.
- `ExitButton` raises `PanelClosed` static event when closing.
- Other systems (e.g., tutorials) subscribe to these static events to track the active panel.
- All panel transitions go through `ScreenTransition.Instance` coroutine.

### Coroutines Everywhere
Turn sequences, animations, and selection flows use `IEnumerator` coroutines heavily. `yield return new WaitUntil(...)` is common for waiting on player input.

## Key Files to Know

| File | Purpose |
|------|---------|
| `Assets/Scripts/局内/回合模块/TurnManager.cs` | Core turn loop — time-axis (Action Value) system with `LinkedList<Combatant>` |
| `Assets/Scripts/局外/总数据/Datas.cs` | Persistent DontDestroyOnLoad singleton — character roster, gold, progression |
| `Assets/Scripts/局内/关卡/BattleLaunchContext.cs` | Static bridge for Main→Fight scene data transfer |
| `Assets/Scripts/局内/技能/SkillExecuteManager.cs` | Static skill execution coordinator |
| `Assets/Scripts/局内/状态/State.cs` | State ScriptableObject with Turn/ActionValue/Special duration types |
| `Assets/Scripts/局内/角色/UnitCombatant.cs` | Mid-layer combatant — HP, DEF, crit, shield, damage/heal/state logic |
| `Assets/Scripts/局外/背包/IBackpackInterfaces.cs` | Backpack module system interfaces (IBackpackInventoryView, IGridModule, IModulePlacementController) |
| `Assets/Scripts/局外/总数据/TimeScaleController.cs` | Centralized time scale management (use instead of raw `Time.timeScale` assignment) |
| `Assets/Editor/CSVReader.cs` | CSV parsing utility for all import pipelines |

## Combatant Hierarchy
```
Combatant (action value, speed, position, virtual PerformTurn/GetSpeed)
  └─ UnitCombatant (HP, ATK, DEF, crit, shield, MMF feedbacks, TakeDamage/Heal/AddState)
       ├─ Character (skills, chaos system, switch cooldown, enter/exit animations)
       └─ Enemy (EnemyRosterData init, enemy-specific AI)

Special Turn-Order Insertions (also extend Combatant):
  ├─ Changer — temporary node for character swap flow
  ├─ ExtraCharacter — extra turn insertion
  └─ AdditionalCharacter — additional turn insertion
```

TurnManager manages insertion/removal of ALL combatant types via `InsertCombatant()` / `RemoveCombatant()`, then re-sorts the `LinkedList<Combatant>` and notifies `TurnImageManager`.

## Known Pitfalls

1. **`Time.timeScale` is overwritten every frame** in `Datas.Update()` — don't rely on it for animation timing. Use `TimeScaleController` for time scale management instead.
2. **`GameManager.Awake()` has no duplicate Instance check** — be careful when working with it.
3. **`BattleLaunchContext` is a static class** — data persists until `ConsumePendingLevelData()` is called; if Fight scene fails to load, stale data may remain.
4. **No asmdef files** — all scripts share one assembly; avoid naming collisions.
5. **CommandButton refresh chain is complex** — after swap cooldown expires, the refresh path is `Character → CharacterManager → CommandButton`. Don't rely solely on `TurnManager.OnTurnOrderChanged` for button state updates.
6. **`CombatantDeathMonitor` and `Commander` are lazy-created DontDestroyOnLoad singletons** — they persist across scenes; be aware of potential stale state.
7. **StarterBranchRuntimeController no longer exists** — character unlocking is now primarily handled via `LevelCharacterUnlockConfig` based on level completion.

## Third-Party Dependencies
- **DOTween** — tweening
- **More Mountains Feedbacks (MMF)** — visual feedback on `UnitCombatant`
- **Cinemachine** — camera management
- **TextMesh Pro** — UI text
- **Cartoon FX Remaster (CFXR)** — particle effects
