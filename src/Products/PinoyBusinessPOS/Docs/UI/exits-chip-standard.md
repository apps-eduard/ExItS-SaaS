# ExItS Chip Standard

**Status:** APPROVED / LOCKED  
**Scope:** `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.React`  
**Canonical foundation:** `ExItS.PinoyBusinessPOS.React/src/components/exits/chip-variants.ts`  
**Canonical components:**

| Family | Component |
|--------|-----------|
| STATUS CHIP | `components/exits/StatusChip.tsx` (`StatusPill` alias) |
| FILTER CHIP | `components/exits/FilterChip.tsx` |
| TAG CHIP | `components/exits/TagChip.tsx` |
| REMOVABLE CHIP | `components/exits/RemovableChip.tsx` |
| COUNT CHIP | `components/exits/CountChip.tsx` (`CountChip`) |
| COUNT BADGE | `components/exits/CountChip.tsx` (`CountBadge`) |

**Visual authority:** `/ui-standards` → **Chips**  
(`ExItS.PinoyBusinessPOS.React/src/features/ui-standards/UiStandardsChipsPanel.tsx`)

ExItS chip components and the shared chip variant foundation are the **canonical chip / badge / filter / tag presentation system** for Pinoy Business POS React.

Pages **MUST** reuse this system.

Pages **MUST NOT** create page-local chip / pill / badge visual systems when the shared ExItS chip foundation can express the requirement.

This standard evolves with the shared component contract. Do **not** pin the standard permanently to a commit SHA. Reference the component paths and `/ui-standards` → Chips instead.

Chip shapes are **independent** from the locked ExItS Button Standard shapes (`soft` / `pill` / `round` on `Button`). Do not conflate the two standards.

---

## Architecture

```
React chip components (Status / Filter / Tag / Removable / Count / CountBadge)
    ↓
Shared chip variants / CVA (`chipSurfaceVariants`, `filterChipVariants`)
    ↓
Tailwind utility classes
    ↓
ExItS semantic CSS tokens (`--exits-*`, density, theme)
```

| Layer | Role |
|-------|------|
| **Family components** | Correct HTML semantics (read-only vs button vs remove action) |
| **CVA / chip-variants** | Shared tone + shape classes |
| **Tailwind** | Applies styles |
| **ExItS tokens** | Visual identity (theme, density, semantic colors, motion) |

No third-party visual chip library controls appearance.

---

## Canonical families

### STATUS CHIP

Read-only business / application state.

Examples: Active, Pending, Approved, Paid, Completed, Paused, Low stock, Overdue, Declined, Failed, Void.

- Normally **not** clickable
- No pointer cursor, button semantics, pressed animation, or fake interactive hover
- Default shape: **PILL**

### FILTER CHIP

Interactive filter / selection control.

Examples: All, Active, Inactive, Low stock, Needs attention.

- Button semantics (`<button>`, `aria-pressed`)
- Default shape: **AUTO** (follows Preferences → Control Shape via `--exits-control-radius`)
- Explicit `pill` / `soft` / `square` override the global preference
- Selected state uses **PRIMARY** semantic treatment (not a full Primary action Button)

### TAG CHIP

Read-only descriptive metadata / attribute.

Examples: B2B, Weighted, Tracked, Preferred, Warehouse, PO, Direct, Beta.

- Not automatically a status
- Default shape: **SQUARE** (prevents all-capsule UI)
- No pointer / press unless explicitly interactive (normally read-only)

### REMOVABLE CHIP

Selected value / active filter with explicit trailing remove.

Examples: Branch: Main, Category: Fruits, Supplier: Mica.

- Structure: `Label [X]` (Lucide `X`)
- Accessible remove control with descriptive `aria-label` (e.g. `Remove Branch: Main filter`)
- Quiet X at rest; clearer on hover / focus
- Default shape: **PILL**

### COUNT CHIP

Compact **label + count** in one chip.

Canonical layout (locked): **inline integrated** — `[ Pending  3 ]`

- Count uses `tabular-nums` / equivalent
- Default shape: **SOFT**
- Read-only unless explicitly made interactive

### COUNT BADGE

Very small standalone numeric indicator — **separate** from CountChip.

Examples: `[3]`, `[12]`, `[99+]`

- Navigation / notification / tab counts
- Fully rounded compact badge (PILL)
- Supported tones: **NEUTRAL**, **PRIMARY**, **DANGER** (other tones only when meaningful)
- Do not automatically attach CountBadge to every CountChip

---

## Shapes (family-independent)

