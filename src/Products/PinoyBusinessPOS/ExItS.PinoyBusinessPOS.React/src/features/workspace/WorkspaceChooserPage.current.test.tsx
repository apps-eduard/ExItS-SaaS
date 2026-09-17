import { describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";
import { WorkspaceChooserPage } from "@/features/workspace/WorkspaceChooserPage";

const ids = vi.hoisted(() => ({
  orgId: "11111111-1111-1111-1111-111111111111",
  areaId: "aaaa1111-1111-1111-1111-111111111111",
  whId: "cccc1111-1111-1111-1111-111111111111",
  retailId: "dddd1111-1111-1111-1111-111111111111",
  grant: {
    accessToken: "t",
    productAccessAllowed: true,
    mappedPosRoleCode: "Owner",
    productLocalRoleCode: "Owner",
    organizationManagementAuthority: true,
    membershipRole: "OrganizationOwner",
  },
}));

vi.mock("@/api/platform/organization-branches-client", async (importOriginal) => {
  const actual =
    await importOriginal<typeof import("@/api/platform/organization-branches-client")>();
  return {
    ...actual,
    listBranchManagementSummaries: vi.fn(async () => ({
      ok: true,
      value: [],
    })),
  };
});

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    status: "ready",
    workspaces: [
      {
        organizationId: ids.orgId,
        displayName: "Kizy Store",
        branches: [
          {
            branchId: ids.whId,
            name: "Panay Warehouse",
            secondaryLine: "Active",
            isPrimary: false,
            isActive: true,
            areaId: ids.areaId,
            areaName: "Pacifica Nort Area",
            branchType: "Warehouse",
          },
          {
            branchId: ids.retailId,
            name: "Main Branch",
            secondaryLine: "Active",
            isPrimary: true,
            isActive: true,
            areaId: ids.areaId,
            areaName: "Pacifica Nort Area",
            branchType: "Retail",
          },
        ],
      },
    ],
    boundWorkspace: {
      organizationId: ids.orgId,
      organizationDisplayName: "Kizy Store",
      branchId: ids.whId,
      branchName: "Panay Warehouse",
      branchType: "Warehouse",
      areaId: ids.areaId,
      areaName: "Pacifica Nort Area",
      experience: "operations",
    },
    accessDeniedDetail: null,
    bindFailureKind: null,
    failureDiagnostic: null,
    bindDestination: vi.fn(async () => true),
    grantByOrganizationId: new Map([[ids.orgId, ids.grant]]),
    grantProbeFailureByOrganizationId: new Map(),
    ensureOrganizationGrantHint: vi.fn(async () => ids.grant),
    retryOrganizationGrantHint: vi.fn(async () => undefined),
  }),
}));

describe("WorkspaceChooserPage current location marker", () => {
  it("marks the bound location Current and never marks Area groups", async () => {
    const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    render(
      <QueryClientProvider client={client}>
        <PreferencesProvider>
          <I18nProvider>
            <MemoryRouter initialEntries={["/workspace"]}>
              <WorkspaceChooserPage />
            </MemoryRouter>
          </I18nProvider>
        </PreferencesProvider>
      </QueryClientProvider>,
    );

    await waitFor(() => {
      expect(screen.getByTestId(`workspace-branch-${ids.whId}`)).toBeInTheDocument();
    });

    expect(screen.getByTestId(`workspace-branch-${ids.whId}`)).toHaveAttribute("data-current", "true");
    expect(screen.getByTestId(`workspace-branch-current-${ids.whId}`)).toHaveTextContent("Current");
    expect(screen.getByTestId(`workspace-branch-${ids.retailId}`)).toHaveAttribute(
      "data-current",
      "false",
    );
    expect(screen.queryByTestId(`workspace-branch-current-${ids.retailId}`)).not.toBeInTheDocument();

    const area = screen.getByTestId(`workspace-area-group-${ids.areaId}`);
    expect(within(area).getByRole("heading", { name: "Pacifica Nort Area" })).toBeInTheDocument();
    expect(within(area).getByTestId(`workspace-branch-current-${ids.whId}`)).toBeInTheDocument();
    expect(area.getAttribute("data-current")).toBeNull();
  });
});
