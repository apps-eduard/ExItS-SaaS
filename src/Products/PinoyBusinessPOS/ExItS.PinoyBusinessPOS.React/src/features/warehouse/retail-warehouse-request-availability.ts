import { formatQuantityDisplay, roundQuantity } from "@/cart/sell-cart-helpers";

export type RequestAvailabilityLine = {
  productId: string;
  name: string;
  quantity: number;
  warehouseAvailableQuantity: number;
  unitOfMeasure: string;
};

/** True when 0 < qty <= available (3-dp aware). */
export function isRequestQuantityAllowed(
  quantity: number,
  warehouseAvailable: number,
): boolean {
  const qty = roundQuantity(quantity);
  const available = roundQuantity(Math.max(0, warehouseAvailable));
  if (!Number.isFinite(qty) || qty <= 0) {
    return false;
  }
  return qty <= available + 1e-9;
}

export function clampRequestQuantityToAvailable(
  quantity: number,
  warehouseAvailable: number,
): number | null {
  const available = roundQuantity(Math.max(0, warehouseAvailable));
  if (available <= 0) {
    return null;
  }
  const qty = roundQuantity(quantity);
  if (!Number.isFinite(qty) || qty <= 0) {
    return null;
  }
  return qty > available + 1e-9 ? available : qty;
}

export function requestAvailabilityWarning(
  line: RequestAvailabilityLine,
  warehouseName: string,
): string | null {
  const qty = roundQuantity(line.quantity);
  const available = roundQuantity(Math.max(0, line.warehouseAvailableQuantity));
  const uom = line.unitOfMeasure;
  if (available <= 0) {
    return `${line.name} is out of stock at ${warehouseName}.`;
  }
  if (qty > available + 1e-9) {
    return `Warehouse stock changed. Only ${formatQuantityDisplay(available)} ${uom} is now available.`;
  }
  return null;
}

export function findRequestAvailabilityIssues(
  lines: ReadonlyArray<RequestAvailabilityLine>,
  warehouseName: string,
): Array<{ productId: string; message: string }> {
  const issues: Array<{ productId: string; message: string }> = [];
  for (const line of lines) {
    const message = requestAvailabilityWarning(line, warehouseName);
    if (message) {
      issues.push({ productId: line.productId, message });
    }
  }
  return issues;
}
