import type { StatusChipAppearance, StatusChipShape, StatusChipTone } from "@/components/exits/StatusChip";
import type { DashboardMetricTone } from "@/features/reports/DashboardMetricCards";

/**
 * Locked Personal ownership colors — keep Home, Utang hub, and list tags familiar.
 *
 * | Ownership        | Metric card tone | Ownership chip |
 * |------------------|------------------|----------------|
 * | My record        | emphasis         | primary        |
 * | Shared info      | shared           | info           |
 * | Read only (tag)  | —                | warning        |
 *
 * All ownership/read-only chips use emphasis + square.
 */
export const UTANG_OWNERSHIP_CHIP = {
  appearance: "emphasis" as StatusChipAppearance,
  shape: "square" as StatusChipShape,
} as const;

export const UTANG_OWNERSHIP_MINE = {
  metricTone: "emphasis" as DashboardMetricTone,
  chipTone: "primary" as StatusChipTone,
  ...UTANG_OWNERSHIP_CHIP,
} as const;

export const UTANG_OWNERSHIP_SHARED = {
  metricTone: "shared" as DashboardMetricTone,
  chipTone: "info" as StatusChipTone,
  ...UTANG_OWNERSHIP_CHIP,
} as const;

export const UTANG_READ_ONLY_CHIP = {
  tone: "warning" as StatusChipTone,
  ...UTANG_OWNERSHIP_CHIP,
} as const;
