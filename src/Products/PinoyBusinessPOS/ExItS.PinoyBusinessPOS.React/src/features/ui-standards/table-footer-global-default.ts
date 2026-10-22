/**
 * Locked table footer chrome (Quasar-style page size + nav carets).
 * Appearance follows `table-global-default.ts` (glossy-raise).
 * Change only when the user asks. Keep CSS and this file aligned.
 */
export const TABLE_FOOTER_GLOBAL_DEFAULT_INTENT = "standard" as const;
export const TABLE_FOOTER_GLOBAL_DEFAULT_APPEARANCE = "glossy-raise" as const;

/** Page-size option row (dense). */
export const TABLE_FOOTER_GLOBAL_OPTION_MIN_HEIGHT = "32px" as const;
export const TABLE_FOOTER_GLOBAL_OPTION_PADDING = "2px 8px" as const;
export const TABLE_FOOTER_GLOBAL_OPTION_MIN_WIDTH = "2.5rem" as const;
export const TABLE_FOOTER_GLOBAL_OPTION_FONT_SIZE = "14px" as const;

/** First / prev / next / last Material Symbol carets. */
export const TABLE_FOOTER_GLOBAL_CARET_SIZE = "1.5rem" as const;

/** Always append Quasar “All” (0) when any ladder size is available. */
export const TABLE_FOOTER_GLOBAL_INCLUDE_ALL = true as const;

/**
 * At this many rows or fewer: hide pagination footer controls
 * (page size + nav) and show a row count instead (`exits-table-pagination--count-only`).
 * Soft Glass / Money / Activity tables without paging still show this count when ≤ threshold.
 */
export const TABLE_FOOTER_GLOBAL_HIDE_CONTROLS_AT_OR_BELOW = 10 as const;

/** Row-count placement when footer controls are hidden. */
export const TABLE_FOOTER_GLOBAL_ROW_COUNT_ALIGN = "start" as const;

/** Row-count weight when footer controls are hidden. */
export const TABLE_FOOTER_GLOBAL_ROW_COUNT_WEIGHT = 700 as const;

/** i18n key for the count label (`{total}` placeholder). */
export const TABLE_FOOTER_GLOBAL_ROW_COUNT_MESSAGE_KEY = "exitsTable.rowCount" as const;

/** True when the glossy pagination footer (page size + nav) should render. */
export function exitsTableShouldShowPaginationFooter(total: number): boolean {
  return total > TABLE_FOOTER_GLOBAL_HIDE_CONTROLS_AT_OR_BELOW;
}

/** True when the start-aligned bold row count should render (1…threshold inclusive). */
export function exitsTableShouldShowRowCount(total: number): boolean {
  return total > 0 && total <= TABLE_FOOTER_GLOBAL_HIDE_CONTROLS_AT_OR_BELOW;
}
