import { formatQuantityDisplay, type SellCardStock } from "@/cart/sell-cart-helpers";
import type { MessageKey } from "@/i18n/messages";

export function sellStockCaption(t: (key: MessageKey) => string, stock: SellCardStock): string {
  if (stock.tone === "untracked") {
    return t("sell.stockNotTracked");
  }

  const quantityLine = t(stock.quantityLabel === "sellable" ? "sell.stockSellable" : "sell.stockOnHand")
    .replace("{qty}", formatQuantityDisplay(stock.quantity))
    .replace("{unit}", stock.unitOfMeasure);

  if (stock.tone === "low") {
    return `${quantityLine} · ${t("sell.stockLow")}`;
  }
  if (stock.tone === "out") {
    return `${quantityLine} · ${t("sell.stockOut")}`;
  }
  return quantityLine;
}
