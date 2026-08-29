import type { MessageKey } from "@/i18n/messages";

export const PRODUCTION_DEFINITION_STATUSES = ["Active", "Inactive"] as const;
export type ProductionDefinitionStatusCode =
  (typeof PRODUCTION_DEFINITION_STATUSES)[number];

export const PRODUCTION_RUN_STATUSES = ["Posted", "Voided"] as const;
export type ProductionRunStatusCode = (typeof PRODUCTION_RUN_STATUSES)[number];

export const PRODUCTION_COST_STATUSES = ["Complete", "Partial", "Unavailable"] as const;
export type ProductionCostStatusCode = (typeof PRODUCTION_COST_STATUSES)[number];

export function productionDefinitionStatusLabelKey(status: string): MessageKey {
  switch (status) {
    case "Inactive":
      return "production.setups.status.inactive";
    case "Active":
    default:
      return "production.setups.status.active";
  }
}

export function productionRunStatusLabelKey(status: string): MessageKey {
  switch (status) {
    case "Voided":
      return "production.runs.status.voided";
    case "Posted":
    default:
      return "production.runs.status.posted";
  }
}

export function productionCostStatusLabelKey(status: string): MessageKey {
  switch (status) {
    case "Complete":
      return "production.runs.costComplete";
    case "Partial":
      return "production.runs.costPartial";
    case "Unavailable":
    default:
      return "production.runs.costUnavailable";
  }
}

export function formatProductionDate(iso: string): string {
  const parsed = new Date(iso);
  if (Number.isNaN(parsed.getTime())) {
    return iso;
  }
  return parsed.toLocaleDateString(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
  });
}

/**
 * Scale factor for a produce run: actual output ÷ definition base output.
 * Returns null when definition output is not positive.
 */
export function productionScaleFactor(
  definitionOutputQuantity: number,
  runOutputQuantity: number,
): number | null {
  if (
    !Number.isFinite(definitionOutputQuantity) ||
    !Number.isFinite(runOutputQuantity) ||
    definitionOutputQuantity <= 0
  ) {
    return null;
  }
  return runOutputQuantity / definitionOutputQuantity;
}

export function scaleProductionQuantity(
  baseQuantity: number,
  scale: number,
): number {
  return baseQuantity * scale;
}
