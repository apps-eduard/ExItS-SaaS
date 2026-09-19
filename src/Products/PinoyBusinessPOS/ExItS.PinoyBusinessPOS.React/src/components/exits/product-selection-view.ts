import type { ResponsiveColumnMeta } from "@/components/exits/responsive-data-view";
import { RESPONSIVE_DATA_TABLE_MIN_MD } from "@/components/exits/responsive-data-view";

/** Few-column product pickers — table from md; list/cards on phone. */
export const PRODUCT_SELECTION_TABLE_MIN_PX = RESPONSIVE_DATA_TABLE_MIN_MD;

/** Receive Stock find-products column priorities. */
export const RECEIVE_FIND_PRODUCT_COLUMNS: readonly ResponsiveColumnMeta[] = [
  { id: "product", label: "Product", priority: "primary" },
  { id: "category", label: "Category", priority: "secondary" },
  { id: "tracking", label: "Inventory tracking", priority: "status" },
  { id: "actions", label: "Action", priority: "hiddenOnCompact" },
] as const;

/** Create PO linked-ordering column priorities. SKU is under product name (not a column). */
export const PO_LINKED_PRODUCT_COLUMNS: readonly ResponsiveColumnMeta[] = [
  { id: "product", label: "Product", priority: "primary" },
  { id: "category", label: "Category", priority: "secondary" },
  { id: "stock", label: "Stock", priority: "status" },
  { id: "price", label: "Price", priority: "metric" },
  { id: "actions", label: "Qty", priority: "hiddenOnCompact" },
] as const;

/** Branch Transfer product picker column priorities. */
export const TRANSFER_FIND_PRODUCT_COLUMNS: readonly ResponsiveColumnMeta[] = [
  { id: "product", label: "Product", priority: "primary" },
  { id: "sku", label: "SKU", priority: "secondary" },
  { id: "category", label: "Category", priority: "secondary" },
  { id: "available", label: "Available at source", priority: "status" },
  { id: "unit", label: "Unit", priority: "secondary" },
  { id: "actions", label: "Action", priority: "hiddenOnCompact" },
] as const;
