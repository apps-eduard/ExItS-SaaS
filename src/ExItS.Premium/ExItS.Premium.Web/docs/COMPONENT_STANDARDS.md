# Component standards

Build original components with React, TypeScript, and SCSS Modules. Use a native element when it already has the behavior.

## States

Expose only states that belong to the control.

- Button: variant, hover, focus-visible, pressed, disabled, loading. No error variant.
- Input and select: error and success text, disabled. Select has no loading state.
- Search: error and clear. No success state.
- Switch, checkbox, radio, segmented control: checked, disabled, focus-visible.
- Dialog and bottom sheet: open, closing, dismiss.
- Confirm dialog: loading and error on the action.
- Menu: disabled item, danger tone.
- Tabs: selected, disabled.
- Toast: info, success, warning, danger.
- Skeleton and empty state are not interactive controls.

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
