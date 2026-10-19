import {
  INTENT_GLOBAL_INFO,
  INTENT_GLOBAL_SECONDARY,
  INTENT_GLOBAL_SUCCESS,
} from "@/features/ui-standards/intent-color-global-default";

/**
 * Locked table export icons.
 * CSV follows Preferences primary. Excel, PDF, and Print use locked intent fills.
 * The mark is an outlined document or printer with a format badge on a white tile.
 */
export const TABLE_EXPORT_GLOBAL_CSV_INTENT = "primary" as const;
export const TABLE_EXPORT_GLOBAL_EXCEL_INTENT = "secondary" as const;
export const TABLE_EXPORT_GLOBAL_PDF_INTENT = "success" as const;
export const TABLE_EXPORT_GLOBAL_PRINT_INTENT = "info" as const;

export const TABLE_EXPORT_GLOBAL_COLOR = {
  csv: "var(--exits-primary)",
  xlsx: INTENT_GLOBAL_SECONDARY,
  pdf: INTENT_GLOBAL_SUCCESS,
  print: INTENT_GLOBAL_INFO,
} as const;
