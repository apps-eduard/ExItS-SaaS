import type { InputAppearance } from "../../components/input/input-appearance";
import type { InputFocusMotion } from "../../components/input/focus-motion";

export type InputCursorCommandOptions = {
  appearance: InputAppearance;
  focusMotion: InputFocusMotion;
  kind: "text" | "search";
  status: "none" | "error" | "success";
  disabled: boolean;
  readOnly: boolean;
  required: boolean;
};

/** Compact Cursor recipe for the Premium Input playground (`ext input …`). */
export function inputCursorCommand({
  appearance,
  focusMotion,
  kind,
  status,
  disabled,
  readOnly,
  required,
}: InputCursorCommandOptions) {
  const parts = ["ext input"];
  if (kind === "search") parts.push("search");
  if (appearance !== "standard") parts.push(appearance);
  if (focusMotion !== "none") parts.push(focusMotion);
  if (status === "error") parts.push("error");
  if (status === "success") parts.push("success");
  if (required) parts.push("required");
  if (readOnly) parts.push("readonly");
  if (disabled) parts.push("disabled");
  return parts.join(" ");
}
