import {
  platformRequest,
  PlatformApiError,
  type PlatformProblemDetails,
} from "@/api/platform/platform-http";

export type PersonalWorkplaceBranchWire = {
  branchId: string;
  name: string;
  code: string;
  isPrimary: boolean;
};

export type PersonalWorkplaceWire = {
  organizationId: string;
  organizationDisplayName: string;
  publicOrganizationId: string | null;
  staffUserId: string;
  staffLogin: string;
  membershipId: string;
  membershipRole: string;
  membershipRoleDisplay: string;
  membershipStatus: string;
  productRole: string | null;
  productRoleDisplay: string | null;
  branches: PersonalWorkplaceBranchWire[];
  passwordResetStatus?: string | null;
  passwordResetRequestId?: string | null;
};

function asRecord(value: unknown): Record<string, unknown> {
  return typeof value === "object" && value !== null ? (value as Record<string, unknown>) : {};
}

function normalizeBranch(raw: unknown): PersonalWorkplaceBranchWire {
  const r = asRecord(raw);
  return {
    branchId: String(r.branchId ?? r.BranchId ?? ""),
    name: String(r.name ?? r.Name ?? ""),
    code: String(r.code ?? r.Code ?? ""),
    isPrimary: Boolean(r.isPrimary ?? r.IsPrimary ?? false),
  };
}

function normalizeWorkplace(raw: unknown): PersonalWorkplaceWire {
  const r = asRecord(raw);
  const branchesRaw = r.branches ?? r.Branches;
  const branches = Array.isArray(branchesRaw) ? branchesRaw.map(normalizeBranch) : [];
  return {
    organizationId: String(r.organizationId ?? r.OrganizationId ?? ""),
    organizationDisplayName: String(
      r.organizationDisplayName ?? r.OrganizationDisplayName ?? "",
    ),
    publicOrganizationId: (r.publicOrganizationId ?? r.PublicOrganizationId ?? null) as
      | string
      | null,
    staffUserId: String(r.staffUserId ?? r.StaffUserId ?? ""),
    staffLogin: String(r.staffLogin ?? r.StaffLogin ?? ""),
    membershipId: String(r.membershipId ?? r.MembershipId ?? ""),
    membershipRole: String(r.membershipRole ?? r.MembershipRole ?? ""),
    membershipRoleDisplay: String(r.membershipRoleDisplay ?? r.MembershipRoleDisplay ?? ""),
    membershipStatus: String(r.membershipStatus ?? r.MembershipStatus ?? ""),
    productRole: (r.productRole ?? r.ProductRole ?? null) as string | null,
    productRoleDisplay: (r.productRoleDisplay ?? r.ProductRoleDisplay ?? null) as string | null,
    branches,
    passwordResetStatus: (r.passwordResetStatus ?? r.PasswordResetStatus ?? null) as string | null,
    passwordResetRequestId: (r.passwordResetRequestId ?? r.PasswordResetRequestId ?? null) as
      | string
      | null,
  };
}

export type StaffPasswordResetStatusWire = {
  id: string;
  status: string;
  expiresAtUtc: string;
};

function normalizeResetStatus(raw: unknown): StaffPasswordResetStatusWire {
  const r = asRecord(raw);
  return {
    id: String(r.id ?? r.Id ?? ""),
    status: String(r.status ?? r.Status ?? ""),
    expiresAtUtc: String(r.expiresAtUtc ?? r.ExpiresAtUtc ?? ""),
  };
}

async function postReset(
  path: string,
  body?: unknown,
): Promise<
  | { ok: true; result: StaffPasswordResetStatusWire }
  | { ok: false; status: number; body: PlatformProblemDetails | null }
> {
  try {
    const payload = await platformRequest<unknown>({ method: "POST", path, body });
    return { ok: true, result: normalizeResetStatus(payload) };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return { ok: false, status: error.status, body: error.problem };
    }
    throw error;
  }
}

export function requestWorkplacePasswordReset(membershipId: string) {
  return postReset(`/api/v1/personal/workplaces/${membershipId}/password-reset-requests`);
}

export function completeWorkplacePasswordReset(
  membershipId: string,
  requestId: string,
  newPassword: string,
) {
  return postReset(
    `/api/v1/personal/workplaces/${membershipId}/password-reset-requests/${requestId}/complete`,
    { newPassword },
  );
}

export async function listPersonalWorkplaces(
  signal?: AbortSignal,
): Promise<
  | { ok: true; workplaces: PersonalWorkplaceWire[] }
  | { ok: false; status: number; body: PlatformProblemDetails | null }
> {
  try {
    const payload = await platformRequest<unknown>({
      method: "GET",
      path: "/api/v1/personal/workplaces",
      signal,
    });
    const list = Array.isArray(payload) ? payload : [];
    return { ok: true, workplaces: list.map(normalizeWorkplace) };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return { ok: false, status: error.status, body: error.problem };
    }
    throw error;
  }
}
