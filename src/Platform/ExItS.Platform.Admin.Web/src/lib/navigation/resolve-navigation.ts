import type { AuthorizationStatus } from "@/hooks/use-authorization";
import { navigationRegistry } from "@/lib/navigation/navigation-registry";
import { reactImplementationStatus } from "@/lib/navigation/react-implementation";
import type {
  NavigationItemDefinition,
  PermissionRequirement,
  ResolvedNavigationItem,
  ResolvedNavigationSection,
} from "@/lib/navigation/navigation-types";

export type ResolveNavigationInput = {
  permissionStatus: AuthorizationStatus;
  hasAnyPermission: (codes: readonly string[]) => boolean;
  isPlatformAdministrator: boolean;
  developmentToolsAllowed: boolean;
};

export function isNavItemAuthorized(
  item: NavigationItemDefinition,
  input: ResolveNavigationInput,
): boolean {
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
  if (reactImplementationStatus(item) === "UNDER_DEVELOPMENT") {
    return "underDevelopment";
  }
  return "link";
}

function isMigrationOrPlanningPresentation(
  presentation: ResolvedNavigationItem["presentation"],
): boolean {
  return (
    presentation === "underDevelopment" || presentation === "planned" || presentation === "context"
  );
}

export function resolveNavigation(input: ResolveNavigationInput): ResolvedNavigationSection[] {
  const developmentItems: ResolvedNavigationItem[] = [];

  const sections = navigationRegistry
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
          if (!isNavItemAuthorized(item, input)) {
            return [];
          }

          const resolved: ResolvedNavigationItem = {
            ...item,
            presentation: presentationFor(item),
          };

          if (isMigrationOrPlanningPresentation(resolved.presentation)) {
            if (input.developmentToolsAllowed) {
              developmentItems.push(resolved);
            }
            return [];
          }

          return [resolved];
        });
      return { id: section.id, labelKey: section.labelKey, items };
    })
    .filter((section) => section.id !== "development" && section.items.length > 0);

  if (developmentItems.length > 0) {
    sections.push({
      id: "development",
      labelKey: "nav.group.development",
      items: developmentItems,
    });
  }

  return sections;
}
