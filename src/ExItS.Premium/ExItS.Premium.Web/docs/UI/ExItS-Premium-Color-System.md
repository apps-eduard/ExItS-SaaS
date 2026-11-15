# ExItS Premium color system

**LOCKED** after contrast tests, component tests, typecheck, and the production build. Authoritative runtime values live in `src/styles/tokens.scss`. Playground: `http://127.0.0.1:5180/design-system#colors`. Contrast lock: `src/theme/tokens.contrast.test.ts`.

Apple references are the visual targets, not the fill for every state. White text fails WCAG AA on those bright references, so solid labels use a dark foreground and the interactive stops are the accessible neighbors.

**Rule:** components consume `var(--color-*)` / `var(--palette-*)`. Do not hardcode hex/OKLCH in SCSS modules unless documenting a one-off sample. Do not wire the 40-family reference library into themes or components.

---

## Attributes (documentElement)

| Attribute | Values | Notes |
| --- | --- | --- |
| `data-theme` | `light` \| `dark` | Resolved from System; never the string `system` |
| `data-palette` | `teal` (Mint) \| `blue` \| `violet` (Purple) \| `orange` \| `rose` | Default `teal` |

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
| **Warning** | Due / caution | No — `#ff8d28`, dark text |
| **Help** | Assistance / tutorial | No — purple-600 `#9333ea` |
| **Danger** | Error / destructive | No — `#ff383c`, dark text |
| **Contrast** | High-contrast fill/action | No (aliases text / inverse) |

Status roles (Success, Info, Warning, Help, Danger) **must not** change when the user switches Primary.

### Success / Warning / Danger / Info

Reference fills: Success `#34c759`, Warning `#ff8d28`, Danger `#ff383c`, Info `#0088ff`. White text fails AA on each, so the foreground stays dark. Hover and pressed are nearby shades. Ink is darker on light surfaces and lighter on dark surfaces.

| Token | Light | Dark |
| --- | --- | --- |
| `--color-success` | `#34c759` | `#34c759` |
| `--color-success-hover` | `#2db250` | `#59d177` |
| `--color-success-pressed` | `#2aa54a` | `#2fb852` |
| `--color-success-foreground` | `#052e16` | `#052e16` |
| `--color-success-ink` | `#166534` | `#5ee08a` |
| `--color-warning` | `#ff8d28` | `#ff8d28` |
| `--color-warning-hover` | `#da7822` | `#ff9f4a` |
| `--color-warning-pressed` | `#c36b1e` | `#e88024` |
| `--color-warning-foreground` | `#1a0a00` | `#1a0a00` |
| `--color-warning-ink` | `#9a3412` | `#ffb36b` |
| `--color-danger` | `#ff383c` | `#ff383c` |
| `--color-danger-hover` | `#f23539` | `#ff585b` |
| `--color-danger-pressed` | `#ec3337` | `#f43539` |
| `--color-danger-foreground` | `#2a0506` | `#2a0506` |
| `--color-danger-ink` | `#b42318` | `#ffb4b6` |
| `--color-info` | `#0088ff` | `#0088ff` |
| `--color-info-hover` | `#0081f2` | `#2e9dff` |
| `--color-info-pressed` | `#007eeb` | `#0082f4` |
| `--color-info-foreground` | `#041426` | `#041426` |
| `--color-info-ink` | `#1d4ed8` | `#7dd3fc` |

Soft / border (both themes): `color-mix` of the role into `--color-surface` / `--color-border` (see `[data-theme]` block).

Never use white text on Success, Warning, Danger, or Info fills.

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
Dark Mint (`teal`): **400 / 300 / 500**. Dark Purple and the other palettes: **300 / 200 / 400**. Dark foregrounds are per palette.

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
| Elevated | `--color-surface-elevated` | `#ffffff` | `#31445f` |
| Text | `--color-text` | `#111827` | `#f8fafc` |
| Text secondary | `--color-text-secondary` | `#4b5563` | `#cbd5e1` |
| Caption | `--color-text-caption` | `#5c6778` | `#94a3b8` |
| Inverse | `--color-text-inverse` | `#f8fafc` | `#0f172a` |
| Border | `--color-border` | `#e4e7ec` | `#3d4f68` |
| Strong border | `--color-border-strong` | `#d0d5dd` | `#5b6d86` |
| Disabled fill | `--color-disabled-fill` | `#e5e7eb` | `#334155` |
| Disabled text | `--color-disabled-text` | `#4b5563` | `#cbd5e1` |
| Overlay | `--color-overlay` | `rgba(15, 23, 42, 0.46)` | same (`:root`) |
| Hover wash | `--color-hover` | mix text into surface | same recipe |

Neutral scale `--neutral-50`…`900` documents shared greys; dark page stays on `--color-page` (slightly deeper than neutral-900).

