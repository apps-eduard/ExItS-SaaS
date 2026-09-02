import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactNode } from "react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import * as assignmentsClient from "@/api/platform/membership-branch-assignments-client";
import * as membersClient from "@/api/platform/organization-members-client";
import * as authClient from "@/api/platform/platform-auth-client";
import * as roleDefsClient from "@/api/platform/product-local-role-definitions-client";
import * as rolesClient from "@/api/platform/product-local-roles-client";
import { OrgStaffAssignPage } from "@/features/staff/OrgStaffAssignPage";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";

vi.mock("@/api/platform/organization-members-client", async (importOriginal) => {
  const actual = await importOriginal<typeof membersClient>();
  return {
    ...actual,
    listOrganizationMembers: vi.fn(),
  };
});

vi.mock("@/api/platform/product-local-roles-client", async (importOriginal) => {
  const actual = await importOriginal<typeof rolesClient>();
  return {
    ...actual,
    listProductLocalRoles: vi.fn(),
    assignProductLocalRole: vi.fn(),
    changeProductLocalRole: vi.fn(),
  };
});

vi.mock("@/api/platform/product-local-role-definitions-client", async (importOriginal) => {
  const actual = await importOriginal<typeof roleDefsClient>();
  return {
    ...actual,
    listProductLocalRoleDefinitions: vi.fn(),
  };
});

vi.mock("@/api/platform/platform-auth-client", async (importOriginal) => {
  const actual = await importOriginal<typeof authClient>();
  return {
    ...actual,
    listOrganizationBranches: vi.fn(),
  };
});

vi.mock("@/api/platform/membership-branch-assignments-client", async (importOriginal) => {
  const actual = await importOriginal<typeof assignmentsClient>();
  return {
    ...actual,
    listMembershipBranchAssignments: vi.fn(),
    setMembershipBranchAssignments: vi.fn(),
  };
});

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: {
      organizationId: "22222222-2222-4222-8222-222222222222",
      organizationDisplayName: "Corner Store",
      branchId: null,
      branchName: null,
    },
    sessionGrant: { membershipRole: "OrganizationOwner" },
    status: "ready",
  }),
  WorkspaceProvider: ({ children }: { children: ReactNode }) => children,
}));

const orgId = "22222222-2222-4222-8222-222222222222";
const staffUserId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
const staffMembershipId = "33333333-3333-4333-8333-333333333333";
const mainBranchId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const secondBranchId = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";

function renderAssignPage() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <PreferencesProvider>
        <I18nProvider>
          <MemoryRouter initialEntries={[`/org/staff/assign?userId=${staffUserId}`]}>
            <Routes>
              <Route path="/org/staff/assign" element={<OrgStaffAssignPage />} />
              <Route path="/org/staff" element={<div data-testid="org-staff-redirect" />} />
            </Routes>
          </MemoryRouter>
        </I18nProvider>
      </PreferencesProvider>
    </QueryClientProvider>,
  );
}

