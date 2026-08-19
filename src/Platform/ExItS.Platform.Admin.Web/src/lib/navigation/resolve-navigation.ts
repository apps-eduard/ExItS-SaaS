import { navigationRegistry } from "@/lib/navigation/navigation-registry";
import type {
  NavigationItemDefinition,
  PermissionRequirement,
  ResolvedNavigationItem,
  ResolvedNavigationSection,
} from "@/lib/navigation/navigation-types";
import type { AuthorizationStatus } from "@/hooks/use-authorization";

export type ResolveNavigationInput = {
  permissionStatus: AuthorizationStatus;
  hasAnyPermission: (codes: readonly string[]) => boolean;
  isPlatformAdministrator: boolean;
  developmentToolsAllowed: boolean;
};

function isAuthorized(item: NavigationItemDefinition, input: ResolveNavigationInput): boolean {
  const requirement: PermissionRequirement = item.permission;
  if (requirement.kind === "authenticated") {
    return true;
  }
  if (input.permissionStatus !== "loaded") {
    return false;
  }
  if (requirement.kind === "platformAdministrator") {
    return input.isPlatformAdministrator;
  }
  return input.hasAnyPermission(requirement.codes);
}

function presentationFor(item: NavigationItemDefinition): ResolvedNavigationItem["presentation"] {
  if (item.lifecycle === "PLANNED_DISABLED") {
    return "planned";
  }
  if (item.lifecycle === "CONTEXT_REQUIRED") {
    return "context";
  }
  return "link";
}

export function resolveNavigation(input: ResolveNavigationInput): ResolvedNavigationSection[] {
  return navigationRegistry
    .slice()
    .sort((a, b) => a.order - b.order)
    .map((section) => {
      const items = section.items
        .slice()
        .sort((a, b) => a.order - b.order)
        .flatMap((item): ResolvedNavigationItem[] => {
          if (item.lifecycle === "DEV_TEST_ONLY" && !input.developmentToolsAllowed) {
            return [];
          }
          if (!isAuthorized(item, input)) {
            return [];
          }
          return [{ ...item, presentation: presentationFor(item) }];
        });
      return { id: section.id, labelKey: section.labelKey, items };
    })
    .filter((section) => section.items.length > 0);
}
