import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { ReportsHubPage } from "@/features/reports/ReportsHubPage";
import {
  FEATURE_STORE_ADVANCED_REPORTS,
  FEATURE_STORE_REPORTS_VIEW,
} from "@/access/pos-capabilities";
import {
  buildReportHubCatalog,
  filterReportHubEntries,
} from "@/features/reports/report-hub-catalog";
import { operationalReportNeedsDates } from "@/features/reports/report-access";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const useWorkspaceMock = vi.fn();

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => useWorkspaceMock(),
}));

function ownerGrant(extra: Record<string, unknown> = {}) {
  return {
    accessToken: "token",
    productAccessAllowed: true,
    mappedPosRoleCode: "Owner",
    productLocalRoleCode: "Owner",
    membershipRole: "OrganizationOwner",
    organizationManagementAuthority: true,
    featureCodes: [],
    grantedFeatureCodes: [],
    ...extra,
  };
}

function workspace(branchType: "Retail" | "Warehouse" = "Retail") {
  return {
    organizationId: "11111111-1111-1111-1111-111111111111",
    organizationDisplayName: "Kizy Store",
    branchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    branchName: branchType === "Warehouse" ? "Main Warehouse" : "Main",
    branchType,
    experience: "manage_business" as const,
  };
}

function renderHub() {
  return render(
    <AppProviders>
      <MemoryRouter initialEntries={["/reports"]}>
        <ReportsHubPage />
      </MemoryRouter>
    </AppProviders>,
  );
}

