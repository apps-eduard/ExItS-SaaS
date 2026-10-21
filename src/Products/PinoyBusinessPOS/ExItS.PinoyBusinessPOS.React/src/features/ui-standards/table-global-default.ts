/**
 * Locked by the Table playground default standard sample / Cursor shorthand.
 * Change only when the user asks. Keep `.cursor/rules/exits-table-default.mdc`
 * and Soft Glass + glossy-raise CSS (globals.css + ui-standards.css) aligned.
 *
 * Command: ext table soft-glass header-glossy-raise footer-glossy-raise search
 */
export const TABLE_GLOBAL_DEFAULT_INTENT = "standard" as const;
export const TABLE_GLOBAL_DEFAULT_SHELL = "soft-glass" as const;
export const TABLE_GLOBAL_DEFAULT_HEADER_INTENT = "standard" as const;
export const TABLE_GLOBAL_DEFAULT_HEADER_APPEARANCE = "glossy-raise" as const;
export const TABLE_GLOBAL_DEFAULT_FOOTER_INTENT = "standard" as const;
export const TABLE_GLOBAL_DEFAULT_FOOTER_APPEARANCE = "glossy-raise" as const;
export const TABLE_GLOBAL_DEFAULT_SEARCH = true as const;

/** Soft Glass shell radius (theme primary wash). */
export const TABLE_GLOBAL_SOFT_GLASS_RADIUS = "1.1rem" as const;

/** Cursor shorthand for a new standard table. */
export const TABLE_GLOBAL_DEFAULT_COMMAND =
  "ext table soft-glass header-glossy-raise footer-glossy-raise search" as const;

/**
 * Locked glossy-raise header chrome (soft bottom raise + column side edges).
 * Keep CSS in sync: `.exits-table__header[data-appearance="glossy-raise"]` and
 * `.ui-standards-qtable thead[data-appearance="glossy-raise"] th`.
 */
export const TABLE_GLOBAL_HEADER_RAISE_SHADOW = "0 1px 2px rgba(0, 0, 0, 0.08)" as const;
export const TABLE_GLOBAL_HEADER_COL_EDGE_LIGHT =
  "inset 2px 0 3px -1px rgba(255, 255, 255, 0.7)" as const;
export const TABLE_GLOBAL_HEADER_COL_EDGE_DARK =
  "inset -2px 0 3px -1px rgba(0, 0, 0, 0.12)" as const;
