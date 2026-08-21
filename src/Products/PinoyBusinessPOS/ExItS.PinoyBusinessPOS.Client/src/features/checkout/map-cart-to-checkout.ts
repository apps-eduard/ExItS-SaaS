import type { CheckoutSaleLineRequest } from "@/api/pos/pos-sales-client";
import { roundMoney } from "@/cart/sell-cart-helpers";
import type { SessionCartLine } from "@/cart/SessionCartProvider";
import {
  isPriceAuthorityUsable,
  priceAuthorityLeaseKey,
  type PriceAuthorityLookup,
} from "@/offline/price-authority-cache";

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

export type OfflineCheckoutMapping =
  | { ok: true; lines: CheckoutSaleLineRequest[]; total: number }
  | { ok: false; unleasedLineKeys: string[] };

/**
 * Map session cart lines to an offline Cash checkout (RMAP-21 Review Repair 01).
 *
 * OFFLINE: every line must carry a server-signed, still-valid price lease, and every amount is
 * derived from that lease rather than from the cached catalog row. If even one line has no usable
 * lease the whole cart is refused — a sale that is priced partly by the server and partly by the
 * device is exactly the outcome this path exists to prevent.
 */
export function mapCartLinesToOfflineCheckoutRequest(
  lines: SessionCartLine[],
  authorities: PriceAuthorityLookup,
  now: Date = new Date(),
): OfflineCheckoutMapping {
  const unleasedLineKeys: string[] = [];
  const mapped: CheckoutSaleLineRequest[] = [];
  let total = 0;

  for (const line of lines) {
    const lease = authorities.get(priceAuthorityLeaseKey(line.productId, line.productUnitId));
    if (!lease || !isPriceAuthorityUsable(lease, now)) {
      unleasedLineKeys.push(line.lineKey);
      continue;
    }

    const lineTotal = roundMoney(lease.unitPrice * line.quantity);
    total = roundMoney(total + lineTotal);
    mapped.push({
      productId: line.productId,
      quantity: line.quantity,
      ...(line.productUnitId
        ? { sellingUnitId: line.productUnitId, enteredQuantity: line.quantity }
        : {}),
      unitPriceSnapshot: lease.unitPrice,
      unitOfMeasure: lease.unitOfMeasure,
      sellingMode: lease.sellingMode,
      lineTotal,
      offlinePriceAuthority: {
        authorityId: lease.authorityId,
        organizationId: lease.organizationId,
        productId: lease.productId,
        signature: lease.signature,
        issuedAtUtc: lease.issuedAtUtc,
        expiresAtUtc: lease.expiresAtUtc,
        unitPrice: lease.unitPrice,
        unitOfMeasure: lease.unitOfMeasure,
        sellingMode: lease.sellingMode,
        branchId: lease.branchId ?? null,
        sellingUnitId: lease.sellingUnitId ?? null,
      },
    });
  }

  if (unleasedLineKeys.length > 0) {
    return { ok: false, unleasedLineKeys };
  }
  return { ok: true, lines: mapped, total };
}
