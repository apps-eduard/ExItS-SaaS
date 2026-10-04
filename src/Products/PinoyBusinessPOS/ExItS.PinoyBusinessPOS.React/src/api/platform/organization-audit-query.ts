export const ORGANIZATION_AUDIT_PAGE_SIZE = 20;

export const ORGANIZATION_AUDIT_OUTCOMES = ["Succeeded", "Denied", "Failed"] as const;
export type OrganizationAuditOutcome = (typeof ORGANIZATION_AUDIT_OUTCOMES)[number];

export type OrganizationAuditQuery = {
  fromDate: string;
  toDate: string;
  actor: string;
  action: string;
  targetType: string;
  outcome: OrganizationAuditOutcome | "";
  branchId: string;
  page: number;
};

export const EMPTY_ORGANIZATION_AUDIT_QUERY: OrganizationAuditQuery = {
  fromDate: "",
  toDate: "",
  actor: "",
  action: "",
  targetType: "",
  outcome: "",
  branchId: "",
  page: 1,
};

const GUID_PATTERN =
  /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

export function isOrganizationAuditOutcome(value: string): value is OrganizationAuditOutcome {
  return (ORGANIZATION_AUDIT_OUTCOMES as readonly string[]).includes(value);
}

export function isAuditBranchId(value: string): boolean {
  return GUID_PATTERN.test(value);
}

/** Date-only input → UTC start, matching the legacy Organization Web filter. */
export function auditFromUtc(date: string): string | undefined {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(date)) {
    return undefined;
  }
  return `${date}T00:00:00.000Z`;
}

/** Date-only input → UTC end of that day. */
export function auditToUtc(date: string): string | undefined {
  const start = auditFromUtc(date);
  if (!start) {
    return undefined;
  }
  return new Date(Date.parse(start) + 24 * 60 * 60 * 1000 - 1).toISOString();
}

export function hasActiveOrganizationAuditFilters(query: OrganizationAuditQuery): boolean {
  return Boolean(
    query.fromDate ||
      query.toDate ||
      query.actor.trim() ||
      query.action.trim() ||
      query.targetType.trim() ||
      query.outcome ||
      query.branchId,
  );
}

export function organizationAuditRequestPath(
  organizationId: string,
  query: OrganizationAuditQuery,
): string {
  const params = new URLSearchParams();
  const fromUtc = auditFromUtc(query.fromDate);
  const toUtc = auditToUtc(query.toDate);
  if (fromUtc) params.set("fromUtc", fromUtc);
  if (toUtc) params.set("toUtc", toUtc);
  if (query.actor.trim()) params.set("actor", query.actor.trim());
  if (query.action.trim()) params.set("action", query.action.trim());
  if (query.targetType.trim()) params.set("targetType", query.targetType.trim());
  if (query.outcome) params.set("outcome", query.outcome);
  if (isAuditBranchId(query.branchId)) params.set("branchId", query.branchId);
  params.set("page", String(Math.max(1, query.page)));
  params.set("pageSize", String(ORGANIZATION_AUDIT_PAGE_SIZE));
  return `/api/v1/platform/organizations/${organizationId}/audit?${params.toString()}`;
}

/** Display labels for known governance actions. Unknown codes stay as the server code. */
export function formatGovernanceAuditAction(actionCode: string): string {
  switch (actionCode) {
    case "platform.organization.updated":
      return "Organization profile updated";
    case "platform.organization.branch.created":
      return "Branch created";
    case "platform.organization.branch.updated":
      return "Branch updated";
    case "platform.membership.added":
      return "Staff member added";
    case "platform.membership.role_changed":
      return "Staff role changed";
    default:
      return actionCode;
  }
}
