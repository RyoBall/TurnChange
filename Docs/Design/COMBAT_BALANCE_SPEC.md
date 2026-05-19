# Combat Balance Specification (Current Version)

> Date: 2026-05-19
> Version: v3 (consolidates C1/C2/C3 skill reworks + C4 introduction)
> Purpose: This document consolidates and freezes the **current combat formulas, growth curves, timing rules, standard enemy model, C1 / C2 / C3 / C4 stat models, skill specifications, DOT rules, detonate/field/shield/true-damage mechanics, and validation methodology**.
> Audience: Designers, technical designers, gameplay engineers, AI tooling, spreadsheet pipelines.
> Style goal: explicit, machine-readable, low ambiguity.

---

# 1. Core Design Principles

## 1.1 Action system is fixed
The battle system uses an **action value timeline**.

- Every unit has an `ActionValue`.
- Global battle time starts at `0` and increases monotonically.
- A unit acts whenever the current timeline reaches its next scheduled action point.
- After that unit acts, its next action point is increased by its own `ActionValue`.

Example:
- `ActionValue = 40` → acts at time `40, 80, 120, 160, ...`
- `ActionValue = 45` → acts at time `45, 90, 135, 180, ...`
- `ActionValue = 50` → acts at time `50, 100, 150, 200, ...`
- `ActionValue = 65` → acts at time `65, 130, 195, 260, ...`

This system is **not allowed to change**.

---

## 1.2 DOT resolution timing is fixed
- DOT damage is resolved on the **target's turn**, immediately **before** the target acts.
- If the DOT damage kills the target, the target dies and **does not act**.
- Applying a DOT does **not** trigger an immediate DOT tick on the same cast turn.
- DOT damage frequency is governed by the **target's action schedule**, not the caster's.

This rule is final for the current version.

---

## 1.3 The `DOT Damage` stat has been removed
DOT identity is now expressed by:
- lower or specialized `ATK` growth,
- access to stronger `DOT SkillCoef` ranges,
- DOT-specific skill structures,
- field-effect multipliers,
- different action/time pressure patterns.

There is **no standalone DOT Damage panel stat**. **Bonus_dot is also removed for current characters (set to 0).**

---

## 1.4 Buff behavior in DOT
For DOT:
- `ATK`, `SkillCoef` are snapshotted when the DOT is applied.
- `Buff` and **field-effect multipliers** are **not snapshotted** (dynamic).
- External effects that modify ongoing DOT damage should operate through `Buff` or field multipliers.

---

## 1.5 RNG convention
All damage formulas end with `Rand(0.85, 1.15)`. For deterministic validation, use `Rand = 1.0`.

## 1.6 TTK measurement convention
TTK is measured in **enemy action count**:

\[
TTK_{enemy\_turns} = \left\lfloor \frac{t^*}{AV_{enemy}} \right\rfloor
\]

---

# 2. Global Growth Function

## 2.1 Level range
\[
Lv \in [1,10]
\]

## 2.2 Shared growth curve
\[
f(Lv) = \sqrt{5 \cdot Lv - 4}
\]

## 2.3 Reference values

| Level | f(Lv) |
|------:|------:|
| 1 | 1.00 |
| 2 | 2.45 |
| 3 | 3.32 |
| 4 | 4.00 |
| 5 | 4.58 |
| 6 | 5.10 |
| 7 | 5.57 |
| 8 | 6.00 |
| 9 | 6.40 |
| 10 | 6.78 |

---

# 3. Defense Model

## 3.1 Defense reduction ratio
\[
Mit(DEF;K) = \frac{K}{DEF + K}, \qquad DR(DEF;K) = \frac{DEF}{DEF + K}
\]

## 3.2 K constant
For the standard medium-armor target baseline:
\[
K = 100
\]

---

# 4. Unified Damage Formulas

## 4.1 Direct damage formula
\[
Damage =
\left(
(ATK \cdot SkillCoef + SkillBase)
\cdot E[Crit]
\cdot \frac{K}{DEF_{target}+K}
+
Bonus
\right)
\cdot Rand(0.85,1.15)
\]

\[
E[Crit] = 1 + CritRate \times (CritMult - 1)
\]

## 4.2 DOT tick formula
\[
DOT_{tick} =
\left(
ATK_{snap} \cdot SkillCoef_{dot,snap} \cdot \frac{K}{DEF_{target}+K}
+
Bonus_{snap}
\right)
\cdot Buff
\cdot FieldMult_{dot}
\cdot Rand(0.85,1.15)
\]

- `ATK_snap`, `SkillCoef_dot,snap`, `Bonus_snap`: snapshotted at apply
- `Buff`, `FieldMult_dot`: dynamic
- DOT does **not** crit and does **not** use `SkillBase`

## 4.3 Structural differences
**Direct damage:** ATK·SkillCoef + SkillBase + E[Crit] + post-defense Bonus
**DOT:** ATK_snap·SkillCoef_dot,snap + Bonus_snap + dynamic Buff + field multipliers; no SkillBase; no Crit

## 4.4 True damage variant (new)
For attacks tagged as **true damage**:
\[
Mit_{true} = 1.0 \quad (\text{ignores target DEF})
\]
All other terms (SkillCoef, SkillBase, E[Crit], Bonus, Buff) behave normally. True damage may still crit if the source skill allows it.

## 4.5 Chain attack multiplier (new)
For mechanics that retrigger an attack on critical hit (e.g. C1 Berserker Feast):
\[
ChainMult(p, r) = \frac{1}{1 - p \cdot r}
\]
where `p` = retrigger probability (typically crit rate), `r` = follow-up damage ratio.
**Hard cap:** `p · r ≤ 0.25` ⇒ `ChainMult ≤ 1.333`.

