import { describe, expect, it, vi, afterEach } from "vitest";
import {
  buildCsv,
  buildCsvWithMetadata,
  buildReportCsvFilename,
  downloadCsvFile,
  formatCsvCell,
  neutralizeCsvInjection,
  sanitizeCsvFilenamePart,
} from "@/lib/csv";

describe("csv helpers", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("escapes commas, quotes, and newlines", () => {
    const csv = buildCsv(
      {
        headers: ["Name", "Note"],
        rows: [["Rice, 5kg", 'He said "ok"'], ["Line1\nLine2", "plain"]],
      },
      { includeBom: false },
    );
    expect(csv).toContain('"Rice, 5kg"');
    expect(csv).toContain('"He said ""ok"""');
    expect(csv).toContain('"Line1\nLine2"');
  });

  it("preserves Unicode Philippine text", () => {
    const csv = buildCsv(
      {
        headers: ["Produkto"],
        rows: [["Tinapay nga bugas"], ["Kape"], ["Gatas"]],
      },
      { includeBom: false },
    );
    expect(csv).toContain("Tinapay nga bugas");
    expect(csv).toContain("Kape");
  });

  it("treats null and undefined as empty cells", () => {
    expect(formatCsvCell(null)).toBe("");
    expect(formatCsvCell(undefined)).toBe("");
    expect(buildCsv({ headers: ["A"], rows: [[null], [undefined]] }, { includeBom: false })).toBe(
      "A\r\n\r\n",
    );
  });

  it("keeps negative numeric values numeric", () => {
    expect(formatCsvCell(-12.5)).toBe("-12.5");
    expect(neutralizeCsvInjection("-12.5")).toBe("'-12.5");
    const csv = buildCsv({ headers: ["Amount"], rows: [[-12.5]] }, { includeBom: false });
    expect(csv).toBe("Amount\r\n-12.5");
  });

  it("neutralizes textual formula injection prefixes", () => {
    expect(formatCsvCell("=1+1")).toBe("'=1+1");
    expect(formatCsvCell("+cmd")).toBe("'+cmd");
    expect(formatCsvCell("-total")).toBe("'-total");
    expect(formatCsvCell("@sum")).toBe("'@sum");
  });

  it("includes UTF-8 BOM by default for Excel PH locales", () => {
    const csv = buildCsv({ headers: ["A"], rows: [["1"]] });
    expect(csv.startsWith("\uFEFF")).toBe(true);
  });

  it("builds metadata then blank line then table", () => {
    const csv = buildCsvWithMetadata(
      [
        ["Report", "product-profitability"],
        ["Scope", "Main Branch"],
      ],
      { headers: ["Product"], rows: [["Milk"]] },
      { includeBom: false },
    );
    expect(csv).toBe("Report,product-profitability\r\nScope,Main Branch\r\n\r\nProduct\r\nMilk");
  });

  it("sanitizes filesystem-safe filename parts", () => {
    expect(sanitizeCsvFilenamePart("Main Branch / A")).toBe("main-branch-a");
    expect(buildReportCsvFilename({
      reportName: "product-profitability",
      scopeLabel: "Main Branch",
      fromDate: "2026-08-01",
      toDate: "2026-08-30",
    })).toBe("product-profitability_main-branch_2026-08-01_2026-08-30.csv");
  });

  it("triggers a single object-URL download", () => {
    const createObjectURL = vi.fn(() => "blob:csv");
    const revokeObjectURL = vi.fn();
    vi.stubGlobal("URL", { createObjectURL, revokeObjectURL });
    const click = vi.fn();
    const remove = vi.fn();
    const appendChild = vi.spyOn(document.body, "appendChild").mockImplementation((node) => node);
    vi.spyOn(document, "createElement").mockImplementation(() => {
      return {
        href: "",
        download: "",
        rel: "",
        click,
        remove,
      } as unknown as HTMLAnchorElement;
    });

    downloadCsvFile("sales.csv", "A\r\n1");

    expect(createObjectURL).toHaveBeenCalledTimes(1);
    expect(click).toHaveBeenCalledTimes(1);
    expect(revokeObjectURL).toHaveBeenCalledWith("blob:csv");
    appendChild.mockRestore();
  });
});
