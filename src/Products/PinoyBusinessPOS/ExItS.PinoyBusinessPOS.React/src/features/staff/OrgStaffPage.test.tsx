import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, within } from "@testing-library/react";
import type { ReactNode } from "react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import * as membersClient from "@/api/platform/organization-members-client";
import * as assignmentsClient from "@/api/platform/membership-branch-assignments-client";
import * as authClient from "@/api/platform/platform-auth-client";
import * as rolesClient from "@/api/platform/product-local-roles-client";
import * as inviteClient from "@/api/platform/staff-invitation-client";
import {
  isOrganizationOwnerMembershipRole,
  OrgStaffPage,
} from "@/features/staff/OrgStaffPage";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";

vi.mock("@/api/platform/organization-members-client", async (importOriginal) => {
  const actual = await importOriginal<typeof membersClient>();
  return {
    ...actual,
    listOrganizationMembers: vi.fn(),
    suspendOrganizationMembership: vi.fn(),
    revokeOrganizationMembership: vi.fn(),
  };
});

vi.mock("@/api/platform/product-local-roles-client", async (importOriginal) => {
  const actual = await importOriginal<typeof rolesClient>();
  return {
    ...actual,
    listProductLocalRoles: vi.fn(),
    revokeProductLocalRole: vi.fn(),
  };
});

vi.mock("@/api/platform/staff-invitation-client", async (importOriginal) => {
  const actual = await importOriginal<typeof inviteClient>();
  return {
    ...actual,
    listOrganizationInvitations: vi.fn(),
    revokeStaffInvitation: vi.fn(),
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
  };
});

vi.mock("@/connectivity/browser-online", () => ({
  useBrowserOnline: () => true,
}));

const sessionMock = vi.hoisted(() => ({
  userId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa" as string | null,
}));

vi.mock("@/session/SessionProvider", () => ({
  useSession: () => ({
    session: sessionMock.userId ? { userId: sessionMock.userId } : null,
    status: "authenticated",
  }),
}));

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
const ownerUserId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const staffUserId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
const noRoleUserId = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";
const ownerMembershipId = "11111111-1111-4111-8111-111111111111";
const staffMembershipId = "33333333-3333-4333-8333-333333333333";
const noRoleMembershipId = "44444444-4444-4444-8444-444444444444";
const ownerGrantId = "55555555-5555-4555-8555-555555555555";
const staffGrantId = "66666666-6666-4666-8666-666666666666";
const mainBranchId = "77777777-7777-4777-8777-777777777777";

function renderPage() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <PreferencesProvider>
        <I18nProvider>
          <MemoryRouter initialEntries={["/org/staff"]}>
            <Routes>
              <Route path="/org/staff" element={<OrgStaffPage />} />
            </Routes>
          </MemoryRouter>
        </I18nProvider>
      </PreferencesProvider>
    </QueryClientProvider>,
  );
}

describe("isOrganizationOwnerMembershipRole", () => {
  it("recognizes OrganizationOwner", () => {
    expect(isOrganizationOwnerMembershipRole("OrganizationOwner")).toBe(true);
    expect(isOrganizationOwnerMembershipRole("organizationowner")).toBe(true);
    expect(isOrganizationOwnerMembershipRole("OrganizationMember")).toBe(false);
  });
});

