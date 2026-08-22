import { parsePagedResult, type PagedResult } from "@/api/platform/paged-result";
import { platformRequest } from "@/api/platform-http";
import {
  platformAuditDetailPath,
  platformAuditListPath,
  type PlatformAuditRecord,
  type PlatformAuditUrlState,
} from "@/api/audit/audit-list-query";
import { assertDashboardPageSize, auditListPath } from "@/features/overview/dashboard-bounds";

/** Bounded dashboard list item (overview widget). */
export type AuditListItem = {
  id: string;
  occurredAtUtc: string;
  actorIdentifier: string;
  actionCode: string;
  targetType: string;
  outcome: string;
  summary?: string | null;
};

export function listAuditRecords(
  baseUrl: string,
  options: { pageSize: number; signal?: AbortSignal },
): Promise<PagedResult<AuditListItem>> {
  assertDashboardPageSize(options.pageSize);
  return platformRequest<unknown>(baseUrl, {
    path: auditListPath({ pageSize: options.pageSize }),
    signal: options.signal,
  }).then((payload) => parsePagedResult<AuditListItem>(payload));
}

export function queryPlatformAuditRecords(
  baseUrl: string,
  query: PlatformAuditUrlState,
  signal?: AbortSignal,
): Promise<PagedResult<PlatformAuditRecord>> {
  return platformRequest<unknown>(baseUrl, {
    path: platformAuditListPath(query),
    signal,
  }).then((payload) => parsePagedResult<PlatformAuditRecord>(payload));
}

export function getPlatformAuditRecord(
  baseUrl: string,
  auditId: string,
  signal?: AbortSignal,
): Promise<PlatformAuditRecord> {
  return platformRequest<PlatformAuditRecord>(baseUrl, {
    path: platformAuditDetailPath(auditId),
    signal,
  });
}
