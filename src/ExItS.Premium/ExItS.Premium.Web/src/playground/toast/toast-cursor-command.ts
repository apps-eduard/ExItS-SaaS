import type { ToastTone } from "../../components/toast/ToastProvider";

/** Compact Cursor recipe for the Premium Toast playground (`ext toast …`). */
export function toastCursorCommand({
  tone = "info",
  description = false,
}: {
  tone?: ToastTone;
  description?: boolean;
} = {}) {
  const parts = ["ext toast"];
  if (tone !== "info") parts.push(tone);
  if (description) parts.push("description");
  return parts.join(" ");
}
