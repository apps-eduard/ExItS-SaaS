import { platformRequest } from "@/api/platform/platform-http";
import {
  organizationAuditRequestPath,
  type OrganizationAuditQuery,
} from "@/api/platform/organization-audit-query";

export type OrganizationAuditRecord = {
  id: string;
  occurredAtUtc: string;
  actorIdentifier: string;
  actionCode: string;
  targetType: string;
  targetId: string;
  organizationId: string | null;
  outcome: string;
  summary: string | null;
  reason: string | null;
};

export type OrganizationAuditPage = {
  items: OrganizationAuditRecord[];
  totalCount: number;
  page: number;
  pageSize: number;
};

function asRecord(value: unknown): Record<string, unknown> {
  return typeof value === "object" && value !== null ? (value as Record<string, unknown>) : {};
}

function pick(raw: Record<string, unknown>, camel: string, pascal: string): unknown {
  return raw[camel] ?? raw[pascal];
}

function readString(raw: Record<string, unknown>, camel: string, pascal: string): string | null {
  const value = pick(raw, camel, pascal);
  return typeof value === "string" && value.trim().length > 0 ? value.trim() : null;
}

export function mapAuditRecord(raw: unknown): OrganizationAuditRecord {
  const record = asRecord(raw);
  return {
    id: readString(record, "id", "Id") ?? "",
    occurredAtUtc: readString(record, "occurredAtUtc", "OccurredAtUtc") ?? "",
    actorIdentifier: readString(record, "actorIdentifier", "ActorIdentifier") ?? "",
    actionCode: readString(record, "actionCode", "ActionCode") ?? "",
    targetType: readString(record, "targetType", "TargetType") ?? "",
    targetId: readString(record, "targetId", "TargetId") ?? "",
    organizationId: readString(record, "organizationId", "OrganizationId"),
    outcome: readString(record, "outcome", "Outcome") ?? "",
    summary: readString(record, "summary", "Summary"),
    reason: readString(record, "reason", "Reason"),
  };
}

export function mapAuditPage(raw: unknown): OrganizationAuditPage {
  const record = asRecord(raw);
  const itemsRaw = pick(record, "items", "Items");
  const total = pick(record, "totalCount", "TotalCount");
  const page = pick(record, "page", "Page");
  const pageSize = pick(record, "pageSize", "PageSize");
  return {
    items: Array.isArray(itemsRaw) ? itemsRaw.map(mapAuditRecord) : [],
    totalCount: typeof total === "number" ? total : 0,
    page: typeof page === "number" ? page : 1,
    pageSize: typeof pageSize === "number" ? pageSize : 20,
  };
}

export async function getOrganizationAudit(
  organizationId: string,
  query: OrganizationAuditQuery,
  signal?: AbortSignal,
): Promise<OrganizationAuditPage> {
  const raw = await platformRequest<unknown>({
    path: organizationAuditRequestPath(organizationId, query),
    signal,
  });
  return mapAuditPage(raw);
}
