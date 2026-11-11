export const INPUT_APPEARANCES = ["outlined", "soft", "stacked"] as const;

export type InputAppearance = (typeof INPUT_APPEARANCES)[number];

/** Shipped Premium field: label above the bordered control. Other appearances are opt-in. */
export const INPUT_APPEARANCE_DEFAULT: InputAppearance = "stacked";

export function isInputAppearance(value: string): value is InputAppearance {
  return (INPUT_APPEARANCES as readonly string[]).includes(value);
}
