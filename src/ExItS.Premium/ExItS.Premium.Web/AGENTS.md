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
- Primary palettes: Mint (default, stored as teal), Blue, Purple (stored as violet), Orange, Rose.
- Corner shape: Standard, Soft (default), Pill. Pill does not turn cards, dialogs, or sheets into ovals.
- Density: Compact, Standard (default), Large.
- Mobile-first layout. Layout and organization is locked in `docs/LAYOUT_ORGANIZATION.md`. Keep the sidebar, top bar, drawer, inspector, and breakpoints. Drawer rows stay at least 44px. Do not add a second safe-area inset. Do not add a list or table until a product screen needs one. A notched-phone safe-area check is not verified. Motion follows the system reduced-motion setting.
- Content is locked in `docs/CONTENT_REVIEW.md`. Use the approved type and financial numeric tokens. Informational text stays readable and selectable. Keep `ExIcon`, accessible names, and text contrast. Keep the single-line `Input` until a workflow needs a longer note. Do not add a chart, image, or web-view library until a screen needs that content. Charts, photos, logos, product images, and multiline notes can be added when a real workflow requires them.
- Personal module only. Do not call Personal APIs from this playground.
- Do not change backend contracts, V1, POS, or ports 5177 and 5178.

Before adding UI:

- Search `src/components/` for an existing component. Do not add a second one.
- Reuse semantic tokens and the component API. Do not invent colors, font sizes, radius values, or elevation levels.
- Style with SCSS Modules. Keep Light, Dark, density, focus, and responsive behavior.
- Do not replace an approved design without a verified issue.
- Run the relevant tests before calling the work done.

Do not claim a numeric quality score without visual evidence from the running playground.
