import { describe, expect, it } from "vitest";
import {
  BUYER_PURCHASE_PROJECTION_PATH_MARKERS,
  FAKE_PL_PATH_MARKERS,
  TAX_REPORT_PATH_MARKERS,
  dashboardPath,
  formatReportPaymentMethod,
  managementOverviewPath,
  salesByPaymentPath,
  salesSummaryPath,
} from "@/api/pos/pos-reporting-client";
import {
  canAccessOperationalReport,
  canAccessReportsHub,
  canViewDashboard,
  canViewReports,
} from "@/features/reports/report-access";
import { isReportRangeValid, resolveReportDatePreset } from "@/features/reports/report-date-range";
import type { SessionGrantResponse } from "@/api/platform/platform-auth-client";

function grant(partial: Partial<SessionGrantResponse>): SessionGrantResponse {
  return {
    accessToken: "token",
    productAccessAllowed: true,
    ...partial,
  };
}

describe("report-date-range", () => {
  it("resolves presets to explicit UTC dates", () => {
    const now = new Date(Date.UTC(2026, 7, 21)); // Fri
    expect(resolveReportDatePreset("today", now)).toEqual({
      fromDate: "2026-08-21",
      toDate: "2026-08-21",
    });
    expect(resolveReportDatePreset("yesterday", now)).toEqual({
      fromDate: "2026-08-20",
      toDate: "2026-08-20",
    });
    expect(resolveReportDatePreset("thisWeek", now)).toEqual({
      fromDate: "2026-08-17",
      toDate: "2026-08-21",
    });
    expect(resolveReportDatePreset("thisMonth", now)).toEqual({
      fromDate: "2026-08-01",
      toDate: "2026-08-21",
    });
  });

  it("validates inclusive span", () => {
    expect(isReportRangeValid({ fromDate: "2026-01-01", toDate: "2026-01-01" })).toBe(true);
    expect(isReportRangeValid({ fromDate: "2026-01-02", toDate: "2026-01-01" })).toBe(false);
  });
});

describe("report-access", () => {
  it("allows owner dashboard and reports; denies cashier dashboard", () => {
    const owner = grant({ mappedPosRoleCode: "Owner", productLocalRoleCode: "Owner" });
    const cashier = grant({ mappedPosRoleCode: "Cashier", productLocalRoleCode: "Cashier" });
    expect(canViewDashboard(owner)).toBe(true);
    expect(canViewReports(owner)).toBe(true);
    expect(canAccessReportsHub(owner)).toBe(true);
    expect(canViewDashboard(cashier)).toBe(false);
    expect(canViewReports(cashier)).toBe(false);
    expect(canAccessReportsHub(cashier)).toBe(true); // shifts
    expect(canAccessOperationalReport(cashier, "sales-summary")).toBe(false);
    expect(canAccessOperationalReport(cashier, "shifts")).toBe(true);
  });

  it("allows inventory staff inventory/purchasing reports only", () => {
    const staff = grant({
      mappedPosRoleCode: "InventoryStaff",
      productLocalRoleCode: "InventoryStaff",
    });
    expect(canAccessOperationalReport(staff, "inventory-status")).toBe(true);
    expect(canAccessOperationalReport(staff, "purchasing-summary")).toBe(true);
    expect(canAccessOperationalReport(staff, "sales-summary")).toBe(false);
  });
});

describe("pos-reporting-client paths", () => {
  it("builds management overview and dated report paths", () => {
    expect(managementOverviewPath()).toBe("/api/v1/pos/management/overview");
    expect(dashboardPath({ fromDate: "2026-08-01", toDate: "2026-08-21" })).toBe(
      "/api/v1/pos/dashboard?fromDate=2026-08-01&toDate=2026-08-21",
    );
    expect(salesSummaryPath({ fromDate: "2026-08-21", toDate: "2026-08-21" })).toContain(
      "sales-summary",
    );
    expect(salesByPaymentPath({ fromDate: "2026-08-21", toDate: "2026-08-21" })).toContain(
      "sales-by-payment",
    );
  });

  it("formats payment methods without tax terminology", () => {
    expect(formatReportPaymentMethod("Cash")).toBe("Cash");
    expect(formatReportPaymentMethod("ManualGCash")).toBe("GCash");
    expect(formatReportPaymentMethod("Utang")).toBe("Utang");
  });

  it("documents excluded tax / fake P&L / buyer projection markers", () => {
    expect(TAX_REPORT_PATH_MARKERS.some((m) => m.includes("tax"))).toBe(true);
    expect(FAKE_PL_PATH_MARKERS.some((m) => m.includes("pnl"))).toBe(true);
    expect(BUYER_PURCHASE_PROJECTION_PATH_MARKERS.some((m) => m.includes("purchase"))).toBe(true);
  });
});

describe("report user-facing terminology boundary", () => {
  it("keeps ManualGCash labeled as GCash", () => {
    expect(formatReportPaymentMethod("ManualGCash")).toBe("GCash");
  });

  it("does not expose roadmap/developer wording in normal message catalogs", async () => {
    const { catalogs } = await import("@/i18n/messages");
    const forbidden = [
      "RMAP_TAX_AUTHORIZED",
      "RMAP-B04",
      "contracts are not proven",
      "backend supports",
      "deferred until the backend",
      "purchase-history projection",
      "not invented here",
      "report contract",
    ];
    for (const [locale, catalog] of Object.entries(catalogs)) {
      for (const [key, value] of Object.entries(catalog)) {
        for (const needle of forbidden) {
          expect(value, `${locale}:${key}`).not.toContain(needle);
        }
        expect(value, `${locale}:${key}`).not.toContain("ManualGCash");
      }
      expect(catalog["connected.guidRejected"], `${locale}:connected.guidRejected`).not.toMatch(
        /\bGuid\b/,
      );
      expect(catalog["connected.requestHelp"], `${locale}:connected.requestHelp`).not.toMatch(
        /\bGuid\b/,
      );
    }
  });
});
