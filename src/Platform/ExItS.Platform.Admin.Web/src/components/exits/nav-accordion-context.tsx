import {
  createContext,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { ListCollapse, ListTree } from "lucide-react";
import { useAuthorizedCatalogProductsQuery } from "@/features/navigation/use-catalog-products-query";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import { areDevelopmentToolsAllowed } from "@/lib/auth/development-tools";
import { resolveNavigation } from "@/lib/navigation/resolve-navigation";
import type { ResolvedNavigationItem } from "@/lib/navigation/navigation-types";
import { cn } from "@/lib/utils";

const DEFAULT_OPEN_SECTIONS = [
  "home",
  "organizations",
  "people",
  "products",
  "billing",
  "catalog",
  "governance",
  "operations",
  "settings",
  "development",
] as const;

function collectGroupIds(items: ResolvedNavigationItem[]): string[] {
  const ids: string[] = [];
  for (const item of items) {
    if (item.presentation === "group") {
      ids.push(item.id);
    }
    if (item.children?.length) {
      ids.push(...collectGroupIds(item.children));
    }
  }
  return ids;
}

type NavAccordionContextValue = {
  openSections: Set<string>;
  openGroups: Set<string>;
  toggleSection: (id: string) => void;
  toggleGroup: (id: string) => void;
  allExpanded: boolean;
  toggleAllSections: () => void;
};

const NavAccordionContext = createContext<NavAccordionContextValue | null>(null);

export function NavAccordionProvider({ children }: { children: ReactNode }) {
  const authorization = useAuthorization();
  const catalogQuery = useAuthorizedCatalogProductsQuery();
  const catalogProducts = useMemo(
    () =>
      (catalogQuery.data?.items ?? []).map((product) => ({
        code: product.code,
        displayName: product.displayName,
      })),
    [catalogQuery.data],
  );
  const sections = resolveNavigation(
    {
      permissionStatus: authorization.status,
      hasAnyPermission: authorization.hasAnyPermission,
      isPlatformAdministrator: authorization.isPlatformAdministrator,
      developmentToolsAllowed: areDevelopmentToolsAllowed(),
    },
    catalogProducts,
  );

  const expandableSectionIds = useMemo(() => sections.map((section) => section.id), [sections]);
  const expandableGroupIds = useMemo(
    () => sections.flatMap((section) => collectGroupIds(section.items)),
    [sections],
  );

  const [openSections, setOpenSections] = useState<Set<string>>(
    () => new Set(DEFAULT_OPEN_SECTIONS),
  );
  const [openGroups, setOpenGroups] = useState<Set<string>>(() => new Set(["PWEB-NAV-BY-PRODUCT"]));

  const allExpanded =
    expandableSectionIds.every((id) => openSections.has(id)) &&
    expandableGroupIds.every((id) => openGroups.has(id));

  function toggleSection(id: string) {
    setOpenSections((current) => {
      const next = new Set(current);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  }

  function toggleGroup(id: string) {
    setOpenGroups((current) => {
      const next = new Set(current);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  }

  function toggleAllSections() {
    if (allExpanded) {
      setOpenSections(new Set());
      setOpenGroups(new Set());
      return;
    }
    setOpenSections(new Set(expandableSectionIds));
    setOpenGroups(new Set(expandableGroupIds));
  }

  const value = useMemo(
    () => ({
      openSections,
      openGroups,
      toggleSection,
      toggleGroup,
      allExpanded,
      toggleAllSections,
    }),
    [allExpanded, openGroups, openSections],
  );

  return <NavAccordionContext.Provider value={value}>{children}</NavAccordionContext.Provider>;
}

export function useNavAccordion(): NavAccordionContextValue {
  const context = useContext(NavAccordionContext);
  if (!context) {
    throw new Error("useNavAccordion must be used within NavAccordionProvider.");
  }
  return context;
}

export function NavBulkAccordionToggle({
  className,
  hidden,
}: {
  className?: string;
  hidden?: boolean;
}) {
  const { t } = usePreferences();
  const { allExpanded, toggleAllSections } = useNavAccordion();

  if (hidden) {
    return null;
  }

  const label = allExpanded ? t("nav.collapseAll") : t("nav.expandAll");
  const Icon = allExpanded ? ListCollapse : ListTree;

  return (
    <button
      type="button"
      data-testid="nav-bulk-accordion"
      className={cn(
        "inline-flex size-8 shrink-0 items-center justify-center rounded-md text-muted hover:bg-surface-muted/70 hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
        className,
      )}
      aria-label={label}
      title={label}
      onClick={toggleAllSections}
    >
      <Icon aria-hidden="true" size={16} strokeWidth={2} />
    </button>
  );
}