---

## Five Primary themes

| Palette | Stored value | Reference stop | Light primary | Dark primary |
| --- | --- | --- | --- | --- |
| Mint (default) | `teal` | 400 `#00c8b3` | 700 `#1b756c` | 400 `#00c8b3` |
| Blue | `blue` | unchanged | 700 | 300 |
| Purple | `violet` | 500 `#cb30e0` | 700 `#9a22ab` | 300 `#e598f0` |
| Orange | `orange` | unchanged | 700 | 300 |
| Rose | `rose` | unchanged | 700 | 300 |

Saved `teal` and `violet` still select Mint and Purple. No preference migration is required. `#cb30e0` is the scale’s 500 stop. It is not the button fill, because neither black nor white text reaches 4.5:1 on it.

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
| **Badge** | Soft and outline use ink. Solid uses the role foreground on the role fill. Solid accent uses the primary fill and primary foreground. |
| **Toast / alerts** | Tones `info` \| `success` \| `warning` \| `danger` — border tint from `--color-{tone}`. |
| **Select selected row** | Primary soft / selected washes. |
| **Future Personal Home** | Prefer semantic roles + feature tokens (`--color-feature-lent|owe|people|stores|reminders-*`). Do not invent new hexes or import reference-library colors. |

Do **not** auto-add Help / Accent / Contrast variants to every component — only when the role is needed.

---

## Agent checklist

1. Read `tokens.scss` before changing colors.
2. Use semantic/foundation tokens in new UI.
3. Keep Success `#34c759`, Warning `#ff8d28`, Danger `#ff383c`, Info `#0088ff`, and Help `#9333ea`, with the Light/Dark variants in this document.
4. Never promote reference swatches into runtime themes.
5. Update this doc when tokens change; keep `DESIGN_TOKENS.md` as the short index pointing here for color detail.

---

## Apple Color review

Reviewed against [Apple’s Color guidance](https://developer.apple.com/design/human-interface-guidelines/color). The approved finalization is the tables above.

### Preserve

- Five palettes on `data-palette`: `teal` (Mint), `blue`, `violet` (Purple), `orange`, `rose`. Light uses the 700 stop and a white foreground. Dark uses a lighter stop and a dark foreground. Status roles do not follow the palette.
- Light, Dark, and System. The attribute is `light` or `dark`, never the string `system`.
- Nine semantic roles and the foundation tokens. Success, Warning, Danger, and Info use the reference fills with dark foregrounds. Help stays `#9333ea`.
- Dark page, surface, and elevated surface stay three separate fills. Five elevations stay 0 through 4.
- Buttons use `--color-{role}-foreground` on solid fills. The contrast test covers those pairs, plus ink on page and surface, for every palette in Light and Dark.
- Focus is `--color-focus` (the primary). Hover is the text wash. Disabled fill and text are their own tokens. Error and success on fields use danger and success ink, with the message as words. Selected calendar days use the primary fill and foreground. Today adds a dot and a ring, so it is not color alone.
- Toasts pair info, success, warning, and danger with an icon and a title. Danger uses `role="alert"`.
- Financial amounts in the playground use the text color. A transaction row does not inherit a status color.

### Adopt as rules, using the current tokens

- One color, one meaning. Primary is the main action. Success, Warning, and Danger are statuses. Accent is a selected wash, not a sixth palette.
- Keep one primary action visually ahead of secondary and ghost actions.
- Do not use color as the only signal when a status has no visible word. Toasts already add an icon. A new status chip needs a word, and an icon when the word is absent.
- Money stays on `--color-text`. Do not paint a balance with primary, success, or danger. Put Paid, Due, or Failed in a status that uses the matching role.

### Applied in this lock

- Solid badges use `--color-{role}-foreground`. Solid accent uses the primary foreground. Neutral solid uses inverse text.
- Orange primary and Warning are both warm, and Mint sits near Success. The fills differ. Keep the words on the status.
- Dark reminders `#fdba74` still matches Dark orange primary. That was left unchanged.
- `prefers-contrast` is not implemented.

### Do not adopt

- A second copy of Apple’s system colors, SwiftUI names, or a native color API. The reference hexes above are the status bases and the Mint/Purple scale anchors only.
- Liquid Glass, blur, or a translucent material system.
- A sixth palette, a second theme attribute, or duplicate tokens for the same role.
- Increased Contrast as a separate `data-theme` value.
- Coloring numeric totals green or red.

### Recorded decisions

1. Solid badges use the role foreground. Done.
2. Dark reminders stay `#fdba74` and can match Dark orange primary.
3. `prefers-contrast` does not add a theme or thicker chrome in this lock.
4. Amounts stay on the text color. Status roles sit beside a word.
