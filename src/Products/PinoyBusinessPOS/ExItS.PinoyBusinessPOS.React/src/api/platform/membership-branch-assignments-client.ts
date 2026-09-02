import {
  platformRequest,
  PlatformApiError,
  type PlatformProblemDetails,
} from "@/api/platform/platform-http";

function assignmentsPath(organizationId: string, membershipId: string): string {
  return `/api/v1/platform/organizations/${organizationId}/members/${membershipId}/branch-assignments`;
}

function asRecord(value: unknown): Record<string, unknown> {
  return typeof value === "object" && value !== null ? (value as Record<string, unknown>) : {};
}

export type BranchAccessScopeDto = "Explicit" | "AllActive";

export type MembershipBranchAssignmentDto = {
  branchId: string;
  name: string;
  code: string;
  isPrimary: boolean;
};

export type MembershipBranchAccessDto = {
  scope: BranchAccessScopeDto;
  branches: MembershipBranchAssignmentDto[];
};

export type MembershipBranchAssignmentsClientResult<T> =
  | { ok: true; value: T }
  | { ok: false; status: number; body: PlatformProblemDetails | null; errorCode?: string };

async function wrap<T>(fn: () => Promise<T>): Promise<MembershipBranchAssignmentsClientResult<T>> {
  try {
    return { ok: true, value: await fn() };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return {
        ok: false,
        status: error.status,
        body: error.problem,
        errorCode: error.errorCode,
      };
    }
    throw error;
  }
}

function normalizeAssignment(raw: unknown): MembershipBranchAssignmentDto {
  const r = asRecord(raw);
  return {
    branchId: String(r.branchId ?? r.BranchId ?? ""),
    name: String(r.name ?? r.Name ?? ""),
    code: String(r.code ?? r.Code ?? ""),
    isPrimary: Boolean(r.isPrimary ?? r.IsPrimary ?? false),
  };
}

function normalizeScope(raw: unknown): BranchAccessScopeDto {
  const value = String(raw ?? "").trim();
  if (value.localeCompare("AllActive", undefined, { sensitivity: "accent" }) === 0) {
    return "AllActive";
  }
  return "Explicit";
}

function normalizeAccess(raw: unknown): MembershipBranchAccessDto {
  const r = asRecord(raw);
  const branchesRaw = r.branches ?? r.Branches;
  const list = Array.isArray(branchesRaw) ? branchesRaw : [];
  return {
    scope: normalizeScope(r.scope ?? r.Scope),
    branches: list.map(normalizeAssignment),
  };
}

export async function listMembershipBranchAssignments(
  organizationId: string,
  membershipId: string,
  signal?: AbortSignal,
): Promise<MembershipBranchAssignmentsClientResult<MembershipBranchAccessDto>> {
  return wrap(async () => {
    const payload = await platformRequest<unknown>({
      method: "GET",
      path: assignmentsPath(organizationId, membershipId),
      signal,
    });
    return normalizeAccess(payload);
  });
}

export async function setMembershipBranchAssignments(
  organizationId: string,
  membershipId: string,
  input: { scope: BranchAccessScopeDto; branchIds?: string[] },
  signal?: AbortSignal,
): Promise<MembershipBranchAssignmentsClientResult<MembershipBranchAccessDto>> {
  return wrap(async () => {
    const payload = await platformRequest<unknown>({
      method: "PUT",
      path: assignmentsPath(organizationId, membershipId),
      body: {
        scope: input.scope,
        branchIds: input.scope === "Explicit" ? (input.branchIds ?? []) : [],
      },
      signal,
    });
    return normalizeAccess(payload);
  });
}
