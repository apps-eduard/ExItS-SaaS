import { parsePagedResult, type PagedResult } from "@/api/platform/paged-result";
import { platformRequest } from "@/api/platform-http";
import { assertDashboardPageSize, auditListPath } from "@/features/overview/dashboard-bounds";

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
