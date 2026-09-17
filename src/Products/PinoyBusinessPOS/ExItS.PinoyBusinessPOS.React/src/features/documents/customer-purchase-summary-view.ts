import type { DirectPurchaseB2bDetail } from "@/api/pos/pos-direct-purchases-client";
import type { LinkedCustomerSaleReceipt } from "@/api/pos/pos-linked-customers-client";
import { formatPaymentMethodLabel, type PosSaleDto } from "@/api/pos/pos-sales-client";
import { stripPersonalRunStamp } from "@/features/customer-ordering/format-personal-store-label";

/** Normalized sale facts for CustomerPurchaseSummaryDocument (presentation only). */
export type CustomerPurchaseSummaryLine = {
  lineNumber: number;
  name: string;
  sku?: string | null;
  unitOfMeasure: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
  lineDiscountAmount?: number;
};

export type CustomerPurchaseSummaryView = {
  saleNumber: string;
  recordedAtUtc: string;
  status: string;
  paymentMethod: string;
  paymentLabel: string;
  customerDisplayName?: string | null;
  gCashReference?: string | null;
  subtotal: number;
  taxAmount: number;
  total: number;
  discountTotal: number;
  amountTendered?: number | null;
  changeAmount?: number | null;
  /** When set, prefer this for Utang / balance row. */
  utangBalance?: number | null;
  lines: CustomerPurchaseSummaryLine[];
};

export function customerPurchaseSummaryFromPosSale(
  sale: PosSaleDto,
  paymentLabel = formatPaymentMethodLabel(sale.paymentMethod),
): CustomerPurchaseSummaryView {
  const discountTotal =
    sale.discountTotal ??
    sale.lines.reduce(
      (sum, line) =>
        sum + (line.lineDiscountAmount ?? 0) + (line.saleDiscountAllocatedAmount ?? 0),
      0,
    );
  return {
    saleNumber: sale.saleNumber,
    recordedAtUtc: sale.recordedAtUtc,
    status: sale.status,
    paymentMethod: sale.paymentMethod,
    paymentLabel,
    customerDisplayName: sale.customerDisplayName,
    gCashReference: sale.gCashReference,
    subtotal: sale.subtotal,
    taxAmount: sale.taxAmount,
    total: sale.total,
    discountTotal,
    amountTendered: sale.amountTendered,
    changeAmount: sale.changeAmount,
    lines: sale.lines.map((line) => ({
      lineNumber: line.lineNumber,
      name: line.name,
      sku: line.sku,
      unitOfMeasure: line.unitOfMeasure,
      unitPrice: line.unitPrice,
      quantity: line.quantity,
      lineTotal: line.lineTotal,
      lineDiscountAmount:
        (line.lineDiscountAmount ?? 0) + (line.saleDiscountAllocatedAmount ?? 0),
    })),
  };
}

export function customerPurchaseSummaryFromLinkedReceipt(
  receipt: LinkedCustomerSaleReceipt,
): CustomerPurchaseSummaryView {
  const paymentLabel = formatPaymentMethodLabel(receipt.paymentMethod);
  const discountTotal = receipt.discountAmount ?? 0;
  const isUtang =
    receipt.paymentMethod.trim().toLowerCase() === "utang" ||
    (receipt.utangAmount != null && receipt.utangAmount > 0);
  return {
    saleNumber: receipt.receiptNumber,
    recordedAtUtc: receipt.occurredAtUtc,
    status: receipt.status,
    paymentMethod: receipt.paymentMethod,
    paymentLabel,
    customerDisplayName: stripPersonalRunStamp(receipt.customerDisplayName ?? "") || null,
    subtotal: receipt.subtotal,
    taxAmount: receipt.taxAmount,
    total: receipt.total,
    discountTotal,
    amountTendered: isUtang ? null : receipt.paidAmount,
    changeAmount: isUtang ? null : (receipt.changeAmount ?? null),
    utangBalance: isUtang ? (receipt.utangAmount ?? receipt.total) : null,
    lines: receipt.lines.map((line) => ({
      lineNumber: line.lineNumber,
      name: line.productNameSnapshot,
      unitOfMeasure: line.unitOfMeasure,
      unitPrice: line.unitPriceSnapshot,
      quantity: line.quantity,
      lineTotal: line.lineTotal,
      lineDiscountAmount: line.lineDiscountAmount ?? 0,
    })),
  };
}

export function customerPurchaseSummaryFromB2bDetail(
  detail: DirectPurchaseB2bDetail,
): CustomerPurchaseSummaryView {
  return {
    saleNumber: detail.saleNumber,
    recordedAtUtc: detail.occurredAtUtc,
    status: detail.status,
    paymentMethod: detail.paymentMethod,
    paymentLabel: formatPaymentMethodLabel(detail.paymentMethod),
    customerDisplayName: detail.buyerDisplayNameSnapshot?.trim() || null,
    subtotal: detail.subtotal,
    taxAmount: detail.taxAmount,
    total: detail.totalAmount,
    discountTotal: detail.discountTotal,
    amountTendered: null,
    changeAmount: null,
    utangBalance:
      detail.paymentMethod.trim().toLowerCase() === "utang" ? detail.totalAmount : null,
    lines: detail.lines.map((line) => ({
      lineNumber: line.lineNumber,
      name: line.productNameSnapshot,
      sku: line.skuSnapshot,
      unitOfMeasure: line.unitOfMeasure,
      unitPrice: line.unitPrice,
      quantity: line.quantity,
      lineTotal: line.lineTotal,
      lineDiscountAmount: line.lineDiscountAmount,
    })),
  };
}
