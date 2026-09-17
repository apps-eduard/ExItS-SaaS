# ExItS Motion Standard

## User preference

**Appearance → Animations**

| Option | Behavior |
|--------|----------|
| **System** | Follow OS `prefers-reduced-motion`. Normal when OS allows motion. |
| **Reduced** | Always minimal motion; ambient off. Overrides OS “no preference”. |

Persistence (unchanged): `motion: "system" | "reduced"` → `data-motion` on `<html>`.

## Internal categories

| Category | Use | Examples |
|----------|-----|----------|
| **TRANSITION** | Structural UI | Sidebar Reveal, drawer/sheet, dialog, menu |
| **INTERACTION** | Direct feedback | Button press, chip selection, interactive card lift, search clear |
| **AMBIENT** | Decorative continuous | Settings gear rotation + Primary color cycle only |

## Tokens

| Token | Typical range |
|-------|----------------|
| `--exits-motion-instant` | ~60ms |
| `--exits-motion-fast` | ~130ms |
| `--exits-motion-normal` (base) | ~180ms |
| `--exits-motion-slow` | ~240ms |
| `--exits-motion-shell` | ~280ms |
| `--exits-ease-standard` | `cubic-bezier(0.2, 0, 0, 1)` |

## Performance

- Prefer **transform** and **opacity**.
- No animation libraries / no persistent `requestAnimationFrame` loops.
- Ambient: **one** Settings gear; pause color timer when `document.hidden`.
- No table-row or product-grid entrance cascades.
- Never delay business APIs, navigation, or checkout for animation.

## Reduced

Still show selected/open/error/loading/focus. Kill scale, lift, ambient rotation, and color cycling.
Utilities: `.exits-motion-press`, `.exits-motion-lift` are neutralized under reduced.
