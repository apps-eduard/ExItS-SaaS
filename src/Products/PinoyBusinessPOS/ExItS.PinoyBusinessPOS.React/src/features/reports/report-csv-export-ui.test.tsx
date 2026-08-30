import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { OperationalReportPage } from "@/features/reports/OperationalReportPage";
import * as posReportingClient from "@/api/pos/pos-reporting-client";
import * as csvExport from "@/features/reports/report-csv-export";
import { FEATURE_STORE_ADVANCED_REPORTS, FEATURE_STORE_EXPORT } from "@/access/pos-capabilities";
import { TEST_BRANCH_A_ID, TEST_ORG_A_ID } from "@/test/session-context";

const workspaceState = {
  boundWorkspace: {
    organizationId: TEST_ORG_A_ID,
    organizationDisplayName: "Kizzy Store",
    branchId: TEST_BRANCH_A_ID,
    branchName: "Main Branch",
    experience: "manage_business" as const,
  },
  sessionGrant: {
    accessToken: "token",
    productAccessAllowed: true,
    mappedPosRoleCode: "Owner",
    productLocalRoleCode: "Owner",
    featureCodes: [FEATURE_STORE_ADVANCED_REPORTS, FEATURE_STORE_EXPORT],
  } as {
    accessToken: string;
    productAccessAllowed: boolean;
    mappedPosRoleCode: string;
    productLocalRoleCode: string;
    featureCodes: string[];
  },
};

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: workspaceState.boundWorkspace,
    sessionGrant: workspaceState.sessionGrant,
  }),
}));

const getSalesByProductReport = vi.spyOn(posReportingClient, "getSalesByProductReport");
const triggerDownload = vi.spyOn(csvExport, "triggerReportCsvDownload");

function renderSalesByProduct() {
  return render(
    <AppProviders>
      <MemoryRouter initialEntries={["/reports/operational/sales-by-product"]}>
        <Routes>
          <Route path="/reports/operational/:kind" element={<OperationalReportPage />} />
          <Route path="/reports" element={<div>hub</div>} />
        </Routes>
      </MemoryRouter>
    </AppProviders>,
  );
}

describe("OperationalReportPage CSV export", () => {
  beforeEach(() => {
    workspaceState.sessionGrant.featureCodes = [
      FEATURE_STORE_ADVANCED_REPORTS,
      FEATURE_STORE_EXPORT,
    ];
    getSalesByProductReport.mockReset();
    triggerDownload.mockReset();
    getSalesByProductReport.mockResolvedValue({
      fromDate: "2026-08-30",
      toDate: "2026-08-30",
      rows: [
        {
          productId: "p1",
          productName: "Tinapay",
          unitOfMeasure: "pc",
          sellingMode: "Unit",
          quantitySold: 3,
          quantityReturned: 0,
          netQuantity: 3,
          grossSaleAmount: 90,
          refundAmount: 0,
          netAmount: 90,
          preDiscountGrossSaleAmount: 90,
          commercialDiscountAmount: 0,
        },
      ],
    });
  });

  it("shows Export CSV for entitled users and downloads filtered data once", async () => {
    const user = userEvent.setup();
    renderSalesByProduct();

    expect(await screen.findByTestId("report-export-csv")).toBeInTheDocument();
    await user.click(screen.getByTestId("report-export-csv"));

    await waitFor(() => {
      expect(triggerDownload).toHaveBeenCalledTimes(1);
    });
    const result = triggerDownload.mock.calls[0]![0]!;
    expect(result.csvText).toContain("Tinapay");
    expect(result.filename).toContain("sales-by-product");
    expect(result.filename).toContain("main-branch");
    expect(getSalesByProductReport).toHaveBeenCalled();
  });

  it("hides Export CSV when store-export entitlement is missing", async () => {
    workspaceState.sessionGrant.featureCodes = [FEATURE_STORE_ADVANCED_REPORTS];
    renderSalesByProduct();
    expect(await screen.findByTestId("operational-report-page")).toBeInTheDocument();
    expect(screen.queryByTestId("report-export-csv")).not.toBeInTheDocument();
  });

  it("shows export failure without downloading", async () => {
    const user = userEvent.setup();
    renderSalesByProduct();
    expect(await screen.findByTestId("report-export-csv")).toBeInTheDocument();
    getSalesByProductReport.mockRejectedValueOnce(new Error("boom"));
    await user.click(screen.getByTestId("report-export-csv"));
    expect(await screen.findByTestId("report-export-error")).toBeInTheDocument();
    expect(triggerDownload).not.toHaveBeenCalled();
  });
});

describe("customer-facing surfaces", () => {
  it("keeps report CSV export out of customer-ordering source", async () => {
    const { readdirSync, readFileSync, statSync } = await import("node:fs");
    const { join } = await import("node:path");
    const root = join(process.cwd(), "src/features/customer-ordering");

    function walk(dir: string): string[] {
      const entries = readdirSync(dir);
      const files: string[] = [];
      for (const entry of entries) {
        const full = join(dir, entry);
        if (statSync(full).isDirectory()) {
          files.push(...walk(full));
        } else if (/\.(ts|tsx)$/.test(entry)) {
          files.push(full);
        }
      }
      return files;
    }

    const files = walk(root);
    expect(files.length).toBeGreaterThan(0);
    for (const file of files) {
      const text = readFileSync(file, "utf8");
      expect(text).not.toMatch(/ReportCsvExportButton|report-csv-export|canExportData/);
    }
  });
});
