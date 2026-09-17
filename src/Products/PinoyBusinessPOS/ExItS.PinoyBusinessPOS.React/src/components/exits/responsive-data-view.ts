/**
 * ExItS Responsive Data View — shared types + layout resolution.
 * Pairs with ExitsTable (desktop) and list/record cards (narrow viewports).
 * @see Docs/UI/exits-responsive-data-view-standard.md
 */

/** Presentation mode for collection data. */
export type ResponsiveDataLayout = "table" | "list";

/**
 * Page strategy:
 * - auto: table when viewport ≥ tableMinWidthPx, else list
 * - table: always table (use with allowHorizontalScroll only when justified)
 * - list: always list/card records
 */
export type ResponsiveDataStrategy = "auto" | "table" | "list";

/**
 * Column importance for list/card mapping.
 * Soft appearance ≠ Soft shape — this is layout priority, not chip fill.
 */
export type ResponsiveColumnPriority =
  | "primary"
  | "secondary"
  | "metric"
  | "status"
  | "detail"
  | "hiddenOnCompact";

export type ResponsiveColumnMeta = {
  /** Stable column id (page-owned). */
  id: string;
  /** Desktop header / accessible label. */
  label: string;
  priority: ResponsiveColumnPriority;
  /** Optional shorter label in list/card key-value rows. */
  mobileLabel?: string;
};

/** Comfortable table breakpoint (lg) — default for multi-column business tables. */
export const RESPONSIVE_DATA_TABLE_MIN_LG = 1024;

/** Simple / few-column tables may keep TABLE from md. */
export const RESPONSIVE_DATA_TABLE_MIN_MD = 768;

export type ResolveResponsiveDataLayoutArgs = {
  strategy?: ResponsiveDataStrategy;
  /** True when `min-width: tableMinWidthPx` matches. */
  isWideEnoughForTable: boolean;
  /**
   * Exception mode: keep TABLE even when narrow (caller must enable overflow-x).
   * Default global behavior never opts into this.
   */
  allowHorizontalScroll?: boolean;
};

export function resolveResponsiveDataLayout({
  strategy = "auto",
  isWideEnoughForTable,
  allowHorizontalScroll = false,
}: ResolveResponsiveDataLayoutArgs): ResponsiveDataLayout {
  if (strategy === "list") {
    return "list";
  }
  if (strategy === "table" || allowHorizontalScroll) {
    return "table";
  }
  return isWideEnoughForTable ? "table" : "list";
}

/** Multi-select is allowed on TABLE layout only by default. */
export function responsiveDataMultiSelectAllowed(
  layout: ResponsiveDataLayout,
  pageOptInOnList = false,
): boolean {
  if (layout === "table") {
    return true;
  }
  return pageOptInOnList;
}

export function pickColumnsByPriority(
  columns: readonly ResponsiveColumnMeta[],
  priorities: readonly ResponsiveColumnPriority[],
): ResponsiveColumnMeta[] {
  const wanted = new Set(priorities);
  return columns.filter((c) => wanted.has(c.priority));
}
