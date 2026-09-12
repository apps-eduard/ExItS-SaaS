import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";

const globalsCss = readFileSync(resolve(__dirname, "../../styles/globals.css"), "utf8");
const pageHeaderSrc = readFileSync(resolve(__dirname, "./PageHeader.tsx"), "utf8");
const managerHomeSrc = readFileSync(
  resolve(__dirname, "../../features/role/ManagerRetailHome.tsx"),
  "utf8",
);
const dashboardSrc = readFileSync(
  resolve(__dirname, "../../features/reports/ManagementDashboardPage.tsx"),
  "utf8",
);
const shiftsSrc = readFileSync(
  resolve(__dirname, "../../features/shifts/ShiftsHubPage.tsx"),
  "utf8",
);
const expensesSrc = readFileSync(
  resolve(__dirname, "../../features/expenses/ExpenseListPage.tsx"),
  "utf8",
);
const customersSrc = readFileSync(
  resolve(__dirname, "../../features/customers/CustomersListPage.tsx"),
  "utf8",
);
const inventorySrc = readFileSync(
  resolve(__dirname, "../../features/inventory/InventoryListPage.tsx"),
  "utf8",
);
const purchasingSrc = readFileSync(
  resolve(__dirname, "../../features/purchasing/PurchasingHubPage.tsx"),
  "utf8",
);
const suppliersSrc = readFileSync(
  resolve(__dirname, "../../features/suppliers/SuppliersListPage.tsx"),
  "utf8",
);

describe("POS-GLOBAL-CONTENT-GUTTER-AND-PAGE-HEADER-STANDARD-18", () => {
  it("defines semantic page gutter tokens for density modes", () => {
    expect(globalsCss).toContain("--exits-page-gutter-inline: 1.25rem");
    expect(globalsCss).toMatch(/\[data-density="compact"\][\s\S]*?--exits-page-gutter-inline:\s*1rem/);
    expect(globalsCss).toMatch(/\[data-density="comfort"\][\s\S]*?--exits-page-gutter-inline:\s*1\.5rem/);
    expect(globalsCss).toContain("--exits-page-gutter-block");
    expect(globalsCss).toContain("padding-inline-start: var(--exits-page-gutter-inline)");
  });

  it("applies desktop content gutter without shell start gap (no seam)", () => {
    expect(globalsCss).toMatch(
      /\.operations-shell--viewport-lock:not\(\.operations-shell--sell-floor\) \.operations-shell__content[\s\S]*?padding-inline-start:\s*var\(--exits-page-gutter-inline\)/,
    );
    expect(globalsCss).toMatch(
      /\.admin-shell__content[\s\S]*?padding-inline-start:\s*var\(--exits-page-gutter-inline\)/,
    );
    expect(globalsCss).not.toMatch(/operations-shell__column[\s\S]{0,200}padding-inline-start:\s*var\(--exits-page-gutter/);
  });

  it("styles PageHeader as a calm structural surface", () => {
    expect(globalsCss).toMatch(
      /\.page-header\s*\{[\s\S]*?background:\s*var\(--exits-surface\)[\s\S]*?box-shadow:\s*none/,
    );
    expect(pageHeaderSrc).toContain('data-testid="page-header"');
    expect(pageHeaderSrc).toContain("descriptionCollapsible = false");
    expect(pageHeaderSrc).toContain("actions");
    expect(pageHeaderSrc).toContain("rtl:rotate-180");
  });

  it("representative pages use shared PageHeader", () => {
    for (const src of [
      managerHomeSrc,
      dashboardSrc,
      shiftsSrc,
      expensesSrc,
      customersSrc,
      inventorySrc,
      purchasingSrc,
      suppliersSrc,
    ]) {
      expect(src).toContain("PageHeader");
      expect(src).toContain("<PageHeader");
    }
  });
});
