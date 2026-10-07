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

const GUID_TEXT =
  /[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}/g;

const ACTION_LABELS: Record<string, string> = {
  "platform.organization.updated": "Organization profile updated",
  "platform.organization.branch.created": "Branch created",
  "platform.organization.branch.updated": "Branch updated",
  "platform.membership.added": "Staff member added",
  "platform.membership.role_changed": "Staff role changed",
  "platform.access.checked": "Access checked",
};

const PERMISSION_LABELS: Record<string, string> = {
  view_portfolio: "view the portfolio",
  manage_organizations: "manage organizations",
  manage_catalog: "manage the catalog",
  manage_platform_users: "manage users",
  manage_memberships: "manage staff",
  manage_product_access: "manage product access",
  manage_subscriptions: "manage the subscription",
  manage_manual_payments: "manage payments",
  manage_entitlement_overrides: "change plan limits",
  view_audit_records: "view this audit trail",
};

/** Display labels for governance actions. Unknown codes become readable words, not raw dotted codes. */
export function formatGovernanceAuditAction(actionCode: string): string {
  const known = ACTION_LABELS[actionCode];
  if (known) return known;
  const words = actionCode
    .replace(/^platform\./, "")
    .split(/[._]+/)
    .filter((word) => word.length > 0);
  if (words.length === 0) return actionCode;
  return words
    .map((word, index) => (index === 0 ? word.charAt(0).toUpperCase() + word.slice(1) : word))
    .join(" ");
}

const PLATFORM_USER_ACTOR = /^platform-user:([0-9a-fA-F-]{36})$/i;

/** User id stored as `platform-user:{id}`. Emails and other actors have no id to resolve. */
export function auditActorUserId(actorIdentifier: string): string | null {
  return PLATFORM_USER_ACTOR.exec(actorIdentifier.trim())?.[1] ?? null;
}

/** Emails stay. A resolved person name replaces a technical user id. */
export function formatAuditActor(actorIdentifier: string, displayName?: string | null): string {
  const actor = actorIdentifier.trim();
  const name = displayName?.trim();
  if (name) return name;
  if (!actor) return "—";
  if (actor.includes("@")) return actor;
  if (PLATFORM_USER_ACTOR.test(actor) || GUID_PATTERN.test(actor)) return "—";
  return actor;
}

/** Prefer a readable target. Hide raw ids. */
export function formatAuditTarget(targetType: string, targetId: string): string {
  const type = targetType.trim();
  const id = targetId.trim();
  if (type === "AuditRecord" && id.toLowerCase() === "query") return "Audit search";
  const label = type
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .replace(/[_-]+/g, " ")
    .trim();
  if (!id || id.toLowerCase() === "query" || GUID_PATTERN.test(id)) {
    return label || "—";
  }
  return label ? `${label} · ${id}` : id;
}

function permissionPhrase(permissionCode: string): string {
  const key = permissionCode.replace(/^platform\.permission\./, "");
  return PERMISSION_LABELS[key] ?? key.replace(/[_-]+/g, " ");
}

/** Plain-language summary. Permission denials do not repeat account or organization ids. */
export function formatAuditSummary(summary: string | null, reason: string | null): string {
  const raw = (summary ?? reason ?? "").trim();
  if (!raw) return "—";
  const denied =
    /^Actor '[^']+' does not hold permission '([^']+)'(?: for organization '[^']+')?\.?$/i.exec(raw);
  if (denied?.[1]) {
    return `This account cannot ${permissionPhrase(denied[1])}.`;
  }
  const scrubbed = raw
    .replace(/platform-user:[0-9a-fA-F-]{36}/gi, "this account")
    .replace(/for organization '[^']+'/gi, "for this organization")
    .replace(GUID_TEXT, "")
    .replace(/\s{2,}/g, " ")
    .replace(/\s+([.,])/g, "$1")
    .trim();
  return scrubbed || "—";
}