## 4.6 Field-effect multipliers (new)
\[
D_{effective} = D_{base} \cdot \prod_{i} FieldMult_i
\]
Field effects are global modifiers, multiplicative with each other, not snapshotted, and may apply to direct damage, DOT, or both depending on tag.

Currently defined:
| Effect | Tag | Multiplier |
|---|---|---|
| 重裁域场 (Verdict Field) | DOT × | **2.0** |
| 裁断 (Severance, DOT part) | DOT × | `1 + max(0, 0.45 − 0.15·n)` |
| 裁断 (Severance, Direct part) | Direct × | **0.70** (fixed) |

`n` = number of times C2 has acted since Severance activated.

## 4.7 Counter-strike damage (Pursuit)
Counter-strike attacks (e.g. 追惩 Pursuit) use direct damage formula and may crit, but are exempt from field effects that reduce direct damage:
\[
D_{追惩} = ATK \cdot SkillCoef_{追惩} \cdot E[Crit] \cdot Mit \cdot (1 + 0.15 \cdot \mathbb{1}_{\text{off-turn}})
\]

## 4.8 Multi-DOT stacking (C3)
Multiple distinct DOTs on the same target resolve independently within the same enemy turn, each using its own snapshot:
\[
D_{dot,total} = \sum_{j} D_{tick,j} \cdot \mathbb{1}[j \text{ active}]
\]

## 4.9 Shield formula (new, C4)
\[
Shield =
\left(
HP_{target,max} \cdot HPDebuffMult \cdot SkillCoef_{shield} + SkillBase_{shield} \cdot C_{fix}
\right)
\cdot Rand(0.85,1.15)
\]

| Variable | Default | Notes |
|---|---|---|
| `HP_{target,max}` | dynamic | Target's current max HP |
| `HPDebuffMult` | 1.0 | Max-HP debuff multiplier zone (< 1.0 if debuffed) |
| `SkillCoef_shield` | 0.40 | Default shield coefficient |
| `SkillBase_shield` | 20 | Flat scalar |
| `C_fix` | 1.0 | Fixed scaling reserve (future per-level scaling possible) |

**Shield behaves as extra HP.** Incoming damage depletes shield first; the shield consumes the **holder's** mitigation (i.e. shield absorbs post-mitigation damage).

---

# 5. DOT Timing Rules

## 5.1 Resolution timing
1. Target reaches its action point.
2. Before acting, all DOT effects on target resolve.
3. If HP ≤ 0 → target dies → no action.
4. Otherwise target acts.

## 5.2 No immediate tick on cast
Applying a DOT does **not** trigger an immediate first tick.

## 5.3 Practical consequence
- Fast targets take DOT more frequently.
- Slow targets take DOT less frequently.

## 5.4 DOT instant-refresh resolution (new)
When a skill refreshes an existing DOT (e.g. C2 序焰 refresh, C3 三DOT refresh):
\[
D_{refresh} = 0.60 \times D_{dot,effective,current}
\]
- Counts as DOT-type damage (no crit, includes current field multipliers).
- Does not affect the target's DOT tick schedule (only refreshes duration).

## 5.5 Single-application refresh rule (C3)
C3 Skill 1 applies/refreshes **exactly one** DOT per cast:
- If target lacks one of {冰寒, 腐蚀, 风蚀}: apply missing one (random among missing).
- If target has all three: randomly select one → trigger §5.4 instant refresh + reset duration.

## 5.6 Stun (震慑)
- Target skips its next action.
- Target takes **+40%** damage from all sources until its next action.

---

# 6. Bonus Reference Table (legacy reference; current characters use Bonus = 0)

| Tier | Bonus per Tick | Typical Tick Count | Use Case |
|------|----------------|--------------------|----------|
| Trace | 0.8% – 2.0% of target HP | 4 – 5 | incidental DOT |
| Support | 2.5% – 4.0% | 3 – 4 | support / combo DOT |
| Tactical | 5.0% – 6.0% | 3 | secondary DOT damage |
| Main DOT | 7.0% – 8.0% | 3 | dedicated DOT archetype |
| Burst | 9.0% – 12.0% | 1 – 2 | short-duration burst DOT |

> **All current playable characters use `Bonus_dot = 0`**, after the v2/v3 rework. This table is preserved for future tuning reference only.

---

# 7. DOT Skill Coefficient Tiers

| Tier | Suggested `SkillCoef_dot` | Intended Role |
|------|----------------------------|---------------|
| Trace | 0.05 – 0.10 | minor DOT / sub-character |
| Support | 0.12 – 0.18 | support / synergy DOT |
| Tactical | 0.18 – 0.22 | hybrid / sub-DOT |
| Main DOT | 0.22 – 0.28 | dedicated DOT |
| Extreme | 0.28 – 0.32 | rare / dangerous |

Current usage:
- C2 序焰: **0.22** (Main DOT lower edge — relies on field effects)
- C3 冰寒/腐蚀/风蚀: **0.10** each (Trace upper edge — three-stack)

---

# 8. Standard Medium-Armor Enemy Model

## 8.1 HP curve
\[
HP_{enemy}(Lv) = 120 + 112 \cdot f(Lv)
\]

## 8.2 DEF curve
\[
DEF_{enemy}(Lv) = 100 \cdot \left(1 - \frac{1}{f(Lv)}\right)
\]

## 8.3 Action value
\[
ActionValue_{enemy} = 65
\]

## 8.4 Defense constant
\[
K = 100
\]

## 8.5 Full level table

