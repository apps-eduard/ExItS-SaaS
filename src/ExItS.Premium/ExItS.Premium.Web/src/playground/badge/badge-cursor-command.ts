import type { BadgeTone } from "../../components/badge/Badge";

export type BadgeCursorCommandOptions = {
  tone: BadgeTone;
};

/** Compact Cursor recipe for the Premium Badge playground (`ext badge …`). */
export function badgeCursorCommand({ tone }: BadgeCursorCommandOptions) {
  const parts = ["ext badge"];
  if (tone !== "neutral") parts.push(tone);
  return parts.join(" ");
}
