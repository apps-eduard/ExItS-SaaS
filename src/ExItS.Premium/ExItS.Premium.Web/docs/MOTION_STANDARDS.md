# Motion standards

Compared with [Apple’s Motion guidance](https://developer.apple.com/design/human-interface-guidelines/motion). This is a review, not an implementation. Do not add motion tokens or replace the existing system.

Tokens live in `src/styles/tokens.scss`.

- Fast: 120ms
- Base: 180ms
- Slow: 280ms
- Ease: `cubic-bezier(0.2, 0.8, 0.2, 1)`

`MOTION_SPRING` in `src/motion/motion.ts` is stiffness 420, damping 32. Dialog and sheet springs are separate. See exceptions below.

Use transform and opacity. Theme color changes are instant.

`prefers-reduced-motion: reduce` sets the durations to 1ms and removes dialog, sheet, menu, tab, and skeleton animation. `MotionConfig reducedMotion="user"` does the same for Motion for React. Do not override that preference in product code.

Do not animate an action so the user has to wait before continuing.

## Review

Apple’s useful rules here are: motion explains a change, stays brief, can be interrupted, and yields to Reduce Motion. ExItS already follows those for controls. Do not copy Apple’s navigation transitions or replace the dialog spring.

### Preserve

- Duration tokens, shared ease, and transform/opacity for controls.
- Button press is a 120ms scale to 0.98. Reduced motion removes the press transform and the ripple.
- Inputs, search, checkbox, radio, and switch use the fast or base token. Shipped input focus motion is `none`. Focus Draw and Border Reveal stay opt-in.
- Select chevron and menu animations stop under Reduce Motion. Mobile select, date, time, and date-time use `BottomSheet`, which can be dragged and skips its animation while dragging.
- Toasts move about 0.45rem and disappear under Reduce Motion. They do not block the page.
- Dialog close switches the panel to the exit phase, so a spring can be interrupted. Reduce Motion fades the panel in about 10ms with no travel.
- The mobile dialog sheet slides on the Y axis only. Its exit spring is snappy and does not bounce.
- There is no route or page transition. Navigation stays instant.

### Adopt

Nothing new. Reduce Motion, short control feedback, and a sheet that comes from the edge it belongs to are already present.

### Refine

Dialog sheets and picker sheets are the same kind of motion with two implementations. `Dialog` on a narrow viewport uses `DIALOG_SHEET_SPRING` (stiffness 270, damping 15, mass 0.92) and a small settle. `BottomSheet` uses `--motion-slow` (280ms) on the way in and `--motion-base` (180ms) on the way out, with `--motion-ease` and no spring. Both honor Reduce Motion. Aligning them is the only change this review justifies, and it is not done yet.

### Do not adopt

- Replacing the desktop dialog spring (scale 0.5, rotateX 80°, stiffness 260, damping 15) with a smaller Apple-style fade. Reduce Motion already removes that travel. The spring is the Premium dialog.
- Springs on every button, input, or color change.
- Automatic page or shared-element transitions. Premium has no navigation stack that those transitions would explain.
- A second duration scale, or copying Apple’s system timing values.

## Specialized exceptions

- Desktop dialog spring and the gentle sheet settle stay as they are until the sheet alignment above is approved.
- Select menus keep their existing 300ms menu motion and local ease. Reduce Motion sets that animation to none.
- Toast uses a local 200ms ease (`--toast-motion`). It is the same order as `--motion-base` and is not a second product scale. The entrance and exit stay as locked. The right-edge countdown is locked: linear 4000ms, the same clock as dismissal. Reduced motion hides that countdown.
- Opt-in focus draw is 1100ms and linear. It is not the default focus treatment.

## Playground review

The Apple HIG review is `#hig-motion`. Foundations Motion stays at `#motion`. The review is not locked. It mounts the existing dialog, popover, tooltip, menu, sheet, button, soft input, select, segmented control, switch, toast, button spinner, and skeleton. It does not change their durations, easing, or reduced-motion rules. There is still no page transition. Aligning the dialog sheet spring with the picker sheet remains unapproved and is not done here.
