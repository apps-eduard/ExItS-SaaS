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

export const ORGANIZATION_WORKSPACE_SECTIONS = [
  "branches",
  "people",
  "products",
  "subscription",
  "entitlements",
  "billing",
] as const;

export type OrganizationWorkspaceSection = (typeof ORGANIZATION_WORKSPACE_SECTIONS)[number];

export type OrganizationWorkspaceNavSection =
  "overview" | "branches" | "people" | "products" | "subscription" | "entitlements";

export function parseOrganizationWorkspaceSection(
  pathname: string,
): OrganizationWorkspaceSection | null {
  const parts = pathname.replace(/\/+$/, "").split("/");
  const section = parts[4];
  if (
    parts.length !== 5 ||
    parts[1] !== "admin" ||
    parts[2] !== "organizations" ||
    !section ||
    !(ORGANIZATION_WORKSPACE_SECTIONS as readonly string[]).includes(section)
  ) {
    return null;
  }
  return section as OrganizationWorkspaceSection;
}

export function isOrganizationWorkspaceBranchesPath(pathname: string): boolean {
  return parseOrganizationWorkspaceSection(pathname) === "branches";
}

export function isOrganizationWorkspacePeoplePath(pathname: string): boolean {
  return parseOrganizationWorkspaceSection(pathname) === "people";
}

export function isOrganizationWorkspaceSectionPath(pathname: string): boolean {
  return parseOrganizationWorkspaceSection(pathname) != null;
}

export function organizationWorkspaceHref(
  organizationId: string,
  section: OrganizationWorkspaceNavSection = "overview",
): string {
  return section === "overview"
    ? `/admin/organizations/${organizationId}`
    : `/admin/organizations/${organizationId}/${section}`;
}
