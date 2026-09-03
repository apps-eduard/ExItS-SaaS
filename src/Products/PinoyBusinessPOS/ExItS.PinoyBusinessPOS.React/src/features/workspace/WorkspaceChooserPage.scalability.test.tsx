import "fake-indexeddb/auto";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";
import { SessionProvider } from "@/session/SessionProvider";
import { WorkspaceProvider } from "@/workspace/WorkspaceProvider";
import { WorkspaceChooserPage } from "@/features/workspace/WorkspaceChooserPage";
import { jsonResponse } from "@/test/render";
import { isOfflinePinAndDekConfigured } from "@/offline/local-store-key";
import * as branchClient from "@/api/platform/organization-branches-client";
import * as membersClient from "@/api/platform/organization-members-client";
import * as assignmentsClient from "@/api/platform/membership-branch-assignments-client";

const E2E_ORG_ID = "11111111-1111-1111-1111-111111111111";
const E2E_BRANCH_ID = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const E2E_BRANCH_2_ID = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
const E2E_BRANCH_3_ID = "cccccccc-cccc-cccc-cccc-cccccccccccc";

const probeMock = vi.fn();
const listEligibleOrganizations = vi.fn();
const listOrganizationBranches = vi.fn();

vi.mock("@/offline/local-store-key", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/offline/local-store-key")>();
  return {
    ...actual,
    isOfflinePinAndDekConfigured: vi.fn(() => true),
  };
});

vi.mock("@/api/platform/platform-auth-client", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/api/platform/platform-auth-client")>();
  return {
    ...actual,
    listEligibleOrganizations: (...args: unknown[]) => listEligibleOrganizations(...args),
    listOrganizationBranches: (...args: unknown[]) => listOrganizationBranches(...args),
    probeOrganizationSessionGrant: (...args: unknown[]) => probeMock(...args),
  };
});

vi.mock("@/api/platform/organization-branches-client", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/api/platform/organization-branches-client")>();
  return {
    ...actual,
    listBranchManagementSummaries: vi.fn(),
  };
});

vi.mock("@/api/platform/organization-members-client", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/api/platform/organization-members-client")>();
  return {
    ...actual,
    listOrganizationMembers: vi.fn(),
  };
});

vi.mock("@/api/platform/membership-branch-assignments-client", async (importOriginal) => {
  const actual =
    await importOriginal<typeof import("@/api/platform/membership-branch-assignments-client")>();
  return {
    ...actual,
    listMembershipBranchAssignments: vi.fn(),
  };
});

function ownerGrantProbe() {
  return {
    ok: true as const,
    grant: {
      accessToken: "probe-token",
      productAccessAllowed: true,
      mappedPosRoleCode: "Owner",
      productLocalRoleCode: "Owner",
      organizationManagementAuthority: true,
      membershipRole: "OrganizationOwner",
    },
  };
}

function managerGrantProbe() {
  return {
    ok: true as const,
    grant: {
      accessToken: "probe-token",
      productAccessAllowed: true,
      mappedPosRoleCode: "StoreManager",
      productLocalRoleCode: "StoreManager",
      organizationManagementAuthority: false,
      membershipRole: "OrganizationMember",
    },
  };
}

function allBranches() {
  return [
    {
      id: E2E_BRANCH_ID,
      organizationId: E2E_ORG_ID,
      code: "MAIN",
      name: "Main Branch",
      isPrimary: true,
      status: "Active",
    },
    {
      id: E2E_BRANCH_2_ID,
      organizationId: E2E_ORG_ID,
      code: "K02",
      name: "Kizy Store 02",
      isPrimary: false,
      status: "Active",
    },
    {
      id: E2E_BRANCH_3_ID,
      organizationId: E2E_ORG_ID,
      code: "K03",
      name: "Warehouse",
      isPrimary: false,
      status: "Active",
    },
  ];
}

function renderWorkspaceChooser() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <PreferencesProvider>
        <I18nProvider>
          <SessionProvider>
            <MemoryRouter initialEntries={["/workspace"]}>
              <WorkspaceProvider>
                <WorkspaceChooserPage />
              </WorkspaceProvider>
            </MemoryRouter>
          </SessionProvider>
        </I18nProvider>
      </PreferencesProvider>
    </QueryClientProvider>,
  );
}

