import type { ResponsiveColumnMeta } from "@/components/exits/responsive-data-view";
import { RESPONSIVE_DATA_TABLE_MIN_LG } from "@/components/exits/responsive-data-view";
import {
  PRODUCT_SELECTION_TABLE_MIN_PX,
  RECEIVE_FIND_PRODUCT_COLUMNS,
} from "@/components/exits/product-selection-view";

/** Multi-column receipt lines — table from lg; list/cards below. */
export const RECEIVE_STOCK_RECEIPT_TABLE_MIN_PX = RESPONSIVE_DATA_TABLE_MIN_LG;

/** Few-column product picker — shared product selection breakpoint (md). */
export const RECEIVE_STOCK_FIND_PRODUCTS_TABLE_MIN_PX = PRODUCT_SELECTION_TABLE_MIN_PX;

export const RECEIVE_STOCK_RECEIPT_COLUMNS: readonly ResponsiveColumnMeta[] = [
  { id: "product", label: "Product", priority: "primary" },
  { id: "qty", label: "Qty", priority: "metric", mobileLabel: "Qty" },
  { id: "cost", label: "Cost", priority: "metric" },
  { id: "selling", label: "Selling", priority: "metric", mobileLabel: "Selling price" },
  { id: "lineTotal", label: "Line total", priority: "metric" },
  { id: "expiry", label: "Expiry", priority: "detail", mobileLabel: "Expiry date" },
  { id: "lot", label: "Batch / lot", priority: "detail", mobileLabel: "Lot number" },
  { id: "actions", label: "Action", priority: "hiddenOnCompact" },
] as const;

export const RECEIVE_STOCK_FIND_PRODUCT_COLUMNS = RECEIVE_FIND_PRODUCT_COLUMNS;
