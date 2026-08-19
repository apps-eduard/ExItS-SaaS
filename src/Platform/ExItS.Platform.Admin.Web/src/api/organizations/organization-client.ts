import { parsePagedResult, type PagedResult } from "@/api/platform/paged-result";
import { platformRequest } from "@/api/platform-http";
import { organizationsListPath } from "@/features/overview/dashboard-bounds";
import { organizationsListRequestPath } from "@/api/organizations/organization-list-query";
import type {
  OrganizationListItem,
  OrganizationListQuery,
} from "@/api/organizations/organization-types";

function readString(record: Record<string, unknown>, ...keys: string[]): string | undefined {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "string" && value.length > 0) {
      return value;
    }
  }
  return undefined;
}

export function mapOrganizationListItem(payload: unknown): OrganizationListItem {
  if (typeof payload !== "object" || payload === null) {
    throw new Error("Invalid organization list item.");
  }
  const record = payload as Record<string, unknown>;
  const id = readString(record, "id", "Id");
  const displayName = readString(record, "displayName", "DisplayName");
  const slug = readString(record, "slug", "Slug");
  const status = readString(record, "status", "Status");
  if (!id || !displayName || !slug || !status) {
    throw new Error("Invalid organization list item.");
  }
  return {
    id,
    displayName,
    slug,
    status,
    createdAtUtc: readString(record, "createdAtUtc", "CreatedAtUtc"),
    updatedAtUtc: readString(record, "updatedAtUtc", "UpdatedAtUtc"),
  };
}

export function listOrganizations(
  baseUrl: string,
  options: OrganizationListQuery & { signal?: AbortSignal },
): Promise<PagedResult<OrganizationListItem>> {
  const dashboardShaped =
    options.search == null &&
    options.sortBy == null &&
    options.sortDesc == null &&
    (options.page == null || options.page === 1);
  const path = dashboardShaped
    ? organizationsListPath({
        status: options.status,
        pageSize: options.pageSize ?? 1,
      })
    : organizationsListRequestPath(options);

  return platformRequest<unknown>(baseUrl, {
    path,
    signal: options.signal,
  }).then((payload) => {
    const page = parsePagedResult<unknown>(payload);
    return {
      ...page,
      items: page.items.map(mapOrganizationListItem),
    };
  });
}
