# Component standards

Build original components with React, TypeScript, and SCSS Modules. Use a native element when it already has the behavior.

## States

Expose only states that belong to the control.

- Button: tone, appearance (solid, gradient, soft, outline, ghost, raise, soft raise, solid raise, convex solid), hover, focus-visible, pressed, disabled, loading. No error variant. The default is solid, a flat tone fill. Raise and solid raise sit above the surface on elevation level 3, lift to level 4 on hover, and settle to level 1 while pressed. Solid raise keeps the solid fill. Convex solid is opt-in: the center stays the semantic color and the rim darkens toward the pressed token, with no drop shadow. The button lock is in `DESIGN_SYSTEM.md`.
- Input and select: error and success text, disabled. Select has no loading state. Selection & Input is locked in `DESIGN_SYSTEM.md`.
- Input appearance is separate from focus motion. Standard is the default: the label stays above the bordered control. Floating label uses that same control, with the label starting in the placeholder and moving up. Outlined floats the label on the border and does not change the control height. Soft keeps a primary tint, floats its label inside the field, and uses 48px Compact, 56px Standard, and 64px Large. Focus Draw and Border Reveal follow the rounded border. Running accent is one 960ms lap. Theme, primary, radius, and density stay on the shared tokens.
- Select uses the same field height, type, radius, border, and density as Input. Stacked keeps the label above. Outlined floats the label on the border. Soft keeps the label inside the tint. The menu matches the trigger width, opens below, and flips above when space is short. Option rows are 36px Compact, 40px Standard, and 48px Large, and at least 44px under 768px. Searchable and multiple are opt-in. Search stays borderless. Narrow viewports open a bottom sheet. The native select stays in the form for name, value, and reset. The dropdown lock is in `DESIGN_SYSTEM.md`.
- Search: `SearchField` in `src/components/search/SearchField.tsx`. Error and clear. No success state. The field does not filter, suggest, or store history. A searchable select filters its own menu and is not a second search component. Navigation & Search is locked in `DESIGN_SYSTEM.md`. The open searching decisions stay in that same file.
- Switch, checkbox, radio, segmented control: checked, disabled, focus-visible.
- Dialog and bottom sheet: open, closing, dismiss.
- Confirm dialog: loading and error on the action. System experiences stays on this dialog. The lock is in `DESIGN_SYSTEM.md`.
- Menu: disabled item, danger tone. One ordinary `DropdownMenu` or `ActionMenu` is open at a time. The menu lock is in `DESIGN_SYSTEM.md`.
- Tabs: selected, disabled.
- Toast: info, success, warning, danger. Status and System experiences are locked in `DESIGN_SYSTEM.md`. Danger uses `role="alert"`. Other tones use `role="status"`. The right-edge countdown is locked: 6px, toast tone, full height shrinking toward the top on the 4000ms dismiss clock. Hover or keyboard focus pauses it. Reduced motion hides it and keeps the dismiss timing.
- Skeleton and empty state are not interactive controls.

## Elevation

Use the semantic elevation token on the component. Do not paste a shadow value into a module.

- Cards at rest use `--elevation-1` on `--color-surface`. A raised card uses `--elevation-2` on that same surface. The Presentation lock is in `DESIGN_SYSTEM.md`.
- Menus, tooltips, popovers, and toasts use `--elevation-3` and `--color-surface-elevated`. The tooltip adds a directional tail. The popover does not.
- Bottom sheets use `--elevation-4`. Dark dialogs use `--elevation-4`. The light dialog panel keeps its inset highlight and two-layer shadow. That is a pending visual exception, not a new elevation level, and it stays until the look is approved.
- Flat lists and inputs stay at `--elevation-0` and rely on the border.
- Do not add a user setting for elevation.

## Overlays

- Dialog and bottom sheet use `<dialog>` with `showModal`.
- Escape, the close control, and the backdrop dismiss them.
- Focus moves inside while open and returns to the trigger on close.
- Bottom sheet drag is optional. Close, Escape, backdrop, and the handle click work without a swipe.
- Menus and tooltips use the Popover API when the browser has it, with fixed positioning and viewport flipping.
- Menus support Arrow keys, Home, End, Escape, and click outside. Opening one ordinary menu closes another without moving focus to the closed trigger. Escape still returns focus to the trigger that was open.
- Toasts are not modal. Danger uses `role="alert"`. Other tones use `role="status"`.

## Files

One component folder holds the component, its module, and its test when it has behavior worth locking. Shared overlay math lives in `src/components/overlay/overlay.ts`.