| Lv | f(Lv) | Enemy HP | Enemy DEF | Mit |
|---:|------:|---------:|----------:|----:|
| 1 | 1.00 | 232 | 0 | 1.0000 |
| 2 | 2.45 | 395 | 59 | 0.6289 |
| 3 | 3.32 | 492 | 70 | 0.5882 |
| 4 | 4.00 | 568 | 75 | 0.5714 |
| 5 | 4.58 | 633 | 78 | 0.5618 |
| 6 | 5.10 | 691 | 80 | 0.5556 |
| 7 | 5.57 | 744 | 82 | 0.5495 |
| 8 | 6.00 | 792 | 83 | 0.5464 |
| 9 | 6.40 | 837 | 84 | 0.5435 |
| 10 | 6.78 | 880 | 86 | 0.5376 |

> Lv.1 DEF = 0 is intentional (tutorial design).

Working reference at Lv.10:
\[
HP=880,\quad DEF=86,\quad Mit \approx 0.5376
\]

---

# 9. Character 1 (C1) — Berserker Burst Main DPS

## 9.1 Archetype
Single-target burst DPS with **Burn-Blood + Crit-Chain** identity. Damage scales with missing HP (reserved). Critical hits trigger follow-up attacks. Self-damage when failing to kill.

## 9.2 Growth curves
\[
HP_{C1}(Lv) = 100 + 77 \cdot f(Lv)
\]
\[
ATK_{C1}(Lv) = 35 + 150 \cdot f(Lv)
\]
\[
DEF_{C1}(Lv) = 40 + 20 \cdot f(Lv)
\]

## 9.3 Action value
\[
ActionValue_{C1} = 40
\]

## 9.4 Crit profile

| State | Crit Rate | Crit Mult | E[Crit] |
|---|---:|---:|---:|
| Base | 30% | 2.5 | 1.45 |
| Berserker Feast active | **50%** | 2.5 | **1.75** |

DOT damage does not crit (N/A for C1).

## 9.5 Skill specifications

| Skill | Field | Value | Notes |
|---|---|---|---|
| Skill 1 (single-target direct) | SkillCoef_cast | **0.10** | Description: "high direct damage" |
| Skill 1 | SkillBase_cast | **20** | — |
| Skill 1 (vs debuffed target) | SkillCoef_cast | **0.15** | +0.05 additive on coef |
| Switch-out (true damage, no crit) | ATK SkillCoef | **0.40** | — |
| Switch-out | Target Max HP coef | **0.06** (普通) / **0.03** (Boss) | — |
| Switch-out | SkillBase | **25** | — |
| Switch-out | Execute threshold | **30%** (普通) / **15%** (Boss) | of target Max HP |
| Berserker Feast (狂暴盛宴) | Crit Rate Δ | **+20%** | Duration: 200 AV |
| Berserker Feast | Chain ratio r | **0.40** | follow-up = 40% of triggering hit |
| Berserker Feast | ChainMult | **1/(1 − 0.50·0.40) = 1.25** | within §4.5 cap |
| Burn-Blood (燃血) | Self-damage on no-kill | **15% × Max HP** | — |
| Fatal Penetration (致命穿甲) | Damage bonus | **×1.25** | Treat attacks as true damage |
| Fatal Penetration | On-kill team action advance | **+50%** | reduced from +100% |
| Fatal Penetration | CD | **3 turns** | 1 stack, consumed at turn end |
| Skill 2 (Fatal Penetration) | Cost | **0 action** | — |

## 9.6 Switch-in / Switch-out
- **Switch-in:** Grants Berserker Feast (200 AV) + Burn-Blood. Both effects lost on switch-out.
- **Switch-out:** Deals true damage to a single target (see above). Cannot crit. Below execute threshold → instant kill.

## 9.7 Full level table (effective per-event damage during Berserker Feast, Rand=1)

\[
D_{S1,berserk} = (ATK \cdot 0.10 + 20) \cdot 1.75 \cdot Mit \cdot ChainMult
\]

| Lv | f(Lv) | HP | ATK | DEF | D_{S1} (Berserk) |
|---:|------:|---:|----:|----:|----:|
| 1 | 1.00 | 177 | 185 | 60 | 84.4 |
| 2 | 2.45 | 289 | 403 | 89 | 82.7 |
| 3 | 3.32 | 356 | 533 | 106 | 96.4 |
| 4 | 4.00 | 408 | 635 | 120 | 107.4 |
| 5 | 4.58 | 453 | 722 | 132 | 116.7 |
| 6 | 5.10 | 493 | 800 | 142 | 125.0 |
| 7 | 5.57 | 529 | 871 | 151 | 132.6 |
| 8 | 6.00 | 562 | 935 | 160 | 139.4 |
| 9 | 6.40 | 593 | 995 | 168 | 145.8 |
| 10 | 6.78 | 623 | 1052 | 176 | **152.0** |

Frozen Lv.10:
\[
HP=623,\quad ATK=1052,\quad DEF=176
\]
\[
EHP_{C1} \approx 1720
\]

## 9.8 Ideal-rotation TTK vs standard medium enemy (Lv.10)

Reference rotation (Berserker Feast + Fatal Penetration + Switch-out execute):

| t | Event | Δ | Cum | Enemy HP left |
|---|---|---|---|---|
| 40 | S1 #1 | 152 | 152 | 728 |
| 80 | S1 #2 | 152 | 304 | 576 |
| 120 | S1 #3 | 152 | 456 | 424 |
| 160 | S1 #4 + Fatal Penetration (true ×1.25) | 343 | 799 | 81 |
| 200 | Switch-out execute (≤ 30% threshold) | — | — | **0** ✅ |

\[
t^* = 200 \Rightarrow TTK_{C1} = \mathbf{3 \text{ enemy turns}} \quad \text{(ideal)}
\]

Pure Skill 1 spam (no Fatal Penetration, no switch-out):
\[
t^* \approx 240\text{–}280 \Rightarrow TTK_{C1} = 3\text{–}4 \text{ enemy turns}
\]

---

# 10. Character 2 (C2) — DOT Main DPS / Field Controller

