import type { CheckoutSaleLineRequest } from "@/api/pos/pos-sales-client";
import type { SessionCartLine } from "@/cart/SessionCartProvider";

/**
 * Map session cart lines to online CheckoutSaleLineRequest.
 * ONLINE: omit all snapshot fields. Prefer sellingUnitId + enteredQuantity;
 * server recomputes base quantity when a sell unit is set.
 */
export function mapCartLinesToCheckoutRequest(lines: SessionCartLine[]): CheckoutSaleLineRequest[] {
  return lines.map((line) => {
    const base: CheckoutSaleLineRequest = {
      productId: line.productId,
      quantity: line.quantity,
    };
    if (line.productUnitId) {
      return {
        ...base,
        sellingUnitId: line.productUnitId,
        enteredQuantity: line.quantity,
      };
    }
    return base;
  });
}
