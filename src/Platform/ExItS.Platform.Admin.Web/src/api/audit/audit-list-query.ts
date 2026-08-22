import { withQuery } from "@/lib/http/query-string";

export const PLATFORM_AUDIT_PAGE_SIZE = 20;

export const PLATFORM_AUDIT_OUTCOMES = ["Succeeded", "Denied", "Failed"] as const;
export type PlatformAuditOutcome = (typeof PLATFORM_AUDIT_OUTCOMES)[number];

export type PlatformAuditRecord = {
  id: string;
  occurredAtUtc: string;
  actorIdentifier: string;
  actorType: string;
  actionCode: string;
  targetType: string;
  targetId: string;
  organizationId?: string | null;
  productCode?: string | null;
  correlationId?: string | null;
  outcome: string;
  reason?: string | null;
  summary?: string | null;
};

export type PlatformAuditUrlState = {
  fromUtc: string;
  toUtc: string;
  actor: string;
  action: string;
  organizationId: string;
  productCode: string;
  outcome: PlatformAuditOutcome | "";
  page: number;
};

export function isPlatformAuditOutcome(value: string): value is PlatformAuditOutcome {
  return (PLATFORM_AUDIT_OUTCOMES as readonly string[]).includes(value);
}

const GUID_PATTERN =
  /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

export function isAuditGuid(value: string): boolean {
  return GUID_PATTERN.test(value);
}

function parsePage(raw: string | null): number {
  const value = Number(raw ?? "1");
  return Number.isFinite(value) && value >= 1 ? Math.floor(value) : 1;
}

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

export function parsePlatformAuditSearchParams(params: URLSearchParams): PlatformAuditUrlState {
  const outcomeRaw = params.get("outcome") ?? "";
  const organizationRaw = (params.get("organizationId") ?? "").trim();
  return {
    fromUtc: sanitizeInstant(params.get("fromUtc")),
    toUtc: sanitizeInstant(params.get("toUtc")),
    actor: params.get("actor")?.trim() ?? "",
    action: params.get("action")?.trim() ?? "",
    organizationId: isAuditGuid(organizationRaw) ? organizationRaw : "",
    productCode: params.get("productCode")?.trim() ?? "",
    outcome: isPlatformAuditOutcome(outcomeRaw) ? outcomeRaw : "",
    page: parsePage(params.get("page")),
  };
}

export function platformAuditSearchParams(state: PlatformAuditUrlState): URLSearchParams {
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
  if (state.organizationId && isAuditGuid(state.organizationId)) {
    params.set("organizationId", state.organizationId);
  }
  if (state.productCode) {
    params.set("productCode", state.productCode);
  }
  if (state.outcome) {
    params.set("outcome", state.outcome);
  }
  if (state.page > 1) {
    params.set("page", String(state.page));
  }
  return params;
}

export function hasActivePlatformAuditFilters(state: PlatformAuditUrlState): boolean {
  return Boolean(
    state.fromUtc ||
      state.toUtc ||
      state.actor ||
      state.action ||
      state.organizationId ||
      state.productCode ||
      state.outcome,
  );
}

export function platformAuditListPath(
  query: PlatformAuditUrlState & { pageSize?: number },
): string {
  return withQuery("/api/v1/platform/audit", {
    fromUtc: query.fromUtc || undefined,
    toUtc: query.toUtc || undefined,
    actor: query.actor || undefined,
    action: query.action || undefined,
    organizationId: query.organizationId || undefined,
    productCode: query.productCode || undefined,
    outcome: query.outcome || undefined,
    page: query.page,
    pageSize: query.pageSize ?? PLATFORM_AUDIT_PAGE_SIZE,
  });
}

export function platformAuditDetailPath(auditId: string): string {
  return `/api/v1/platform/audit/${auditId}`;
}

export function parseAuditRecordId(raw: string | undefined): string | null {
  if (!raw || !isAuditGuid(raw)) {
    return null;
  }
  return raw;
}
