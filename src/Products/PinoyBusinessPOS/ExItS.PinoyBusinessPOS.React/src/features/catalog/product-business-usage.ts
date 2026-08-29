/** Buyer-owned product business usage (maps to server CanBeSold / usage presets). */
import type { MessageKey } from "@/i18n/messages";

export type ProductBusinessUsage = "Resale" | "Ingredient" | "InternalUse";

export const PRODUCT_BUSINESS_USAGES: readonly ProductBusinessUsage[] = [
  "Resale",
  "Ingredient",
  "InternalUse",
] as const;

export function resolveBusinessUsage(product: {
  businessUsage?: string | null;
  canBeSold?: boolean | null;
  canBeUsedAsIngredient?: boolean | null;
  usagePreset?: string | null;
}): ProductBusinessUsage {
  const raw = product.businessUsage?.trim();
  if (raw === "Resale" || raw === "Ingredient" || raw === "InternalUse") {
    return raw;
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
  }
}
