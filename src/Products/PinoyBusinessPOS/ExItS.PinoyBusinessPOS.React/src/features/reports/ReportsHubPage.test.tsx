import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { ReportsHubPage } from "@/features/reports/ReportsHubPage";
import { FEATURE_STORE_ADVANCED_REPORTS, FEATURE_STORE_REPORTS_VIEW } from "@/access/pos-capabilities";
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
      boundWorkspace: {
        organizationId: "11111111-1111-1111-1111-111111111111",
        organizationDisplayName: "Kizy Store",
        branchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        branchName: "Main",
        experience: "manage_business",
      },
      sessionGrant: ownerGrant({
        grantedFeatureCodes: [FEATURE_STORE_REPORTS_VIEW, FEATURE_STORE_ADVANCED_REPORTS],
        featureCodes: [FEATURE_STORE_REPORTS_VIEW, FEATURE_STORE_ADVANCED_REPORTS],
      }),
    });
  });

  it("renders Dashboard and Business reports cards with correct routes", async () => {
    renderHub();

    await waitFor(() => {
      expect(screen.getByTestId("reports-hub-page")).toBeInTheDocument();
    });

    expect(screen.getByTestId("reports-group-overview")).toBeInTheDocument();
    expect(screen.getByTestId("reports-group-classic")).toHaveTextContent("Business reports");
    expect(screen.queryByText("Classic reports")).not.toBeInTheDocument();

    expect(screen.getByTestId("reports-open-dashboard")).toHaveAttribute("href", "/dashboard");
    expect(screen.getByTestId("report-link-sales")).toHaveAttribute("href", "/reports/sales");
    expect(screen.getByTestId("report-link-utang")).toHaveAttribute("href", "/reports/utang");
    expect(screen.getByTestId("report-link-inventory")).toHaveAttribute(
      "href",
      "/reports/inventory",
    );
    expect(screen.getByTestId("report-link-expenses")).toHaveAttribute(
      "href",
      "/reports/expenses",
    );

    expect(screen.getByTestId("reports-classic-grid").className).toContain("reports-hub-grid");
  });

  it("hides Dashboard section when canViewDashboard is false", () => {
    useWorkspaceMock.mockReturnValue({
      boundWorkspace: {
        organizationId: "11111111-1111-1111-1111-111111111111",
        branchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        experience: "operations",
      },
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

    expect(screen.queryByTestId("reports-group-overview")).not.toBeInTheDocument();
    expect(screen.queryByTestId("reports-group-classic")).not.toBeInTheDocument();
    expect(screen.getByTestId("reports-group-shifts")).toBeInTheDocument();
  });

  it("hides Business reports when canViewReports is false but keeps operational inventory group", () => {
    useWorkspaceMock.mockReturnValue({
      boundWorkspace: {
        organizationId: "11111111-1111-1111-1111-111111111111",
        branchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        experience: "operations",
      },
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

    expect(screen.queryByTestId("reports-group-classic")).not.toBeInTheDocument();
    expect(screen.queryByTestId("reports-group-overview")).not.toBeInTheDocument();
    expect(screen.getByTestId("reports-group-inventory")).toBeInTheDocument();
  });

  it("removes Classic reports wording from React POS locale resources", () => {
    const root = resolve(dirname(fileURLToPath(import.meta.url)), "../../i18n/locales");
    for (const file of ["en.ts", "fil-PH.ts", "ceb-PH.ts", "hil-PH.ts", "ilo-PH.ts"]) {
      const text = readFileSync(resolve(root, file), "utf8");
      expect(text, file).not.toMatch(/Classic reports/);
      expect(text, file).toContain('"reports.classicSection": "Business reports"');
      expect(text, file).toContain('"reports.hub.dashboardDetail"');
    }
  });
});