describe("OrgStaffPage owner protection", () => {
  beforeEach(() => {
    sessionMock.userId = ownerUserId;
    vi.mocked(membersClient.listOrganizationMembers).mockResolvedValue({
      ok: true,
      members: [
        {
          id: ownerMembershipId,
          organizationId: orgId,
          userId: ownerUserId,
          role: "OrganizationOwner",
          status: "Active",
          displayName: "Mica Uy",
          email: "mica@gmail.com",
        },
        {
          id: staffMembershipId,
          organizationId: orgId,
          userId: staffUserId,
          role: "OrganizationMember",
          status: "Active",
          displayName: "Juan Dela Cruz",
          email: "juan@example.com",
        },
        {
          id: noRoleMembershipId,
          organizationId: orgId,
          userId: noRoleUserId,
          role: "OrganizationMember",
          status: "Active",
          displayName: "Ana Reyes",
          email: "ana@example.com",
        },
      ],
    });
    vi.mocked(rolesClient.listProductLocalRoles).mockResolvedValue({
      ok: true,
      grants: [
        {
          id: ownerGrantId,
          organizationId: orgId,
          userIdentityId: ownerUserId,
          productCode: "PinoyBusinessPOS",
          roleCode: "Owner",
          mappedPosRoleCode: "Owner",
          roleDisplay: "POS Owner",
          status: "Active",
          grantedAtUtc: "2026-01-01T00:00:00Z",
          grantedByUserIdentityId: ownerUserId,
          source: "Seed",
        },
        {
          id: staffGrantId,
          organizationId: orgId,
          userIdentityId: staffUserId,
          productCode: "PinoyBusinessPOS",
          roleCode: "Cashier",
          mappedPosRoleCode: "Cashier",
          roleDisplay: "Cashier",
          status: "Active",
          grantedAtUtc: "2026-01-01T00:00:00Z",
          grantedByUserIdentityId: ownerUserId,
          source: "Seed",
        },
      ],
    });
    vi.mocked(inviteClient.listOrganizationInvitations).mockResolvedValue([]);
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
    vi.mocked(assignmentsClient.listMembershipBranchAssignments).mockImplementation(
      async (_organizationId, membershipId) => ({
        ok: true as const,
        value:
          membershipId === staffMembershipId || membershipId === noRoleMembershipId
            ? [
                {
                  branchId: mainBranchId,
                  name: "Main Store",
                  code: "MAIN",
                  isPrimary: true,
                },
              ]
            : [],
      }),
    );
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it("protects the organization owner row and keeps staff controls", async () => {
    renderPage();

    const ownerRow = await screen.findByTestId(`org-staff-row-${ownerMembershipId}`);
    expect(ownerRow).toHaveAttribute("data-owner-protected", "true");
    expect(within(ownerRow).getByText("Organization Owner")).toBeInTheDocument();
    expect(within(ownerRow).getByText("POS Owner")).toBeInTheDocument();
    expect(within(ownerRow).getByText("Protected owner account")).toBeInTheDocument();
    expect(within(ownerRow).getByTestId(`org-staff-branch-access-${ownerMembershipId}`)).toHaveTextContent(
      "All active branches",
    );
    expect(within(ownerRow).queryByText("Assign POS role")).not.toBeInTheDocument();
    expect(within(ownerRow).queryByText("Change POS role")).not.toBeInTheDocument();
    expect(within(ownerRow).queryByTestId(`org-staff-more-${ownerMembershipId}`)).not.toBeInTheDocument();

    const staffRow = screen.getByTestId(`org-staff-row-${staffMembershipId}`);
    expect(staffRow).not.toHaveAttribute("data-owner-protected");
    expect(within(staffRow).getByText("Staff member")).toBeInTheDocument();
    expect(within(staffRow).getByText("Cashier")).toBeInTheDocument();
    expect(within(staffRow).getByTestId(`org-staff-branch-access-${staffMembershipId}`)).toHaveTextContent(
      "Main Store",
    );
    expect(within(staffRow).getAllByText("POS role").length).toBeGreaterThan(0);
    expect(within(ownerRow).getAllByText("POS role").length).toBeGreaterThan(0);
    expect(within(staffRow).getByText("Change POS role")).toBeInTheDocument();
    expect(within(staffRow).getByTestId(`org-staff-more-${staffMembershipId}`)).toBeInTheDocument();

    const noRoleRow = screen.getByTestId(`org-staff-row-${noRoleMembershipId}`);
    expect(within(noRoleRow).getByText("No POS role")).toBeInTheDocument();
    expect(within(noRoleRow).getByText("Assign POS role")).toBeInTheDocument();
  });

  it("hides membership actions on the signed-in staff member's own row", async () => {
    sessionMock.userId = staffUserId;
    renderPage();

    const staffRow = await screen.findByTestId(`org-staff-row-${staffMembershipId}`);
    expect(within(staffRow).queryByText("Change POS role")).not.toBeInTheDocument();
    expect(within(staffRow).queryByTestId(`org-staff-more-${staffMembershipId}`)).not.toBeInTheDocument();
  });

  it("lists pending invitations with cancel action", async () => {
    const inviteId = "77777777-7777-4777-8777-777777777777";
    vi.mocked(inviteClient.listOrganizationInvitations).mockResolvedValue([
      {
        id: inviteId,
        organizationId: orgId,
        email: "maria@example.com",
        role: "OrganizationMember",
        status: "Pending",
        inviteeDisplayName: "Maria Santos",
        productRole: "InventoryStaff",
        targetPublicUserId: "EX-1234-5678",
      },
    ]);

    renderPage();

    const pending = await screen.findByTestId("org-staff-pending-invites");
    expect(within(pending).getByText("Maria Santos")).toBeInTheDocument();
    expect(within(pending).getByText("Invitation pending")).toBeInTheDocument();
    expect(within(pending).getByTestId(`org-staff-cancel-invite-${inviteId}`)).toBeInTheDocument();
  });
});