## 10.1 Archetype
Main DOT archetype. Identity: **field-effect amplification + DOT detonation**. Without field activation, performs as a baseline DOT character; with 重裁域场 active, matches C1 ideal TTK.

## 10.2 Growth curves
\[
HP_{C2}(Lv) = 110 + 100 \cdot f(Lv)
\]
\[
ATK_{C2}(Lv) = 30 + 107 \cdot f(Lv)
\]
\[
DEF_{C2}(Lv) = 55 + 24 \cdot f(Lv)
\]

## 10.3 Action value
\[
ActionValue_{C2} = 50
\]

## 10.4 Crit profile
| Stat | Value |
|---|---:|
| Crit Rate | 10% |
| Crit Mult | 1.5 |
| E[Crit] | **1.05** |

DOT does not crit.

## 10.5 Full level table

| Lv | f(Lv) | HP | ATK | DEF |
|---:|------:|---:|----:|----:|
| 1 | 1.00 | 210 | 137 | 79 |
| 2 | 2.45 | 355 | 292 | 114 |
| 3 | 3.32 | 442 | 385 | 135 |
| 4 | 4.00 | 510 | 458 | 151 |
| 5 | 4.58 | 568 | 520 | 165 |
| 6 | 5.10 | 620 | 575 | 177 |
| 7 | 5.57 | 667 | 625 | 189 |
| 8 | 6.00 | 710 | 672 | 199 |
| 9 | 6.40 | 750 | 715 | 209 |
| 10 | 6.78 | 788 | 756 | 218 |

Frozen Lv.10:
\[
HP=788,\quad ATK=756,\quad DEF=218
\]
\[
EHP_{C2} \approx 2506
\]

---

# 11. Character 2 Skill Specifications

## 11.1 Skill 1 — AoE direct + apply 序焰 DOT

| Field | Value | Notes |
|---|---|---|
| SkillCoef_cast | **0.12** | AoE direct, "medium direct damage" |
| SkillBase_cast | **15** | — |
| SkillCoef_dot (序焰) | **0.22** | "powerful continuous damage" |
| Bonus_dot | **0** | removed |
| 序焰 Duration | **2 enemy turns** | refresh resets to 2 |
| Refresh instant settle | **60% × D_tick_effective** | §5.4 |

## 11.2 Skill 2 — Pursuit (追惩)

- **Cost:** 0 action.
- **CD:** 3 turns. Not stackable.
- Self-buff: off-turn damage **+15%** (incl. DOT). Duration 2 turns.
- Off-turn trigger: when an enemy gains a debuff → C2 deals one extra attack:
\[
D_{追惩} = ATK \cdot 0.20 \cdot E[Crit] \cdot Mit \cdot 1.15
\]

## 11.3 Switch-in / Switch-out
- **Switch-in:** Activates **重裁域场** (DOT ×2.0) + **裁断** (Direct ×0.70, DOT ×(1 + max(0, 0.45 − 0.15n))) for 200 AV. Effects lost on switch-out.
- **Switch-out:** All active DOTs on all enemies immediately resolve **120%** of their current effective tick damage as a one-shot DOT-type settlement.

## 11.4 重裁域场 (Verdict Field)
Environmental: all DOT damage on the field ×2.0. Lasts as long as C2 is active.

## 11.5 裁断 (Severance) — Field Effect
Environmental:
- Direct damage taken by enemies × **0.70** (fixed).
- DOT damage taken by enemies × `1 + max(0, 0.45 − 0.15·n)` where `n` = times C2 has acted since activation. Decays from +45% → +30% → +15% → 0% over 3 C2 actions.

## 11.6 序焰 (Pyre)
DOT. Each time the target acts, takes `ATK_snap · 0.22 · Mit · field × Buff` damage.

## 11.7 Solo TTK vs standard medium enemy (Lv.10)

\[
D_{tick,base} = 756 \times 0.22 \times 0.5376 = 89.3
\]
\[
D_{tick,Verdict} = 89.3 \times 2.0 = 178.6
\]
\[
D_{cast} = (756 \times 0.12 + 15) \times 1.05 \times 0.5376 = 59.7
\]
\[
D_{refresh,Verdict} = 0.6 \times 178.6 = 107.2
\]

| Scenario | t* | TTK (enemy turns) |
|---|---:|---:|
| Verdict Field active | 195 | **3** |
| No field (baseline) | 300 | **4** |

---

# 12. Effective Durability Metric
\[
EHP = HP \cdot \left(1 + \frac{DEF}{K}\right), \quad K=100
\]

---

# 13. Character 3 (C3) — Debuff Applier / Sub-DOT

## 13.1 Archetype
Sub-DOT specialist. Applies multiple distinct DOTs and stacking control debuffs. Feeds C2 detonation/field amplification combos. Solo damage is intentionally lower than C2.

## 13.2 Growth curves
\[
HP_{C3}(Lv) = 130 + 110 \cdot f(Lv)
\]
\[
ATK_{C3}(Lv) = 20 + 80 \cdot f(Lv)
\]
\[
DEF_{C3}(Lv) = 60 + 25 \cdot f(Lv)
\]

## 13.3 Action value
\[
ActionValue_{C3} = 45
\]
(C3 acts faster than C2 to ensure DOT pre-stacking before C2 detonation.)

## 13.4 Crit profile (locked)
| Stat | Value |
|---|---:|
| Crit Rate | **20%** (locked) |
| Crit Mult | **1.25** (locked) |
| E[Crit] | **1.05** |

DOT does not crit.

## 13.5 Full level table