| Shape | Radius | Role |
|-------|--------|------|
| **PILL** | Fully rounded (`9999px` / `rounded-full`) | Status, filters, removable selections |
| **SOFT** | `--exits-radius-sm` (~6px / `0.375rem`) | Count chips; structured metadata; optional status |
| **SQUARE** | `--exits-radius-xs` (`0.25rem` / 4px) | Compact metadata tags (Beta, B2B, SKU, PO, …) |

**SQUARE is not sharp 0-radius.**

### Family defaults (locked)

| Family | Default shape |
|--------|----------------|
| STATUS CHIP | **PILL** |
| FILTER CHIP | **PILL** |
| TAG CHIP | **SQUARE** |
| REMOVABLE CHIP | **PILL** |
| COUNT CHIP | **SOFT** |
| COUNT BADGE | **PILL** / fully rounded |

Defaults are **not** restrictions. Explicit task instructions may request another supported shape when semantically appropriate:

- `STATUS CHIP WARNING SOFT`
- `TAG CHIP INFO PILL`
- `COUNT CHIP DANGER SQUARE`

### Architecture rule — FAMILY ≠ SHAPE

Do **not** create component explosion (`SquareTagChip`, `PillStatusChip`, …).

Use:

```
family/component + tone + shape
```

Example:

```tsx
<TagChip tone="info" shape="square">
  B2B
</TagChip>
```

Existing `StatusChip` without `shape` remains **pill** (API-compatible; do not mass-add `shape="pill"`).

---

## Semantic tones

| Tone | Meaning | Tokens (conceptually) |
|------|---------|------------------------|
| **NEUTRAL** | General / muted metadata | muted text / border / soft surface |
| **PRIMARY** | Brand / selected / preferred emphasis | `--exits-primary`, soft mixes |
| **INFO** | Informational | `--exits-info` |
| **SUCCESS** | Positive / completed / active | `--exits-success` |
| **WARNING** | Pending / caution / attention | `--exits-warning` |
| **DANGER** | Failure / overdue / critical | `--exits-danger` |

### PRIMARY color rule

**PRIMARY must never mean hardcoded green.**

PRIMARY consumes ExItS semantic primary tokens (`--exits-primary`, `--exits-primary-soft`, related mixes).

Today’s default brand accent happens to be green. Future **Preferences → Primary Color** may change Primary to Blue, Purple, Teal, Orange, etc.

When Primary changes:

- `FILTER CHIP PRIMARY SELECTED` → follows the chosen primary palette
- **SUCCESS** remains semantic success green
- **WARNING** remains amber
- **DANGER** remains red
- **INFO** remains the information semantic color

---

## Status icons

Icons are **supported but not mandatory**.

Default: **NO ICON** unless the icon materially improves recognition.

Recommended Lucide mappings:

| Status | Icon |
|--------|------|
| Approved / Active | `CheckCircle2` |
| Paid | `CircleCheck` |
| Pending | `Clock3` |
| Low stock | `TriangleAlert` |
| Paused | `Pause` |
| Overdue | `CircleAlert` |
| Failed | `CircleX` |
| Declined | `CircleX` or `Ban` (by meaning) |
| Void | `Ban` |

Icon size stays smaller than normal Button icons (`--exits-status-chip-icon-size` / `--exits-chip-square-icon-size`).

Standard icon position: `[ICON] Label`. Removable uses `Label [X]`. Count uses `Label [COUNT]`. Do not decorate both sides unless semantics require it.

---

## Filter selection & motion

### Single vs multi

Both approved:

- **Single select** — one selected value
- **Multi select** — multiple values; optional check mark when it improves clarity

### Motion (interactive chips only)

Allowed: color / border transition, tiny press (`~0.98–0.99` scale), subtle selected transition.

Forbidden: bounce, shake, large hover lift, continuous pulse.

Respect `prefers-reduced-motion` / `motion-reduce`.

---

## Count layout

Locked canonical CountChip layout:

```
[ Pending  3 ]
```

(label + count inside **one** CountChip, `layout="inline"`).

A historical split comparison may remain on `/ui-standards` for education; new work uses the integrated layout.

---

## Density tokens

Chips inherit global Preferences density: **compact** / **balance** / **comfort**.

No page-specific chip density selector.

### Status / soft / pill sizing (`--exits-status-chip-*`)

| Token | Role |
|-------|------|
| `--exits-status-chip-height` | Height |
| `--exits-status-chip-padding-x` | Horizontal padding |
| `--exits-status-chip-font-size` | Font size |
| `--exits-status-chip-border-width` | Border width |
| `--exits-status-chip-gap` | Icon / content gap |
| `--exits-status-chip-icon-size` | Leading icon size |

