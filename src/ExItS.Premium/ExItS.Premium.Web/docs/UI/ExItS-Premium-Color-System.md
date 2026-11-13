# ExItS Premium color system

Authoritative runtime values live in `src/styles/tokens.scss`. Playground: `http://127.0.0.1:5180/design-system#colors`. Contrast lock: `src/theme/tokens.contrast.test.ts`.

**Rule:** components consume `var(--color-*)` / `var(--palette-*)`. Do not hardcode hex/OKLCH in SCSS modules unless documenting a one-off sample. Do not wire the 40-family reference library into themes or components.

---

## Attributes (documentElement)

| Attribute | Values | Notes |
| --- | --- | --- |
| `data-theme` | `light` \| `dark` | Resolved from System; never the string `system` |
| `data-palette` | `teal` \| `blue` \| `violet` \| `orange` \| `rose` | Default `teal` |

Storage keys: `exits.premium.theme`, `exits.premium.palette` (not V1 POS prefs).

---

## Nine semantic roles

Shared pattern (where defined):

`--color-{role}` · `-hover` · `-pressed` · `-foreground` · `-ink` · `-soft` · `-border`

| Role | Meaning | Follows Primary palette? |
| --- | --- | --- |
| **Primary** | Main actions, focus | Yes (`data-palette`) |
| **Secondary** | Neutral supporting actions | No (aliases surface / border) |
| **Accent** | Subtle selected/highlight wash | Yes (derived from Primary) |
| **Success** | Positive / paid / saved | No |
| **Info** | Neutral information | No |
| **Warning** | Due / caution | No — locked `#ff5722` |
| **Help** | Assistance / tutorial | No — purple-600 `#9333ea` |
| **Danger** | Error / destructive | No — `#dc2626` |
| **Contrast** | High-contrast fill/action | No (aliases text / inverse) |

Status roles (Success, Info, Warning, Help, Danger) **must not** change when the user switches Primary.

### Success / Info (OKLCH)

Bright fills use **dark** foregrounds — white text fails AA on these bases.

| Token | Light | Dark |
| --- | --- | --- |
| `--color-success` | `oklch(0.64 0.22 160)` | same base |
| `--color-success-hover` | `oklch(0.58 0.22 160)` | `oklch(0.7 0.2 160)` |
| `--color-success-pressed` | `oklch(0.55 0.21 160)` | `oklch(0.58 0.22 160)` |
| `--color-success-foreground` | `oklch(0.18 0.04 160)` | same |
| `--color-success-ink` | `oklch(0.4 0.16 160)` | `oklch(0.72 0.18 160)` |
| `--color-info` | `oklch(0.69 0.22 220)` | same base |
| `--color-info-hover` | `oklch(0.63 0.22 220)` | `oklch(0.75 0.2 220)` |
| `--color-info-pressed` | `oklch(0.56 0.21 220)` | `oklch(0.63 0.22 220)` |
| `--color-info-foreground` | `oklch(0.16 0.05 220)` | same |
| `--color-info-ink` | `oklch(0.4 0.16 220)` | `oklch(0.78 0.14 220)` |

Soft / border (both themes): `color-mix` of the role into `--color-surface` / `--color-border` (see `[data-theme]` block).

### Warning (locked)

| Token | Light | Dark |
| --- | --- | --- |
| `--color-warning` | `#ff5722` | `#ff5722` |
| `--color-warning-hover` | `#f4511e` | `#f4511e` |
| `--color-warning-pressed` | `#e64a19` | `#e64a19` |
| `--color-warning-foreground` | `#1a0a00` | `#1a0a00` |
| `--color-warning-ink` | `#1a0a00` | `#ffab91` |

Never use white text on Warning fill.

### Danger

| Token | Light | Dark |
| --- | --- | --- |
| `--color-danger` | `#dc2626` | `#dc2626` |
| `--color-danger-hover` | `#b91c1c` | `#b91c1c` |
| `--color-danger-pressed` | `#991b1b` | `#991b1b` |
| `--color-danger-foreground` | `#ffffff` | `#ffffff` |
| `--color-danger-ink` | `#b91c1c` | `#fca5a5` |

`--color-danger-soft`: `color-mix(in srgb, var(--color-danger) 12%, var(--color-surface))`.

### Help

| Token | Light | Dark |
| --- | --- | --- |
| `--color-help` | `#9333ea` (purple-600) | `#9333ea` |
| `--color-help-hover` | `#7e22ce` | `#7e22ce` |
| `--color-help-pressed` | `#6b21a8` | `#6b21a8` |
| `--color-help-foreground` | `#ffffff` | `#ffffff` |
| `--color-help-ink` | `#7e22ce` | `#c084fc` |

