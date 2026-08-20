const GUID_PATTERN =
  /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

export function parsePlatformUserId(value: string | undefined): string | null {
  if (!value || !GUID_PATTERN.test(value)) {
    return null;
  }
  return value;
}

export const USERS_LIST_STATE_KEY = "usersListSearch";

export type UsersLocationState = {
  [USERS_LIST_STATE_KEY]?: string;
};

export function usersListHref(listSearch?: string): string {
  if (!listSearch) {
    return "/admin/users";
  }
  return listSearch.startsWith("?") ? `/admin/users${listSearch}` : `/admin/users?${listSearch}`;
}

export function platformUserDetailHref(userId: string): string {
  return `/admin/users/${userId}`;
}