| Lv | f(Lv) | HP | ATK | DEF |
|---:|------:|---:|----:|----:|
| 1 | 1.00 | 240 | 100 | 85 |
| 2 | 2.45 | 400 | 216 | 121 |
| 3 | 3.32 | 495 | 286 | 143 |
| 4 | 4.00 | 570 | 340 | 160 |
| 5 | 4.58 | 634 | 386 | 175 |
| 6 | 5.10 | 691 | 428 | 188 |
| 7 | 5.57 | 742 | 466 | 199 |
| 8 | 6.00 | 790 | 500 | 210 |
| 9 | 6.40 | 834 | 531 | 221 |
| 10 | 6.78 | 876 | 563 | 230 |

Frozen Lv.10:
\[
HP=876,\quad ATK=563,\quad DEF=230
\]
\[
EHP_{C3} \approx 2891
\]

---

# 14. Character 3 Skill Specifications

## 14.1 Skill 1 — AoE direct + apply random DOT

| Field | Value | Notes |
|---|---|---|
| SkillCoef_cast | **0.20** | "medium direct damage" |
| SkillBase_cast | **10** | — |
| SkillCoef_dot (冰寒/腐蚀/风蚀) | **0.10** each | "continuous damage" |
| Bonus_dot | **0** | removed |
| DOT duration | **3 enemy turns** | refresh resets |
| Refresh selection | random among held | §5.5 |
| Refresh instant settle | **60% × D_tick_current** | §5.4 |

## 14.2 Skill 2 — Pursuit (same as C2)

Identical to §11.2:
- 0 action cost, CD 3, off-turn +15% damage.
- \( D_{追惩} = ATK_{C3} \cdot 0.20 \cdot 1.05 \cdot Mit \cdot 1.15 \)

## 14.3 Switch-in / Switch-out
- **Switch-in:** All enemies gain **持续煎熬** for 300 AV.
- **Switch-out:** Extend duration of all turn-based debuffs on all enemies by +1 turn.

## 14.4 持续煎熬 (Prolonged Torment)
Stackable up to **N = 5**.
- Outgoing damage reduction: **5% × N**, capped at 25%.
- On the target's action: roll **10% × N** to apply 震慑, capped at 50%. Independent rolls per unit.

## 14.5 震慑 (Stun)
- Target skips its next action.
- Target takes **+40%** damage from all sources until its next action.

## 14.6 冰寒 / 腐蚀 / 风蚀 (Frostbite / Corrosion / Wind Erosion)
Three distinct DOT slots. Mechanically identical at present (narrative/visual differentiation only):
- On each action: target takes `ATK_snap · 0.10 · Mit · field` damage.
- Duration 3 enemy turns.

## 14.7 追惩 (Pursuit) — see §14.2

## 14.8 Solo TTK vs standard medium enemy (Lv.10)

\[
D_{cast} = (563 \cdot 0.20 + 10) \cdot 1.05 \cdot 0.5376 = 69.2
\]
\[
D_{tick,single} = 563 \cdot 0.10 \cdot 0.5376 = 30.3
\]
\[
D_{refresh} = 0.60 \times 30.3 = 18.2
\]
\[
D_{3DOT,total/turn} = 90.9
\]

| Scenario | t* | TTK (enemy turns) |
|---|---:|---:|
| C3 solo (3-DOT max stack, no field) | 325 | **5** |

---

# 15. Character 4 (C4) — Shield / Speed Support

## 15.1 Archetype
**Pure support.** No offensive skills currently. Provides **60% damage reduction** to self (and potentially allies, mechanic TBD) and grants **shields scaled on ally Max HP**, plus action-advance utility. Slightly higher DEF, modest HP. ATK is a reserved field, currently unused.

## 15.2 Growth curves
\[
HP_{C4}(Lv) = 120 + 95 \cdot f(Lv)
\]
\[
ATK_{C4}(Lv) = 15 + 40 \cdot f(Lv) \quad \text{(reserved field, no skill reference)}
\]
\[
DEF_{C4}(Lv) = 70 + 30 \cdot f(Lv)
\]

## 15.3 Action value
\[
ActionValue_{C4} = 45
\]
(Equal to C3 — when tied, manual cast order determines priority.)

## 15.4 Crit profile
Not applicable (no offensive skills). Default values reserved:
| Stat | Value |
|---|---:|
| Crit Rate | 5% |
| Crit Mult | 1.5 |
| E[Crit] | 1.025 |

## 15.5 Full level table

| Lv | f(Lv) | HP | ATK | DEF | EHP (normal) | EHP (60% DR) |
|---:|------:|---:|----:|----:|---:|---:|
| 1 | 1.00 | 215 | 55 | 100 | 430 | 1075 |
| 2 | 2.45 | 353 | 113 | 143 | 858 | 2145 |
| 3 | 3.32 | 435 | 148 | 170 | 1175 | 2938 |
| 4 | 4.00 | 500 | 175 | 190 | 1450 | 3625 |
| 5 | 4.58 | 555 | 198 | 207 | 1704 | 4260 |
| 6 | 5.10 | 605 | 219 | 223 | 1954 | 4885 |
| 7 | 5.57 | 649 | 238 | 237 | 2187 | 5468 |
| 8 | 6.00 | 690 | 255 | 250 | 2415 | 6038 |
| 9 | 6.40 | 728 | 271 | 262 | 2635 | 6588 |
| 10 | 6.78 | 764 | 286 | 273 | 2850 | 7125 |

Frozen Lv.10:
\[
HP=764,\quad ATK=286 \text{ (reserved)},\quad DEF=273
\]
\[
EHP_{C4,normal} \approx 2850, \quad EHP_{C4,DR60\%} \approx 7125
\]

## 15.6 Shield skill

\[
Shield = (HP_{target,max} \cdot HPDebuffMult \cdot 0.40 + 20 \cdot 1.0) \cdot Rand(0.85,1.15)
\]

Lv.10 reference (Rand=1, HPDebuffMult=1):

