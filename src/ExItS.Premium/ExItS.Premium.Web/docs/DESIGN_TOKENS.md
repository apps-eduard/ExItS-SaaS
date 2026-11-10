# Design tokens

`src/styles/tokens.scss` is the only place appearance values are declared. Components read variables.

## Attributes

Set on `document.documentElement`:

| Attribute | Values | Default |
| --- | --- | --- |
| `data-theme` | `light`, `dark` | resolved from System |
| `data-palette` | `teal`, `blue`, `violet`, `orange`, `rose` | `teal` |
| `data-radius` | `standard`, `soft`, `pill` | `soft` |
| `data-density` | `compact`, `standard`, `large` | `standard` |

System is stored separately and resolved with `prefers-color-scheme`. The attribute is never the string `system`.

## Storage

- `exits.premium.theme`
- `exits.premium.palette`
- `exits.premium.radius`
- `exits.premium.density`

Do not read or write `exits.pos-client.ui-preferences.v1`.

## Density

Control heights are 36, 44, and 52 on desktop. On screens under 768px, Compact uses at least 44px so a primary control stays tappable. Height is a minimum: it grows with the control font so text zoom does not clip the value.

Density also changes padding, page padding, card padding, and stack gap.

Control text is 14, 15, and 16px. Button text is 13, 14, and 16px. Form labels and list text are 13, 14, and 15px. The tokens are `--density-control-font`, `--density-button-font`, `--density-label-font`, and `--density-list-font`, in rem.

Page headings, display type, body copy, and numeric totals stay on the type scale.

## Neutral scale

`--neutral-50` through `--neutral-900` document the existing page, border, and text hex values. They are not a second palette. The dark page `#0f172a` stays on `--color-page` because it is slightly deeper than `--neutral-900` (`#111827`).

| Step | Hex | Used by |
| --- | --- | --- |
| 50 | `#f5f6f8` | Light page |
| 100 | `#e5e7eb` | Disabled fill, light |
| 200 | `#e4e7ec` | Default border, light |
| 300 | `#d0d5dd` | Strong border, light |
| 400 | `#94a3b8` | Caption, dark |
| 500 | `#5c6778` | Caption, light |
| 600 | `#4b5563` | Secondary text, light |
| 700 | `#334155` | Border, dark |
| 800 | `#1e293b` | Surface, dark |
| 900 | `#111827` | Text, light |

## Primary palettes

Each palette has `--palette-{name}-50` through `900` in `:root`. Light and dark `--color-primary`, `--color-primary-hover`, and `--color-primary-pressed` point at those stops. Do not copy the hex into a component.

Teal is the default. Light palettes use 700 / 800 / 900 for primary / hover / pressed. Dark teal uses 400 / 300 / 500. The other dark palettes use 300 / 200 / 400. Foreground stays a separate token when the readable ink is white or a dark page color.

`--color-primary-soft` is the subtle background. `--color-primary-selected` is the selected background. `--color-focus` follows primary.

Stops that are not referenced by those roles are catalog steps only.

## Semantic colors

Success, warning, danger, and information have solid, hover, pressed, foreground, ink, soft, and border tokens. They do not change with the primary palette.

Warning solid is `#ff5722` in both themes. Foreground is `#1a0a00`. White on that fill fails text contrast.

## Personal identity colors

Lent, owe, people, stores, and reminders have fill, foreground, ink, and soft tokens. Soft is a tint of the ink on the surface. They do not follow the selected primary palette.

## Surfaces, text, and borders

| Role | Token |
| --- | --- |
| Page | `--color-page` |
| Card and input | `--color-surface` |
| Elevated | `--color-surface-elevated` |
| Hover | `--color-hover` |
| Selected | `--color-primary-selected` |
| Overlay scrim | `--color-overlay` |
| Primary text | `--color-text` |
| Secondary text | `--color-text-secondary` |
| Muted text | `--color-text-caption` |
| Disabled text | `--color-disabled-text` |
| Inverse text | `--color-text-inverse` |
| Border | `--color-border` |
| Subtle border | `--color-border-subtle` |
| Strong border | `--color-border-strong` |
| Focus border | `--color-focus` |
| Error border | `--color-danger` |

## Elevation

Shadows are `--elevation-0` through `--elevation-4`. Light and dark define them separately. They do not change with palette, radius, or density.

| Level | Name | Use |
| --- | --- | --- |
| 0 | Flat | Backgrounds, lists, inputs. No shadow. |
| 1 | Resting | Regular cards and sections. |
| 2 | Raised | Floating cards and hover surfaces. |
| 3 | Floating | Dropdowns, menus, popovers, and toasts. |
| 4 | Overlay | Dialogs and sheets. |

`--shadow-1`, `--shadow-2`, and `--shadow-3` alias levels 1, 3, and 4 so older references stay aligned. New surfaces should use `--elevation-*`.

Stacking is separate: `--z-sticky` 20, `--z-dropdown` 40, `--z-menu` 50, `--z-toast` 80. Dialogs and sheets use the browser top layer.

## Contrast

Text on a solid fill, including primary, status, and feature foreground, must reach 4.5:1. Status and feature ink on the page and on a surface use the same bar. `tokens.contrast.test.ts` locks those pairs.
