# Accessibility

- Use native buttons, inputs, selects, checkboxes, radios, and dialogs.
- Name icon-only buttons with `aria-label`. Decorative icons set `aria-hidden`.
- Meaningful icons set `role="img"` and `aria-label`.
- Focus uses the shared `:focus-visible` ring, 2px in the primary color.
- Dialogs and sheets move focus inside and restore it on close.
- Menus and tabs use arrow keys. Disabled items and tabs are skipped.
- Tooltips are supplementary. The trigger keeps its own name.
- Toasts use a live region. Hover pauses dismissal.
- Text on fills is locked at 4.5:1 by `src/theme/tokens.contrast.test.ts`.
- Reduced motion is a system setting. Tests may simulate it. The app must not force motion back on.
