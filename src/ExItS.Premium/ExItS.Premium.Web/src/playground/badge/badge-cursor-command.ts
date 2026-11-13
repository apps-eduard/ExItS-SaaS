import type { BadgeAppearance, BadgeTone } from "../../components/badge/Badge";

export type BadgeCursorCommandOptions = {
  tone: BadgeTone;
  appearance: BadgeAppearance;
  allCaps: boolean;
  indicator: boolean;
  removable: boolean;
};

/** Compact Cursor recipe for the Premium Badge playground (`ext badge …`). */
export function badgeCursorCommand({
  tone,
  appearance,
  allCaps,
  indicator,
  removable,
}: BadgeCursorCommandOptions) {
  const parts = ["ext badge"];
  if (tone !== "neutral") parts.push(tone);
  if (appearance !== "soft") parts.push(appearance);
  if (allCaps) parts.push("all-caps");
  if (indicator) parts.push("indicator");
  if (removable) parts.push("remove");
  return parts.join(" ");
}
