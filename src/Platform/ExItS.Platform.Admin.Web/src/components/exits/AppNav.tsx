import { useMemo } from "react";
import { NavLink, useLocation } from "react-router-dom";
import { ChevronDown } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Tooltip } from "@/components/ui/tooltip";
import { NavExpandable } from "@/components/exits/nav-expandable";
import { NavIcon } from "@/components/exits/nav-icons";
import { navLinkClass, navRowBase, navSectionHeaderClass } from "@/components/exits/nav-item-styles";
import { useNavAccordion } from "@/components/exits/nav-accordion-context";
import { useAuthorizedCatalogProductsQuery } from "@/features/navigation/use-catalog-products-query";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import { areDevelopmentToolsAllowed } from "@/lib/auth/development-tools";
import type { MessageKey } from "@/lib/i18n/messages";
import { resolveNavigation } from "@/lib/navigation/resolve-navigation";
import type { ResolvedNavigationItem } from "@/lib/navigation/navigation-types";
import { cn } from "@/lib/utils";

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

function itemIsActive(
  item: ResolvedNavigationItem,
  pathname: string,
  search: string,
): boolean {
  if (pathMatches(item.href, pathname, search)) {
    return true;
  }
  return (item.children ?? []).some((child) => itemIsActive(child, pathname, search));
}

export function AppNav({ collapsed, onNavigate }: { collapsed: boolean; onNavigate?: () => void }) {
  const { t } = usePreferences();
  const location = useLocation();
  const authorization = useAuthorization();
  const { openSections, openGroups, toggleSection, toggleGroup } = useNavAccordion();
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

  return (
    <nav aria-label={t("shell.primaryNav")} className="flex flex-col gap-1 px-2 py-3">
      {sections.map((section, sectionIndex) => {
        const sectionOpen = collapsed || openSections.has(section.id);
        const sectionHasActive = section.items.some((item) =>
          itemIsActive(item, location.pathname, location.search),
        );
        return (
          <div
            key={section.id}
            className={cn(
              sectionIndex > 0 && !collapsed && "border-t border-border/70 pt-2",
            )}
          >
            {collapsed ? null : (
              <button
                type="button"
                className={navSectionHeaderClass(sectionHasActive)}
                aria-expanded={sectionOpen}
                onClick={() => toggleSection(section.id)}
              >
                <span>{t(section.labelKey)}</span>
                <ChevronDown
                  aria-hidden="true"
                  size={14}
                  className={cn(
                    "shrink-0 transition-transform duration-[var(--exits-motion-base)] ease-[var(--exits-ease-emphasized)]",
                    sectionOpen ? "rotate-0" : "-rotate-90",
                    sectionHasActive && "text-primary",
                  )}
                />
              </button>
            )}
            <NavExpandable
              open={sectionOpen}
              className={cn(!collapsed && sectionOpen && "mt-1")}
              contentClassName={cn(collapsed && sectionIndex > 0 && "pt-1")}
            >
              <ul className="grid gap-0.5">
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
            </NavExpandable>
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
  const active = itemIsActive(item, location.pathname, location.search);
  const leafActive = pathMatches(item.href, location.pathname, location.search);

  if (isGroup) {
    const children = item.children ?? [];
    const groupExpanded = collapsed ? true : groupOpen;
    const groupButton = (
      <button
        type="button"
        className={cn(
          navRowBase,
          "group/nav text-muted hover:bg-surface-muted/70 hover:text-foreground",
          collapsed && "justify-center px-0",
          active && !collapsed && "text-foreground",
        )}
        aria-expanded={groupExpanded}
        aria-label={label}
        onClick={onToggleGroup}
      >
        <NavIcon name={item.icon} active={active} compact={collapsed} />
        {collapsed ? null : (
          <>
            <span className="min-w-0 flex-1 truncate text-left">{label}</span>
            <ChevronDown
              aria-hidden="true"
              size={14}
              className={cn(
                "shrink-0 transition-transform duration-[var(--exits-motion-base)] ease-[var(--exits-ease-emphasized)]",
                groupExpanded ? "rotate-0" : "-rotate-90",
                active && "text-primary",
              )}
            />
          </>
        )}
      </button>
    );

    return (
      <div>
        {collapsed ? <Tooltip content={label}>{groupButton}</Tooltip> : groupButton}
        <NavExpandable
          open={groupExpanded && children.length > 0}
          className={cn(!collapsed && "mt-0.5")}
        >
          <ul
            className={cn(
              "grid gap-0.5",
              collapsed ? "mt-1" : "ml-3 border-l border-border/80 pl-2",
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
        </NavExpandable>
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
        navRowBase,
        "group/nav",
        collapsed && "justify-center px-0",
        !leafActive && !planned && !underDevelopment && "text-inherit",
      )}
    >
      <NavIcon name={item.icon} active={leafActive} compact={collapsed} />
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
    const hint = item.presentation === "context" ? t("nav.contextHint") : t("nav.plannedHint");
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

  const link = (
    <NavLink
      to={item.href}
      end={item.href === "/admin"}
      onClick={onNavigate}
      className={() => navLinkClass(leafActive)}
    >
      {content}
    </NavLink>
  );

  return collapsed ? <Tooltip content={label}>{link}</Tooltip> : link;
}
