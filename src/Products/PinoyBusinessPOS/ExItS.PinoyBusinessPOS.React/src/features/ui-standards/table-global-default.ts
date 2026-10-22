/**
 * Locked by the Table playground default standard sample / Cursor shorthand.
 * Change only when the user asks. Keep `.cursor/rules/exits-table-default.mdc`
 * and Soft Glass + glossy-raise CSS (globals.css + ui-standards.css) aligned.
 *
 * Command: ext table soft-glass header-glossy-raise footer-glossy-raise search sort
 */
export const TABLE_GLOBAL_DEFAULT_INTENT = "standard" as const;
export const TABLE_GLOBAL_DEFAULT_SHELL = "soft-glass" as const;
export const TABLE_GLOBAL_DEFAULT_HEADER_INTENT = "standard" as const;
export const TABLE_GLOBAL_DEFAULT_HEADER_APPEARANCE = "glossy-raise" as const;
export const TABLE_GLOBAL_DEFAULT_FOOTER_INTENT = "standard" as const;
export const TABLE_GLOBAL_DEFAULT_FOOTER_APPEARANCE = "glossy-raise" as const;
export const TABLE_GLOBAL_DEFAULT_SEARCH = true as const;
/**
 * LOCKED: sort is ON by default for data columns.
 * `ExitsTableHead` resolves sortable from this unless overridden.
 * Actions (`colSize="actions"`) and checkbox columns stay off.
 * Change only when the user asks. Keep `exits-table-default.mdc` aligned.
 */
export const TABLE_GLOBAL_DEFAULT_SORT = true as const;

/** Soft Glass shell radius (theme primary wash). */
export const TABLE_GLOBAL_SOFT_GLASS_RADIUS = "1.1rem" as const;

/**
 * Soft Glass toolbar search field radius (not Control Shape).
 * Keep CSS aligned: `.exits-table-container[data-shell="soft-glass"] …__search .ui-standards-qinput__box`
 * and Money Soft Glass search (`.utang-money-table …`).
 */
export const TABLE_GLOBAL_SOFT_GLASS_SEARCH_RADIUS = "0.75rem" as const;

/**
 * Soft Glass empty state radius (inside Soft Glass table / Soft Glass card).
 * Keep CSS aligned: `[data-shell="soft-glass"] .exits-empty-state` and
 * `.exits-empty-state--soft-glass`.
 */
export const TABLE_GLOBAL_SOFT_GLASS_EMPTY_RADIUS = "0.75rem" as const;

/** Cursor shorthand for a new standard table. */
export const TABLE_GLOBAL_DEFAULT_COMMAND =
  "ext table soft-glass header-glossy-raise footer-glossy-raise search sort" as const;

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
