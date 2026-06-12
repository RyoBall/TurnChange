# AGENTS.md

This file provides guidance to AI coding agents (GitHub Copilot, Claude, etc.) when working with code in this repository.

## Core Principle

**尽可能复用已有的代码。** 在实现新功能前，先搜索项目中是否已有类似的实现、工具方法、UI 组件或数据类。优先扩展现有代码而非重复造轮子。如果现有代码不完全满足需求，优先考虑重构扩展现有代码，而非新建文件。

## Quick Reference

- **Engine:** Unity 2022.3 LTS (URP)
- **Language:** C#, no asmdef files — all game code compiles into `Assembly-CSharp`
- **Entry Points:** `Assets/Scenes/Main.unity` (lobby) → `Assets/Scenes/Fight.unity` (battle)
- **No automated test suite** for gameplay code

> For detailed architecture, directory map, and system descriptions, see [CLAUDE.md](./CLAUDE.md).

## Critical Conventions

### Naming
- **Classes/Methods/Variables:** English, PascalCase
- **Private fields:** `m_` prefix (e.g., `m_initialized`)
- **Static private fields:** `s_` prefix (e.g., `s_isExecutingSkill`)
- **Comments & UI strings:** Chinese
- **Directory names:** Chinese (局内, 局外, 回合模块, 角色, 技能, 状态, etc.)
- **Enum names:** Chinese pinyin/English mix (e.g., `CharacterSkillType.PursuitPunish`, `StateType.Daze`)

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

### Event System
Use C# native `event Action` / `event Action<T>` — no custom event bus:
```csharp
public event Action CharacterRosterChanged;
// Raise: CharacterRosterChanged?.Invoke();
```

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
| `Assets/Editor/CSVReader.cs` | CSV parsing utility for all import pipelines |

## Combatant Hierarchy
```
Combatant (action value, speed, position)
  └─ UnitCombatant (HP, ATK, DEF, crit, shield, MMF feedbacks)
       ├─ Character (skills, chaos system, switch cooldown)
       └─ Enemy (enemy-specific behavior)
```

## Known Pitfalls

1. **`Time.timeScale` is overwritten every frame** in `Datas.Update()` — don't rely on it for animation timing.
2. **`GameManager.Awake()` has no duplicate Instance check** — be careful when working with it.
3. **`BattleLaunchContext` is a static class** — data persists until `ConsumePendingLevelData()` is called; if Fight scene fails to load, stale data may remain.
4. **No asmdef files** — all scripts share one assembly; avoid naming collisions.

## Third-Party Dependencies
- **DOTween** — tweening
- **More Mountains Feedbacks (MMF)** — visual feedback on `UnitCombatant`
- **Cinemachine** — camera management
- **TextMesh Pro** — UI text
- **Cartoon FX Remaster (CFXR)** — particle effects
