import { platformRequest, PlatformApiError } from "@/api/platform/platform-http";

export type OrganizationStaffPasswordResetWire = {
  id: string;
  staffUserId: string;
  staffDisplayName: string;
  staffLogin: string;
  membershipId: string;
  status: string;
  createdAtUtc: string;
  expiresAtUtc: string;
};

function asRecord(value: unknown): Record<string, unknown> {
  return typeof value === "object" && value !== null ? (value as Record<string, unknown>) : {};
}

function normalize(raw: unknown): OrganizationStaffPasswordResetWire {
  const r = asRecord(raw);
  return {
    id: String(r.id ?? r.Id ?? ""),
    staffUserId: String(r.staffUserId ?? r.StaffUserId ?? ""),
    staffDisplayName: String(r.staffDisplayName ?? r.StaffDisplayName ?? ""),
    staffLogin: String(r.staffLogin ?? r.StaffLogin ?? ""),
    membershipId: String(r.membershipId ?? r.MembershipId ?? ""),
    status: String(r.status ?? r.Status ?? ""),
    createdAtUtc: String(r.createdAtUtc ?? r.CreatedAtUtc ?? ""),
    expiresAtUtc: String(r.expiresAtUtc ?? r.ExpiresAtUtc ?? ""),
  };
}

export async function listOrganizationStaffPasswordResets(
  organizationId: string,
  signal?: AbortSignal,
): Promise<OrganizationStaffPasswordResetWire[]> {
  const payload = await platformRequest<unknown>({
    method: "GET",
    path: `/api/v1/platform/organizations/${organizationId}/staff-password-reset-requests`,
    signal,
  });
  return Array.isArray(payload) ? payload.map(normalize) : [];
}

export async function decideOrganizationStaffPasswordReset(
  organizationId: string,
  requestId: string,
  approve: boolean,
): Promise<{ ok: true } | { ok: false; detail: string }> {
  try {
    await platformRequest({
      method: "POST",
      path: `/api/v1/platform/organizations/${organizationId}/staff-password-reset-requests/${requestId}/${approve ? "approve" : "deny"}`,
    });
    return { ok: true };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return { ok: false, detail: error.problem.detail ?? error.message };
    }
    throw error;
  }
}
