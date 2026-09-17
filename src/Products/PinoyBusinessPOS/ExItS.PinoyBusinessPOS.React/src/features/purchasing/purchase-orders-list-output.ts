import { jsPDF } from "jspdf";
import autoTable from "jspdf-autotable";
import * as XLSX from "xlsx";
import type { PosPurchaseOrderDto } from "@/api/pos/pos-purchase-orders-client";
import { buildCsvWithMetadata, downloadCsvFile, sanitizeCsvFilenamePart } from "@/lib/csv";
import { downloadBlob } from "@/lib/download-blob";

export type PurchaseOrderListExportRow = {
  poNumber: string;
  supplier: string;
  orderDate: string;
  lines: number;
  status: string;
  payment: string;
};

export type PurchaseOrderListExportModel = {
  rows: PurchaseOrderListExportRow[];
  filenameBase: string;
  statusFilter: string;
  exportedAt: string;
};

export function toPurchaseOrderListExportRow(po: PosPurchaseOrderDto): PurchaseOrderListExportRow {
  return {
    poNumber: po.poNumber?.trim() || "Purchase order",
    supplier: po.supplierName?.trim() || "—",
    orderDate: po.orderDate,
    lines: po.lines.length,
    status: po.displayStatus || po.status,
    payment: po.paymentTermLabel || po.paymentTerm || "—",
  };
}

export function buildPurchaseOrderListExportModel(
  orders: ReadonlyArray<PosPurchaseOrderDto>,
  statusFilterLabel: string,
): PurchaseOrderListExportModel {
  return {
    rows: orders.map(toPurchaseOrderListExportRow),
    filenameBase: `PO-list-${sanitizeCsvFilenamePart(statusFilterLabel || "all")}`,
    statusFilter: statusFilterLabel || "All",
    exportedAt: new Date().toISOString(),
  };
}

function headers(): string[] {
  return ["PO number", "Supplier", "Order date", "Lines", "Status", "Payment"];
}

function bodyRows(rows: PurchaseOrderListExportRow[]): Array<Array<string | number>> {
  return rows.map((row) => [
    row.poNumber,
    row.supplier,
    row.orderDate,
    row.lines,
    row.status,
    row.payment,
  ]);
}

export function downloadPurchaseOrderListCsv(model: PurchaseOrderListExportModel): void {
  const text = buildCsvWithMetadata(
    [
      ["Purchase orders", model.statusFilter],
      ["Exported at", model.exportedAt],
      ["Row count", String(model.rows.length)],
    ],
    { headers: headers(), rows: bodyRows(model.rows) },
  );
  downloadCsvFile(`${model.filenameBase}.csv`, text);
}

export function downloadPurchaseOrderListXlsx(model: PurchaseOrderListExportModel): void {
  const workbook = XLSX.utils.book_new();
  const sheetRows: Array<Array<string | number>> = [
    ["Purchase orders", model.statusFilter],
    ["Exported at", model.exportedAt],
    ["Row count", model.rows.length],
    [],
    headers(),
    ...bodyRows(model.rows),
  ];
  const sheet = XLSX.utils.aoa_to_sheet(sheetRows);
  XLSX.utils.book_append_sheet(workbook, sheet, "Purchase orders");
  const buffer = XLSX.write(workbook, { bookType: "xlsx", type: "array" }) as ArrayBuffer;
  downloadBlob(
    `${model.filenameBase}.xlsx`,
    buffer,
    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
  );
}

export function downloadPurchaseOrderListPdf(model: PurchaseOrderListExportModel): void {
  const doc = new jsPDF({ orientation: "landscape", unit: "pt", format: "a4" });
  doc.setFontSize(14);
  doc.text("Purchase orders", 40, 40);
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

export function printPurchaseOrderListDocument(): void {
  document.body.classList.add("exits-printing");
  window.print();
  window.setTimeout(() => {
    document.body.classList.remove("exits-printing");
  }, 300);
}
