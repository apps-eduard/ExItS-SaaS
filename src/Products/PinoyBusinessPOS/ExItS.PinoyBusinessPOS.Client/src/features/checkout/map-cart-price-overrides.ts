import type { SalePriceOverrideIntentRequest } from "@/api/pos/pos-sales-client";
import type { SessionCartLine } from "@/cart/SessionCartProvider";

/**
 * Build quote/checkout PriceOverrides intents from cart lines (1-based line numbers).
 * Catalog UnitPrice stays on the line as baseline; override never rewrites Today's Price.
 */
export function mapCartPriceOverridesToRequest(
  lines: SessionCartLine[],
): SalePriceOverrideIntentRequest[] {
  const intents: SalePriceOverrideIntentRequest[] = [];
  lines.forEach((line, index) => {
    const override = line.priceOverride;
    if (!override) {
      return;
    }
    intents.push({
      requestedUnitPrice: override.requestedUnitPrice,
      reason: override.reason,
      lineNumber: index + 1,
      productId: line.productId,
      expectedBaselineUnitPrice: override.expectedBaselineUnitPrice,
    });
  });
  return intents;
}

/** Inclusive 100% manager ceiling — mirrors SalePriceOverrideRules.ExceedsManagerLimit. */
export function exceedsManagerSalePriceLimit(
  baselineUnitPrice: number,
  requestedUnitPrice: number,
): boolean {
  if (!(baselineUnitPrice > 0)) {
    return true;
  }
  const deviation = Math.abs(requestedUnitPrice - baselineUnitPrice) / baselineUnitPrice;
  return deviation > 1;
}
