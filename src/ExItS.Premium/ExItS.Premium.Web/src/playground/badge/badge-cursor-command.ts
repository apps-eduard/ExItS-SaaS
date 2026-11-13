import type { BadgeAppearance, BadgeTone } from "../../components/badge/Badge";

export type BadgeCursorCommandOptions = {
  tone: BadgeTone;
  appearance: BadgeAppearance;
  allCaps: boolean;
  indicator: boolean;
  removable: boolean;
  /** Count chips — examples-only recipe (not a playground Properties switch). */
  count?: boolean;
};

/** Compact Cursor recipe for the Premium Badge playground (`ext badge …`). */
export function badgeCursorCommand({
  tone,
  appearance,
  allCaps,
  indicator,
  removable,
  count = false,
}: BadgeCursorCommandOptions) {
  const parts = ["ext badge"];
  if (tone !== "neutral") parts.push(tone);
  if (appearance !== "soft") parts.push(appearance);
  if (allCaps) parts.push("all-caps");
  if (indicator) parts.push("indicator");
  if (removable) parts.push("remove");
  if (count) parts.push("count");
  return parts.join(" ");
}
