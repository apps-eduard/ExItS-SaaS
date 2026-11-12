# Component standards

Build original components with React, TypeScript, and SCSS Modules. Use a native element when it already has the behavior.

## States

Expose only states that belong to the control.

- Button: tone, appearance (solid, soft, outline, raise, solid raise), hover, focus-visible, pressed, disabled, loading. No error variant. Raise and solid raise sit above the surface on elevation level 3, lift to level 4 on hover, and settle to level 1 while pressed. Solid raise keeps the solid fill.
- Input and select: error and success text, disabled. Select has no loading state.
- Input appearance is separate from focus motion. Standard is the default: the label stays above the bordered control. Floating label uses that same control, with the label starting in the placeholder and moving up. Outlined floats the label on the border. Soft keeps a primary tint and floats its label inside the field. Neither changes the control height. Focus Draw and Border Reveal follow the rounded border. Theme, primary, radius, and density stay on the shared tokens.
- Select uses the same field height, type, radius, border, and density as Input. Stacked keeps the label above. Outlined floats the label on the border. Soft keeps the label inside the tint. The menu matches the trigger width, opens below, and flips above when space is short. Option rows follow Compact, Standard, and Large. Searchable and multiple are opt-in. Narrow viewports open a bottom sheet. The native select stays in the form for name, value, and reset.
- Search: error and clear. No success state.
- Switch, checkbox, radio, segmented control: checked, disabled, focus-visible.
- Dialog and bottom sheet: open, closing, dismiss.
- Confirm dialog: loading and error on the action.
- Menu: disabled item, danger tone.
- Tabs: selected, disabled.
- Toast: info, success, warning, danger.
- Skeleton and empty state are not interactive controls.

## Elevation

Use the semantic elevation token on the component. Do not paste a shadow value into a module.

- Cards at rest use `--elevation-1`. A raised card uses `--elevation-2`.
- Menus, tooltips, and toasts use `--elevation-3`.
- Dialogs and bottom sheets use `--elevation-4`.
- Flat lists and inputs stay at `--elevation-0` and rely on the border.
- Do not add a user setting for elevation.

## Overlays

- Dialog and bottom sheet use `<dialog>` with `showModal`.
- Escape, the close control, and the backdrop dismiss them.
- Focus moves inside while open and returns to the trigger on close.
- Bottom sheet drag is optional. Close, Escape, backdrop, and the handle click work without a swipe.
- Menus and tooltips use the Popover API when the browser has it, with fixed positioning and viewport flipping.
- Menus support Arrow keys, Home, End, Escape, and click outside.
- Toasts are not modal. Danger uses `role="alert"`. Other tones use `role="status"`.

## Files

One component folder holds the component, its module, and its test when it has behavior worth locking. Shared overlay math lives in `src/components/overlay/overlay.ts`.
