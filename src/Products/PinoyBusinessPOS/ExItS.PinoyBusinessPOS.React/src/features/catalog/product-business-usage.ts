/** Buyer-owned product business usage (maps to server CanBeSold / usage presets). */
import type { MessageKey } from "@/i18n/messages";

export type ProductBusinessUsage = "Resale" | "Ingredient" | "InternalUse" | "ProducedItem";

export const PRODUCT_BUSINESS_USAGES: readonly ProductBusinessUsage[] = [
  "Resale",
  "Ingredient",
  "InternalUse",
  "ProducedItem",
] as const;

export function resolveBusinessUsage(product: {
  businessUsage?: string | null;
  canBeSold?: boolean | null;
  canBeUsedAsIngredient?: boolean | null;
  isProduced?: boolean | null;
  usagePreset?: string | null;
}): ProductBusinessUsage {
  const raw = product.businessUsage?.trim();
  if (
    raw === "Resale" ||
    raw === "Ingredient" ||
    raw === "InternalUse" ||
    raw === "ProducedItem"
  ) {
    return raw;
  }
  if (raw === "MadeProduct") {
    return "ProducedItem";
  }

  if (
    product.isProduced === true ||
    product.usagePreset === "MadeProduct" ||
    product.usagePreset === "ProducedItem"
  ) {
    return "ProducedItem";
  }

  if (product.canBeSold === false) {
    if (
      product.canBeUsedAsIngredient === true ||
      product.usagePreset === "Ingredient"
    ) {
      return "Ingredient";
    }
    return "InternalUse";
  }
  return "Resale";
}

export function businessUsageLabelKey(usage: ProductBusinessUsage): MessageKey {
  switch (usage) {
    case "Resale":
      return "catalog.businessUsage.resale";
    case "Ingredient":
      return "catalog.businessUsage.ingredient";
    case "InternalUse":
      return "catalog.businessUsage.internalUse";
    case "ProducedItem":
      return "catalog.businessUsage.producedItem";
  }
}

export function businessUsageHintKey(usage: ProductBusinessUsage): MessageKey {
  switch (usage) {
    case "Resale":
      return "catalog.businessUsage.resaleHint";
    case "Ingredient":
      return "catalog.businessUsage.ingredientHint";
    case "InternalUse":
      return "catalog.businessUsage.internalUseHint";
    case "ProducedItem":
      return "catalog.businessUsage.producedItemHint";
  }
}

/** Resale and produced items remain eligible for the Sell floor. */
export function isSellFloorBusinessUsage(usage: ProductBusinessUsage): boolean {
  return usage === "Resale" || usage === "ProducedItem";
}
