/**
 * Soft Glass · Primary, global default.
 * Showcase card under the Input playground: glass input, glass select, glass search.
 * Color follows Preferences primary. Change only when the user asks.
 *
 * Keep `.cursor/rules/exits-soft-glass-primary-fields.mdc` and
 * `.ui-standards-soft-glass-primary-fields` in `ui-standards.css` aligned.
 *
 * Command: ext soft-glass primary global-default
 */
export const SOFT_GLASS_PRIMARY_FIELDS_COMMAND = "ext soft-glass primary global-default" as const;

export const SOFT_GLASS_PRIMARY_FIELDS_TITLE = "Soft Glass · Primary, global default" as const;

/** Preferences primary. Do not hard-code the sample blue. */
export const SOFT_GLASS_PRIMARY_FIELDS_COLOR = "var(--exits-primary)" as const;

/** Same width on the input, select, and search. */
export const SOFT_GLASS_PRIMARY_FIELDS_WIDTH = "18rem" as const;

/**
 * Raised label and bottom bar. Same speed and position as Add local person
 * (`people-add-dialog--soft-glass`): flush start, no surface chip.
 */
export const SOFT_GLASS_PRIMARY_FIELDS_LABEL_DURATION = "0.45s" as const;
export const SOFT_GLASS_PRIMARY_FIELDS_LABEL_LIFT = "calc(-50% - 0.85rem)" as const;

/** Select menu. Hover is Soft Glass. Selected row has a thick primary start edge. */
export const SOFT_GLASS_PRIMARY_FIELDS_MENU_BORDER = "2px" as const;
export const SOFT_GLASS_PRIMARY_FIELDS_OPTION_RADIUS = "3px" as const;
export const SOFT_GLASS_PRIMARY_FIELDS_SELECTED_EDGE = "3px" as const;

/** The glass input shows the locked primary clear mark when it has a value. */
export const SOFT_GLASS_PRIMARY_FIELDS_INPUT_CLEARABLE = true as const;
