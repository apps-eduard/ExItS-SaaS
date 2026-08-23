import type { ResolvedNavigationItem, ResolvedNavigationSection } from "@/lib/navigation/navigation-types";

export function pathMatches(href: string | undefined, pathname: string, search: string): boolean {
  if (!href) {
    return false;
  }
  const url = new URL(href, "http://local.invalid");
  const isSettingsWorkspace =
    url.pathname === "/admin/settings" &&
    (pathname === "/admin/settings" || pathname.startsWith("/admin/settings/"));
  if (!isSettingsWorkspace && url.pathname !== pathname) {
    return false;
  }
  if (!url.search) {
    return search.length === 0 || search === "?" || isSettingsWorkspace;
  }
  return url.search === search;
}

export function itemIsActive(
  item: ResolvedNavigationItem,
  pathname: string,
  search: string,
): boolean {
  if (pathMatches(item.href, pathname, search)) {
    return true;
  }
  return (item.children ?? []).some((child) => itemIsActive(child, pathname, search));
}

function collectActiveGroupIds(
  items: ResolvedNavigationItem[],
  pathname: string,
  search: string,
  groupIds: string[],
): void {
  for (const item of items) {
    if (
      item.presentation === "group" &&
      (item.children ?? []).some((child) => itemIsActive(child, pathname, search))
    ) {
      groupIds.push(item.id);
    }
    if (item.children?.length) {
      collectActiveGroupIds(item.children, pathname, search, groupIds);
    }
  }
}

export function collectOpenStateForPath(
  sections: ResolvedNavigationSection[],
  pathname: string,
  search: string,
): { sectionIds: string[]; groupIds: string[] } {
  const sectionIds: string[] = [];
  const groupIds: string[] = [];

  for (const section of sections) {
    if (section.items.some((item) => itemIsActive(item, pathname, search))) {
      sectionIds.push(section.id);
      collectActiveGroupIds(section.items, pathname, search, groupIds);
    }
  }

  return { sectionIds, groupIds };
}
