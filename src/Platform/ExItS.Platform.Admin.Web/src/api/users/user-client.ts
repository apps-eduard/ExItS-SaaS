import { parsePagedResult, type PagedResult } from "@/api/platform/paged-result";
import { platformRequest } from "@/api/platform-http";
import { usersListRequestPath } from "@/api/users/user-list-query";
import type { PlatformUserListItem, UserListQuery } from "@/api/users/user-types";

function readString(record: Record<string, unknown>, ...keys: string[]): string | undefined {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "string" && value.length > 0) {
      return value;
    }
  }
  return undefined;
}

function readStringArray(record: Record<string, unknown>, ...keys: string[]): string[] {
  for (const key of keys) {
    const value = record[key];
    if (Array.isArray(value)) {
      return value.filter((item): item is string => typeof item === "string" && item.length > 0);
    }
  }
  return [];
}

export function mapPlatformUserListItem(payload: unknown): PlatformUserListItem {
  if (typeof payload !== "object" || payload === null) {
    throw new Error("Invalid platform user list item.");
  }
  const record = payload as Record<string, unknown>;
  const id = readString(record, "id", "Id");
  const displayName = readString(record, "displayName", "DisplayName");
  const username = readString(record, "username", "Username");
  const email = readString(record, "email", "Email");
  const status = readString(record, "status", "Status");
  if (!id || !displayName || !username || !email || !status) {
    throw new Error("Invalid platform user list item.");
  }
  return {
    id,
    displayName,
    username,
    email,
    status,
    createdAtUtc: readString(record, "createdAtUtc", "CreatedAtUtc"),
    updatedAtUtc: readString(record, "updatedAtUtc", "UpdatedAtUtc"),
    accountClasses: readStringArray(record, "accountClasses", "AccountClasses"),
    organizationNames: readStringArray(record, "organizationNames", "OrganizationNames"),
  };
}

export function listDirectoryUsers(
  baseUrl: string,
  options: UserListQuery & { signal?: AbortSignal },
): Promise<PagedResult<PlatformUserListItem>> {
  return platformRequest<unknown>(baseUrl, {
    path: usersListRequestPath(options),
    signal: options.signal,
  }).then((payload) => {
    const page = parsePagedResult<unknown>(payload);
    return {
      ...page,
      items: page.items.map(mapPlatformUserListItem),
    };
  });
}
