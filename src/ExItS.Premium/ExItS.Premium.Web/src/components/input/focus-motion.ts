export const INPUT_FOCUS_MOTIONS = ["none", "border-reveal", "soft-glow", "running-accent"] as const;

export type InputFocusMotion = (typeof INPUT_FOCUS_MOTIONS)[number];

/** Shipped focus treatment. Experimental variants stay opt-in until one is approved. */
export const INPUT_FOCUS_MOTION_DEFAULT: InputFocusMotion = "none";

/** Focus ring entrance. Matches the border-reveal and glow transitions. */
export const INPUT_FOCUS_REVEAL_MS = 220;

/** One lap of the running accent. It does not repeat while the field stays focused. */
export const INPUT_FOCUS_TRACE_MS = 680;

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
    id: "running-accent",
    title: "Running Border Accent",
    detail: "A short highlight travels the outline once, following Standard, Soft, and Pill radii, then fades. The steady focus stroke remains. Typing does not restart it.",
  },
];

export function isInputFocusMotion(value: string | undefined): value is InputFocusMotion {
  return !!value && (INPUT_FOCUS_MOTIONS as readonly string[]).includes(value);
}
