import type { BreadcrumbItem } from "../components/breadcrumb/Breadcrumb";
import { isNavBranch, NAV_GROUPS, type SectionId } from "./nav";

const HOME: BreadcrumbItem = {
  id: "design-system",
  label: "Design System",
  href: "#overview",
  icon: "dashboard",
};

function firstLeafHref(groupItems: (typeof NAV_GROUPS)[number]["items"]): string {
  for (const entry of groupItems) {
    if (isNavBranch(entry)) {
      return `#${entry.children[0]?.id ?? "overview"}`;
    }
    return `#${entry.id}`;
  }
  return "#overview";
}

/**
 * Central Playground route → breadcrumb mapping.
 * Example: `#button` → Design System › Buttons › Button
 */
export function playgroundBreadcrumbTrail(section: SectionId): BreadcrumbItem[] {
  if (section === "overview") {
    return [{ ...HOME, href: undefined }];
  }

  for (const group of NAV_GROUPS) {
    for (const entry of group.items) {
      if (isNavBranch(entry)) {
        const child = entry.children.find((item) => item.id === section);
        if (!child) continue;
        return [
          HOME,
          {
            id: `branch-${entry.label}`,
            label: entry.label,
            href: `#${entry.children[0]?.id ?? child.id}`,
          },
          {
            id: child.id,
            label: child.label,
          },
        ];
      }

      if (entry.id === section) {
        return [
          HOME,
          {
            id: `group-${group.label}`,
            label: group.label,
            href: firstLeafHref(group.items),
          },
          {
            id: entry.id,
            label: entry.label,
          },
        ];
      }
    }
  }

  return [HOME, { id: section, label: section }];
}