describe("workspace chooser scalable branch cards", () => {
  beforeEach(() => {
    probeMock.mockReset();
    listEligibleOrganizations.mockReset();
    listOrganizationBranches.mockReset();
    vi.mocked(branchClient.listBranchManagementSummaries).mockReset();
    vi.mocked(membersClient.listOrganizationMembers).mockReset();
    vi.mocked(assignmentsClient.listMembershipBranchAssignments).mockReset();
    vi.mocked(isOfflinePinAndDekConfigured).mockReturnValue(true);

    listEligibleOrganizations.mockResolvedValue({
      ok: true,
      organizations: [
        {
          organizationId: E2E_ORG_ID,
          displayName: "Kizy Store",
          slug: "kizy-store",
          membershipRole: "OrganizationOwner",
        },
      ],
    });
    listOrganizationBranches.mockResolvedValue({
      ok: true,
      branches: allBranches(),
    });
    vi.mocked(branchClient.listBranchManagementSummaries).mockResolvedValue({
      ok: true,
      value: [
        {
          id: E2E_BRANCH_ID,
          organizationId: E2E_ORG_ID,
          code: "MAIN",
          name: "Main Branch",
          isPrimary: true,
          status: "Active",
          city: null,
          region: null,
          addressLine1: null,
          pickupEnabled: false,
          deliveryEnabled: false,
          customerOrderingEnabled: false,
          assignedStaffCount: 12,
          activeDeviceCount: 0,
          pickupSectionsComplete: 0,
          pickupSectionsTotal: 2,
          deliverySectionsComplete: 0,
          deliverySectionsTotal: 5,
        },
        {
          id: E2E_BRANCH_2_ID,
          organizationId: E2E_ORG_ID,
          code: "K02",
          name: "Kizy Store 02",
          isPrimary: false,
          status: "Active",
          city: null,
          region: null,
          addressLine1: null,
          pickupEnabled: false,
          deliveryEnabled: false,
          customerOrderingEnabled: false,
          assignedStaffCount: 1,
          activeDeviceCount: 0,
          pickupSectionsComplete: 0,
          pickupSectionsTotal: 2,
          deliverySectionsComplete: 0,
          deliverySectionsTotal: 5,
        },
        {
          id: E2E_BRANCH_3_ID,
          organizationId: E2E_ORG_ID,
          code: "K03",
          name: "Warehouse",
          isPrimary: false,
          status: "Active",
          city: null,
          region: null,
          addressLine1: null,
          pickupEnabled: false,
          deliveryEnabled: false,
          customerOrderingEnabled: false,
          assignedStaffCount: 3,
          activeDeviceCount: 0,
          pickupSectionsComplete: 0,
          pickupSectionsTotal: 2,
          deliverySectionsComplete: 0,
          deliverySectionsTotal: 5,
        },
      ],
    });

    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/api/v1/platform/auth/me")) {
          return jsonResponse(200, {
            sessionId: "22222222-2222-2222-2222-222222222222",
            userId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
            username: "owner",
            displayName: "Owner One",
            email: "owner@example.com",
            accountClass: "Organization",
            homeOrganizationId: E2E_ORG_ID,
            organizationContextLocked: false,
          });
        }
        if (url.includes("/api/v1/platform/antiforgery/token")) {
          return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "csrf-token" });
        }
        return jsonResponse(404, { detail: "not mocked" });
      }),
    );
  });

  it("WORKSPACE-01 owner branch cards contain no staff names and show staff counts", async () => {
    probeMock.mockResolvedValue(ownerGrantProbe());
    renderWorkspaceChooser();

    await waitFor(() => {
      expect(screen.getByTestId(`workspace-branch-meta-${E2E_BRANCH_ID}`)).toHaveTextContent(
        "12 staff",
      );
    });
    expect(screen.getByTestId(`workspace-branch-meta-${E2E_BRANCH_2_ID}`)).toHaveTextContent(
      "1 staff",
    );
    expect(screen.queryByText(/John Jones/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/— Manager/)).not.toBeInTheDocument();
  });

  it("WORKSPACE-02 ordinary staff sees only authorized branches", async () => {
    listEligibleOrganizations.mockResolvedValue({
      ok: true,
      organizations: [
        {
          organizationId: E2E_ORG_ID,
          displayName: "Kizy Store",
          slug: "kizy-store",
          membershipRole: "OrganizationMember",
        },
      ],
    });
    listOrganizationBranches.mockResolvedValue({
      ok: true,
      branches: allBranches().filter((b) => b.id === E2E_BRANCH_ID || b.id === E2E_BRANCH_2_ID),
    });
    probeMock.mockResolvedValue(managerGrantProbe());
    renderWorkspaceChooser();

    await waitFor(() => {
      expect(screen.getByTestId(`workspace-branch-${E2E_BRANCH_ID}`)).toBeInTheDocument();
    });
    expect(screen.getByTestId(`workspace-branch-${E2E_BRANCH_2_ID}`)).toBeInTheDocument();
    expect(screen.queryByTestId(`workspace-branch-${E2E_BRANCH_3_ID}`)).not.toBeInTheDocument();
    expect(screen.queryByText("Warehouse")).not.toBeInTheDocument();
  });

  it("WORKSPACE-03 ordinary staff may see own role without management-summary fetch", async () => {
    listEligibleOrganizations.mockResolvedValue({
      ok: true,
      organizations: [
        {
          organizationId: E2E_ORG_ID,
          displayName: "Kizy Store",
          slug: "kizy-store",
          membershipRole: "OrganizationMember",
        },
      ],
    });
    listOrganizationBranches.mockResolvedValue({
      ok: true,
      branches: [allBranches()[0]],
    });
    probeMock.mockResolvedValue(managerGrantProbe());
    renderWorkspaceChooser();

    await waitFor(() => {
      expect(screen.getByTestId(`workspace-branch-meta-${E2E_BRANCH_ID}`)).toHaveTextContent(
        "Your role: Manager",
      );
    });
    expect(screen.queryByText(/John Jones/i)).not.toBeInTheDocument();
    expect(vi.mocked(branchClient.listBranchManagementSummaries)).not.toHaveBeenCalled();
  });

  it("WORKSPACE-04 does not N+1 staff list or assignment requests", async () => {
    probeMock.mockResolvedValue(ownerGrantProbe());
    renderWorkspaceChooser();

    await waitFor(() => {
      expect(screen.getByTestId(`workspace-branch-meta-${E2E_BRANCH_ID}`)).toHaveTextContent(
        "12 staff",
      );
    });

    expect(vi.mocked(branchClient.listBranchManagementSummaries)).toHaveBeenCalledTimes(1);
    expect(vi.mocked(membersClient.listOrganizationMembers)).not.toHaveBeenCalled();
    expect(vi.mocked(assignmentsClient.listMembershipBranchAssignments)).not.toHaveBeenCalled();
  });

  it("WORKSPACE-05..07 Operations, Start selling, and Manage business remain", async () => {
    probeMock.mockResolvedValue(ownerGrantProbe());
    renderWorkspaceChooser();

    await waitFor(() => {
      expect(screen.getByTestId("workspace-destination-manage_business")).toBeInTheDocument();
    });
    expect(screen.getAllByTestId("workspace-destination-operations")).toHaveLength(3);
    expect(screen.getAllByTestId("workspace-destination-start_selling")).toHaveLength(3);
  });

  it("WORKSPACE-08 keeps compact branch cards without staff directories", async () => {
    probeMock.mockResolvedValue(ownerGrantProbe());
    renderWorkspaceChooser();

    await waitFor(() => {
      expect(screen.getByTestId(`workspace-branch-meta-${E2E_BRANCH_ID}`)).toHaveTextContent(
        "12 staff",
      );
    });
    const main = screen.getByTestId(`workspace-branch-${E2E_BRANCH_ID}`);
    expect(within(main).getByText("Main Branch")).toBeInTheDocument();
    expect(within(main).queryAllByRole("list")).toHaveLength(0);
  });

  it("falls back to Active when management summary is unavailable", async () => {
    vi.mocked(branchClient.listBranchManagementSummaries).mockResolvedValue({
      ok: false,
      status: 403,
      body: { detail: "forbidden" },
    });
    probeMock.mockResolvedValue(ownerGrantProbe());
    renderWorkspaceChooser();

    await waitFor(() => {
      expect(screen.getByTestId(`workspace-branch-${E2E_BRANCH_ID}`)).toBeInTheDocument();
    });
    await waitFor(() => {
      expect(screen.getByTestId(`workspace-branch-meta-${E2E_BRANCH_ID}`)).toHaveTextContent(
        "Active",
      );
    });
  });
});
