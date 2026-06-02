# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity turn-based RPG with a lobby/battle two-scene structure (局外/局内). The game uses a time-axis (Action Value) turn order system, character swapping, multi-wave enemies, and a roguelike-style temporary battle modifier system.

## Development Environment

- **Engine:** Unity 2022.3 (URP)
- **IDE:** VS Code with C# extension (`.vscode/settings.json` configured)
- **Solution file:** `Github Store.slnx`
- **Language:** C# (no asmdef files for game code — all scripts compile into the default `Assembly-CSharp` assemblies)
- **No automated test suite** exists for gameplay code.

## How to Build & Run

1. Open the project in Unity Editor (2022.3 LTS).
2. Open `Assets/Scenes/Main.unity` (lobby) or `Assets/Scenes/Fight.unity` (battle) in the Editor.
3. Press Play in the Unity Editor to run.
4. Standard Unity build: **File → Build Settings → Build**.

## Scenes

| Scene | Purpose |
|---|---|
| `Assets/Scenes/Main.unity` | Lobby — character selection, level selection, shop, backpack |
| `Assets/Scenes/Fight.unity` | Battle — turn-based combat, spawned by `StartBattleButton` via `SceneManager.LoadScene` |

## Architecture: Bootstrap & Scene Flow

### Cold Start (Main scene)
1. `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` fires in `DictionaryManager.cs` — loads all State/Environment/Skill/EnemySkill `ScriptableObject` assets from `Resources/` into static dictionaries.
2. `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` — `GameAudioInputObserver` creates a DontDestroyOnLoad click-listener.
3. `Datas.Awake()` — central persistent singleton (DontDestroyOnLoad). Initializes character roster, clamps progression values, sets `Time.timeScale`.
4. `StarterBranchRuntimeController.Start()` — shows starter branch (流派) selection overlay if not yet chosen.

### Entering Battle (Main → Fight)
1. Player selects characters & level in lobby → clicks `StartBattleButton`.
2. `BattleLaunchContext.SetPendingLevelData()` — static bridge: serializes level/enemy/character data into a `PendingBattleLevelData` object.
3. `SceneManager.LoadScene("Fight")` loads the battle scene.

### Battle Bootstrap (Fight scene)
1. `LevelSetupManager.InitializeLevel()` (runs at `[DefaultExecutionOrder(-500)]`) orchestrates:
   - Consumes `BattleLaunchContext.ConsumePendingLevelData()`
   - Starts `TemporaryBattleModifierRuntimeManager` session
   - Resolves player characters from `Datas`
   - Spawns characters/enemies via `LevelCharacterSpawner`
   - Hands off to `CharacterManager`, `EnemyManager`, then `TurnManager.InitializeTurnOrder()`
2. `TurnManager.StartFight()` coroutine — waits for camera intro, initializes turn images, triggers opening enter skills, then enters `RunTurnLoop()` (infinite while loop).

### Returning to Lobby
- `BattleSettlementView` shows rewards, then `SceneManager.LoadScene(exitSceneName)` back to Main.

## Key Systems

### Turn System (`Assets/Scripts/局内/回合模块/`)

**TurnManager** — the core loop. Maintains a `LinkedList<Combatant>` sorted by `currentActionValue` (ascending; lower = acts sooner). Each tick:
1. Pops the head of the list (lowest action value).
2. Subtracts that combatant's action value from ALL combatants (time-axis advance).
3. Ticks state durations, switch cooldowns, command point recovery, environments.
4. Yields to `combatant.PerformTurn()`.
5. After turn: re-inserts the combatant with `BaseActionValue`, checks enemy death → wave spawn or settlement.

**Combatant hierarchy:**
- `Combatant` (base) — action value, speed, stand position, `PerformTurn()`
- `UnitCombatant` — HP, attack, defense, crit, shield, MMF feedbacks, death tracking
- `Character` — skills, chaos system, switch cooldown, enter/exit animations
- `Enemy` — enemy-specific behavior
- `Changer` / `ExtraCharacter` / `AdditionalCharacter` — special turn-order insertions for character swap / extra turns

**TurnManager handles insertion/removal** — external systems call `InsertCombatant()` / `RemoveCombatant()`, and TurnManager re-sorts and notifies `TurnImageManager` to re-render.

### State System (`Assets/Scripts/局内/状态/`)

`State` is a `ScriptableObject` with a `StateType` enum. States support three duration types: Turn-based, ActionValue-based, and Special. States are loaded from `Resources/` into `StateDictionaryManager` (static dictionary keyed by `StateType`). To get a state instance, call `StateDictionaryManager.GetState(stateType)` — this returns an `Instantiate()`d clone so runtime modifications don't pollute the source asset.

### Skill System (`Assets/Scripts/局内/技能/`)

- `SkillBase` — ScriptableObject base class with `Execute(UnitCombatant, List<Enemy>)` coroutine.
- `CharacterSkillBase` — uses `CharacterSkillType` enum to differentiate enter/exit/normal skills.
- `EnemySkillBase` — enemy skill variant.
- `SkillExecuteManager` — static skill execution coordinator. Tracks `s_isExecutingSkill` flag.
- `SkillManager` — handles enemy/ally target selection via click (coroutine-based: `SelectEnemiesCoroutine`, `SelectCharactersCoroutine`).

### Character Manager (`Assets/Scripts/局内/交换角色/`)

Three pools: `allCharacters`, `fieldCharacters` (active, max 2), `reserveCharacters`. Swap flow: select field character → select reserve character → `ReplaceCharacter()` coroutine plays exit/enter animations, triggers enter skill, updates TurnManager.