### Square compact sizing (`--exits-chip-square-*`)

| Token | Role |
|-------|------|
| `--exits-radius-xs` | Square radius (`0.25rem` / 4px) |
| `--exits-chip-square-height` | Height (balance ≈ `1.3125rem` / 21px) |
| `--exits-chip-square-padding-x` | Horizontal padding (balance ≈ `0.375rem` / 6px) |
| `--exits-chip-square-padding-y` | Vertical padding (balance ≈ `0.0625rem` / 1px) |
| `--exits-chip-square-font-size` | Font (balance → `--exits-text-xs` / ~12px) |
| `--exits-chip-square-gap` | Gap |
| `--exits-chip-square-icon-size` | Compact icon size |

Density blocks redefine these tokens; numbers above are **balance** reference only — tokens remain authoritative.

### Interactive filter sizing

Filter chips use `--exits-chip-min-height`, `--exits-chip-padding-x`, `--exits-chip-font-size`, `--exits-chip-gap` (toolbar / control density — larger than read-only status chips).

---

## Theme & responsive

- Light / dark / system via ExItS semantic tokens — no page-local dark-mode chip colors
- Chip groups wrap naturally with compact gaps
- Avoid horizontal overflow on mobile unless a page explicitly requires it
- Filter chips retain adequate interactive hit targets

---

## Accessibility

| Family | Semantics |
|--------|-----------|
| STATUS / TAG | Read-only |
| COUNT | Read-only unless explicitly interactive |
| FILTER | Interactive button; keyboard; visible focus; `aria-pressed`; disabled |
| REMOVABLE | Accessible removal control; `aria-label`; keyboard; focus |

Color must not be the sole indicator when meaning would otherwise be ambiguous.

---

## Cursor shorthand (locked)

### Families

`STATUS CHIP` · `FILTER CHIP` · `TAG CHIP` · `REMOVABLE CHIP` · `COUNT CHIP` · `COUNT BADGE`

### Tones

`NEUTRAL` · `PRIMARY` · `INFO` · `SUCCESS` · `WARNING` · `DANGER`

### Shapes

`PILL` · `SOFT` · `SQUARE`

### Options

`WITH ICON` · `NO ICON` · `SELECTED` · `DISABLED`

### Examples

| Phrase | Meaning |
|--------|---------|
| Approved → `STATUS CHIP SUCCESS` | = SUCCESS **PILL** |
| Pending → `STATUS CHIP WARNING` | = WARNING **PILL** |
| Failed → `STATUS CHIP DANGER` | = DANGER **PILL** |
| B2B → `TAG CHIP INFO` | = INFO **SQUARE** |
| Weighted → `TAG CHIP NEUTRAL` | = NEUTRAL **SQUARE** |
| Beta → `TAG CHIP INFO SQUARE` | explicit square |
| Selected Active filter → `FILTER CHIP PRIMARY SELECTED` | = PRIMARY **PILL** SELECTED |
| Branch: Main → `REMOVABLE CHIP` | = **PILL** |
| Pending 3 → `COUNT CHIP WARNING` | = WARNING **SOFT** integrated |
| Notifications 12 → `COUNT BADGE PRIMARY` | compact round badge |

Because defaults are locked:

- `TAG CHIP INFO` ≡ `TAG CHIP INFO SQUARE`
- `FILTER CHIP SELECTED` ≡ `FILTER CHIP PRIMARY PILL SELECTED`
- `STATUS CHIP SUCCESS` ≡ `STATUS CHIP SUCCESS PILL`
- `COUNT CHIP WARNING` ≡ `COUNT CHIP WARNING SOFT`

---

## Explicit overrides

Priority:

1. Explicit task instruction  
2. ExItS Chip Standard (this document)  
3. Existing page presentation  

Example: `Use STATUS CHIP WARNING SOFT` overrides Status default PILL.

Accessibility and domain correctness still take priority.

---

## Migration policy

Do **not** mass-migrate legacy chips / badges / filters in unrelated tasks.

Prefer focused conversions when the task asks, e.g.:

- Convert status to ExItS Chips  
- `TAG CHIP INFO SQUARE`

Preserve existing `StatusChip` call sites (`tone` without `shape`) — they remain valid pill chips.

---

## Related standards

- [ExItS Button Standard](exits-button-standard.md) — APPROVED / LOCKED (separate; do not conflate shapes)
- [ExItS Table Standard](exits-table-standard.md)
