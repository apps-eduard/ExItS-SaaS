# Motion standards

Tokens live in `src/styles/tokens.scss`.

- Fast: 120ms
- Base: 180ms
- Slow: 280ms
- Ease: `cubic-bezier(0.2, 0.8, 0.2, 1)`
- Spring for entrance: stiffness 420, damping 32 (`src/motion/motion.ts`)

Use transform and opacity. Theme color changes are instant.

`prefers-reduced-motion: reduce` sets the durations to 1ms and removes dialog, sheet, menu, tab, and skeleton animation. `MotionConfig reducedMotion="user"` does the same for Motion for React. Do not override that preference in product code.

Do not animate an action so the user has to wait before continuing.
