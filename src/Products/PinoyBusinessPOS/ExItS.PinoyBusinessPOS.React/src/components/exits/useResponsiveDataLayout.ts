import { useMediaMin } from "@/hooks/useMediaQuery";
import {
  RESPONSIVE_DATA_TABLE_MIN_LG,
  resolveResponsiveDataLayout,
  type ResponsiveDataLayout,
  type ResponsiveDataStrategy,
} from "@/components/exits/responsive-data-view";

export type UseResponsiveDataLayoutOptions = {
  strategy?: ResponsiveDataStrategy;
  /** Default 1024 (lg). Use 768 for simple few-column tables. */
  tableMinWidthPx?: number;
  /** Explicit exception — keep table + horizontal scroll when narrow. */
  allowHorizontalScroll?: boolean;
  /**
   * Preview / demo only: resolve layout as if the viewport were this wide.
   * Production callers omit this — layout follows the real browser viewport.
   */
  layoutWidthPx?: number;
};

/**
 * Resolves TABLE vs LIST using the shared Control Shape–style breakpoint helper.
 * Does not duplicate Preferences state — only viewport (+ page strategy flags).
 */
export function useResponsiveDataLayout(
  options: UseResponsiveDataLayoutOptions = {},
): {
  layout: ResponsiveDataLayout;
  isTable: boolean;
  isList: boolean;
  tableMinWidthPx: number;
  /** Width used for resolution (override or undefined when using live media). */
  layoutWidthPx?: number;
} {
  const {
    strategy = "auto",
    tableMinWidthPx = RESPONSIVE_DATA_TABLE_MIN_LG,
    allowHorizontalScroll = false,
    layoutWidthPx,
  } = options;
  const mediaWideEnough = useMediaMin(tableMinWidthPx);
  const isWideEnoughForTable =
    layoutWidthPx !== undefined ? layoutWidthPx >= tableMinWidthPx : mediaWideEnough;
  const layout = resolveResponsiveDataLayout({
    strategy,
    isWideEnoughForTable,
    allowHorizontalScroll,
  });
  return {
    layout,
    isTable: layout === "table",
    isList: layout === "list",
    tableMinWidthPx,
    layoutWidthPx,
  };
}
