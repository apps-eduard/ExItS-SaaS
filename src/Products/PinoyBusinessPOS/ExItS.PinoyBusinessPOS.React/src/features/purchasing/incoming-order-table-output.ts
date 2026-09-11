import { jsPDF } from "jspdf";
import autoTable from "jspdf-autotable";
import * as XLSX from "xlsx";
import type { ConnectedPurchaseOrder, ConnectedPurchaseOrderLine } from "@/api/pos/pos-connected-suppliers-client";
import { buildCsvWithMetadata, downloadCsvFile, sanitizeCsvFilenamePart } from "@/lib/csv";
import { downloadBlob } from "@/lib/download-blob";
import { formatUnitOfMeasureLabel } from "@/features/purchasing/purchase-order-create-connected";

export type IncomingOrderExportLine = {
  productId: string;
  product: string;
  sku: string;
  quantity: number;
  unit: string;
  unitCost: number;
  lineTotal: number;
};

export type IncomingOrderExportScope = "matching" | "selected";

export type IncomingOrderExportModel = {
  scope: IncomingOrderExportScope;
  lines: IncomingOrderExportLine[];
  selectedLinesTotal: number;
  orderTotal: number;
  poNumber: string;
  buyer: string;
  branch: string;
  orderDate: string;
  paymentTerm: string;
  filenameBase: string;
};

function lineUnit(line: ConnectedPurchaseOrderLine): string {
  return line.unitOfMeasureCode ? formatUnitOfMeasureLabel(line.unitOfMeasureCode) : "";
}

export function toIncomingOrderExportLine(line: ConnectedPurchaseOrderLine): IncomingOrderExportLine {
  return {
    productId: line.productId,
    product: line.nameSnapshot,
    sku: line.skuSnapshot?.trim() || "",
    quantity: line.qty,
    unit: lineUnit(line),
    unitCost: line.unitPriceSnapshot,
    lineTotal: line.lineTotal,
  };
}

/** Matching filtered/sorted view, or selected subset when selection is non-empty. */
export function resolveIncomingOrderExportLines(
  matchingLines: ConnectedPurchaseOrderLine[],
  selectedIds: ReadonlySet<string>,
): { scope: IncomingOrderExportScope; lines: IncomingOrderExportLine[] } {
  if (selectedIds.size > 0) {
    const selected = matchingLines.filter((line) => selectedIds.has(line.productId));
    return {
      scope: "selected",
      lines: selected.map(toIncomingOrderExportLine),
    };
  }
  return {
    scope: "matching",
    lines: matchingLines.map(toIncomingOrderExportLine),
  };
}

export function buildIncomingOrderExportFilenameBase(poNumber: string | null | undefined): string {
  const raw = (poNumber ?? "").trim();
  if (raw) {
    const kept = raw.replace(/[^A-Za-z0-9._-]+/g, "-").replace(/^-+|-+$/g, "");
    if (kept) {
      return kept.slice(0, 64);
    }
  }
  return `PO-${sanitizeCsvFilenamePart("incoming-order")}`;
}

export function buildIncomingOrderExportModel(
  order: ConnectedPurchaseOrder,
  matchingLines: ConnectedPurchaseOrderLine[],
  selectedIds: ReadonlySet<string>,
): IncomingOrderExportModel {
  const resolved = resolveIncomingOrderExportLines(matchingLines, selectedIds);
  return {
    scope: resolved.scope,
    lines: resolved.lines,
    selectedLinesTotal: resolved.lines.reduce((sum, line) => sum + line.lineTotal, 0),
    orderTotal: order.totalAmount,
    poNumber: order.buyerPoNumber?.trim() || "Purchase order",
    buyer: order.buyerDisplayName?.trim() || "Connected buyer",
    branch: order.supplierBranchName?.trim() || "",
    orderDate: order.orderDate,
    paymentTerm: order.paymentTermLabel || order.paymentTerm || "",
    filenameBase: buildIncomingOrderExportFilenameBase(order.buyerPoNumber),
  };
}

function tableHeaders(): string[] {
  return ["Product", "SKU", "Quantity", "Unit", "Unit cost", "Line total"];
}

function tableRows(lines: IncomingOrderExportLine[]): Array<Array<string | number>> {
  return lines.map((line) => [
    line.product,
    line.sku,
    line.quantity,
    line.unit,
    line.unitCost,
    line.lineTotal,
  ]);
}

export function buildIncomingOrderCsvText(model: IncomingOrderExportModel): string {
  const metadata: Array<[string, string]> = [
    ["Incoming order", model.poNumber],
    ["Buyer", model.buyer],
    ["Order date", model.orderDate],
  ];
  if (model.branch) {
    metadata.push(["Fulfill from", model.branch]);
  }
  if (model.paymentTerm) {
    metadata.push(["Payment term", model.paymentTerm]);
  }
  metadata.push(["Order total", String(model.orderTotal)]);
  if (model.scope === "selected") {
    metadata.push(["Export scope", "Selected lines"]);
    metadata.push(["Selected lines total", String(model.selectedLinesTotal)]);
  } else {
    metadata.push(["Export scope", "Matching lines"]);
  }

  return buildCsvWithMetadata(metadata, {
    headers: tableHeaders(),
    rows: tableRows(model.lines),
  });
}

