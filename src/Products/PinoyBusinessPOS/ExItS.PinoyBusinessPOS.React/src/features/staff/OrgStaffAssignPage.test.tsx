import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactNode } from "react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import * as assignmentsClient from "@/api/platform/membership-branch-assignments-client";
import * as membersClient from "@/api/platform/organization-members-client";
import * as areasClient from "@/api/platform/organization-areas-client";
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

vi.mock("@/api/platform/organization-areas-client", async (importOriginal) => {
  const actual = await importOriginal<typeof areasClient>();
  return {
    ...actual,
    listOrganizationAreas: vi.fn(),
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
    vi.mocked(areasClient.listOrganizationAreas).mockResolvedValue({
      ok: true,
      value: { areas: [], unassignedBranchCount: 0, activeAreaCount: 0, maxAreas: 0 },
    });
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
      value: {
        scope: "Explicit",
        areas: [],
        branches: [
          {
            branchId: mainBranchId,
            name: "Main",
            code: "MAIN",
            isPrimary: true,
          },
        ],
      },
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
      value: {
        scope: "Explicit",
        areas: [],
        branches: [
          {
            branchId: mainBranchId,
            name: "Main Store",
            code: "MAIN",
            isPrimary: true,
          },
        ],
      },
    });

    const user = userEvent.setup();
    renderAssignPage();

    expect(await screen.findByTestId("org-staff-assign-branch-single")).toHaveTextContent(
      "Automatic: Main Store",
    );
    expect(screen.queryByTestId("org-staff-branch-scope-all")).not.toBeInTheDocument();

    await user.click(screen.getByTestId("org-staff-role-cashier"));
    await user.click(screen.getByTestId("org-staff-assign-submit"));

    await waitFor(() => {
      expect(rolesClient.assignProductLocalRole).toHaveBeenCalled();
    });
    expect(assignmentsClient.setMembershipBranchAssignments).not.toHaveBeenCalled();
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
      value: {
        scope: "Explicit",
        areas: [],
        branches: [
          {
            branchId: mainBranchId,
            name: "Main Store",
            code: "MAIN",
            isPrimary: true,
          },
        ],
      },
    });
    vi.mocked(assignmentsClient.setMembershipBranchAssignments).mockResolvedValue({
      ok: true,
      value: {
        scope: "AllActive",
        areas: [],
        branches: [
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
      },
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
        { scope: "AllActive", branchIds: [] },
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
      value: {
        scope: "Explicit",
        areas: [],
        branches: [
          {
            branchId: mainBranchId,
            name: "Main Store",
            code: "MAIN",
            isPrimary: true,
          },
        ],
      },
    });
    vi.mocked(assignmentsClient.setMembershipBranchAssignments).mockResolvedValue({
      ok: true,
      value: {
        scope: "Explicit",
        areas: [],
        branches: [
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
      },
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
        {
          scope: "Explicit",
          branchIds: expect.arrayContaining([mainBranchId, secondBranchId]),
        },
      );
    });
  });

  it("offers area scope with retail/warehouse counts and saves multi-area access", async () => {
    const southAreaId = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";
    const northAreaId = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee";
    const warehouseId = "ffffffff-ffff-4fff-8fff-ffffffffffff";

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
          branchType: "Retail",
          areaId: southAreaId,
        },
        {
          id: secondBranchId,
          organizationId: orgId,
          code: "NORTH",
          name: "North Branch",
          isPrimary: false,
          status: "Active",
          branchType: "Retail",
          areaId: northAreaId,
        },
        {
          id: warehouseId,
          organizationId: orgId,
          code: "WH1",
          name: "Iloilo Warehouse",
          isPrimary: false,
          status: "Active",
          branchType: "Warehouse",
          areaId: southAreaId,
        },
      ],
    });
    vi.mocked(areasClient.listOrganizationAreas).mockResolvedValue({
      ok: true,
      value: {
        areas: [
          {
            id: southAreaId,
            organizationId: orgId,
            name: "South",
            code: "SOUTH",
            status: "Active",
            branchCount: 2,
          },
          {
            id: northAreaId,
            organizationId: orgId,
            name: "North",
            code: "NORTH",
            status: "Active",
            branchCount: 1,
          },
        ],
        unassignedBranchCount: 0,
        activeAreaCount: 2,
        maxAreas: 10,
      },
    });
    vi.mocked(assignmentsClient.listMembershipBranchAssignments).mockResolvedValue({
      ok: true,
      value: {
        scope: "Explicit",
        areas: [],
        branches: [
          {
            branchId: mainBranchId,
            name: "Main Store",
            code: "MAIN",
            isPrimary: true,
          },
        ],
      },
    });
    vi.mocked(assignmentsClient.setMembershipBranchAssignments).mockResolvedValue({
      ok: true,
      value: {
        scope: "Areas",
        areas: [
          { areaId: southAreaId, name: "South", code: "SOUTH" },
          { areaId: northAreaId, name: "North", code: "NORTH" },
        ],
        branches: [
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
          {
            branchId: warehouseId,
            name: "Iloilo Warehouse",
            code: "WH1",
            isPrimary: false,
          },
        ],
      },
    });

    const user = userEvent.setup();
    renderAssignPage();

    expect(await screen.findByTestId("org-staff-assign-branches")).toHaveTextContent(
      "Location access",
    );
    expect(screen.getByTestId("org-staff-branch-scope-all")).toHaveTextContent(
      "All active locations",
    );
    expect(screen.getByTestId("org-staff-branch-scope-areas")).toBeInTheDocument();
    expect(screen.getByTestId("org-staff-branch-scope-specific")).toHaveTextContent(
      "Specific locations",
    );

    await user.click(screen.getByTestId("org-staff-branch-scope-areas"));
    const checklist = await screen.findByTestId("org-staff-area-checklist");
    expect(checklist).toHaveTextContent("2 locations · 1 Retail · 1 Warehouse");
    expect(checklist).toHaveTextContent("1 locations · 1 Retail");

    await user.click(screen.getByTestId(`org-staff-area-${southAreaId}`));
    await user.click(screen.getByTestId(`org-staff-area-${northAreaId}`));
    await user.click(screen.getByTestId("org-staff-role-manager"));

    expect(await screen.findByTestId("org-staff-assign-save-summary")).toHaveTextContent(
      "Location access",
    );
    expect(screen.getByTestId("org-staff-assign-save-summary")).toHaveTextContent("Manager");

    await user.click(screen.getByTestId("org-staff-assign-submit"));

    await waitFor(() => {
      expect(assignmentsClient.setMembershipBranchAssignments).toHaveBeenCalledWith(
        orgId,
        staffMembershipId,
        {
          scope: "Areas",
          branchIds: [],
          areaIds: expect.arrayContaining([southAreaId, northAreaId]),
        },
      );
    });
  });

  it("hides area scope when no active areas exist", async () => {
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
    vi.mocked(areasClient.listOrganizationAreas).mockResolvedValue({
      ok: true,
      value: { areas: [], unassignedBranchCount: 2, activeAreaCount: 0, maxAreas: 0 },
    });
    vi.mocked(assignmentsClient.listMembershipBranchAssignments).mockResolvedValue({
      ok: true,
      value: {
        scope: "Explicit",
        areas: [],
        branches: [
          {
            branchId: mainBranchId,
            name: "Main Store",
            code: "MAIN",
            isPrimary: true,
          },
        ],
      },
    });

    renderAssignPage();

    expect(await screen.findByTestId("org-staff-branch-scope-all")).toBeInTheDocument();
    expect(screen.queryByTestId("org-staff-branch-scope-areas")).not.toBeInTheDocument();
  });

  it("shows retail and warehouse chips for specific locations", async () => {
    const warehouseId = "ffffffff-ffff-4fff-8fff-ffffffffffff";
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
          branchType: "Retail",
        },
        {
          id: warehouseId,
          organizationId: orgId,
          code: "WH1",
          name: "Iloilo Warehouse",
          isPrimary: false,
          status: "Active",
          branchType: "Warehouse",
        },
      ],
    });
    vi.mocked(areasClient.listOrganizationAreas).mockResolvedValue({
      ok: true,
      value: { areas: [], unassignedBranchCount: 2, activeAreaCount: 0, maxAreas: 0 },
    });
    vi.mocked(assignmentsClient.listMembershipBranchAssignments).mockResolvedValue({
      ok: true,
      value: {
        scope: "Explicit",
        areas: [],
        branches: [
          {
            branchId: mainBranchId,
            name: "Main Store",
            code: "MAIN",
            isPrimary: true,
          },
        ],
      },
    });

    const user = userEvent.setup();
    renderAssignPage();

    await user.click(await screen.findByTestId("org-staff-branch-scope-specific"));
    const checklist = await screen.findByTestId("org-staff-branch-checklist");
    expect(checklist).toHaveTextContent("Retail");
    expect(checklist).toHaveTextContent("Primary");
    expect(checklist).toHaveTextContent("Warehouse");
  });
});
