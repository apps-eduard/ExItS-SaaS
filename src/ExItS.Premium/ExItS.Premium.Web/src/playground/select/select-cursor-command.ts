import type { InputFocusMotion } from "../../components/input/focus-motion";
import type { SelectMenuMotion } from "../../components/select/select-menu-motion";

export type SelectCursorCommandOptions = {
  appearance: "outlined" | "soft" | "stacked";
  focusMotion: InputFocusMotion;
  menuMotion: SelectMenuMotion;
  searchable: boolean;
  multiple: boolean;
};

/** Compact Cursor recipe for the Premium Select playground (`ext dropdown …`). */
export function selectCursorCommand({
  appearance,
  focusMotion,
  menuMotion,
  searchable,
  multiple,
}: SelectCursorCommandOptions) {
  const parts = ["ext dropdown"];
  if (appearance !== "stacked") parts.push(appearance);
  if (focusMotion !== "none") parts.push(focusMotion);
  if (menuMotion !== "scale") parts.push(menuMotion);
  if (searchable) parts.push("searchable");
  if (multiple) parts.push("multi");
  return parts.join(" ");
}
