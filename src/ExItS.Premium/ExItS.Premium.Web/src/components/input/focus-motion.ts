export const INPUT_FOCUS_MOTIONS = ["none", "border-reveal", "soft-glow", "running-accent", "focus-draw"] as const;

export type InputFocusMotion = (typeof INPUT_FOCUS_MOTIONS)[number];

/** Shipped focus treatment. Experimental variants stay opt-in until one is approved. */
export const INPUT_FOCUS_MOTION_DEFAULT: InputFocusMotion = "none";

/** Border reveal travel. Same even pace as Focus Draw, with no fade-in wait. */
export const INPUT_FOCUS_REVEAL_MS = 1100;

/** One lap of the running accent. It does not repeat while the field stays focused. */
export const INPUT_FOCUS_TRACE_MS = 680;

/** Signature draw from the bottom center to the top center. Starts on focus, with no delay. */
export const INPUT_FOCUS_DRAW_MS = 1100;

/** Idle return. A short fade, not a reverse trip around the border. */
export const INPUT_FOCUS_DRAW_RELEASE_MS = 120;

/** Even travel. Starts immediately, with no slow ease-in and no late rush. */
export const INPUT_FOCUS_DRAW_EASING = "linear";

/** Accent thickness. The path is inset by half of this so the stroke stays inside the border box. */
export const INPUT_FOCUS_DRAW_STROKE = 2.5;

export const INPUT_FOCUS_MOTION_OPTIONS: Array<{
  id: Exclude<InputFocusMotion, "none">;
  title: string;
  detail: string;
}> = [
  {
    id: "border-reveal",
    title: "Animated Border Reveal",
    detail: "On focus, one stroke starts at the middle of the top and follows the right border to the bottom. The other starts at the middle of the bottom and follows the left border to the top. Both hold, then clear on blur.",
  },
  {
    id: "soft-glow",
    title: "Soft Focus Glow",
    detail: "The border eases to the active primary, with a tight low-opacity halo. The 2px focus stroke stays visible when the halo is faint. No pulse and no background blur.",
  },
  {
    id: "focus-draw",
    title: "ExItS Focus Draw",
    detail: "A solid stroke starts at the bottom center the moment the field is focused. Two strokes follow the rounded border and meet at the top center in 1100ms, then hold. Typing does not restart it.",
  },
];

export function isInputFocusMotion(value: string | undefined): value is InputFocusMotion {
  return !!value && (INPUT_FOCUS_MOTIONS as readonly string[]).includes(value);
}
