export const INPUT_APPEARANCES = ["stacked", "outlined", "soft", "filled"] as const;

export type InputAppearance = (typeof INPUT_APPEARANCES)[number];

/** Shipped Premium field: label above the bordered control. Other appearances are opt-in. */
export const INPUT_APPEARANCE_DEFAULT: InputAppearance = "stacked";

export function isInputAppearance(value: string): value is InputAppearance {
  return (INPUT_APPEARANCES as readonly string[]).includes(value);
}

/**
 * Appearance is the surface. Focus motion is independent.
 * Stacked, outlined, and soft keep Focus Draw and Border Reveal on the real rounded border.
 * Filled keeps those motions on the bottom edge, because that field has no side border.
 */
export function inputFocusFrame(appearance: InputAppearance): "box" | "baseline" {
  return appearance === "filled" ? "baseline" : "box";
}