### Data Layer (`Assets/Scripts/局外/总数据/Datas.cs`)

`Datas` is the global persistent singleton (DontDestroyOnLoad). Stores:
- Unlocked character rosters
- Level/floor progression
- Gold, team level, experience
- Backpack/grid module state
- Active temporary battle modifiers

Events: `CharacterRosterChanged`, `ModuleStateChanged`, `BackpackWidthChanged`, `LevelCompleted`.

### Battle Modifiers (`Assets/Scripts/局外/总数据/TemporaryBattleModifierRuntime.cs`)

Roguelike-style run modifiers. Session lifecycle: `BeginBattleModifierSession()` → modifiers affect gameplay → `CompleteBattleModifierSession()`. Hooks into character swapping (`NotifyPlayerCharacterSwapped`), battle start, and reserve action value advancement.

### Out-of-Battle Systems (`Assets/Scripts/局外/`)

- `PreparationPanelView` — character selection (pick 2 for field), level data display
- `ShopPanelView` / `ShopModuleManager` — shop for grid modules
- `BackpackInventoryView` / `ModulePlacementController` — grid-based module placement
- `StarterBranchRuntimeController` — starter branch (流派) selection
- `StartBattleButton` — transitions to Fight scene, bridges data via `BattleLaunchContext`

## Editor Tools (Unity Menu Items)

All Tools menu items are in `Assets/Editor/`:

| Menu Path | File | Purpose |
|---|---|---|
| `Tools/ImportCharacterDatas` | `CharacterDataImporter.cs` | Import character stats from CSV |
| `Tools/ImportEnemyDatas` | `EnemyDataImporter.cs` | Import enemy stats from CSV |
| `Tools/Import Character Skills` | `CharacterSkillImporter.cs` | Import/create skill SOs from CSV |
| `Tools/Import States` | `StateImporter.cs` | Import/create state SOs from CSV |
| `Tools/Import Grid Modules` | `GridModuleImporter.cs` | Import grid module SOs from CSV |
| `Tools/TA/一键修复所有特效的自动销毁` | `CFXFixer.cs` | Batch-fix CFXR particle auto-destruct |
| `TurnChange/Field Domain/*` | `FieldDomainRenderFeatureInstaller.cs` | Install/validate URP render feature |

`AppConfig` ScriptableObject (at `Resources/AppConfig.asset`) holds CSV source paths and asset output paths. Access via `Config.Instance`.

## Key Third-Party Dependencies

- **DOTween** — tweening (UI animations, character movement, etc.)
- **More Mountains Feedbacks (MMF)** — visual feedback for hit/enter/exit/die/shield/heal events on `UnitCombatant`
- **Cinemachine** — camera management (`CinemachineCameraManager`)
- **TextMesh Pro** — UI text
- **Coffee UI Effect** — UI visual effects
- **Cartoon FX Remaster (CFXR)** — particle effects
- **Unity URP** — render pipeline

## Directory Map (game code only)

```
Assets/
├── Data/                    # CSV source data for import pipeline
├── Editor/                  # Editor-only scripts (importers, tools, drawers)
├── Resources/               # Runtime-loaded assets (ScriptableObjects, prefabs, fonts)
├── Scenes/                  # Main.unity, Fight.unity
├── Scripts/
│   ├── Audio/              # BGM, audio input
│   ├── Utils/              # ScreenTransition, BGMPlayer, etc.
│   ├── 局内/               # In-battle systems
│   │   ├── 关卡/           # Level setup, spawn points, battle context bridge
│   │   ├── 回合模块/       # TurnManager, TurnStateManager, TurnImageManager
│   │   ├── 角色/           # Combatant hierarchy (Character, Enemy, UnitCombatant, etc.)
│   │   ├── 技能/           # Skill SOs, SkillExecuteManager, SkillManager, CommandSkill
│   │   ├── 交换角色/       # CharacterManager (field/reserve swap)
│   │   ├── 状态/           # State SO, StateDictionaryManager
│   │   ├── 环境/           # Environment/battlefield effects
│   │   ├── 背景/           # Background management
│   │   ├── 指挥点技能/     # Commander/command point skills
│   │   ├── UI/             # Battle UI (damage text, character state, command buttons)
│   │   └── MFF/            # MMF feedbacks
│   └── 局外/               # Out-of-battle systems
│       ├── 总数据/          # Datas, starter branch, battle modifier runtime
│       ├── 切换界面/        # Level selection, preparation panel, start battle
│       ├── 商店/            # Shop for modules
│       ├── 背包/            # Grid backpack / module placement
│       └── 角色界面/        # Character panel, skill display
├── Settings/               # URP settings assets
├── VFX/                    # Particle effects (CFXR, FieldDomain)
└── Feel/                   # MMTools, MMFeedbacks, NiceVibrations
```

## Coding Conventions

- **Language:** The codebase uses Chinese for class/field comments, enum names (e.g., 角色/技能/状态 directories), and UI-facing strings. Code identifiers are in English.
- **Singleton pattern:** Most managers use `public static X Instance { get; private set; }` with duplicate-destroy logic in `Awake()` and null-on-destroy in `OnDestroy()`.
- **ScriptableObjects:** All game data (skills, states, characters, enemies, grid modules) are ScriptableObjects created via the CSV import pipeline.
- **Coroutines:** Heavy use of `IEnumerator` coroutines for turn sequences, animations, and selection flows. `yield return new WaitUntil(...)` is commonly used for waiting on player input.
- **Events:** Managers expose C# `event Action` / `event Action<T>` for decoupled communication (e.g., `CharacterRosterChanged`, `OnTurnOrderChanged`).
