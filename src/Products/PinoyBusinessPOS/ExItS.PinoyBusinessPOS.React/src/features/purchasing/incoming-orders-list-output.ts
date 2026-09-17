import { jsPDF } from "jspdf";
import autoTable from "jspdf-autotable";
import * as XLSX from "xlsx";
import type { ConnectedPurchaseOrder } from "@/api/pos/pos-connected-suppliers-client";
import {
  countIncomingLines,
  countIncomingUnits,
} from "@/features/purchasing/incoming-orders-helpers";
import { buildCsvWithMetadata, downloadCsvFile, sanitizeCsvFilenamePart } from "@/lib/csv";
import { downloadBlob } from "@/lib/download-blob";

export type IncomingOrderListExportRow = {
  poNumber: string;
  buyer: string;
  branch: string;
  orderDate: string;
  products: number;
  units: number;
  total: number;
  status: string;
};

export type IncomingOrderListExportModel = {
  rows: IncomingOrderListExportRow[];
  filenameBase: string;
  statusFilter: string;
  exportedAt: string;
};

export function toIncomingOrderListExportRow(
  order: ConnectedPurchaseOrder,
  statusLabel: string,
  buyerFallback: string,
): IncomingOrderListExportRow {
  return {
    poNumber: order.buyerPoNumber?.trim() || "Purchase order",
    buyer: order.buyerDisplayName?.trim() || buyerFallback,
    branch: order.supplierBranchName?.trim() || "—",
    orderDate: order.orderDate,
    products: countIncomingLines(order),
    units: countIncomingUnits(order),
    total: order.totalAmount,
    status: statusLabel,
  };
}

export function buildIncomingOrderListExportModel(
  orders: ReadonlyArray<ConnectedPurchaseOrder>,
  statusFilterLabel: string,
  resolveStatusLabel: (order: ConnectedPurchaseOrder) => string,
  buyerFallback: string,
): IncomingOrderListExportModel {
  return {
    rows: orders.map((order) =>
      toIncomingOrderListExportRow(order, resolveStatusLabel(order), buyerFallback),
    ),
    filenameBase: `Incoming-orders-${sanitizeCsvFilenamePart(statusFilterLabel || "all")}`,
    statusFilter: statusFilterLabel || "All",
    exportedAt: new Date().toISOString(),
  };
}

function headers(): string[] {
  return ["PO number", "Buyer", "Fulfill from", "Order date", "Products", "Units", "Total", "Status"];
}

function bodyRows(rows: IncomingOrderListExportRow[]): Array<Array<string | number>> {
  return rows.map((row) => [
    row.poNumber,
    row.buyer,
    row.branch,
    row.orderDate,
    row.products,
    row.units,
    row.total,
    row.status,
  ]);
}

export function downloadIncomingOrderListCsv(model: IncomingOrderListExportModel): void {
  const text = buildCsvWithMetadata(
    [
      ["Incoming orders", model.statusFilter],
      ["Exported at", model.exportedAt],
      ["Row count", String(model.rows.length)],
    ],
    { headers: headers(), rows: bodyRows(model.rows) },
  );
  downloadCsvFile(`${model.filenameBase}.csv`, text);
}

export function downloadIncomingOrderListXlsx(model: IncomingOrderListExportModel): void {
  const workbook = XLSX.utils.book_new();
  const sheetRows: Array<Array<string | number>> = [
    ["Incoming orders", model.statusFilter],
    ["Exported at", model.exportedAt],
    ["Row count", model.rows.length],
    [],
    headers(),
    ...bodyRows(model.rows),
  ];
  const sheet = XLSX.utils.aoa_to_sheet(sheetRows);
  XLSX.utils.book_append_sheet(workbook, sheet, "Incoming orders");
  const buffer = XLSX.write(workbook, { bookType: "xlsx", type: "array" }) as ArrayBuffer;
  downloadBlob(
    `${model.filenameBase}.xlsx`,
    buffer,
    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
  );
}

export function downloadIncomingOrderListPdf(model: IncomingOrderListExportModel): void {
  const doc = new jsPDF({ orientation: "landscape", unit: "pt", format: "a4" });
  doc.setFontSize(14);
  doc.text("Incoming orders", 40, 40);
  doc.setFontSize(10);
  doc.text(`Filter: ${model.statusFilter}`, 40, 58);
  doc.text(`Rows: ${model.rows.length}`, 40, 72);

  autoTable(doc, {
    startY: 88,
    head: [headers()],
    body: bodyRows(model.rows).map((row) => row.map(String)),
    styles: { fontSize: 9 },
  });

  const buffer = doc.output("arraybuffer");
  downloadBlob(`${model.filenameBase}.pdf`, buffer, "application/pdf");
}

export function printIncomingOrderListDocument(): void {
  document.body.classList.add("exits-printing");
  window.print();
  window.setTimeout(() => {
    document.body.classList.remove("exits-printing");
  }, 300);
}
