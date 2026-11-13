import {
  TOAST_DEFAULT_POSITION,
  type ToastPosition,
  type ToastTone,
} from "../../components/toast/ToastProvider";

/** Compact Cursor recipe for the Premium Toast playground (`ext toast …`). */
export function toastCursorCommand({
  tone = "info",
  position = TOAST_DEFAULT_POSITION,
  description = false,
}: {
  tone?: ToastTone;
  position?: ToastPosition;
  description?: boolean;
} = {}) {
  const parts = ["ext toast"];
  if (tone !== "info") parts.push(tone);
  if (position !== TOAST_DEFAULT_POSITION) parts.push(position);
  if (description) parts.push("description");
  return parts.join(" ");
}
