import type { BadgeAppearance, BadgeTone } from "../../components/badge/Badge";

export type BadgeCursorCommandOptions = {
  tone: BadgeTone;
  appearance: BadgeAppearance;
};

/** Compact Cursor recipe for the Premium Badge playground (`ext badge …`). */
export function badgeCursorCommand({ tone, appearance }: BadgeCursorCommandOptions) {
  const parts = ["ext badge"];
  if (tone !== "neutral") parts.push(tone);
  if (appearance !== "soft") parts.push(appearance);
  return parts.join(" ");
}
