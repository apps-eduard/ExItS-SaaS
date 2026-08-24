import {
  platformRequest,
  PlatformApiError,
  type PlatformProblemDetails,
} from "@/api/platform/platform-http";

export type OrganizationMemberWire = {
  id: string;
  organizationId: string;
  userId: string;
  role: string;
  status: string;
  displayName?: string | null;
  username?: string | null;
  email?: string | null;
  roleDisplay?: string | null;
  productRoles?: string[] | null;
  branch?: string | null;
  employeeCode?: string | null;
};

type PagedMembersWire = {
  items?: OrganizationMemberWire[];
  totalCount?: number;
};

function membersPath(organizationId: string, status?: string): string {
  let path = `/api/v1/platform/organizations/${organizationId}/members?page=1&pageSize=100`;
  if (status?.trim()) {
    path += `&status=${encodeURIComponent(status.trim())}`;
  }
  return path;
}

/**
 * Owner ManageMemberships path. Returns null body on 403 — callers omit roster.
 * Informational only; never used as authorization.
 * Defaults to Active for workspace roster; pass status undefined for staff management.
 */
export async function listOrganizationMembers(
  organizationId: string,
  status: string | undefined = "Active",
): Promise<
  | { ok: true; members: OrganizationMemberWire[] }
  | { ok: false; status: number; body: PlatformProblemDetails | null }
> {
  try {
    const page = await platformRequest<PagedMembersWire>({
      method: "GET",
      path: membersPath(organizationId, status),
    });
    return { ok: true, members: page.items ?? [] };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return { ok: false, status: error.status, body: error.problem };
    }
    throw error;
  }
}

export async function suspendOrganizationMembership(input: {
  membershipId: string;
  reason?: string;
}): Promise<
  | { ok: true; member: OrganizationMemberWire }
  | { ok: false; status: number; body: PlatformProblemDetails | null }
> {
  try {
    const member = await platformRequest<OrganizationMemberWire>({
      method: "POST",
      path: `/api/v1/platform/memberships/${input.membershipId}/suspend`,
      body: { reason: input.reason ?? null },
    });
    return { ok: true, member };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return { ok: false, status: error.status, body: error.problem };
    }
    throw error;
  }
}

export async function revokeOrganizationMembership(input: {
  membershipId: string;
  reason?: string;
}): Promise<
  | { ok: true; member: OrganizationMemberWire }
  | { ok: false; status: number; body: PlatformProblemDetails | null }
> {
  try {
    const member = await platformRequest<OrganizationMemberWire>({
      method: "POST",
      path: `/api/v1/platform/memberships/${input.membershipId}/revoke`,
      body: { reason: input.reason ?? null },
    });
    return { ok: true, member };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return { ok: false, status: error.status, body: error.problem };
    }
    throw error;
  }
}

export function friendlyMembershipRoleLabel(
  role: string,
): "Owner" | "Admin" | "Manager" | "Cashier" | "Staff" {
  const normalized = role.trim().toLowerCase();
  if (normalized === "organizationowner") {
    return "Owner";
  }
  if (normalized === "organizationadministrator") {
    return "Admin";
  }
  return "Staff";
}

export function friendlyProductRoleLabel(
  productRoles: string[] | null | undefined,
): "Manager" | "Cashier" | null {
  if (!productRoles?.length) {
    return null;
  }
  const lower = productRoles.map((r) => r.toLowerCase());
  if (lower.some((r) => r === "storemanager" || r === "manager")) {
    return "Manager";
  }
  if (lower.some((r) => r === "cashier")) {
    return "Cashier";
  }
  return null;
}

export type WorkspaceRosterPerson = {
  membershipId: string;
  displayName: string;
  roleLabel: string;
  branchName: string | null;
};

/** Management team = Owner / Admin memberships. Branch staff = members with product roles. */
export function buildWorkspaceRoster(members: OrganizationMemberWire[]): {
  managementTeam: WorkspaceRosterPerson[];
  branchStaff: WorkspaceRosterPerson[];
} {
  const managementTeam: WorkspaceRosterPerson[] = [];
  const branchStaff: WorkspaceRosterPerson[] = [];

  for (const member of members) {
    if (
      member.status &&
      member.status.localeCompare("Active", undefined, { sensitivity: "accent" }) !== 0
    ) {
      continue;
    }
    const name =
      member.displayName?.trim() ||
      member.username?.trim() ||
      member.email?.trim() ||
      "Team member";
    const membershipLabel = friendlyMembershipRoleLabel(member.role);
    if (membershipLabel === "Owner" || membershipLabel === "Admin") {
      managementTeam.push({
        membershipId: member.id,
        displayName: name,
        roleLabel: membershipLabel,
        branchName: null,
      });
      continue;
    }
    const productLabel = friendlyProductRoleLabel(member.productRoles);
    if (productLabel) {
      branchStaff.push({
        membershipId: member.id,
        displayName: name,
        roleLabel: productLabel,
        branchName: member.branch?.trim() || null,
      });
    }
  }

  return { managementTeam, branchStaff };
}