describe("OrgStaffAssignPage branch access", () => {
  beforeEach(() => {
    vi.mocked(roleDefsClient.listProductLocalRoleDefinitions).mockResolvedValue({
      ok: true,
      roles: [
        {
          code: "Cashier",
          displayName: "Cashier",
          description: "Sell products",
          isAssignable: true,
          sortOrder: 1,
          isSystemRole: true,
          mappedPosRoleCode: "Cashier",
          activeStaffCount: 0,
          permissionGroups: [],
        },
        {
          code: "Manager",
          displayName: "Manager",
          description: "Run operations",
          isAssignable: true,
          sortOrder: 2,
          isSystemRole: true,
          mappedPosRoleCode: "Manager",
          activeStaffCount: 0,
          permissionGroups: [],
        },
      ],
    });
    vi.mocked(membersClient.listOrganizationMembers).mockResolvedValue({
      ok: true,
      members: [
        {
          id: staffMembershipId,
          organizationId: orgId,
          userId: staffUserId,
          role: "OrganizationMember",
          status: "Active",
          displayName: "John Jones",
          email: "john@example.com",
        },
      ],
    });
    vi.mocked(rolesClient.listProductLocalRoles).mockResolvedValue({
      ok: true,
      grants: [],
    });
    vi.mocked(rolesClient.assignProductLocalRole).mockResolvedValue({
      ok: true,
      grant: {
        id: "grant-1",
        organizationId: orgId,
        userIdentityId: staffUserId,
        productCode: "PinoyBusinessPOS",
        roleCode: "Cashier",
        mappedPosRoleCode: "Cashier",
        roleDisplay: "Cashier",
        status: "Active",
        grantedAtUtc: "2026-01-01T00:00:00Z",
        grantedByUserIdentityId: "owner",
        source: "Manual",
      },
    });
    vi.mocked(assignmentsClient.setMembershipBranchAssignments).mockResolvedValue({
      ok: true,
      value: [
        {
          branchId: mainBranchId,
          name: "Main",
          code: "MAIN",
          isPrimary: true,
        },
      ],
    });
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it("locks single-branch orgs to the main branch automatically", async () => {
    vi.mocked(authClient.listOrganizationBranches).mockResolvedValue({
      ok: true,
      branches: [
        {
          id: mainBranchId,
          organizationId: orgId,
          code: "MAIN",
          name: "Main Store",
          isPrimary: true,
          status: "Active",
        },
      ],
    });
    vi.mocked(assignmentsClient.listMembershipBranchAssignments).mockResolvedValue({
      ok: true,
      value: [
        {
          branchId: mainBranchId,
          name: "Main Store",
          code: "MAIN",
          isPrimary: true,
        },
      ],
    });

    const user = userEvent.setup();
    renderAssignPage();

    expect(await screen.findByTestId("org-staff-assign-branch-single")).toHaveTextContent(
      "Main branch (automatic): Main Store",
    );
    expect(screen.queryByTestId("org-staff-branch-scope-all")).not.toBeInTheDocument();

    await user.click(screen.getByTestId("org-staff-role-cashier"));
    await user.click(screen.getByTestId("org-staff-assign-submit"));

    await waitFor(() => {
      expect(rolesClient.assignProductLocalRole).toHaveBeenCalled();
      expect(assignmentsClient.setMembershipBranchAssignments).toHaveBeenCalledWith(
        orgId,
        staffMembershipId,
        [mainBranchId],
      );
    });
    expect(await screen.findByTestId("org-staff-redirect")).toBeInTheDocument();
  });

  it("assigns all active branches when that scope is selected", async () => {
    vi.mocked(authClient.listOrganizationBranches).mockResolvedValue({
      ok: true,
      branches: [
        {
          id: mainBranchId,
          organizationId: orgId,
          code: "MAIN",
          name: "Main Store",
          isPrimary: true,
          status: "Active",
        },
        {
          id: secondBranchId,
          organizationId: orgId,
          code: "NORTH",
          name: "North Branch",
          isPrimary: false,
          status: "Active",
        },
      ],
    });
    vi.mocked(assignmentsClient.listMembershipBranchAssignments).mockResolvedValue({
      ok: true,
      value: [
        {
          branchId: mainBranchId,
          name: "Main Store",
          code: "MAIN",
          isPrimary: true,
        },
      ],
    });
    vi.mocked(assignmentsClient.setMembershipBranchAssignments).mockResolvedValue({
      ok: true,
      value: [
        {
          branchId: mainBranchId,
          name: "Main Store",
          code: "MAIN",
          isPrimary: true,
        },
        {
          branchId: secondBranchId,
          name: "North Branch",
          code: "NORTH",
          isPrimary: false,
        },
      ],
    });

    const user = userEvent.setup();
    renderAssignPage();

    expect(await screen.findByTestId("org-staff-branch-scope-all")).toBeInTheDocument();
    await user.click(screen.getByTestId("org-staff-branch-scope-all"));
    await user.click(screen.getByTestId("org-staff-role-manager"));
    await user.click(screen.getByTestId("org-staff-assign-submit"));

    await waitFor(() => {
      expect(assignmentsClient.setMembershipBranchAssignments).toHaveBeenCalledWith(
        orgId,
        staffMembershipId,
        expect.arrayContaining([mainBranchId, secondBranchId]),
      );
    });
  });

  it("assigns only checked branches in specific scope", async () => {
    vi.mocked(authClient.listOrganizationBranches).mockResolvedValue({
      ok: true,
      branches: [
        {
          id: mainBranchId,
          organizationId: orgId,
          code: "MAIN",
          name: "Main Store",
          isPrimary: true,
          status: "Active",
        },
        {
          id: secondBranchId,
          organizationId: orgId,
          code: "NORTH",
          name: "North Branch",
          isPrimary: false,
          status: "Active",
        },
      ],
    });
    vi.mocked(assignmentsClient.listMembershipBranchAssignments).mockResolvedValue({
      ok: true,
      value: [
        {
          branchId: mainBranchId,
          name: "Main Store",
          code: "MAIN",
          isPrimary: true,
        },
      ],
    });

    const user = userEvent.setup();
    renderAssignPage();

    await screen.findByTestId("org-staff-branch-scope-specific");
    await user.click(screen.getByTestId("org-staff-branch-scope-specific"));
    const checklist = await screen.findByTestId("org-staff-branch-checklist");
    expect(within(checklist).getByTestId(`org-staff-branch-${mainBranchId}`)).toBeChecked();

    await user.click(screen.getByTestId(`org-staff-branch-${secondBranchId}`));
    await user.click(screen.getByTestId("org-staff-role-cashier"));
    await user.click(screen.getByTestId("org-staff-assign-submit"));

    await waitFor(() => {
      expect(assignmentsClient.setMembershipBranchAssignments).toHaveBeenCalledWith(
        orgId,
        staffMembershipId,
        expect.arrayContaining([mainBranchId, secondBranchId]),
      );
    });
  });
});
