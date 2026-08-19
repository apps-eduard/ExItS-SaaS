const GUID_PATTERN =
  /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

export function parseOrganizationId(value: string | undefined): string | null {
  if (!value || !GUID_PATTERN.test(value)) {
    return null;
  }
  return value;
}

export const ORGANIZATIONS_LIST_STATE_KEY = "organizationsListSearch";

export type OrganizationsLocationState = {
  [ORGANIZATIONS_LIST_STATE_KEY]?: string;
};

export function organizationsListHref(listSearch?: string): string {
  if (!listSearch) {
    return "/admin/organizations";
  }
  return listSearch.startsWith("?")
    ? `/admin/organizations${listSearch}`
    : `/admin/organizations?${listSearch}`;
}

export function isOrganizationWorkspacePath(pathname: string): boolean {
  const parts = pathname.replace(/\/+$/, "").split("/");
  return parts.length === 4 && parts[1] === "admin" && parts[2] === "organizations";
}
