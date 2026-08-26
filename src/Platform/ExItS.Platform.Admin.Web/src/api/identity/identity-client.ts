import { parsePagedResult, type PagedResult } from "@/api/platform/paged-result";
import { platformRequest } from "@/api/platform-http";
import { assertDashboardPageSize, usersListPath } from "@/features/overview/dashboard-bounds";

export type PlatformUserListItem = {
  id: string;
  displayName: string;
  username: string;
  email: string;
  status: string;
};

export function listPlatformUsers(
  baseUrl: string,
  options: {
    status?: string;
    directory?: string;
    pageSize: number;
    signal?: AbortSignal;
  },
): Promise<PagedResult<PlatformUserListItem>> {
  assertDashboardPageSize(options.pageSize);
  return platformRequest<unknown>(baseUrl, {
    path: usersListPath({
      status: options.status,
      directory: options.directory,
      pageSize: options.pageSize,
    }),
    signal: options.signal,
  }).then((payload) => parsePagedResult<PlatformUserListItem>(payload));
}
