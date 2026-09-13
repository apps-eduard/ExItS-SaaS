import { jsPDF } from "jspdf";
import autoTable from "jspdf-autotable";
import * as XLSX from "xlsx";
import { buildCsvWithMetadata, downloadCsvFile, sanitizeCsvFilenamePart } from "@/lib/csv";
import { downloadBlob } from "@/lib/download-blob";

export type PurchaseOrderReceiveExportRow = {
  product: string;
  uom: string;
  ordered: number;
  received: number;
  outstanding: number;
  goodReceived: string;
  damaged: string;
  expiry: string;
  lot: string;
};

export type PurchaseOrderReceiveExportModel = {
  rows: PurchaseOrderReceiveExportRow[];
  filenameBase: string;
  poNumber: string;
  filterLabel: string;
  exportedAt: string;
};

function headers(): string[] {
  return [
    "Product",
    "Unit",
    "Ordered",
    "Received before",
    "Outstanding",
    "Receive now",
    "Damaged",
    "Expiry",
    "Lot",
  ];
}

function bodyRows(rows: PurchaseOrderReceiveExportRow[]): Array<Array<string | number>> {
  return rows.map((row) => [
    row.product,
    row.uom,
    row.ordered,
    row.received,
    row.outstanding,
    row.goodReceived,
    row.damaged,
    row.expiry,
    row.lot,
  ]);
}

export function buildPurchaseOrderReceiveExportModel(input: {
  poNumber: string;
  filterLabel: string;
  rows: PurchaseOrderReceiveExportRow[];
}): PurchaseOrderReceiveExportModel {
  const poPart = sanitizeCsvFilenamePart(input.poNumber || "purchase-order");
  return {
    rows: input.rows,
    filenameBase: `PO-receive-${poPart}`,
    poNumber: input.poNumber || "Purchase order",
    filterLabel: input.filterLabel,
    exportedAt: new Date().toISOString(),
  };
}

export function downloadPurchaseOrderReceiveCsv(model: PurchaseOrderReceiveExportModel): void {
  const text = buildCsvWithMetadata(
    [
      ["Receive goods", model.poNumber],
      ["Filter", model.filterLabel],
      ["Exported at", model.exportedAt],
      ["Row count", String(model.rows.length)],
    ],
    { headers: headers(), rows: bodyRows(model.rows) },
  );
  downloadCsvFile(`${model.filenameBase}.csv`, text);
}

export function downloadPurchaseOrderReceiveXlsx(model: PurchaseOrderReceiveExportModel): void {
  const workbook = XLSX.utils.book_new();
  const sheetRows: Array<Array<string | number>> = [
    ["Receive goods", model.poNumber],
    ["Filter", model.filterLabel],
    ["Exported at", model.exportedAt],
    ["Row count", model.rows.length],
    [],
    headers(),
    ...bodyRows(model.rows),
  ];
  const sheet = XLSX.utils.aoa_to_sheet(sheetRows);
  XLSX.utils.book_append_sheet(workbook, sheet, "Receive goods");
  const buffer = XLSX.write(workbook, { bookType: "xlsx", type: "array" }) as ArrayBuffer;
  downloadBlob(
    `${model.filenameBase}.xlsx`,
    buffer,
    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
  );
}

export function downloadPurchaseOrderReceivePdf(model: PurchaseOrderReceiveExportModel): void {
  const doc = new jsPDF({ orientation: "landscape", unit: "pt", format: "a4" });
  doc.setFontSize(14);
  doc.text("Receive goods", 40, 40);
  doc.setFontSize(10);
  doc.text(`PO: ${model.poNumber}`, 40, 58);
  doc.text(`Filter: ${model.filterLabel}`, 40, 72);
  doc.text(`Rows: ${model.rows.length}`, 40, 86);

  autoTable(doc, {
    startY: 102,
    head: [headers()],
    body: bodyRows(model.rows).map((row) => row.map(String)),
    styles: { fontSize: 8 },
  });

  const buffer = doc.output("arraybuffer");
  downloadBlob(`${model.filenameBase}.pdf`, buffer, "application/pdf");
}

export function printPurchaseOrderReceiveDocument(): void {
  document.body.classList.add("exits-printing");
  window.print();
  window.setTimeout(() => {
    document.body.classList.remove("exits-printing");
  }, 300);
}
