/**
 * Org Enterprise Hybrid Glass — Date glass global default (LOCKED).
 * UI Standards Date glass playground copy-command lock.
 *
 * Keep `.cursor/rules/exits-org-ehg-date-glass.mdc`,
 * `OrgEhgDateGlassField.tsx`, `OrgEhgDateGlassPlayground.tsx`,
 * and Quasar time roll CSS in `org-enterprise-hybrid-glass.css` aligned.
 *
 * Command: ext org-ehg date-glass quasar medium datetime md popover focus-thick-below
 */

export const ORG_EHG_DATE_GLASS_COMMAND =
  "ext org-ehg date-glass quasar medium datetime md popover focus-thick-below" as const;

export const ORG_EHG_DATE_GLASS_TITLE = "Date glass · Quasar medium datetime" as const;

/** Quasar calendar + vertical glass HH/MM/AM·PM roll (default variety). */
export const ORG_EHG_DATE_GLASS_DEFAULT_VARIETY = "quasar" as const;

/** Date and time glass. */
export const ORG_EHG_DATE_GLASS_DEFAULT_KIND = "datetime" as const;

/** Medium glass intensity (field + popover + time roll). */
export const ORG_EHG_DATE_GLASS_DEFAULT_INTENSITY = "medium" as const;

/** Standard field size. */
export const ORG_EHG_DATE_GLASS_DEFAULT_SIZE = "md" as const;

/** Anchored popover under the field. */
export const ORG_EHG_DATE_GLASS_DEFAULT_SURFACE = "popover" as const;

/** Thick primary bar under the field when open / focused. */
export const ORG_EHG_DATE_GLASS_DEFAULT_FOCUS = "thick-below" as const;

/** Quasar time: 3-value vertical glass roll, no carets; tap above/below; HH/MM typeable. */
export const ORG_EHG_DATE_GLASS_QUASAR_TIME_ROLL = true as const;
export const ORG_EHG_DATE_GLASS_QUASAR_TIME_CARETS = false as const;
export const ORG_EHG_DATE_GLASS_QUASAR_TIME_TYPEABLE_HH_MM = true as const;
export const ORG_EHG_DATE_GLASS_QUASAR_TIME_EQUAL_ROWS = true as const;

/** Quasar footer: Clear / Now|Today / Set. */
export const ORG_EHG_DATE_GLASS_QUASAR_FOOTER = true as const;
