import type { MessageKey } from "@/i18n/messages";

export type ProductBusinessUsage = "Resale" | "Ingredient" | "InternalUse" | "ProducedItem";

export const PRODUCT_BUSINESS_USAGES: readonly ProductBusinessUsage[] = [
  "Resale",
  "Ingredient",
  "InternalUse",
  "ProducedItem",
] as const;

export type ProductCapabilityFlags = {
  canBeSold: boolean;
  canBeUsedAsIngredient: boolean;
  isProduced: boolean;
};

export function capabilitiesFromProduct(product: {
  canBeSold?: boolean | null;
  canBeUsedAsIngredient?: boolean | null;
  isProduced?: boolean | null;
  businessUsage?: string | null;
  usagePreset?: string | null;
}): ProductCapabilityFlags {
  const hasExplicitFlags =
    product.canBeSold != null ||
    product.canBeUsedAsIngredient != null ||
    product.isProduced != null;

  if (hasExplicitFlags) {
    return {
      canBeSold: product.canBeSold === true,
      canBeUsedAsIngredient:
        product.canBeUsedAsIngredient === true ||
        product.usagePreset === "IngredientAndSellable",
      isProduced: product.isProduced === true,
    };
  }

  const usage = resolveBusinessUsage(product);
  return {
    canBeSold: usage === "Resale" || usage === "ProducedItem",
    canBeUsedAsIngredient:
      usage === "Ingredient" || product.usagePreset === "IngredientAndSellable",
    isProduced: usage === "ProducedItem",
  };
}

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
      product.usagePreset === "Ingredient" ||
      product.usagePreset === "IngredientAndSellable"
    ) {
      return "Ingredient";
    }
    return "InternalUse";
  }

  // Sellable + ingredient stays Resale for primary label; ingredient flag is independent.
  return "Resale";
}

/** Catalog list filter that allows overlapping sell + ingredient + produced flags. */
export function matchesBusinessUsageFilter(
  product: {
    businessUsage?: string | null;
    canBeSold?: boolean | null;
    canBeUsedAsIngredient?: boolean | null;
    isProduced?: boolean | null;
    usagePreset?: string | null;
  },
  filter: ProductBusinessUsage | "all",
): boolean {
  if (filter === "all") {
    return true;
  }
  const caps = capabilitiesFromProduct(product);
  switch (filter) {
    case "Resale":
      return caps.canBeSold && !caps.isProduced;
    case "Ingredient":
      return caps.canBeUsedAsIngredient;
    case "ProducedItem":
      return caps.isProduced;
    case "InternalUse":
      return !caps.canBeSold && !caps.canBeUsedAsIngredient && !caps.isProduced;
    default:
      return resolveBusinessUsage(product) === filter;
  }
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

/** Sell floor eligibility is CanBeSold — ingredient flag must not remove sellable products. */
export function isSellFloorCapable(product: {
  canBeSold?: boolean | null;
  businessUsage?: string | null;
  isProduced?: boolean | null;
  usagePreset?: string | null;
}): boolean {
  if (product.canBeSold === true) {
    return true;
  }
  if (product.canBeSold === false) {
    return false;
  }
  return isSellFloorBusinessUsage(resolveBusinessUsage(product));
}

/** Resale and produced items remain eligible for the Sell floor. */
export function isSellFloorBusinessUsage(usage: ProductBusinessUsage): boolean {
  return usage === "Resale" || usage === "ProducedItem";
}

export function capabilitiesAreValid(flags: ProductCapabilityFlags): boolean {
  return flags.canBeSold || flags.canBeUsedAsIngredient || flags.isProduced;
}
