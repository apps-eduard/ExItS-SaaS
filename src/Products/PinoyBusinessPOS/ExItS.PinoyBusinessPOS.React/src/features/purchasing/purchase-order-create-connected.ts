import type {
  BuyerSupplierProductLink,
  SupplierProductExposure,
} from "@/api/pos/pos-connected-suppliers-client";

export type ConnectedPoReadyProduct = {
  linkId: string;
  buyerProductId: string;
  supplierProductId: string;
  productName: string;
  unitOfMeasure: string;
  supplierSku: string | null;
  unitPurchaseCost: number;
  purchaseUnitId: string | null;
  packageLabel: string | null;
};

export type ConnectedPoDraftLine = {
  productId: string;
  name: string;
  uom: string;
  orderedQty: number;
  unitPurchaseCost: number;
  purchaseUnitId?: string | null;
};

export function roundMoney(value: number): number {
  return Math.round((value + Number.EPSILON) * 100) / 100;
}

export function lineTotal(orderedQty: number, unitPurchaseCost: number): number {
  return roundMoney(orderedQty * unitPurchaseCost);
}

export function orderSubtotal(lines: ReadonlyArray<{ orderedQty: number; unitPurchaseCost: number }>): number {
  return roundMoney(lines.reduce((sum, line) => sum + lineTotal(line.orderedQty, line.unitPurchaseCost), 0));
}

export function orderUnitCount(lines: ReadonlyArray<{ orderedQty: number }>): number {
  return lines.reduce((sum, line) => sum + line.orderedQty, 0);
}

/** Active linked products with a positive PO price (and optional shared/orderable join). */
export function buildConnectedReadyProducts(
  links: ReadonlyArray<BuyerSupplierProductLink>,
  exposures: ReadonlyArray<SupplierProductExposure> | null = null,
): ConnectedPoReadyProduct[] {
  const exposureBySupplierProduct = new Map(
    (exposures ?? [])
      .filter((x) => x.isExposed && x.isOrderable)
      .map((x) => [x.productId, x] as const),
  );
  const requireExposure = exposures != null && exposures.length > 0;

  const ready: ConnectedPoReadyProduct[] = [];
  for (const link of links) {
    if (!link.isActive || link.buyerProductId === "" || link.lastKnownOrderPrice <= 0) {
      continue;
    }
    const exposure = exposureBySupplierProduct.get(link.supplierProductId);
    if (requireExposure && !exposure) {
      continue;
    }
    const unitPurchaseCost =
      exposure?.effectiveSupplierOrderPrice ??
      exposure?.supplierOrderPrice ??
      link.lastKnownOrderPrice;
    if (unitPurchaseCost <= 0) {
      continue;
    }
    ready.push({
      linkId: link.linkId,
      buyerProductId: link.buyerProductId,
      supplierProductId: link.supplierProductId,
      productName: link.supplierNameSnapshot,
      unitOfMeasure: link.unitOfMeasureCode,
      supplierSku: link.supplierSkuSnapshot ?? null,
      unitPurchaseCost,
      purchaseUnitId: link.buyerPurchaseUnitId ?? null,
      packageLabel: link.packageLabel ?? null,
    });
  }

  return ready.sort((a, b) =>
    a.productName.localeCompare(b.productName, undefined, { sensitivity: "base" }),
  );
}

export function filterConnectedReadyProducts(
  products: ReadonlyArray<ConnectedPoReadyProduct>,
  searchText: string,
): ConnectedPoReadyProduct[] {
  const query = searchText.trim().toLowerCase();
  if (!query) {
    return [...products];
  }
  return products.filter((product) => {
    const tokens = [
      product.productName,
      product.supplierSku ?? "",
      product.unitOfMeasure,
      product.packageLabel ?? "",
    ];
    return tokens.some((token) => token.toLowerCase().includes(query));
  });
}

export function applyConnectedQuantityDelta(
  lines: ReadonlyArray<ConnectedPoDraftLine>,
  product: ConnectedPoReadyProduct,
  delta: number,
): ConnectedPoDraftLine[] {
  if (delta === 0) {
    return [...lines];
  }
  const index = lines.findIndex((line) => line.productId === product.buyerProductId);
  if (index < 0) {
    if (delta <= 0) {
      return [...lines];
    }
    return [
      ...lines,
      {
        productId: product.buyerProductId,
        name: product.productName,
        uom: product.unitOfMeasure,
        orderedQty: 1,
        unitPurchaseCost: product.unitPurchaseCost,
        purchaseUnitId: product.purchaseUnitId,
      },
    ];
  }

  const nextQty = lines[index]!.orderedQty + delta;
  if (nextQty <= 0) {
    return lines.filter((line) => line.productId !== product.buyerProductId);
  }
  return lines.map((line, i) => (i === index ? { ...line, orderedQty: nextQty } : line));
}

export function retainCompatibleDraftLines(
  lines: ReadonlyArray<ConnectedPoDraftLine>,
  readyProducts: ReadonlyArray<ConnectedPoReadyProduct>,
): ConnectedPoDraftLine[] {
  const allowed = new Set(readyProducts.map((p) => p.buyerProductId));
  return lines.filter((line) => allowed.has(line.productId));
}

export function formatUnitPriceLabel(unitPurchaseCost: number, unitOfMeasure: string): string {
  const unit = unitOfMeasure.trim() || "pc";
  return `${formatCompactPeso(unitPurchaseCost)} / ${unit}`;
}

export function formatLineMath(orderedQty: number, unitPurchaseCost: number): string {
  return `${formatCompactPeso(lineTotal(orderedQty, unitPurchaseCost))} · ${orderedQty} × ${formatCompactPeso(unitPurchaseCost)}`;
}

function formatCompactPeso(amount: number): string {
  return `₱${amount.toLocaleString("en-PH", {
    minimumFractionDigits: amount % 1 === 0 ? 0 : 2,
    maximumFractionDigits: 2,
  })}`;
}
