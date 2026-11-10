# ExItS Premium frontend

This directory is the Personal design system. It is not V1, POS, or the platform admin.

Before changing UI, read the document that matches the work:

- `docs/DESIGN_SYSTEM.md` — visual direction and locked decisions
- `docs/DESIGN_TOKENS.md` — theme, palette, radius, density
- `docs/COMPONENT_STANDARDS.md` — component APIs and states
- `docs/ICON_STANDARDS.md` — Material Symbols and icon tiles
- `docs/MOTION_STANDARDS.md` — timing and reduced motion
- `docs/RESPONSIVE_STANDARDS.md` — breakpoints and overlays
- `docs/ACCESSIBILITY.md` — keyboard, focus, and contrast
- `docs/ARCHITECTURE.md` — where code lives
- `docs/VISUAL_QA_CHECKLIST.md` — review before calling work done

## Locked decisions

- Apple-inspired original ExItS identity. Modern Solid surfaces. No glass, blur, or a third-party visual kit.
- SCSS Modules plus CSS custom properties. Do not add Tailwind, shadcn, Radix, Material UI, or Bootstrap.
- Google Material Symbols Rounded, self-hosted, through `ExIcon`.
- Colored icon tiles only for feature actions. Navigation icons stay monochrome.
- Theme: Light, Dark, System. Default System.
- Primary palettes: Teal (default), Blue, Violet, Orange, Rose.
- Corner shape: Standard, Soft (default), Pill. Pill does not turn cards, dialogs, or sheets into ovals.
- Density: Compact, Standard (default), Large.
- Mobile-first layout. Motion follows the system reduced-motion setting.
- Personal module only. Do not call Personal APIs from this playground.
- Do not change backend contracts, V1, POS, or ports 5177 and 5178.

Do not claim a numeric quality score without visual evidence from the running playground.
