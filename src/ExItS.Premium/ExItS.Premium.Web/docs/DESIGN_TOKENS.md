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

Control heights are 36, 44, and 52. Density also changes padding, page padding, card padding, and stack gap.

## Feature colors

Lent, owe, people, stores, and reminders have their own fill, foreground, and ink tokens. They do not follow the selected primary palette.
