/**
 * RFC-style CSV helpers for Organization report export.
 * UTF-8 with BOM for Excel PH-locale compatibility.
 */

export type CsvCell = string | number | boolean | null | undefined;

export type CsvTable = {
  headers: string[];
  rows: CsvCell[][];
};

const FORMULA_PREFIX = /^[=+\-@]/;

/** Neutralize spreadsheet formula injection for textual cells only. */
export function neutralizeCsvInjection(value: string): string {
  if (value.length === 0) {
    return value;
  }
  if (FORMULA_PREFIX.test(value)) {
    return `'${value}`;
  }
  return value;
}

export function formatCsvCell(value: CsvCell): string {
  if (value === null || value === undefined) {
    return "";
  }
  if (typeof value === "number") {
    if (!Number.isFinite(value)) {
      return "";
    }
    return String(value);
  }
  if (typeof value === "boolean") {
    return value ? "true" : "false";
  }
  const text = neutralizeCsvInjection(value);
  if (/[",\r\n]/.test(text)) {
    return `"${text.replace(/"/g, '""')}"`;
  }
  return text;
}

export function buildCsv(table: CsvTable, options?: { includeBom?: boolean }): string {
  const lines = [
    table.headers.map((h) => formatCsvCell(h)).join(","),
    ...table.rows.map((row) => row.map((cell) => formatCsvCell(cell)).join(",")),
  ];
  const body = lines.join("\r\n");
  return options?.includeBom === false ? body : `\uFEFF${body}`;
}

export function buildCsvWithMetadata(
  metadata: ReadonlyArray<readonly [string, string]>,
  table: CsvTable,
  options?: { includeBom?: boolean },
): string {
  const metaLines = metadata.map(
    ([key, value]) => `${formatCsvCell(key)},${formatCsvCell(value)}`,
  );
  const tableCsv = buildCsv(table, { includeBom: false });
  const body = [...metaLines, "", tableCsv].join("\r\n");
  return options?.includeBom === false ? body : `\uFEFF${body}`;
}

export function sanitizeCsvFilenamePart(value: string): string {
  return (
    value
      .trim()
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, "-")
      .replace(/^-+|-+$/g, "")
      .slice(0, 48) || "report"
  );
}

export function buildReportCsvFilename(parts: {
  reportName: string;
  scopeLabel?: string | null;
  fromDate?: string | null;
  toDate?: string | null;
}): string {
  const chunks = [
    sanitizeCsvFilenamePart(parts.reportName),
    parts.scopeLabel ? sanitizeCsvFilenamePart(parts.scopeLabel) : null,
    parts.fromDate ? sanitizeCsvFilenamePart(parts.fromDate) : null,
    parts.toDate && parts.toDate !== parts.fromDate
      ? sanitizeCsvFilenamePart(parts.toDate)
      : null,
  ].filter(Boolean);
  return `${chunks.join("_")}.csv`;
}

export function downloadCsvFile(filename: string, csvText: string): void {
  const blob = new Blob([csvText], { type: "text/csv;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = filename;
  anchor.rel = "noopener";
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  URL.revokeObjectURL(url);
}
