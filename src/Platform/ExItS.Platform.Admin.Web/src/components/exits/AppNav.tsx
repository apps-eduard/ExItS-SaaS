import { useMemo, useState } from "react";
import { NavLink, useLocation } from "react-router-dom";
import { ChevronDown, ChevronsDownUp, ChevronsUpDown } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Tooltip } from "@/components/ui/tooltip";
import { NavIcon } from "@/components/exits/nav-icons";
import { useAuthorizedCatalogProductsQuery } from "@/features/navigation/use-catalog-products-query";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import { areDevelopmentToolsAllowed } from "@/lib/auth/development-tools";
import type { MessageKey } from "@/lib/i18n/messages";
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

function itemLabel(item: ResolvedNavigationItem, t: (key: MessageKey) => string): string {
  if (item.label) {
    return item.label;
  }
  if (item.labelKey) {
    return t(item.labelKey);
  }
  return item.id;
}

function pathMatches(href: string | undefined, pathname: string, search: string): boolean {
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

export function AppNav({ collapsed, onNavigate }: { collapsed: boolean; onNavigate?: () => void }) {
  const { t } = usePreferences();
  const location = useLocation();
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

  function expandAll() {
    setOpenSections(new Set(expandableSectionIds));
    setOpenGroups(new Set(expandableGroupIds));
  }

  function collapseAll() {
    setOpenSections(new Set());
    setOpenGroups(new Set());
  }

  const bulkLabel = allExpanded ? t("nav.collapseAll") : t("nav.expandAll");
  const BulkIcon = allExpanded ? ChevronsDownUp : ChevronsUpDown;

  return (
    <nav aria-label={t("shell.primaryNav")} className="flex flex-col gap-3 px-2 py-3">
      {collapsed ? null : (
        <div className="flex justify-end px-1">
          <button
            type="button"
            data-testid="nav-bulk-accordion"
            className="inline-flex size-8 items-center justify-center rounded-md text-muted hover:bg-surface-muted/70 hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            aria-label={bulkLabel}
            title={bulkLabel}
            onClick={() => {
              if (allExpanded) {
                collapseAll();
              } else {
                expandAll();
              }
            }}
          >
            <BulkIcon aria-hidden="true" size={16} />
          </button>
        </div>
      )}
      {sections.map((section) => {
        const sectionOpen = collapsed || openSections.has(section.id);
        const sectionHasActive = section.items.some(
          (item) =>
            pathMatches(item.href, location.pathname, location.search) ||
            item.children?.some((child) =>
              pathMatches(child.href, location.pathname, location.search),
            ),
        );
        return (
          <div key={section.id}>
            {collapsed ? null : (
              <button
                type="button"
                className="flex w-full items-center justify-between gap-2 rounded-md px-2 py-1 text-left text-[11px] font-medium tracking-wide text-muted uppercase hover:bg-surface-muted/60 hover:text-foreground"
                aria-expanded={sectionOpen}
                onClick={() => toggleSection(section.id)}
              >
                <span>{t(section.labelKey)}</span>
                <ChevronDown
                  aria-hidden="true"
                  size={14}
                  className={cn(
                    "shrink-0 transition-transform duration-[var(--exits-motion-fast)]",
                    sectionOpen ? "rotate-0" : "-rotate-90",
                    sectionHasActive && "text-primary",
                  )}
                />
              </button>
            )}
            {sectionOpen ? (
              <ul className={cn("grid gap-1", !collapsed && "mt-1")}>
                {section.items.map((item) => (
                  <li key={item.id}>
                    <NavItem
                      collapsed={collapsed}
                      item={item}
                      onNavigate={onNavigate}
                      groupOpen={openGroups.has(item.id)}
                      onToggleGroup={() => toggleGroup(item.id)}
                    />
                  </li>
                ))}
              </ul>
            ) : null}
          </div>
        );
      })}
    </nav>
  );
}

function NavItem({
  item,
  collapsed,
  onNavigate,
  groupOpen,
  onToggleGroup,
}: {
  item: ResolvedNavigationItem;
  collapsed: boolean;
  onNavigate?: () => void;
  groupOpen: boolean;
  onToggleGroup: () => void;
}) {
  const { t } = usePreferences();
  const location = useLocation();
  const label = itemLabel(item, t);
  const planned = item.presentation === "planned" || item.presentation === "context";
  const underDevelopment = item.presentation === "underDevelopment";
  const isGroup = item.presentation === "group";
  const hint = item.presentation === "context" ? t("nav.contextHint") : t("nav.plannedHint");

  if (isGroup) {
    const children = item.children ?? [];
    const groupExpanded = collapsed ? true : groupOpen;
    const groupButton = (
      <button
        type="button"
        className={cn(
          "flex w-full min-h-11 items-center gap-2 rounded-md px-2 text-[length:var(--exits-text-sm)] font-medium text-muted hover:bg-surface-muted/70 hover:text-foreground lg:min-h-9",
          collapsed && "justify-center",
        )}
        aria-expanded={groupExpanded}
        aria-label={label}
        onClick={onToggleGroup}
      >
        <NavIcon name={item.icon} />
        {collapsed ? null : (
          <>
            <span className="min-w-0 flex-1 truncate text-left">{label}</span>
            <ChevronDown
              aria-hidden="true"
              size={14}
              className={cn(
                "shrink-0 transition-transform duration-[var(--exits-motion-fast)]",
                groupExpanded ? "rotate-0" : "-rotate-90",
              )}
            />
          </>
        )}
      </button>
    );

    return (
      <div>
        {collapsed ? <Tooltip content={label}>{groupButton}</Tooltip> : groupButton}
        {groupExpanded && children.length > 0 ? (
          <ul
            className={cn(
              "grid gap-1",
              collapsed ? "mt-1" : "mt-1 ml-3 border-l border-border pl-2",
            )}
          >
            {children.map((child) => (
              <li key={child.id}>
                <NavItem
                  collapsed={collapsed}
                  item={child}
                  onNavigate={onNavigate}
                  groupOpen={false}
                  onToggleGroup={() => undefined}
                />
              </li>
            ))}
          </ul>
        ) : null}
        {groupExpanded && children.length === 0 && !collapsed ? (
          <p className="mt-1 px-2 text-[length:var(--exits-text-xs)] text-muted">
            {t("nav.byProduct.empty")}
          </p>
        ) : null}
      </div>
    );
  }

  const content = (
    <span
      className={cn(
        "flex min-h-11 items-center gap-2 rounded-md px-2 lg:min-h-9",
        collapsed && "justify-center",
      )}
    >
      <NavIcon name={item.icon} />
      {collapsed ? null : (
        <span className="flex min-w-0 flex-1 items-center justify-between gap-2">
          <span className="truncate">{label}</span>
          {planned ? <Badge tone="neutral">{t("nav.planned")}</Badge> : null}
          {underDevelopment ? <Badge tone="neutral">{t("nav.underDevelopment")}</Badge> : null}
        </span>
      )}
    </span>
  );

  if (underDevelopment || planned || !item.href) {
    const statusLabel = underDevelopment
      ? `${label}. ${t("nav.underDevelopment")}`
      : planned
        ? `${label}. ${t("nav.planned")}`
        : `${label}. ${hint}`;
    const status = (
      <span
        aria-disabled="true"
        aria-label={statusLabel}
        className="block w-full cursor-default text-[length:var(--exits-text-sm)] text-muted"
      >
        {content}
      </span>
    );
    return collapsed ? <Tooltip content={statusLabel}>{status}</Tooltip> : status;
  }

  const active = pathMatches(item.href, location.pathname, location.search);
  const link = (
    <NavLink
      to={item.href}
      end={item.href === "/admin"}
      onClick={onNavigate}
      className={() =>
        cn(
          "block rounded-md text-[length:var(--exits-text-sm)] font-medium",
          active
            ? "bg-surface-muted text-foreground shadow-[inset_2px_0_0_0_var(--exits-primary)]"
            : "text-muted hover:bg-surface-muted/70 hover:text-foreground",
        )
      }
    >
      {content}
    </NavLink>
  );

  return collapsed ? <Tooltip content={label}>{link}</Tooltip> : link;
}