export function downloadIncomingOrderCsv(model: IncomingOrderExportModel): void {
  downloadCsvFile(`${model.filenameBase}.csv`, buildIncomingOrderCsvText(model));
}

export function buildIncomingOrderXlsxArrayBuffer(model: IncomingOrderExportModel): ArrayBuffer {
  const workbook = XLSX.utils.book_new();
  const sheetRows: Array<Array<string | number>> = [
    ["Incoming order", model.poNumber],
    ["Buyer", model.buyer],
    ["Order date", model.orderDate],
  ];
  if (model.branch) {
    sheetRows.push(["Fulfill from", model.branch]);
  }
  if (model.paymentTerm) {
    sheetRows.push(["Payment term", model.paymentTerm]);
  }
  sheetRows.push(["Order total", model.orderTotal]);
  if (model.scope === "selected") {
    sheetRows.push(["Export scope", "Selected lines"]);
    sheetRows.push(["Selected lines total", model.selectedLinesTotal]);
  } else {
    sheetRows.push(["Export scope", "Matching lines"]);
  }
  sheetRows.push([]);
  sheetRows.push(tableHeaders());
  for (const line of model.lines) {
    sheetRows.push([
      line.product,
      line.sku,
      line.quantity,
      line.unit,
      line.unitCost,
      line.lineTotal,
    ]);
  }

  const sheet = XLSX.utils.aoa_to_sheet(sheetRows);
  XLSX.utils.book_append_sheet(workbook, sheet, "Order lines");
  return XLSX.write(workbook, { bookType: "xlsx", type: "array" }) as ArrayBuffer;
}

export function downloadIncomingOrderXlsx(model: IncomingOrderExportModel): void {
  const buffer = buildIncomingOrderXlsxArrayBuffer(model);
  downloadBlob(
    `${model.filenameBase}.xlsx`,
    buffer,
    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
  );
}

export function buildIncomingOrderPdfArrayBuffer(model: IncomingOrderExportModel): ArrayBuffer {
  const doc = new jsPDF({ orientation: "portrait", unit: "pt", format: "a4" });
  const left = 40;
  let y = 48;

  doc.setFontSize(14);
  doc.text("Incoming order", left, y);
  y += 20;
  doc.setFontSize(11);
  doc.text(model.poNumber, left, y);
  y += 18;
  doc.setFontSize(10);
  doc.text(`Buyer: ${model.buyer}`, left, y);
  y += 14;
  if (model.branch) {
    doc.text(`Fulfill from: ${model.branch}`, left, y);
    y += 14;
  }
  doc.text(`Order date: ${model.orderDate}`, left, y);
  y += 14;
  if (model.paymentTerm) {
    doc.text(`Payment term: ${model.paymentTerm}`, left, y);
    y += 14;
  }
  doc.text(`Order total: ${model.orderTotal.toFixed(2)}`, left, y);
  y += 10;
  if (model.scope === "selected") {
    doc.text(`Selected lines total: ${model.selectedLinesTotal.toFixed(2)}`, left, y);
    y += 10;
  }

  autoTable(doc, {
    startY: y + 8,
    head: [["Product", "SKU", "Quantity", "Unit cost", "Line total"]],
    body: model.lines.map((line) => [
      line.product,
      line.sku || "—",
      line.unit ? `${line.quantity} ${line.unit}` : String(line.quantity),
      line.unitCost.toFixed(2),
      line.lineTotal.toFixed(2),
    ]),
    styles: { fontSize: 9, cellPadding: 4 },
    headStyles: { fillColor: [55, 75, 60] },
  });

  return doc.output("arraybuffer") as ArrayBuffer;
}

export function buildIncomingOrderPdfBlob(model: IncomingOrderExportModel): Blob {
  const buffer = buildIncomingOrderPdfArrayBuffer(model);
  return new Blob([new Uint8Array(buffer)], { type: "application/pdf" });
}

export function downloadIncomingOrderPdf(model: IncomingOrderExportModel): void {
  downloadBlob(`${model.filenameBase}.pdf`, buildIncomingOrderPdfBlob(model), "application/pdf");
}

export function printIncomingOrderDocument(): void {
  const previous = document.body.classList.contains("exits-printing");
  document.body.classList.add("exits-printing");
  const cleanup = () => {
    if (!previous) {
      document.body.classList.remove("exits-printing");
    }
    window.removeEventListener("afterprint", cleanup);
  };
  window.addEventListener("afterprint", cleanup);
  window.print();
  // Fallback if afterprint is delayed/missing in some environments.
  window.setTimeout(cleanup, 1000);
}
