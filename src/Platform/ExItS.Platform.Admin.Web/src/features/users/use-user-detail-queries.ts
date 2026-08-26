import { useQuery } from "@tanstack/react-query";
import { listPlatformRoleAssignments } from "@/api/authorization/authorization-client";
import { ASSIGNMENTS_PAGE_SIZE } from "@/api/authorization/assignment-types";
import { getPlatformUser } from "@/api/users/user-client";
import {
  getPlatformUserCredentials,
  listPlatformUserMemberships,
  listPlatformUserProductAccess,
} from "@/api/users/user-mutations";
import { env } from "@/lib/env";

export const platformUserDetailQueryKey = (userId: string) => ["users", "detail", userId] as const;

export const platformUserAssignmentsQueryKey = (userId: string, status: string, page: number) =>
  ["users", "assignments", userId, status, page] as const;

export const platformUserCredentialsQueryKey = (userId: string) =>
  ["users", "credentials", userId] as const;

export const platformUserMembershipsQueryKey = (userId: string) =>
  ["users", "memberships", userId] as const;

export const platformUserProductAccessQueryKey = (userId: string) =>
  ["users", "product-access", userId] as const;

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

export function usePlatformUserCredentialsQuery(userId: string | null) {
  return useQuery({
    queryKey: platformUserCredentialsQueryKey(userId ?? ""),
    enabled: userId != null,
    queryFn: ({ signal }) => getPlatformUserCredentials(env.platformApiBaseUrl, userId!, signal),
  });
}

export function usePlatformUserMembershipsQuery(userId: string | null) {
  return useQuery({
    queryKey: platformUserMembershipsQueryKey(userId ?? ""),
    enabled: userId != null,
    queryFn: ({ signal }) => listPlatformUserMemberships(env.platformApiBaseUrl, userId!, signal),
  });
}

export function usePlatformUserProductAccessQuery(userId: string | null) {
  return useQuery({
    queryKey: platformUserProductAccessQueryKey(userId ?? ""),
    enabled: userId != null,
    queryFn: ({ signal }) => listPlatformUserProductAccess(env.platformApiBaseUrl, userId!, signal),
  });
}
