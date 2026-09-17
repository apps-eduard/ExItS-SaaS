import { jsPDF } from "jspdf";
import autoTable from "jspdf-autotable";
import * as XLSX from "xlsx";
import { buildCsvWithMetadata, downloadCsvFile, sanitizeCsvFilenamePart } from "@/lib/csv";
import { downloadBlob } from "@/lib/download-blob";

export type DirectPurchaseDetailLineExportRow = {
  product: string;
  quantity: number;
  uom: string;
  unitCost: number;
  lineTotal: number;
  expiry: string;
  lot: string;
};

export type DirectPurchaseDetailLinesExportModel = {
  rows: DirectPurchaseDetailLineExportRow[];
  filenameBase: string;
  receiptNumber: string;
  filterLabel: string;
  exportedAt: string;
};

function headers(): string[] {
  return [
    "Product",
    "Quantity",
    "Unit",
    "Unit purchase cost",
    "Line total",
    "Expiry date",
    "Batch / Lot number",
  ];
}

function bodyRows(rows: DirectPurchaseDetailLineExportRow[]): Array<Array<string | number>> {
  return rows.map((row) => [
    row.product,
    row.quantity,
    row.uom,
    row.unitCost,
    row.lineTotal,
    row.expiry,
    row.lot,
  ]);
}

export function buildDirectPurchaseDetailLinesExportModel(input: {
  receiptNumber: string;
  filterLabel: string;
  rows: DirectPurchaseDetailLineExportRow[];
}): DirectPurchaseDetailLinesExportModel {
  const receiptPart = sanitizeCsvFilenamePart(input.receiptNumber || "direct-purchase");
  return {
    rows: input.rows,
    filenameBase: `Direct-purchase-${receiptPart}`,
    receiptNumber: input.receiptNumber || "Direct purchase",
    filterLabel: input.filterLabel,
    exportedAt: new Date().toISOString(),
  };
}

export function downloadDirectPurchaseDetailLinesCsv(
  model: DirectPurchaseDetailLinesExportModel,
): void {
  const text = buildCsvWithMetadata(
    [
      ["Direct purchase items", model.receiptNumber],
      ["Filter", model.filterLabel],
      ["Exported at", model.exportedAt],
      ["Row count", String(model.rows.length)],
    ],
    { headers: headers(), rows: bodyRows(model.rows) },
  );
  downloadCsvFile(`${model.filenameBase}.csv`, text);
}

export function downloadDirectPurchaseDetailLinesXlsx(
  model: DirectPurchaseDetailLinesExportModel,
): void {
  const workbook = XLSX.utils.book_new();
  const sheetRows: Array<Array<string | number>> = [
    ["Direct purchase items", model.receiptNumber],
    ["Filter", model.filterLabel],
    ["Exported at", model.exportedAt],
    ["Row count", model.rows.length],
    [],
    headers(),
    ...bodyRows(model.rows),
  ];
  const sheet = XLSX.utils.aoa_to_sheet(sheetRows);
  XLSX.utils.book_append_sheet(workbook, sheet, "Items");
  const buffer = XLSX.write(workbook, { bookType: "xlsx", type: "array" }) as ArrayBuffer;
  downloadBlob(
    `${model.filenameBase}.xlsx`,
    buffer,
    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
  );
}

export function downloadDirectPurchaseDetailLinesPdf(
  model: DirectPurchaseDetailLinesExportModel,
): void {
  const doc = new jsPDF({ orientation: "landscape", unit: "pt", format: "a4" });
  doc.setFontSize(14);
  doc.text("Direct purchase items", 40, 40);
  doc.setFontSize(10);
  doc.text(`Receipt: ${model.receiptNumber}`, 40, 58);
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

export function printDirectPurchaseDetailLinesDocument(): void {
  document.body.classList.add("exits-printing");
  window.print();
  window.setTimeout(() => {
    document.body.classList.remove("exits-printing");
  }, 300);
}
