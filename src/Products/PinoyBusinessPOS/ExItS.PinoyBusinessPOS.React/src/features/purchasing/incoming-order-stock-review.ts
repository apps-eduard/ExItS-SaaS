import {
  clampQuantityToPrecision,
  formatQuantityValue,
  maxQuantityDecimals,
} from "@/lib/quantity-rules";
import { formatUnitOfMeasureLabel } from "@/features/purchasing/purchase-order-create-connected";

export type StockReviewLine = {
  productId: string;
  qty: number;
  unitOfMeasureCode?: string | null;
  availableToPromise?: number | null;
  confirmQty?: number | null;
  shortageWarning?: boolean | null;
};

export function lineHasShortage(line: StockReviewLine): boolean {
  if (line.shortageWarning === true) {
    return true;
  }
  return line.availableToPromise != null && line.qty > line.availableToPromise;
}

export function countShortageLines(lines: StockReviewLine[]): number {
  return lines.reduce((count, line) => count + (lineHasShortage(line) ? 1 : 0), 0);
}

export function defaultConfirmQty(line: StockReviewLine): number {
  const precision = maxQuantityDecimals(line.unitOfMeasureCode);
  if (line.confirmQty != null && Number.isFinite(line.confirmQty)) {
    return clampQuantityToPrecision(line.confirmQty, precision);
  }
  if (lineHasShortage(line) && line.availableToPromise != null) {
    return clampQuantityToPrecision(Math.min(line.qty, Math.max(0, line.availableToPromise)), precision);
  }
  return clampQuantityToPrecision(line.qty, precision);
}

export function quantitiesDiffer(a: number, b: number, unitOfMeasureCode?: string | null): boolean {
  const precision = maxQuantityDecimals(unitOfMeasureCode);
  return (
    clampQuantityToPrecision(a, precision) !== clampQuantityToPrecision(b, precision)
  );
}

export function hasMaterialProposalChanges(
  lines: StockReviewLine[],
  confirmQtys: Record<string, number>,
): boolean {
  return lines.some((line) => {
    const confirm = confirmQtys[line.productId] ?? line.qty;
    return quantitiesDiffer(confirm, line.qty, line.unitOfMeasureCode);
  });
}

export function formatStockQtyLabel(qty: number, unitOfMeasureCode?: string | null): string {
  const precision = maxQuantityDecimals(unitOfMeasureCode);
  const formatted = formatQuantityValue(qty, precision);
  const uom = unitOfMeasureCode ? formatUnitOfMeasureLabel(unitOfMeasureCode) : "";
  return uom ? `${formatted} ${uom}` : formatted;
}
