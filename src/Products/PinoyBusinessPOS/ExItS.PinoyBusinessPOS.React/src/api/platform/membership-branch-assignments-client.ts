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

export type BranchAccessScopeDto = "Explicit" | "AllActive" | "Areas";

export type MembershipBranchAssignmentDto = {
  branchId: string;
  name: string;
  code: string;
  isPrimary: boolean;
};

export type MembershipAreaAssignmentDto = {
  areaId: string;
  name: string;
  code: string | null;
};

export type MembershipBranchAccessDto = {
  scope: BranchAccessScopeDto;
  branches: MembershipBranchAssignmentDto[];
  /** Granted areas, present only for the Areas scope. Branches above stay the resolved list. */
  areas: MembershipAreaAssignmentDto[];
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

function normalizeAreaAssignment(raw: unknown): MembershipAreaAssignmentDto {
  const r = asRecord(raw);
  const code = r.code ?? r.Code;
  return {
    areaId: String(r.areaId ?? r.AreaId ?? ""),
    name: String(r.name ?? r.Name ?? ""),
    code: code == null || code === "" ? null : String(code),
  };
}

function normalizeScope(raw: unknown): BranchAccessScopeDto {
  const value = String(raw ?? "").trim();
  if (value.localeCompare("AllActive", undefined, { sensitivity: "accent" }) === 0) {
    return "AllActive";
  }
  if (value.localeCompare("Areas", undefined, { sensitivity: "accent" }) === 0) {
    return "Areas";
  }
  return "Explicit";
}

function normalizeAccess(raw: unknown): MembershipBranchAccessDto {
  const r = asRecord(raw);
  const branchesRaw = r.branches ?? r.Branches;
  const list = Array.isArray(branchesRaw) ? branchesRaw : [];
  const areasRaw = r.areas ?? r.Areas;
  const areaList = Array.isArray(areasRaw) ? areasRaw : [];
  return {
    scope: normalizeScope(r.scope ?? r.Scope),
    branches: list.map(normalizeAssignment),
    areas: areaList.map(normalizeAreaAssignment),
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
  input: { scope: BranchAccessScopeDto; branchIds?: string[]; areaIds?: string[] },
  signal?: AbortSignal,
): Promise<MembershipBranchAssignmentsClientResult<MembershipBranchAccessDto>> {
  return wrap(async () => {
    const payload = await platformRequest<unknown>({
      method: "PUT",
      path: assignmentsPath(organizationId, membershipId),
      body: {
        scope: input.scope,
        branchIds: input.scope === "Explicit" ? (input.branchIds ?? []) : [],
        areaIds: input.scope === "Areas" ? (input.areaIds ?? []) : [],
      },
      signal,
    });
    return normalizeAccess(payload);
  });
}