| Recipient | HP_max | Shield |
|---|---:|---:|
| C1 | 623 | 269 |
| C2 | 788 | 335 |
| C3 | 876 | 370 |
| C4 (self) | 764 | 326 |

## 15.7 60% Damage Reduction skill
Applied to self (mechanic for allies TBD). Multiplicative reduction:
\[
D_{taken} = D_{incoming} \times (1 - 0.60) = D_{incoming} \times 0.40
\]

## 15.8 Shield tuning sensitivities

| Parameter | ΔShield |
|---|---|
| `SkillBase_shield` ±1 | ±1.0 × C_fix |
| `SkillBase_shield` ±10 | ±10.0 |
| `C_fix` ±0.1 | ±SkillBase × 0.1 |
| `SkillCoef_shield` ±0.01 | ±HP_target × 0.01 |

> **Use `SkillBase_shield` for fine-tuning, `SkillCoef_shield` for bulk adjustment.**

---

# 16. Offensive Validation — Lv.10 vs Standard Medium Enemy

## 16.1 Inputs
\[
HP_E=880,\quad DEF_E=86,\quad AV_E=65,\quad K=100,\quad Mit \approx 0.5376
\]

## 16.2 Per-event damage (Lv.10, Rand=1, Buff=1, no field unless noted)

| Source | D_cast | D_tick | D_other |
|---|---:|---:|---:|
| C1 Skill 1 (Berserker active, w/ chain) | **152** | — | Switch-out execute ≈ 478 |
| C1 Skill 1 (Berserker + Fatal Penetration, true) | 343 | — | — |
| C2 Skill 1 cast | **59.7** | — | — |
| C2 序焰 (no field) | — | **89.3** | refresh 53.6 |
| C2 序焰 (Verdict Field) | — | **178.6** | refresh 107.2 |
| C2 Pursuit per proc | 50.9 | — | — |
| C3 Skill 1 cast | **69.2** | — | — |
| C3 single DOT tick | — | **30.3** | refresh 18.2 |
| C3 三DOT total / enemy turn | — | **90.9** | — |
| C3 Pursuit per proc | 39.1 | — | — |

## 16.3 Solo TTK summary

| Char | t* | TTK (enemy turns) |
|---|---:|---:|
| C1 (ideal: Berserk + Penetration + execute) | 200 | **3** |
| C1 (Skill 1 spam only) | 240–280 | **3–4** |
| C2 (Verdict Field active) | 195 | **3** |
| C2 (no field) | 300 | **4** |
| C3 (3-DOT max stack) | 325 | **5** |
| C4 | N/A (no offensive skills) | — |

## 16.4 Team synergy reference (Verdict Field + C3 3-DOT stack)
\[
D_{dot,team}/\text{enemy turn} = (89.3 + 90.9) \times 2.0 = 360.4
\]
Expected enemy turns to kill: ≈ 2.4 — strong combo payoff.

---

# 17. Full Cross-Character Comparison (Lv.10)

| Item | C1 | C2 | C3 | C4 |
|---|---|---|---|---|
| Role | Berserker burst DPS | DOT main / field | Debuff/sub-DOT | Shield/support |
| AV | 40 | 50 | 45 | **45** |
| HP | 623 | 788 | 876 | 764 |
| ATK | 1052 | 756 | 563 | 286 (reserved) |
| DEF | 176 | 218 | 230 | **273** |
| EHP | ≈1720 | ≈2506 | ≈2891 | ≈2850 / ≈7125 (DR) |
| Crit Rate | 30% / 50% (Berserk) | 10% | 20% (locked) | 5% (n/a) |
| Crit Mult | 2.5 | 1.5 | 1.25 (locked) | 1.5 (n/a) |
| E[Crit] | 1.45 / 1.75 | 1.05 | 1.05 | 1.025 |
| SkillCoef_cast | 0.10 | 0.12 | 0.20 | — |
| SkillBase_cast | 20 | 15 | 10 | — |
| SkillCoef_dot | — | 0.22 | 0.10 ×3 | — |
| Bonus_dot | — | 0 | 0 | — |
| D_cast | 152 (Berserk+chain) | 59.7 | 69.2 | — |
| D_tick | — | 89.3 (178.6 field) | 30.3 ×3 = 90.9 | — |
| Solo TTK | **3 (ideal)** | **3 (field) / 4 (none)** | **5** | — |

---

# 18. SkillCoef & SkillBase Adjustment Guidelines

## 18.1 Sensitivity per +1 SkillBase (Lv.10, direct damage)
\[
\frac{\partial D_{cast}}{\partial SkillBase} = E[Crit] \cdot Mit
\]

| Char | Lv.10 sensitivity |
|---|---:|
| C1 (Berserk) | 0.941 |
| C2 | 0.565 |
| C3 | 0.565 |

## 18.2 Sensitivity per +0.01 SkillCoef (Lv.10, direct damage)
\[
\frac{\partial D_{cast}}{\partial SkillCoef} = ATK \cdot E[Crit] \cdot Mit \cdot 0.01
\]

| Char | Lv.10 per +0.01 |
|---|---:|
| C1 (Berserk + chain) | ≈12.4 |
| C2 | ≈4.3 |
| C3 | ≈3.2 |
| C2 DOT (no field) | ≈4.1 |
| C2 DOT (Verdict Field) | ≈8.1 ⚠ |
| C3 single DOT | ≈3.0 |

> Use `SkillBase` for fine-tuning. `SkillCoef` under field multipliers is highly leveraged.

## 18.3 Safe SkillBase ranges (current)

| Skill | Current | Safe Range | Hard Limits |
|---|---|---|---|
| C1 Skill 1 | 20 | [10, 40] | > 40 risks 2-turn TTK |
| C2 Skill 1 | 15 | [10, 30] | — |
| C3 Skill 1 | 10 | [0, 25] | > 25 collapses to C2 TTK tier |
| C4 Shield SkillBase | 20 | [10, 40] | shield scaling reserve |

