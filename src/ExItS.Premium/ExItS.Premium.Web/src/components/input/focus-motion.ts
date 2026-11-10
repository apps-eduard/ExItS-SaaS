export const INPUT_FOCUS_MOTIONS = ["none", "border-reveal", "soft-glow", "running-accent", "focus-draw"] as const;

export type InputFocusMotion = (typeof INPUT_FOCUS_MOTIONS)[number];

/** Shipped focus treatment. Experimental variants stay opt-in until one is approved. */
export const INPUT_FOCUS_MOTION_DEFAULT: InputFocusMotion = "none";

/** Focus ring entrance. Matches the border-reveal and glow transitions. */
export const INPUT_FOCUS_REVEAL_MS = 220;

/** One lap of the running accent. It does not repeat while the field stays focused. */
export const INPUT_FOCUS_TRACE_MS = 680;

/** Signature draw from the bottom center to the top center. Starts on focus, with no delay. */
export const INPUT_FOCUS_DRAW_MS = 420;

/** Idle return. A short fade, not a reverse trip around the border. */
export const INPUT_FOCUS_DRAW_RELEASE_MS = 120;

/** Fast at the start, then settles. Avoids a slow ease-in. */
export const INPUT_FOCUS_DRAW_EASING = "cubic-bezier(0.2, 0.9, 0.2, 1)";

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
    detail: "The field keeps a solid border. On focus, a primary stroke draws out from the center in 220ms and then holds. Blur reverses it. Proposed candidate, not the default.",
  },
  {
    id: "soft-glow",
    title: "Soft Focus Glow",
    detail: "The border eases to the active primary, with a tight low-opacity halo. The 2px focus stroke stays visible when the halo is faint. No pulse and no background blur.",
  },
  {
    id: "focus-draw",
    title: "ExItS Focus Draw",
    detail: "A solid stroke starts at the bottom center the moment the field is focused. Two strokes follow the rounded border and meet at the top center in 420ms, then hold. Typing does not restart it.",
  },
];

export function isInputFocusMotion(value: string | undefined): value is InputFocusMotion {
  return !!value && (INPUT_FOCUS_MOTIONS as readonly string[]).includes(value);
}
