import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";

const globalsCss = readFileSync(resolve(__dirname, "../../styles/globals.css"), "utf8");
const pageHeaderSrc = readFileSync(resolve(__dirname, "./PageHeader.tsx"), "utf8");
const reportsSrc = readFileSync(
  resolve(__dirname, "../../features/reports/ReportsHubPage.tsx"),
  "utf8",
);
const sellSrc = readFileSync(
  resolve(__dirname, "../../features/sell/SellFloorPage.tsx"),
  "utf8",
);
const sellCategorySrc = readFileSync(
  resolve(__dirname, "../../features/sell/SellCategoryFilter.tsx"),
  "utf8",
);

describe("POS-SELL-REPORTS-PAGE-HEADER-GUTTER-COMPLETION-19", () => {
  it("Reports uses shared PageHeader + SearchField + wide exits-page", () => {
    expect(reportsSrc).toContain("<PageHeader");
    expect(reportsSrc).toContain('t("reports.lede")');
    expect(reportsSrc).toContain("SearchField");
    expect(reportsSrc).toContain("data-testid=\"reports-hub-search\"");
    expect(reportsSrc).toContain("ExitsChipBar");
    expect(reportsSrc).toContain("exits-page");
    expect(globalsCss).toMatch(/\.reports-hub-page\s*\{[\s\S]*?max-width:\s*80rem/);
    expect(globalsCss).not.toMatch(/\.reports-hub-page\s*\{[\s\S]*?max-width:\s*56rem/);
  });

  it("Sell uses compact shared PageHeader and FilterChip actions", () => {
    expect(sellSrc).toContain('variant="compact"');
    expect(sellSrc).toContain("<PageHeader");
    expect(sellSrc).toContain("FilterChip");
    expect(sellSrc).toContain("sell-info-toggle");
    expect(sellSrc).toContain("sell-out-of-stock-toggle");
    expect(sellSrc).toContain("sell.exitSelling");
    expect(sellSrc).toContain("SearchField");
    expect(sellSrc).not.toContain("sell-floor-toolbar__heading");
    expect(pageHeaderSrc).toContain('variant?: PageHeaderVariant');
    expect(pageHeaderSrc).toContain('compact');
    expect(globalsCss).toContain("--exits-page-gutter-ops-inline");
    expect(globalsCss).toContain("page-header--compact");
  });

  it("Sell category filters use FilterChip (Control Shape)", () => {
    expect(sellCategorySrc).toContain("FilterChip");
    expect(sellCategorySrc).toContain('selected={pressed}');
  });
});
