import { formatQuantityDisplay } from "@/cart/sell-cart-helpers";
import { formatUnitOfMeasureSymbol } from "@/lib/unit-of-measure";

/**
 * Personal / merchant shop cart unit label for the qty × price line.
 * Example display: "Bottle · 1×₱48.00" — not shown inside the QuantityStepper.
 */
export function formatShopCartUnitLabel(unitOfMeasure?: string | null): string {
  const symbol = formatUnitOfMeasureSymbol(unitOfMeasure);
  if (!symbol) {
    return "Pc";
  }
  // Short symbols stay compact (kg, g, L, mL, m, pc).
  if (symbol.length <= 3 && !/^(pack|box|can)$/iu.test(symbol)) {
    if (symbol.toLowerCase() === "pc") {
      return "Pc";
    }
    return symbol;
  }
  return symbol.charAt(0).toUpperCase() + symbol.slice(1);
}

/** `Bottle · 1×₱48.00` */
export function formatShopCartQtyPriceMeta(
  unitOfMeasure: string | null | undefined,
  quantity: number,
  unitPrice: number,
): string {
  const unit = formatShopCartUnitLabel(unitOfMeasure);
  const qty = formatQuantityDisplay(quantity);
  return `${unit} · ${qty}×₱${unitPrice.toFixed(2)}`;
}
