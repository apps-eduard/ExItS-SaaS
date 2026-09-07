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
  // Match backend AwayFromZero rounding to measured quantity scale (3 dp).
  const scaled = baseQuantity * scale;
  return Math.round(scaled * 1000) / 1000;
}

/** Optional max producible from the most limiting ingredient (entered units). */
export function maxProducibleFromStock(args: {
  definitionOutputQuantity: number;
  components: Array<{ quantityEntered: number; available: number | null }>;
}): number | null {
  const { definitionOutputQuantity, components } = args;
  if (!Number.isFinite(definitionOutputQuantity) || definitionOutputQuantity <= 0) {
    return null;
  }
  let maxScale = Number.POSITIVE_INFINITY;
  for (const component of components) {
    if (component.available == null || !Number.isFinite(component.available)) {
      continue;
    }
    if (component.quantityEntered <= 0) {
      continue;
    }
    maxScale = Math.min(maxScale, component.available / component.quantityEntered);
  }
  if (!Number.isFinite(maxScale) || maxScale < 0) {
    return null;
  }
  return scaleProductionQuantity(definitionOutputQuantity, maxScale);
}
