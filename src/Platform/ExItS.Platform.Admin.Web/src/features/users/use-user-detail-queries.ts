import { useQuery } from "@tanstack/react-query";
import { listPlatformRoleAssignments } from "@/api/authorization/authorization-client";
import { ASSIGNMENTS_PAGE_SIZE } from "@/api/authorization/assignment-types";
import { getPlatformUser } from "@/api/users/user-client";
import { env } from "@/lib/env";

export const platformUserDetailQueryKey = (userId: string) => ["users", "detail", userId] as const;

export const platformUserAssignmentsQueryKey = (userId: string, status: string, page: number) =>
  ["users", "assignments", userId, status, page] as const;

export function usePlatformUserDetailQuery(userId: string | null) {
  return useQuery({
    queryKey: platformUserDetailQueryKey(userId ?? ""),
    enabled: userId != null,
    queryFn: ({ signal }) => getPlatformUser(env.platformApiBaseUrl, userId!, signal),
  });
}

export function usePlatformUserAssignmentsQuery(
  userId: string | null,
  options: { status?: string; page: number },
) {
  return useQuery({
    queryKey: platformUserAssignmentsQueryKey(userId ?? "", options.status ?? "", options.page),
    enabled: userId != null,
    queryFn: ({ signal }) =>
      listPlatformRoleAssignments(env.platformApiBaseUrl, {
        platformUserId: userId!,
        status: options.status || undefined,
        page: options.page,
        pageSize: ASSIGNMENTS_PAGE_SIZE,
        signal,
      }),
  });
}
