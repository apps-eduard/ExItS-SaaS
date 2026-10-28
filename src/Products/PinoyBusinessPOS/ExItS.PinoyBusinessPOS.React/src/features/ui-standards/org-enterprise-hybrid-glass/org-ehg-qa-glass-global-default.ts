/**
 * Org EHG Quick Action glass — GLOBAL default (LOCKED).
 * Every Quick Action uses this recipe. Canonical reference:
 * Personal home → Stores to pay → Stores tile (light + dark).
 *
 * Keep `.cursor/rules/exits-org-ehg-qa-glass.mdc`, `SoftGlassQuickAction`
 * (`shell="ehg"` default), and `src/styles/org-ehg-qa-glass.css` aligned.
 *
 * Command: ext org-ehg qa-glass md icon-soft card-soft trail-auto no-shadow no-icon-shadow
 */

/** Class string for Link / non-button Quick Action tiles. */
export const ORG_EHG_QA_GLASS_CLASS_NAME =
  "personal-qa-glass--md personal-qa-glass--card-soft personal-qa-glass--no-shadow personal-qa-glass--icon-soft personal-qa-glass--no-icon-shadow" as const;

export const ORG_EHG_QA_GLASS_DEFAULT_COMMAND =
  "ext org-ehg qa-glass md icon-soft card-soft trail-auto no-shadow no-icon-shadow" as const;

/** Home Stores to pay · Stores tile is the visual reference. */
export const ORG_EHG_QA_GLASS_REFERENCE =
  "personal home · stores to pay · stores quick action" as const;

export const ORG_EHG_QA_GLASS_DEFAULT_SIZE = "md" as const;
export const ORG_EHG_QA_GLASS_DEFAULT_ICON = "icon-soft" as const;
export const ORG_EHG_QA_GLASS_DEFAULT_CARD = "card-soft" as const;
export const ORG_EHG_QA_GLASS_DEFAULT_SHADOW = "no-shadow" as const;
export const ORG_EHG_QA_GLASS_DEFAULT_ICON_SHADOW = "no-icon-shadow" as const;

/**
 * Shell radius — Soft control (`--exits-radius-soft`), not Soft Glass 1.1rem.
 */
export const ORG_EHG_QA_GLASS_RADIUS = "0.6875rem" as const;

/** Filled soft icon chip — squircle (Stores to pay metrics / home QA). */
export const ORG_EHG_QA_GLASS_ICON_RADIUS = "0.65rem" as const;
export const ORG_EHG_QA_GLASS_ICON_SIZE = "2rem" as const;

/**
 * Light shell wash (Stores to pay / home EHG QA).
 * Border: chip 28% into edge. Fill: chip 8% into white-ish fill.
 */
export const ORG_EHG_QA_GLASS_LIGHT_CHIP_BORDER_PCT = 28 as const;
export const ORG_EHG_QA_GLASS_LIGHT_CHIP_FILL_PCT = 8 as const;
export const ORG_EHG_QA_GLASS_LIGHT_HOVER_CHIP_BORDER_PCT = 42 as const;
export const ORG_EHG_QA_GLASS_LIGHT_HOVER_CHIP_FILL_PCT = 11 as const;

/**
 * Dark shell wash — light identity chips retained (not muted Soft Glass dark).
 * Ink: #f1f5f9. Fill: chip 12% into soft dark fill.
 */
export const ORG_EHG_QA_GLASS_DARK_INK = "#f1f5f9" as const;
export const ORG_EHG_QA_GLASS_DARK_CHIP_BORDER_PCT = 28 as const;
export const ORG_EHG_QA_GLASS_DARK_CHIP_FILL_PCT = 12 as const;

/**
 * Color policy (LOCKED):
 * - Convert / restyle existing QA → keep its identity color (lent/owe/stores/…).
 * - Create a new QA with no named identity → Preferences primary.
 */
export const ORG_EHG_QA_GLASS_NEW_IDENTITY = "primary" as const;
export const ORG_EHG_QA_GLASS_NEW_COLOR = "var(--exits-primary)" as const;

/** Class tokens applied by SoftGlassQuickAction when shell="ehg". */
export const ORG_EHG_QA_GLASS_CLASS_TOKENS = [
  "personal-qa-glass--md",
  "personal-qa-glass--card-soft",
  "personal-qa-glass--no-shadow",
  "personal-qa-glass--icon-soft",
  "personal-qa-glass--no-icon-shadow",
] as const;
