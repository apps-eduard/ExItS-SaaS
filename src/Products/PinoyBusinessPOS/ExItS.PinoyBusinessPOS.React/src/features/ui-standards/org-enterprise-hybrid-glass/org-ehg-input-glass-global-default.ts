/**
 * Org Enterprise Hybrid Glass — Input / Search glass global defaults (LOCKED).
 * Use when creating an input or search field. UI Standards Input glass playground seed = input.
 *
 * Keep `.cursor/rules/exits-org-ehg-input-glass.mdc`,
 * `.cursor/rules/exits-input-default.mdc`,
 * `OrgEhgQaInputGlassPlayground.tsx`, and input-glass CSS in
 * `org-enterprise-hybrid-glass.css` aligned.
 *
 * Input:  ext org-ehg input-glass qa primary stacked md icon-none shape-standard no-shadow
 * Search: ext org-ehg input-glass qa primary stacked md icon-bare shape-standard no-shadow
 */

export const ORG_EHG_INPUT_GLASS_DEFAULT_COMMAND =
  "ext org-ehg input-glass qa primary stacked md icon-none shape-standard no-shadow" as const;

export const ORG_EHG_SEARCH_GLASS_DEFAULT_COMMAND =
  "ext org-ehg input-glass qa primary stacked md icon-bare shape-standard no-shadow" as const;

export const ORG_EHG_INPUT_GLASS_TITLE = "Input glass · Primary stacked" as const;
export const ORG_EHG_SEARCH_GLASS_TITLE = "Search glass · Primary stacked bare" as const;

/** Preferences / QA tone. */
export const ORG_EHG_INPUT_GLASS_DEFAULT_TONE = "primary" as const;

/** Label above (stacked) appearance. */
export const ORG_EHG_INPUT_GLASS_DEFAULT_APPEARANCE = "stacked" as const;

/** Standard field size. */
export const ORG_EHG_INPUT_GLASS_DEFAULT_SIZE = "md" as const;

/** Input: no leading icon. */
export const ORG_EHG_INPUT_GLASS_DEFAULT_ICON = "icon-none" as const;

/** Search: bare glyph (search mark), no chip fill. */
export const ORG_EHG_SEARCH_GLASS_DEFAULT_ICON = "icon-bare" as const;

/** Search glyph id when creating a search field. */
export const ORG_EHG_SEARCH_GLASS_DEFAULT_ICON_ID = "search" as const;

/** Card / field shape — Standard (middle of the shape ladder). */
export const ORG_EHG_INPUT_GLASS_DEFAULT_SHAPE = "standard" as const;

/** No elevation shadow on the field shell. */
export const ORG_EHG_INPUT_GLASS_DEFAULT_SHADOW = "no-shadow" as const;

/** Soft control field radius — same Soft control as Org EHG QA / Soft Glass fields. */
export const ORG_EHG_INPUT_GLASS_RADIUS = "0.6875rem" as const;
