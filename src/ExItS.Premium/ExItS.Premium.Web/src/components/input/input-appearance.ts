export const INPUT_APPEARANCES = ["outlined", "soft", "standard", "floating"] as const;

export type InputAppearance = (typeof INPUT_APPEARANCES)[number];

/** Shipped Premium field: label above the bordered control. Other appearances are opt-in. */
export const INPUT_APPEARANCE_DEFAULT: InputAppearance = "standard";

export function inputRestsOnBorder(appearance: InputAppearance) {
  return appearance === "outlined" || appearance === "soft";
}

export function isInputAppearance(value: string): value is InputAppearance {
  return (INPUT_APPEARANCES as readonly string[]).includes(value);
}
