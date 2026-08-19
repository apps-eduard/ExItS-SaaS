import type { AuthorizationStatus } from "@/hooks/use-authorization";
import { navigationRegistry } from "@/lib/navigation/navigation-registry";
import { reactImplementationStatus } from "@/lib/navigation/react-implementation";
import type {
  NavigationItemDefinition,
  PermissionRequirement,
} from "@/lib/navigation/navigation-types";

export function normalizeAdminPathname(pathname: string): string {
  const withoutQuery = pathname.split("?")[0]?.split("#")[0] ?? pathname;
  if (withoutQuery.length > 1 && withoutQuery.endsWith("/")) {
    return withoutQuery.slice(0, -1);
  }
  return withoutQuery;
}

export function hrefPathname(href: string): string {
  return normalizeAdminPathname(href);
}

export function itemsForPathname(pathname: string): NavigationItemDefinition[] {
  const path = normalizeAdminPathname(pathname);
  return navigationRegistry.flatMap((section) =>
    section.items.filter((item) => item.href !== undefined && hrefPathname(item.href) === path),
  );
}

function isAuthorized(
  item: NavigationItemDefinition,
  permissionStatus: AuthorizationStatus,
  hasAnyPermission: (codes: readonly string[]) => boolean,
  isPlatformAdministrator: boolean,
): boolean {
  const requirement: PermissionRequirement = item.permission;
  if (requirement.kind === "authenticated") {
    return true;
  }
  if (permissionStatus !== "loaded") {
    return false;
  }
  if (requirement.kind === "platformAdministrator") {
    return isPlatformAdministrator;
  }
  return hasAnyPermission(requirement.codes);
}

export type KnownRouteResolution = "implemented" | "under-development" | "unknown" | "pending";

export type ResolveKnownRouteInput = {
  pathname: string;
  permissionStatus: AuthorizationStatus;
  hasAnyPermission: (codes: readonly string[]) => boolean;
  isPlatformAdministrator: boolean;
  developmentToolsAllowed: boolean;
};

export function resolveKnownReactRoute(input: ResolveKnownRouteInput): KnownRouteResolution {
  const matches = itemsForPathname(input.pathname);
  if (matches.length === 0) {
    return "unknown";
  }

  if (input.permissionStatus === "loading") {
    return "pending";
  }

  const visible = matches.filter((item) => {
    if (item.lifecycle === "DEV_TEST_ONLY" && !input.developmentToolsAllowed) {
      return false;
    }
    return isAuthorized(
      item,
      input.permissionStatus,
      input.hasAnyPermission,
      input.isPlatformAdministrator,
    );
  });

  if (visible.some((item) => reactImplementationStatus(item) === "IMPLEMENTED")) {
    return "implemented";
  }

  if (visible.some((item) => reactImplementationStatus(item) === "UNDER_DEVELOPMENT")) {
    return "under-development";
  }

  return "unknown";
}
