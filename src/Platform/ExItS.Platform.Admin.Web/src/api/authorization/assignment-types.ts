export const ASSIGNMENT_STATUSES = ["Active", "Revoked"] as const;
export type AssignmentStatus = (typeof ASSIGNMENT_STATUSES)[number];

export const PLATFORM_ROLE_CODES = [
  "PlatformAdministrator",
  "BillingAdministrator",
  "PlatformSupport",
  "PlatformAuditor",
] as const;
export type PlatformRoleCode = (typeof PLATFORM_ROLE_CODES)[number];

export const ASSIGNMENTS_PAGE_SIZE = 10;

export type PlatformRoleAssignment = {
  id: string;
  platformUserId: string;
  role: string;
  organizationId?: string;
  status: string;
  grantedByActor: string;
  grantedAtUtc: string;
  reason?: string;
  revokedByActor?: string;
  revokedAtUtc?: string;
  revokeReason?: string;
};

export type RoleAssignmentsQuery = {
  platformUserId: string;
  role?: string;
  organizationId?: string;
  status?: string;
  page?: number;
  pageSize?: number;
  signal?: AbortSignal;
};
