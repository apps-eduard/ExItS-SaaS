import { formatQuantityDisplay, type SellCardStock } from "@/cart/sell-cart-helpers";
import type { MessageKey } from "@/i18n/messages";

export function catalogStockCaption(t: (key: MessageKey) => string, stock: SellCardStock): string {
  if (stock.tone === "untracked") {
    return t("catalog.stockNotTracked");
  }

  if (stock.tone === "out") {
    return t("catalog.stockOut");
  }

  if (stock.tone === "low") {
    return t("catalog.stockLow").replace("{qty}", formatQuantityDisplay(stock.quantity));
  }

  return t("catalog.stockInStock").replace("{qty}", formatQuantityDisplay(stock.quantity));
}

export function sellAvailableCaption(t: (key: MessageKey) => string, stock: SellCardStock): string {
  if (stock.tone === "untracked") {
    return t("catalog.stockNotTracked");
  }

  if (stock.tone === "out") {
    return t("sell.stockOut");
  }

  const unit = stock.unitOfMeasure.trim();
  const quantityLine =
    unit.localeCompare("Piece", undefined, { sensitivity: "accent" }) === 0 ||
    unit.localeCompare("Each", undefined, { sensitivity: "accent" }) === 0
      ? t("sell.stockAvailablePlain").replace("{qty}", formatQuantityDisplay(stock.quantity))
      : t("sell.stockAvailable")
          .replace("{qty}", formatQuantityDisplay(stock.quantity))
          .replace("{unit}", stock.unitOfMeasure);

  if (stock.tone === "low") {
    return `${quantityLine} · ${t("sell.stockLow")}`;
  }

  return quantityLine;
}