describe("POS-REPORTS-HUB-V2", () => {
  beforeEach(() => {
    useWorkspaceMock.mockReturnValue({
      boundWorkspace: workspace("Retail"),
      sessionGrant: ownerGrant({
        grantedFeatureCodes: [FEATURE_STORE_REPORTS_VIEW, FEATURE_STORE_ADVANCED_REPORTS],
        featureCodes: [FEATURE_STORE_REPORTS_VIEW, FEATURE_STORE_ADVANCED_REPORTS],
      }),
    });
  });

  it("removes duplicated Business/classic section and keeps Dashboard", async () => {
    renderHub();

    expect(screen.getByTestId("reports-hub-page")).toBeInTheDocument();
    expect(screen.getByTestId("reports-open-dashboard")).toHaveAttribute("href", "/dashboard");
    expect(screen.queryByTestId("reports-group-classic")).not.toBeInTheDocument();
    expect(screen.queryByText("Business reports")).not.toBeInTheDocument();
    expect(screen.queryByText("Classic reports")).not.toBeInTheDocument();

    expect(screen.getByTestId("reports-hub-categories")).toBeInTheDocument();
    expect(screen.getByTestId("reports-hub-category-overview")).toBeInTheDocument();
    expect(screen.getByTestId("reports-hub-category-sales")).toBeInTheDocument();
  });

  it("navigates categories and maps classic sales overview under Sales", async () => {
    const user = userEvent.setup();
    renderHub();

    await user.click(screen.getByTestId("reports-hub-category-sales"));
    const sales = screen.getByTestId("reports-group-sales");
    expect(within(sales).getByTestId("report-link-sales")).toHaveAttribute(
      "href",
      "/reports/sales",
    );
    expect(within(sales).getByTestId("report-link-sales-summary")).toHaveAttribute(
      "href",
      "/reports/operational/sales-summary",
    );
    expect(within(sales).queryByTestId("report-link-overview")).not.toBeInTheDocument();
  });

  it("searches across titles and descriptions", async () => {
    const user = userEvent.setup();
    renderHub();

    await user.type(screen.getByTestId("reports-hub-search"), "profit");
    expect(screen.getByTestId("reports-hub-search-results")).toBeInTheDocument();
    expect(screen.getByTestId("report-link-profitability")).toBeInTheDocument();
    expect(screen.getByTestId("report-link-product-profitability")).toBeInTheDocument();

    await user.clear(screen.getByTestId("reports-hub-search"));
    await user.type(screen.getByTestId("reports-hub-search"), "supplier");
    expect(screen.getByTestId("report-link-supplier-purchasing")).toBeInTheDocument();
    expect(screen.getByTestId("report-link-supplier-payables")).toBeInTheDocument();

    await user.clear(screen.getByTestId("reports-hub-search"));
    await user.type(screen.getByTestId("reports-hub-search"), "zzzz-no-match");
    expect(screen.getByTestId("reports-hub-empty")).toHaveTextContent(
      "No reports match your search.",
    );
  });

  it("hides Dashboard when canViewDashboard is false and keeps shift reports for cashier", () => {
    useWorkspaceMock.mockReturnValue({
      boundWorkspace: workspace("Retail"),
      sessionGrant: ownerGrant({
        mappedPosRoleCode: "Cashier",
        productLocalRoleCode: "Cashier",
        organizationManagementAuthority: false,
        membershipRole: "OrganizationMember",
        grantedFeatureCodes: [FEATURE_STORE_ADVANCED_REPORTS],
        featureCodes: [FEATURE_STORE_ADVANCED_REPORTS],
      }),
    });

    renderHub();

    expect(screen.queryByTestId("reports-open-dashboard")).not.toBeInTheDocument();
    expect(screen.queryByTestId("reports-hub-category-sales")).not.toBeInTheDocument();
    expect(screen.getByTestId("reports-hub-category-shifts")).toBeInTheDocument();
  });

  it("hides inaccessible sales for inventory staff but keeps inventory", () => {
    useWorkspaceMock.mockReturnValue({
      boundWorkspace: workspace("Retail"),
      sessionGrant: ownerGrant({
        mappedPosRoleCode: "InventoryStaff",
        productLocalRoleCode: "InventoryStaff",
        organizationManagementAuthority: false,
        membershipRole: "OrganizationMember",
        featureCodes: [FEATURE_STORE_ADVANCED_REPORTS],
        grantedFeatureCodes: [FEATURE_STORE_ADVANCED_REPORTS],
      }),
    });

    renderHub();

    expect(screen.queryByTestId("reports-hub-category-sales")).not.toBeInTheDocument();
    expect(screen.getByTestId("reports-hub-category-inventory")).toBeInTheDocument();
  });

  it("hides retail-only categories for Warehouse workspace", async () => {
    const user = userEvent.setup();
    useWorkspaceMock.mockReturnValue({
      boundWorkspace: workspace("Warehouse"),
      sessionGrant: ownerGrant({
        grantedFeatureCodes: [FEATURE_STORE_REPORTS_VIEW, FEATURE_STORE_ADVANCED_REPORTS],
        featureCodes: [FEATURE_STORE_REPORTS_VIEW, FEATURE_STORE_ADVANCED_REPORTS],
      }),
    });

    renderHub();

    expect(screen.queryByTestId("reports-hub-category-sales")).not.toBeInTheDocument();
    expect(screen.queryByTestId("reports-hub-category-utang")).not.toBeInTheDocument();
    expect(screen.queryByTestId("reports-hub-category-shifts")).not.toBeInTheDocument();
    expect(screen.getByTestId("reports-hub-category-inventory")).toBeInTheDocument();
    expect(screen.getByTestId("reports-hub-category-purchasing")).toBeInTheDocument();

    await user.click(screen.getByTestId("reports-hub-category-inventory"));
    expect(screen.getByTestId("report-link-inventory-status")).toBeInTheDocument();
  });

  it("shows one upgrade message on Growth without locked advanced card walls", () => {
    useWorkspaceMock.mockReturnValue({
      boundWorkspace: workspace("Retail"),
      sessionGrant: ownerGrant({
        grantedFeatureCodes: [FEATURE_STORE_REPORTS_VIEW],
        featureCodes: [FEATURE_STORE_REPORTS_VIEW],
      }),
    });

    renderHub();

    expect(screen.getByTestId("reports-hub-upgrade")).toBeInTheDocument();
    expect(screen.getByTestId("reports-hub-view-plan")).toHaveAttribute("href", "/org");
    expect(screen.queryByTestId("report-link-purchasing-summary")).not.toBeInTheDocument();
    expect(screen.getByTestId("report-link-sales")).toBeInTheDocument();
  });

  it("preserves snapshot vs date-range operational kinds", () => {
    expect(operationalReportNeedsDates("inventory-status")).toBe(false);
    expect(operationalReportNeedsDates("purchase-outstanding")).toBe(false);
    expect(operationalReportNeedsDates("supplier-payables")).toBe(false);
    expect(operationalReportNeedsDates("sales-summary")).toBe(true);
  });

  it("keeps Retail catalog categories when branch is Retail", () => {
    const catalog = buildReportHubCatalog(
      ownerGrant({
        grantedFeatureCodes: [FEATURE_STORE_REPORTS_VIEW, FEATURE_STORE_ADVANCED_REPORTS],
        featureCodes: [FEATURE_STORE_REPORTS_VIEW, FEATURE_STORE_ADVANCED_REPORTS],
      }),
      { branchType: "Retail" },
    );
    expect(catalog.categories).toEqual(
      expect.arrayContaining(["overview", "sales", "inventory", "purchasing", "expenses", "utang", "shifts"]),
    );
  });

  it("filters search helper for stock terms", () => {
    const catalog = buildReportHubCatalog(
      ownerGrant({
        grantedFeatureCodes: [FEATURE_STORE_REPORTS_VIEW, FEATURE_STORE_ADVANCED_REPORTS],
        featureCodes: [FEATURE_STORE_REPORTS_VIEW, FEATURE_STORE_ADVANCED_REPORTS],
      }),
      { branchType: "Retail" },
    );
    const matches = filterReportHubEntries(catalog.entries, "overview", "stock", (entry) => ({
      title: entry.titleKey,
      description: entry.descriptionKey,
    }));
    expect(matches.map((item) => item.id)).toEqual(
      expect.arrayContaining([
        "operational:inventory-status",
        "operational:inventory-movements",
        "operational:stock-count-variance",
      ]),
    );
  });

  it("does not reintroduce Classic reports wording in locale sources", () => {
    const root = resolve(dirname(fileURLToPath(import.meta.url)), "../../i18n/locales");
    for (const file of ["en.ts", "fil-PH.ts", "ceb-PH.ts", "hil-PH.ts", "ilo-PH.ts"]) {
      const text = readFileSync(resolve(root, file), "utf8");
      expect(text, file).not.toMatch(/Classic reports/);
      expect(text, file).toContain('"reports.hub.searchPlaceholder"');
      expect(text, file).toContain('"reports.lede": "Analyze your business across the locations you can access."');
    }
  });
});
