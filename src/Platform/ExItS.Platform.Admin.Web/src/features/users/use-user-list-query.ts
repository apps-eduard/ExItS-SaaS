import { useQuery } from "@tanstack/react-query";
import { listDirectoryUsers } from "@/api/users/user-client";
import { USER_LIST_PAGE_SIZE, type UserListQuery } from "@/api/users/user-types";
import { env } from "@/lib/env";

export const userListQueryKey = (query: UserListQuery) =>
  [
    "users",
    "list",
    query.page ?? 1,
    query.pageSize ?? USER_LIST_PAGE_SIZE,
    query.status ?? "",
    query.search ?? "",
    query.directory ?? "",
    query.sortBy ?? "Username",
    query.sortDesc === true,
  ] as const;

export function useUserListQuery(query: UserListQuery, enabled: boolean) {
  return useQuery({
    queryKey: userListQueryKey(query),
    enabled,
    queryFn: ({ signal }) =>
      listDirectoryUsers(env.platformApiBaseUrl, {
        ...query,
        pageSize: query.pageSize ?? USER_LIST_PAGE_SIZE,
        signal,
      }),
  });
}
