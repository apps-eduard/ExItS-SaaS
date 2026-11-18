/**
 * Org Enterprise Hybrid Glass — Button glass global default (LOCKED).
 * Use when creating a button (not Speed dial / FAB — that uses its own seed).
 * UI Standards Button glass playground seed (non-FAB).
 *
 * Keep `.cursor/rules/exits-org-ehg-btn-glass.mdc`,
 * `OrgEhgQaBtnGlassPlayground.tsx`, and btn-glass CSS in
 * `org-enterprise-hybrid-glass.css` aligned.
 *
 * Command: ext org-ehg btn-glass qa primary flat md icon-none shape-standard no-shadow
 *
 * Speed dial / FAB seed stays separate (people · lg · icon-soft · shape-pill · fab · …).
 */

export const ORG_EHG_BTN_GLASS_DEFAULT_COMMAND =
  "ext org-ehg btn-glass qa primary flat md icon-none shape-standard no-shadow" as const;

export const ORG_EHG_BTN_GLASS_TITLE = "Button glass · Primary flat" as const;

/** Preferences / QA tone. */
export const ORG_EHG_BTN_GLASS_DEFAULT_TONE = "primary" as const;

/** Default appearance (playground Appearance select seeds this). */
export const ORG_EHG_BTN_GLASS_DEFAULT_APPEARANCE = "flat" as const;

/** Standard control size. */
export const ORG_EHG_BTN_GLASS_DEFAULT_SIZE = "md" as const;

/** No leading icon. */
export const ORG_EHG_BTN_GLASS_DEFAULT_ICON = "icon-none" as const;

/** Card / button shape — Standard (middle of the shape ladder). */
export const ORG_EHG_BTN_GLASS_DEFAULT_SHAPE = "standard" as const;

/** No elevation shadow on the button shell. */
export const ORG_EHG_BTN_GLASS_DEFAULT_SHADOW = "no-shadow" as const;

/** Soft control radius — same Soft control as Org EHG QA. */
export const ORG_EHG_BTN_GLASS_RADIUS = "0.6875rem" as const;