## 18.4 Adjustment priority
```
Fine-tuning priority (preferred → caution):
SkillBase ±1   → ±0.565 per event (direct)       ← PRIMARY KNOB
SkillBase ±10  → ±5.65 per event
SkillCoef ±0.01 → ±3.0–8.1 (scales w/ ATK & field) ← USE WITH CARE
SkillCoef ±0.05 → may shift TTK tier              ← DANGEROUS
Field multiplier toggle (×2.0) → single largest variable
```

---

# 19. Frozen Current Values Summary

## 19.1 Global
\[
f(Lv)=\sqrt{5Lv-4}, \quad K=100
\]

DOT rules:
- target-turn resolution
- no immediate cast tick
- no crit
- dynamic Buff + field multipliers
- no standalone DOT Damage stat
- `Bonus_dot = 0` for all current characters

New mechanics:
- §4.4 True damage (`Mit_true = 1.0`)
- §4.5 Chain multiplier (`p·r ≤ 0.25`)
- §4.6 Field multipliers (multiplicative)
- §4.7 Pursuit counter-strike
- §4.8 Multi-DOT stacking
- §4.9 Shield formula
- §5.4 DOT instant refresh (60% × tick)
- §5.5 C3 single-DOT-per-cast rule
- §5.6 Stun (skip + 40% damage taken)

TTK measured in **enemy action count**.

## 19.2 Character 1 (Berserker Burst)
\[
HP=100+77f,\ ATK=35+150f,\ DEF=40+20f,\ AV=40
\]
\[
CR_{base}=30\%,\ CM=2.5,\ E[Crit]_{base}=1.45,\ E[Crit]_{Berserk}=1.75
\]
\[
S1: SC=0.10,\ SB=20\ (+0.05\ SC\ vs\ debuffed)
\]
\[
SwitchOut: ATK\_SC=0.40,\ HP\_SC=0.06/0.03,\ SB=25,\ Execute=30\%/15\%
\]
\[
BerserkerFeast: CR\Delta=+20\%,\ ChainRatio=0.40,\ Duration=200AV
\]
\[
BurnBlood: Self\_damage=15\%MaxHP\ on\ no\_kill
\]
\[
FatalPenetration: \times1.25\ true,\ On\_kill\ team\ advance=+50\%,\ CD=3
\]
Lv.10: HP=623, ATK=1052, DEF=176, D_S1(Berserk)=152

## 19.3 Character 2 (DOT Main / Field)
\[
HP=110+100f,\ ATK=30+107f,\ DEF=55+24f,\ AV=50
\]
\[
CR=10\%,\ CM=1.5,\ E[Crit]=1.05
\]
\[
S1\_cast: SC=0.12,\ SB=15
\]
\[
\text{序焰}: SC_{dot}=0.22,\ Bonus=0,\ Dur=2,\ Refresh=60\%\times D_{tick}
\]
\[
\text{追惩}: SC=0.20,\ \text{off-turn} +15\%,\ CD=3,\ 0\ action
\]
\[
\text{重裁域场}: DOT\times2.0,\ Dur=200AV
\]
\[
\text{裁断}: Direct\times0.70\ (\text{fixed}),\ DOT\times(1+\max(0,0.45-0.15n))
\]
\[
\text{Switch-out}: \text{All DOTs trigger 120\% one-shot DOT settlement}
\]
Lv.10: HP=788, ATK=756, DEF=218
D_cast=59.7, D_tick=89.3 (178.6 field), D_refresh=53.6 (107.2 field)

## 19.4 Character 3 (Debuff Sub-DOT)
\[
HP=130+110f,\ ATK=20+80f,\ DEF=60+25f,\ AV=45
\]
\[
CR=20\%\ (\text{locked}),\ CM=1.25\ (\text{locked}),\ E[Crit]=1.05
\]
\[
S1\_cast: SC=0.20,\ SB=10
\]
\[
\text{冰寒/腐蚀/风蚀}: SC_{dot}=0.10\ each,\ Bonus=0,\ Dur=3,\ Refresh=60\%
\]
\[
\text{追惩}: SC=0.20,\ \text{off-turn} +15\%,\ CD=3
\]
\[
\text{持续煎熬}: -5\%N\ \text{dmg out (≤25\%)},\ Stun\ chance=10\%N\ (\leq50\%),\ N_{max}=5
\]
\[
\text{震慑}: Skip\ next\ action,\ +40\%\ dmg\ taken
\]
\[
\text{Switch-out}: +1\ turn\ to\ all\ enemy\ debuffs
\]
Lv.10: HP=876, ATK=563, DEF=230
D_cast=69.2, D_tick_single=30.3, D_3DOT/turn=90.9

## 19.5 Character 4 (Shield / Support)
\[
HP=120+95f,\ ATK=15+40f\ (\text{reserved}),\ DEF=70+30f,\ AV=45
\]
\[
Shield = (HP_{target,max}\cdot HPDebuffMult\cdot 0.40 + 20\cdot C_{fix})\cdot Rand
\]
\[
\text{60\% DR self skill}: D_{taken}\times 0.40
\]
Lv.10: HP=764, ATK=286 (reserved), DEF=273
Shield(C1)=269, Shield(C2)=335, Shield(C3)=370, Shield(self)=326
EHP_normal=2850, EHP_DR=7125

## 19.6 Standard medium enemy
\[
HP=120+112f,\ DEF=100(1-1/f),\ AV=65
\]
Lv.10: HP=880, DEF=86, Mit≈0.5376

---

# 20. Player-Facing Skill Descriptions (Obfuscated)

