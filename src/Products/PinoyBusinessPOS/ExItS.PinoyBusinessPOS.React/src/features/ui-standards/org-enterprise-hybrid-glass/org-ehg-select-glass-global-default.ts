/**
 * Org Enterprise Hybrid Glass — Select glass global default (LOCKED).
 * Use when creating a select. UI Standards Select glass playground seed.
 *
 * Keep `.cursor/rules/exits-org-ehg-select-glass.mdc`,
 * `.cursor/rules/exits-select-default.mdc`,
 * `OrgEhgQaSelectGlassPlayground.tsx`, and select-glass CSS in
 * `org-enterprise-hybrid-glass.css` aligned.
 *
 * Command: ext org-ehg select-glass qa primary stacked md icon-none shape-standard no-shadow
 */

export const ORG_EHG_SELECT_GLASS_DEFAULT_COMMAND =
  "ext org-ehg select-glass qa primary stacked md icon-none shape-standard no-shadow" as const;

export const ORG_EHG_SELECT_GLASS_TITLE = "Select glass · Primary stacked" as const;

/** Preferences / QA tone. */
export const ORG_EHG_SELECT_GLASS_DEFAULT_TONE = "primary" as const;

/** Label above (stacked) appearance. */
export const ORG_EHG_SELECT_GLASS_DEFAULT_APPEARANCE = "stacked" as const;

/** Standard field size. */
export const ORG_EHG_SELECT_GLASS_DEFAULT_SIZE = "md" as const;

/** No leading icon. */
export const ORG_EHG_SELECT_GLASS_DEFAULT_ICON = "icon-none" as const;

/** Card / field shape — Standard (middle of the shape ladder). */
export const ORG_EHG_SELECT_GLASS_DEFAULT_SHAPE = "standard" as const;

/** No elevation shadow on the field shell. */
export const ORG_EHG_SELECT_GLASS_DEFAULT_SHADOW = "no-shadow" as const;

/** Soft control field radius — same Soft control as Org EHG QA / Soft Glass fields. */
export const ORG_EHG_SELECT_GLASS_RADIUS = "0.6875rem" as const;

/** Default menu transition (Scale) — omitted from the locked command when default. */
export const ORG_EHG_SELECT_GLASS_DEFAULT_MENU_TRANSITION = "scale" as const;
