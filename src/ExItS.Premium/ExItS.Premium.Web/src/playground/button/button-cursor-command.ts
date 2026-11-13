import type { ButtonAppearance, ButtonVariant } from "../../components/button/Button";

export type ButtonCursorCommandOptions = {
  variant: ButtonVariant;
  appearance: ButtonAppearance;
  icon: string;
  iconOnly: boolean;
  fullWidth: boolean;
  loading: boolean;
  disabled: boolean;
  ripple: boolean;
};

/** Compact Cursor recipe for the Premium Button playground (V1-style `ext btn …`). */
export function buttonCursorCommand({
  variant,
  appearance,
  icon,
  iconOnly,
  fullWidth,
  loading,
  disabled,
  ripple,
}: ButtonCursorCommandOptions) {
  const parts = ["ext btn"];
  if (iconOnly) parts.push("icon");
  if (variant !== "primary" && variant !== "ghost") parts.push(variant);
  if (appearance !== "solid") parts.push(appearance);
  if (icon !== "none") parts.push(icon);
  if (fullWidth) parts.push("full-width");
  if (loading) parts.push("loading");
  if (ripple) parts.push("ripple");
  if (disabled) parts.push("disabled");
  return parts.join(" ");
}
