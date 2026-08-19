import { parsePagedResult, type PagedResult } from "@/api/platform/paged-result";
import { platformRequest } from "@/api/platform-http";
import {
  assertDashboardPageSize,
  organizationsListPath,
} from "@/features/overview/dashboard-bounds";
import type { OrganizationListItem } from "@/api/organizations/organization-types";

export function listOrganizations(
  baseUrl: string,
  options: { status?: string; pageSize: number; signal?: AbortSignal },
): Promise<PagedResult<OrganizationListItem>> {
  assertDashboardPageSize(options.pageSize);
  return platformRequest<unknown>(baseUrl, {
    path: organizationsListPath({ status: options.status, pageSize: options.pageSize }),
    signal: options.signal,
  }).then((payload) => parsePagedResult<OrganizationListItem>(payload));
}
