import { withQuery } from "@/lib/http/query-string";

export const ORGANIZATION_AUDIT_PAGE_SIZE = 20;

export const ORGANIZATION_AUDIT_OUTCOMES = ["Succeeded", "Denied", "Failed"] as const;
export type OrganizationAuditOutcome = (typeof ORGANIZATION_AUDIT_OUTCOMES)[number];

export type OrganizationAuditRecord = {
  id: string;
  occurredAtUtc: string;
  actorIdentifier: string;
  actorType: string;
  actionCode: string;
  targetType: string;
  targetId: string;
  organizationId?: string;
  productCode?: string;
  correlationId?: string;
  outcome: string;
  reason?: string;
  summary?: string;
};

export type OrganizationAuditUrlState = {
  fromUtc: string;
  toUtc: string;
  actor: string;
  action: string;
  targetType: string;
  outcome: OrganizationAuditOutcome | "";
  branchId: string;
  page: number;
};

export function isOrganizationAuditOutcome(value: string): value is OrganizationAuditOutcome {
  return (ORGANIZATION_AUDIT_OUTCOMES as readonly string[]).includes(value);
}

const GUID_PATTERN =
  /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

export function isAuditBranchId(value: string): boolean {
  return GUID_PATTERN.test(value);
}

function parsePage(raw: string | null): number {
  const value = Number(raw ?? "1");
  return Number.isFinite(value) && value >= 1 ? Math.floor(value) : 1;
}

/** Accept ISO-ish timestamps for URL shareability; ignore invalid values. */
function sanitizeInstant(raw: string | null): string {
  if (!raw) {
    return "";
  }
  const trimmed = raw.trim();
  if (!trimmed) {
    return "";
  }
  const date = new Date(trimmed);
  if (Number.isNaN(date.getTime())) {
    return "";
  }
  return trimmed;
}

export function parseOrganizationAuditSearchParams(
  params: URLSearchParams,
): OrganizationAuditUrlState {
  const outcomeRaw = params.get("outcome") ?? "";
  const branchRaw = (params.get("branchId") ?? "").trim();
  return {
    fromUtc: sanitizeInstant(params.get("fromUtc")),
    toUtc: sanitizeInstant(params.get("toUtc")),
    actor: params.get("actor")?.trim() ?? "",
    action: params.get("action")?.trim() ?? "",
    targetType: params.get("targetType")?.trim() ?? "",
    outcome: isOrganizationAuditOutcome(outcomeRaw) ? outcomeRaw : "",
    branchId: isAuditBranchId(branchRaw) ? branchRaw : "",
    page: parsePage(params.get("page")),
  };
}

export function organizationAuditSearchParams(state: OrganizationAuditUrlState): URLSearchParams {
  const params = new URLSearchParams();
  if (state.fromUtc) {
    params.set("fromUtc", state.fromUtc);
  }
  if (state.toUtc) {
    params.set("toUtc", state.toUtc);
  }
  if (state.actor) {
    params.set("actor", state.actor);
  }
  if (state.action) {
    params.set("action", state.action);
  }
  if (state.targetType) {
    params.set("targetType", state.targetType);
  }
  if (state.outcome) {
    params.set("outcome", state.outcome);
  }
  if (state.branchId && isAuditBranchId(state.branchId)) {
    params.set("branchId", state.branchId);
  }
  if (state.page > 1) {
    params.set("page", String(state.page));
  }
  return params;
}

export function hasActiveOrganizationAuditFilters(state: OrganizationAuditUrlState): boolean {
  return Boolean(
    state.fromUtc ||
    state.toUtc ||
    state.actor ||
    state.action ||
    state.targetType ||
    state.outcome ||
    state.branchId,
  );
}

export function organizationAuditRequestPath(
  organizationId: string,
  query: OrganizationAuditUrlState & { pageSize?: number },
): string {
  return withQuery(`/api/v1/platform/organizations/${organizationId}/audit`, {
    fromUtc: query.fromUtc || undefined,
    toUtc: query.toUtc || undefined,
    actor: query.actor || undefined,
    action: query.action || undefined,
    targetType: query.targetType || undefined,
    outcome: query.outcome || undefined,
    branchId: query.branchId || undefined,
    page: query.page,
    pageSize: query.pageSize ?? ORGANIZATION_AUDIT_PAGE_SIZE,
  });
}
