import {
  platformRequest,
  PlatformApiError,
  type PlatformProblemDetails,
} from "@/api/platform/platform-http";

function areasBase(organizationId: string): string {
  return `/api/v1/platform/organizations/${organizationId}/areas`;
}

function asRecord(value: unknown): Record<string, unknown> {
  return typeof value === "object" && value !== null ? (value as Record<string, unknown>) : {};
}

export type OrganizationAreaStatusDto = "Active" | "Archived";

export type OrganizationAreaDto = {
  id: string;
  organizationId: string;
  name: string;
  code: string | null;
  status: OrganizationAreaStatusDto;
  branchCount: number;
};

export type OrganizationAreaListDto = {
  areas: OrganizationAreaDto[];
  unassignedBranchCount: number;
  activeAreaCount: number;
  maxAreas: number;
};

export type OrganizationAreasClientResult<T> =
  | { ok: true; value: T }
  | { ok: false; status: number; body: PlatformProblemDetails | null; errorCode?: string };

async function wrap<T>(fn: () => Promise<T>): Promise<OrganizationAreasClientResult<T>> {
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

function normalizeStatus(raw: unknown): OrganizationAreaStatusDto {
  const value = String(raw ?? "").trim();
  return value.localeCompare("Archived", undefined, { sensitivity: "accent" }) === 0
    ? "Archived"
    : "Active";
}

function normalizeArea(raw: unknown): OrganizationAreaDto {
  const r = asRecord(raw);
  const code = r.code ?? r.Code;
  return {
    id: String(r.id ?? r.Id ?? ""),
    organizationId: String(r.organizationId ?? r.OrganizationId ?? ""),
    name: String(r.name ?? r.Name ?? ""),
    code: code == null || code === "" ? null : String(code),
    status: normalizeStatus(r.status ?? r.Status),
    branchCount: Number(r.branchCount ?? r.BranchCount ?? 0),
  };
}

function normalizeAreaList(raw: unknown): OrganizationAreaListDto {
  const r = asRecord(raw);
  const areasRaw = r.areas ?? r.Areas;
  const list = Array.isArray(areasRaw) ? areasRaw : [];
  return {
    areas: list.map(normalizeArea),
    unassignedBranchCount: Number(r.unassignedBranchCount ?? r.UnassignedBranchCount ?? 0),
    activeAreaCount: Number(r.activeAreaCount ?? r.ActiveAreaCount ?? 0),
    maxAreas: Number(r.maxAreas ?? r.MaxAreas ?? 0),
  };
}

export async function listOrganizationAreas(
  organizationId: string,
  signal?: AbortSignal,
): Promise<OrganizationAreasClientResult<OrganizationAreaListDto>> {
  return wrap(async () => {
    const payload = await platformRequest<unknown>({
      method: "GET",
      path: areasBase(organizationId),
      signal,
    });
    return normalizeAreaList(payload);
  });
}

export async function createOrganizationArea(
  organizationId: string,
  input: { name: string; code?: string | null },
  signal?: AbortSignal,
): Promise<OrganizationAreasClientResult<OrganizationAreaDto>> {
  return wrap(async () => {
    const payload = await platformRequest<unknown>({
      method: "POST",
      path: areasBase(organizationId),
      body: { name: input.name, code: input.code ?? null },
      signal,
    });
    return normalizeArea(payload);
  });
}

export async function updateOrganizationArea(
  organizationId: string,
  areaId: string,
  input: { name: string; code?: string | null },
  signal?: AbortSignal,
): Promise<OrganizationAreasClientResult<OrganizationAreaDto>> {
  return wrap(async () => {
    const payload = await platformRequest<unknown>({
      method: "PUT",
      path: `${areasBase(organizationId)}/${areaId}`,
      body: { name: input.name, code: input.code ?? null },
      signal,
    });
    return normalizeArea(payload);
  });
}

export async function archiveOrganizationArea(
  organizationId: string,
  areaId: string,
  signal?: AbortSignal,
): Promise<OrganizationAreasClientResult<OrganizationAreaDto>> {
  return wrap(async () => {
    const payload = await platformRequest<unknown>({
      method: "POST",
      path: `${areasBase(organizationId)}/${areaId}/archive`,
      signal,
    });
    return normalizeArea(payload);
  });
}

/**
 * Places a branch in an area, moves it between areas, or removes it from its area.
 * Grouping only — no stock, register, shift, or document ownership moves with it.
 */
export async function setBranchArea(
  organizationId: string,
  branchId: string,
  areaId: string | null,
  signal?: AbortSignal,
): Promise<OrganizationAreasClientResult<{ branchId: string; areaId: string | null }>> {
  return wrap(async () => {
    const payload = await platformRequest<unknown>({
      method: "PUT",
      path: `/api/v1/platform/organizations/${organizationId}/branches/${branchId}/area`,
      body: { areaId },
      signal,
    });
    const r = asRecord(payload);
    const resolvedArea = r.areaId ?? r.AreaId;
    return {
      branchId: String(r.branchId ?? r.BranchId ?? branchId),
      areaId: resolvedArea == null || resolvedArea === "" ? null : String(resolvedArea),
    };
  });
}
