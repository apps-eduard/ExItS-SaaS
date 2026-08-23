import type { AuthorizationStatus } from "@/hooks/use-authorization";
import { catalogProductNavIcon } from "@/lib/navigation/catalog-product-nav-icon";
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

export type CatalogNavProduct = {
  code: string;
  displayName: string;
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
  if (item.kind === "group") {
    return "group";
  }
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

export function catalogProductNavId(productCode: string): string {
  return `PWEB-NAV-ORG-BY-PRODUCT:${productCode}`;
}

export function catalogProductNavHref(productCode: string): string {
  return `/admin/organizations?product=${encodeURIComponent(productCode)}`;
}

export function resolveNavigation(
  input: ResolveNavigationInput,
  catalogProducts: readonly CatalogNavProduct[] = [],
): ResolvedNavigationSection[] {
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
            id: item.id,
            labelKey: item.labelKey,
            icon: item.icon,
            href: item.href,
            presentation: presentationFor(item),
          };

          if (item.id === "PWEB-NAV-BY-PRODUCT") {
            resolved.children = catalogProducts.map((product) => ({
              id: catalogProductNavId(product.code),
              label: product.displayName || product.code,
              icon: catalogProductNavIcon(product.code),
              href: catalogProductNavHref(product.code),
              presentation: "link" as const,
            }));
          }

          return [resolved];
        });
      return { id: section.id, labelKey: section.labelKey, items };
    })
    .filter((section) => {
      if (section.id === "development") {
        return input.developmentToolsAllowed && section.items.length > 0;
      }
      return section.items.length > 0;
    });

  return sections;
}
