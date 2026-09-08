import { describe, expect, it } from "vitest";

/**
 * Pure UX rules for ingredient ↔ inventory tracking (mirrors CatalogProductFormPage).
 */
function nextTrackStockWhenIngredientToggled(args: {
  canBeUsedAsIngredient: boolean;
  trackStockQuantity: boolean;
}): { trackStockQuantity: boolean; locked: boolean } {
  if (args.canBeUsedAsIngredient) {
    return { trackStockQuantity: true, locked: true };
  }
  return { trackStockQuantity: args.trackStockQuantity, locked: false };
}

function tryDisableTracking(args: {
  canBeUsedAsIngredient: boolean;
  nextTracked: boolean;
}): { ok: true; trackStockQuantity: boolean } | { ok: false; messageKey: string } {
  if (!args.nextTracked && args.canBeUsedAsIngredient) {
    return { ok: false, messageKey: "catalog.ingredientRequiresTrackedInventory" };
  }
  return { ok: true, trackStockQuantity: args.nextTracked };
}

function afterDisableIngredient(args: {
  trackStockQuantity: boolean;
}): { trackStockQuantity: boolean } {
  // Disabling ingredient must not auto-disable tracking.
  return { trackStockQuantity: args.trackStockQuantity };
}

describe("ingredient requires tracked inventory UX", () => {
  it("checking ingredient auto-enables and locks tracking", () => {
    const next = nextTrackStockWhenIngredientToggled({
      canBeUsedAsIngredient: true,
      trackStockQuantity: false,
    });
    expect(next.trackStockQuantity).toBe(true);
    expect(next.locked).toBe(true);
  });

  it("tracking cannot be disabled while ingredient checked", () => {
    const blocked = tryDisableTracking({
      canBeUsedAsIngredient: true,
      nextTracked: false,
    });
    expect(blocked).toEqual({
      ok: false,
      messageKey: "catalog.ingredientRequiresTrackedInventory",
    });
  });

  it("disabling ingredient leaves tracking unchanged", () => {
    expect(afterDisableIngredient({ trackStockQuantity: true })).toEqual({
      trackStockQuantity: true,
    });
  });

  it("non-ingredient may leave tracking off", () => {
    const next = nextTrackStockWhenIngredientToggled({
      canBeUsedAsIngredient: false,
      trackStockQuantity: false,
    });
    expect(next.trackStockQuantity).toBe(false);
    expect(next.locked).toBe(false);
  });
});
