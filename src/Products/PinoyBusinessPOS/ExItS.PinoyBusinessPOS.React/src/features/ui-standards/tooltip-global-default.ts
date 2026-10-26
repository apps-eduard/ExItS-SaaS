/**
 * Locked ExItS tooltip default — Soft Glass surface for icon-only / truncated labels.
 * Change only when the user asks. Keep `.exits-tooltip` CSS aligned.
 */

/** Soft Glass wash uses theme primary. */
export const TOOLTIP_GLOBAL_DEFAULT_SHELL = "soft-glass" as const;

/** Preferred placement for table / toolbar icon actions. */
export const TOOLTIP_GLOBAL_DEFAULT_PLACEMENT = "top" as const;

/** Hover open delay (ms). */
export const TOOLTIP_GLOBAL_HOVER_DELAY_MS = 300 as const;

/** Focus open delay (ms). */
export const TOOLTIP_GLOBAL_FOCUS_DELAY_MS = 40 as const;

/** Gap between trigger and tip body (px) — leaves room for the triangle caret. */
export const TOOLTIP_GLOBAL_GAP_PX = 10 as const;

/** Triangle caret edge length (px). */
export const TOOLTIP_GLOBAL_ARROW_PX = 8 as const;

/** Tip label size — keep `.exits-tooltip` in sync. */
export const TOOLTIP_GLOBAL_FONT_SIZE = "0.75rem" as const;

/** Tip label weight — regular, not bold. */
export const TOOLTIP_GLOBAL_FONT_WEIGHT = 400 as const;

export const TOOLTIP_GLOBAL_DEFAULT_COMMAND = "ext tooltip soft-glass top" as const;
