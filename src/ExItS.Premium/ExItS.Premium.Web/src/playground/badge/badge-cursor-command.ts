import type { BadgeAppearance, BadgeTone } from "../../components/badge/Badge";

export type BadgeCursorCommandOptions = {
  tone: BadgeTone;
  appearance: BadgeAppearance;
  allCaps: boolean;
  indicator: boolean;
};

/** Compact Cursor recipe for the Premium Badge playground (`ext badge …`). */
export function badgeCursorCommand({ tone, appearance, allCaps, indicator }: BadgeCursorCommandOptions) {
  const parts = ["ext badge"];
  if (tone !== "neutral") parts.push(tone);
  if (appearance !== "soft") parts.push(appearance);
  if (allCaps) parts.push("all-caps");
  if (indicator) parts.push("indicator");
  return parts.join(" ");
}