> **Rule:** Internal numbers (SkillCoef, SkillBase, multipliers) are NEVER exposed to players. Use qualitative tier words.

Tier word reference (internal-only):

| Word | SkillCoef band | Use |
|---|---|---|
| 微量 / Minor | < 0.10 | trace effects |
| 中等 / Medium | 0.10 – 0.25 | standard skills |
| 高额 / High | 0.25 – 0.60 | primary outputs |
| 极高 / 巨额 / Massive | > 0.60 | finishers, executes |

## 20.1 C1 — Berserker
- **Switch-in:** Self gains **Berserker Feast** for a period, plus **Burn-Blood**. Both lost on switch-out.
- **Switch-out:** Single target true damage based on its Max HP and the attacker's ATK (cannot crit). If target HP below threshold (lower for Bosses), **instantly executed**.
- **Skill 1:** High direct damage to a single target. Damage increases against debuffed targets.
- **Skill 2** (CD 3, 0 cost): Gain one stack of **Fatal Penetration**. While active, attacks become true damage and deal greatly increased damage. Killing under this state grants the whole team a significant action advance.
- **Berserker Feast:** Major crit-rate boost. On crit, immediately re-attack the same target with a reduced-power follow-up; chains until a non-crit or target death.
- **Burn-Blood:** At end of turn, if no kill, lose a portion of Max HP.

## 20.2 C2 — DOT Main
- **Switch-in:** Activate **Verdict Field** + **Severance** for a duration. Effects lost on switch-out.
- **Switch-out:** All active DOTs on the field immediately trigger a powerful one-shot settlement.
- **Skill 1:** Medium direct damage to all enemies; apply **Pyre** (粉焰序焰) for a duration. If already present, trigger one settlement and refresh.
- **Skill 2** (CD 3, 0 cost): Gain **Pursuit**. Off-turn damage slightly increased; off-turn extra attack triggers when enemies gain debuffs.
- **Verdict Field:** Field — DOT damage on enemies is doubled.
- **Severance:** Field — enemy direct damage reduced; enemy DOT damage greatly amplified, gradually fading.

## 20.3 C3 — Debuff / Sub-DOT
- **Switch-in:** All enemies gain **Prolonged Torment** for a long duration.
- **Switch-out:** Extend turn-based debuffs on all enemies by 1 turn.
- **Skill 1:** Medium direct damage to all enemies + apply one of **Frostbite / Corrosion / Wind Erosion** (prefer missing). If all already present, trigger one settlement and refresh one.
- **Skill 2** (CD 3, 0 cost): same as C2 Pursuit.
- **Prolonged Torment** (stackable): reduces enemy outgoing damage and may **Stun** them on their action.
- **Stun:** skip next action, take greatly increased damage until next action.

## 20.4 C4 — Shield Support
- **Shield skill:** Grant an ally a shield based on their Max HP plus a flat amount.
- **Self damage reduction:** Take 60% less damage (mechanic for allies TBD).
- *(Action-advance skill: TBD)*

---

# 21. Open Questions Not Frozen

1. Compensation for DOT vs very slow bosses.
2. Same-source DOT refresh: overwrite vs refresh, snapshot priority, stacking.
3. Full enemy taxonomy (light / tank / boss).
4. Future character archetypes and coef permissions.
5. Enemy minimum DEF floor at Lv.1 (DEF=0 accepted as tutorial).
6. C3 supporting buffs beyond DOT application.
7. Whether C2 switch-out **consumes** DOT ticks or merely **resolves** them.
8. C3 and C4 tie-breaker at AV=45 (proposal: cast order).
9. Shield duration (until depletion vs timed expiry).
10. `Rand` applicability to shield formula (player feel concern).
11. C4 `C_fix` per-level scaling (currently 1.0).
12. C3 Prolonged Torment stack source beyond switch-in (currently only 1 stack).
13. Stun's "+40% damage taken" interaction with DOT (proposal: yes, unified multiplier).
14. C3 三DOT distinguishing tags for future enemy resists (currently identical).
15. C3 Prolonged Torment re-application after duration (re-switch-in?).
16. Pursuit trigger on DOT **refresh** vs initial **application** (proposal: initial only, to prevent loop with C2 Pyre).
17. C1 chain attack: does each chain link re-roll Burn-Blood kill check, or counted as one action?
18. C1 LostHPMult interface (`1 + α · (1 − HP/HPmax)`) — reserved, not in use.
19. C1 Skill 2 (Fatal Penetration) 0-action timing precision: same-tick stacking with Skill 1?
20. C4 ATK reserved field — keep or drop?
21. C4 ally damage-reduction propagation (whether 60% DR applies to allies via shield).

---

# 22. Final Status

This is the authoritative summary for:
- formulas (direct, DOT, true, chain, field, shield, counter)
- growth curves
- timing rules
- DOT behavior + new instant-refresh rule
- field mechanics (Verdict Field, Severance)
- C1 berserker rework
- C2 DOT/field rework
- C3 debuff/multi-DOT rework
- C4 shield/support introduction
- standard validation target
- SkillCoef/SkillBase adjustment guidelines

Any future rebalance MUST cite the specific section being modified.

> **Major changes from v1:**
> - C1 fully reworked (berserker/chain/true-damage identity)
> - C2 fully reworked (DOT base coef ↓ 0.25→0.22, Bonus removed, field mechanics added, detonate replaced by switch-out settlement)
> - C3 fully reworked (DOT coef ↓ 0.25→0.10 ×3, Bonus removed, multi-DOT system, Prolonged Torment + Stun)
> - C4 introduced (shield support, no offensive output)
> - New formula sections: §4.4–4.9, §5.4–5.6
> - All `Bonus_dot` set to 0 for current playable lineup
> - Solo TTK targets recalibrated: C1=3 (ideal), C2=3/4, C3=5