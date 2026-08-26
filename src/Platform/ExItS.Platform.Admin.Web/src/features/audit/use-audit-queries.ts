import { useQuery } from "@tanstack/react-query";
import { getPlatformAuditRecord, queryPlatformAuditRecords } from "@/api/audit/audit-client";
import type { PlatformAuditUrlState } from "@/api/audit/audit-list-query";
import { env } from "@/lib/env";

export const platformAuditListQueryKey = (state: PlatformAuditUrlState) =>
  [
    "audit",
    "list",
    state.fromUtc,
    state.toUtc,
    state.actor,
    state.action,
    state.organizationId,
    state.productCode,
    state.outcome,
    state.page,
  ] as const;

export function usePlatformAuditListQuery(enabled: boolean, state: PlatformAuditUrlState) {
  return useQuery({
    queryKey: platformAuditListQueryKey(state),
    enabled,
    queryFn: ({ signal }) => queryPlatformAuditRecords(env.platformApiBaseUrl, state, signal),
  });
}

export const platformAuditDetailQueryKey = (auditId: string) =>
  ["audit", "detail", auditId] as const;

export function usePlatformAuditDetailQuery(auditId: string | null) {
  return useQuery({
    queryKey: platformAuditDetailQueryKey(auditId ?? ""),
    enabled: auditId != null,
    queryFn: ({ signal }) => getPlatformAuditRecord(env.platformApiBaseUrl, auditId!, signal),
  });
}
