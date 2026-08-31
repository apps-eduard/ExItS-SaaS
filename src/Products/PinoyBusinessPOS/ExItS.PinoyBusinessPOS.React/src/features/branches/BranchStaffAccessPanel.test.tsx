import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { PosSessionGrantFacts } from "@/access/pos-capabilities";
import { BranchStaffAccessPanel } from "@/features/branches/BranchStaffAccessPanel";

vi.mock("@/access/pos-capabilities", () => ({
  canInviteOrganizationStaff: () => true,
}));

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: string) => key,
  }),
}));

const listBranchStaffAccess = vi.fn();
const listOrganizationMembers = vi.fn();
const listMembershipBranchAssignments = vi.fn();
const setMembershipBranchAssignments = vi.fn();

vi.mock("@/api/platform/organization-branches-client", () => ({
  listBranchStaffAccess: (...args: unknown[]) => listBranchStaffAccess(...args),
}));

vi.mock("@/api/platform/organization-members-client", () => ({
  listOrganizationMembers: (...args: unknown[]) => listOrganizationMembers(...args),
  friendlyMembershipRoleLabel: (role: string) => role,
}));

vi.mock("@/api/platform/membership-branch-assignments-client", () => ({
  listMembershipBranchAssignments: (...args: unknown[]) => listMembershipBranchAssignments(...args),
  setMembershipBranchAssignments: (...args: unknown[]) => setMembershipBranchAssignments(...args),
}));

const ownerGrant = {
  productAccessAllowed: true,
  productAccessReasonCode: null,
  mappedPosRoleCode: null,
  productLocalRoleCode: null,
  membershipRole: "Owner",
  organizationManagementAuthority: true,
  featureCodes: [],
  grantedFeatureCodes: [],
} satisfies PosSessionGrantFacts;

function renderPanel() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <BranchStaffAccessPanel
        organizationId="11111111-1111-1111-1111-111111111111"
        branchId="22222222-2222-2222-2222-222222222222"
        sessionGrant={ownerGrant}
      />
    </QueryClientProvider>,
  );
}

describe("BranchStaffAccessPanel", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    listBranchStaffAccess.mockResolvedValue({
      ok: true,
      value: [
        {
          membershipId: "m1",
          userId: "u1",
          displayName: "Juan Dela Cruz",
          membershipRole: "Member",
          membershipStatus: "Active",
          posRoleCode: "Cashier",
          posRoleDisplay: "Cashier",
          hasExplicitAccess: true,
          hasOrganizationWideAccess: false,
        },
        {
          membershipId: "m0",
          userId: "u0",
          displayName: "Owner Person",
          membershipRole: "Owner",
          membershipStatus: "Active",
          posRoleCode: null,
          posRoleDisplay: null,
          hasExplicitAccess: false,
          hasOrganizationWideAccess: true,
        },
      ],
    });
    listOrganizationMembers.mockResolvedValue({
      ok: true,
      members: [
        {
          id: "m2",
          displayName: "Maria Santos",
          email: "maria@example.com",
          username: "maria",
          role: "Member",
          roleDisplay: "Manager",
          status: "Active",
        },
      ],
    });
    listMembershipBranchAssignments.mockResolvedValue({
      ok: true,
      value: { branchIds: ["22222222-2222-2222-2222-222222222222"] },
    });
  });

  it("renders assigned staff and owner automatic access without checkbox wall", async () => {
    renderPanel();

    await waitFor(() => {
      expect(screen.getByText("Juan Dela Cruz")).toBeInTheDocument();
    });
    expect(screen.getByText("branches.staff.automaticAccess")).toBeInTheDocument();
    expect(screen.queryAllByRole("checkbox")).toHaveLength(0);
    expect(screen.getByRole("button", { name: "branches.staff.add" })).toBeInTheDocument();
  });

  it("opens searchable add sheet", async () => {
    const user = userEvent.setup();
    renderPanel();

    await waitFor(() => {
      expect(screen.getByRole("button", { name: "branches.staff.add" })).toBeInTheDocument();
    });
    await user.click(screen.getByTestId("branch-staff-add"));
    await waitFor(() => {
      expect(screen.getByTestId("branch-staff-search")).toBeInTheDocument();
    });
    expect(await screen.findByText("Maria Santos")).toBeInTheDocument();
  });
});
