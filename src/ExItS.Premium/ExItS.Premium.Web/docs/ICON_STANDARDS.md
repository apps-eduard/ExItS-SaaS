# Icon standards

`ExIcon` is the only general icon component. `AppIcon` is the same component at 24px for navigation. `IconTile` is the colored container.

## Defaults

- Family: Material Symbols Rounded
- Fill: 0
- Weight: 400
- Grade: 0
- Optical size: follows the pixel size, clamped from 20 to 48
- General size: 20px
- Navigation size: 24px

## Loading

The font is `src/icons/material-symbols-rounded.woff2`. It is a subset of the names in `src/icons/registry.ts`. `@font-face` uses `font-display: block`, and the glyph box clips fallback text.

Do not load the font from a CDN at runtime.

When adding a name, add it to `ICON_NAMES` and run `scripts/update-material-symbols.ps1`.

## Color

Navigation and controls inherit `currentColor`. Feature tiles use the lent, owe, people, stores, and reminders tokens. Variants are neutral, primary, feature, and solid.

Original SVG feature icons can be added later beside `ExIcon`. They should not replace the wrapper.
