# Theme System — Light, Dark and System

[UI Design System](ui-design-system.md) | [Localization](localization.md)

## Supported preferences

- Light
- Dark
- System default

## Requirements

- Preference is persisted per user and locally for signed-out/login screens.
- System mode follows operating-system changes where supported.
- No flash of incorrect theme during startup where practical.
- Both themes meet accessible contrast and visible focus requirements.
- Charts, tables, modals, date fields, disabled controls and validation states are tested in both themes.
- Theme changes do not restart the app or lose state.

## Native POS tokens

Use semantic variables rather than page-specific colors:

```css
:root {
  --exits-bg: #f5f7f8;
  --exits-surface: #ffffff;
  --exits-text: #17202a;
  --exits-muted: #5f6b76;
  --exits-border: #d7dde3;
  --exits-primary: #166534;
  --exits-danger: #b42318;
}

[data-theme="dark"] {
  --exits-bg: #101418;
  --exits-surface: #182027;
  --exits-text: #f2f5f7;
  --exits-muted: #aab5bf;
  --exits-border: #34414b;
  --exits-primary: #4ade80;
  --exits-danger: #ff8a80;
}
```

New ExITS Platform Admin and PinoyBusinessPOS share the same semantic CSS custom-property approach (`--exits-*`). Existing HealthCare Staff Web keeps its Ant/`--hc-*` styling without a forced migration.