### Primary (dynamic)

Light: palette **700 / 800 / 900** + white foreground.  
Dark teal: **400 / 300 / 500**; other dark palettes **300 / 200 / 400** + dark foreground tokens per palette.

Also: `--color-primary-soft`, `--color-primary-selected`, `--color-primary-ink`, `--color-primary-border`, `--color-focus` → primary.

### Secondary / Accent / Contrast

| Role | Base | Notes |
| --- | --- | --- |
| Secondary | `var(--color-surface)` | Matches Button `secondary` (border-strong + hover wash) |
| Accent | `var(--color-primary-selected)` | Selected nav / filters — **not** a 6th Primary |
| Contrast | `var(--color-text)` | FG = `--color-text-inverse` |

---

## Foundation colors

Separate from semantic roles on the Colors page.

| Role | Token | Light | Dark |
| --- | --- | --- | --- |
| Page | `--color-page` | `#f5f6f8` | `#0f172a` |
| Surface | `--color-surface` | `#ffffff` | `#1e293b` |
| Elevated | `--color-surface-elevated` | `#ffffff` | `#263449` |
| Text | `--color-text` | `#111827` | `#f8fafc` |
| Text secondary | `--color-text-secondary` | `#4b5563` | `#cbd5e1` |
| Caption | `--color-text-caption` | `#5c6778` | `#94a3b8` |
| Inverse | `--color-text-inverse` | `#f8fafc` | `#0f172a` |
| Border | `--color-border` | `#e4e7ec` | `#334155` |
| Strong border | `--color-border-strong` | `#d0d5dd` | `#475569` |
| Disabled fill | `--color-disabled-fill` | `#e5e7eb` | `#334155` |
| Disabled text | `--color-disabled-text` | `#4b5563` | `#cbd5e1` |
| Overlay | `--color-overlay` | `rgba(15, 23, 42, 0.46)` | same (`:root`) |
| Hover wash | `--color-hover` | mix text into surface | same recipe |

Neutral scale `--neutral-50`…`900` documents shared greys; dark page stays on `--color-page` (slightly deeper than neutral-900).

---

## Five Primary themes

| Palette | Light primary stop | Dark primary stop (teal vs others) |
| --- | --- | --- |
| Teal (default) | 700 | 400 |
| Blue | 700 | 300 |
| Violet | 700 | 300 |
| Orange | 700 | 300 |
| Rose | 700 | 300 |

Stops live as `--palette-{name}-50`…`900` in `:root`. Components never embed those hexes.

---

## Accessibility

- Solid fill text (`-foreground` on `-` / `-hover` / `-pressed`) ≥ **4.5:1**.
- Ink on page and surface ≥ **4.5:1**.
- Prefer contrast of dark vs light ink candidates on saturated fills (Success/Info/Warning) — do not assume white text.
- Tests: `tokens.contrast.test.ts` (OKLCH-aware).

---

## Reference color library (not a theme)

- Source: `src/playground/reference-palettes.ts`
- **40 families × 10 shades (50–900) = 400 swatches**
- Search + copy HEX / name on Colors → Reference Color Library
- **Reference only** — do not add to `data-palette`, do not apply to Button/Badge/theme switching

---

## Component usage (current)

| Surface | How colors are used today |
| --- | --- |
| **Button** | `primary` / `secondary` / `ghost` / `danger` / `success` → semantic fills + foreground. Secondary uses surface + border-strong (not `--color-secondary*` yet, but tokens match that recipe). |
| **Input / Select** | Borders: default border; error → `--color-danger`; success → `--color-success`. Messages/ink: `--color-*-ink`. Focus accent → `--color-primary` / motion shell. |
| **Badge** | `neutral` hover wash; `primary` soft+ink; `success`/`danger`/`info` soft+ink; `warning` solid + dark FG. |
| **Toast / alerts** | Tones `info` \| `success` \| `warning` \| `danger` — border tint from `--color-{tone}`. |
| **Select selected row** | Primary soft / selected washes. |
| **Future Personal Home** | Prefer semantic roles + feature tokens (`--color-feature-lent|owe|people|stores|reminders-*`). Do not invent new hexes or import reference-library colors. |

Do **not** auto-add Help / Accent / Contrast variants to every component — only when the role is needed.

---

## Agent checklist

1. Read `tokens.scss` before changing colors.
2. Use semantic/foundation tokens in new UI.
3. Keep Warning `#ff5722` + dark FG; Danger `#dc2626`; Help `#9333ea`; Success/Info OKLCH as above.
4. Never promote reference swatches into runtime themes.
5. Update this doc when tokens change; keep `DESIGN_TOKENS.md` as the short index pointing here for color detail.
