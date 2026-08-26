import { useEffect, useMemo } from "react";
import { NavLink, useLocation } from "react-router-dom";
import { ChevronDown } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { NavExpandable } from "@/components/exits/nav-expandable";
import { NavIcon } from "@/components/exits/nav-icons";
import { NavRailHint } from "@/components/exits/nav-rail-hint";
import { navLinkClass, navNestedTextClass, navRowBase, navRowNested, navRowNestedChild, navSectionHeaderClass } from "@/components/exits/nav-item-styles";
import { useNavAccordion } from "@/components/exits/nav-accordion-context";
import { useAuthorizedCatalogProductsQuery } from "@/features/navigation/use-catalog-products-query";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import { areDevelopmentToolsAllowed } from "@/lib/auth/development-tools";
import type { MessageKey } from "@/lib/i18n/messages";
import { itemIsActive, pathMatches } from "@/lib/navigation/nav-route-utils";
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

/** Icon rail: one click per destination — expand groups into direct product links. */
function itemsForNavPresentation(
  items: ResolvedNavigationItem[],
  collapsed: boolean,
): ResolvedNavigationItem[] {
  if (!collapsed) {
    return items;
  }
  return items.flatMap((item) =>
    item.presentation === "group" ? (item.children ?? []) : [item],
  );
}

export function AppNav({
  collapsed,
  railTooltipsEnabled = true,
  onNavigate,
}: {
  collapsed: boolean;
  railTooltipsEnabled?: boolean;
  onNavigate?: () => void;
}) {
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

  useEffect(() => {
    const active = document.querySelector<HTMLElement>("[data-nav-active='true']");
    if (active && typeof active.scrollIntoView === "function") {
      active.scrollIntoView({ block: "nearest", behavior: "smooth" });
    }
  }, [location.pathname, location.search, collapsed]);

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
              sectionIndex > 0 && "border-t border-border/70",
              sectionIndex > 0 && (collapsed ? "pt-2" : "pt-2"),
            )}
          >
            {collapsed ? null : (
              <button
                type="button"
                className={navSectionHeaderClass(sectionHasActive)}
                aria-expanded={sectionOpen}
                onClick={() => toggleSection(section.id)}
              >
                <span className="flex min-w-0 flex-1 items-center gap-1.5">
                  <NavIcon
                    active={false}
                    className={cn(
                      "!size-4 shrink-0 !bg-transparent",
                      sectionHasActive ? "!text-primary" : "text-muted",
                    )}
                    name={section.icon}
                  />
                  <span className="truncate">{t(section.labelKey)}</span>
                </span>
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
                {itemsForNavPresentation(section.items, collapsed).map((item) => (
                  <li key={item.id}>
                    <NavItem
                      collapsed={collapsed}
                      nested={!collapsed}
                      railTooltipsEnabled={railTooltipsEnabled}
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
  railTooltipsEnabled,
  onNavigate,
  groupOpen,
  onToggleGroup,
  nested = false,
  nestedChild = false,
}: {
  item: ResolvedNavigationItem;
  collapsed: boolean;
  railTooltipsEnabled: boolean;
  onNavigate?: () => void;
  groupOpen: boolean;
  onToggleGroup: () => void;
  nested?: boolean;
  nestedChild?: boolean;
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
    const groupExpanded = groupOpen;
    const groupButton = (
      <button
        type="button"
        className={cn(
          navRowNested,
          "group/nav text-muted hover:bg-surface-muted/70 hover:text-foreground",
          active && "text-foreground",
        )}
        aria-expanded={groupExpanded}
        aria-label={label}
        onClick={onToggleGroup}
      >
        <NavIcon active={active} name={item.icon} size="sm" />
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
      </button>
    );

    return (
      <div>
        {groupButton}
        <NavExpandable open={groupExpanded && children.length > 0} className="mt-0.5">
          <ul className="grid gap-0.5">
            {children.map((child) => (
              <li key={child.id}>
                <NavItem
                  collapsed={collapsed}
                  nested
                  nestedChild
                  railTooltipsEnabled={railTooltipsEnabled}
                  item={child}
                  onNavigate={onNavigate}
                  groupOpen={false}
                  onToggleGroup={() => undefined}
                />
              </li>
            ))}
          </ul>
        </NavExpandable>
        {groupExpanded && children.length === 0 ? (
          <p className={cn("mt-1 px-2 text-muted", navNestedTextClass)}>
            {t("nav.byProduct.empty")}
          </p>
        ) : null}
      </div>
    );
  }

  const rowClass =
    nested && !collapsed ? (nestedChild ? navRowNestedChild : navRowNested) : navRowBase;
  const iconSize = collapsed ? "rail" : nested ? "sm" : "md";

  const content = (
    <span
      className={cn(
        rowClass,
        "group/nav",
        collapsed && "justify-center px-0",
        !leafActive && !planned && !underDevelopment && "text-inherit",
        nested && leafActive && "!text-foreground",
      )}
    >
      <NavIcon active={leafActive} name={item.icon} size={iconSize} />
      {collapsed ? null : (
        <span className="flex min-w-0 flex-1 items-center justify-between gap-2 transition-[opacity,transform] duration-[var(--exits-motion-base)] ease-[var(--exits-ease)]">
          <span className="truncate">{label}</span>
          {planned ? <Badge tone="neutral">{t("nav.planned")}</Badge> : null}
          {underDevelopment ? <Badge tone="neutral">{t("nav.underDevelopment")}</Badge> : null}
        </span>
      )}
    </span>
  );

  if (underDevelopment || planned || !item.href) {
    const hint = item.presentation === "context" ? t("nav.contextHint") : t("nav.plannedHint");
    const statusDescription = underDevelopment
      ? t("nav.underDevelopment")
      : planned
        ? t("nav.planned")
        : hint;
    const status = (
      <span
        aria-disabled="true"
        aria-label={`${label}. ${statusDescription}`}
        className={cn(
          "block w-full cursor-default text-muted",
          nested ? navNestedTextClass : "text-[length:var(--exits-text-sm)]",
        )}
      >
        {content}
      </span>
    );
    return collapsed && railTooltipsEnabled ? (
      <NavRailHint label={label} description={statusDescription}>
        {status}
      </NavRailHint>
    ) : (
      status
    );
  }

  const link = (
    <NavLink
      to={item.href}
      end={item.href === "/admin"}
      onClick={onNavigate}
      aria-label={collapsed ? label : undefined}
      data-nav-active={leafActive ? "true" : undefined}
      className={() => navLinkClass(leafActive, nested)}
    >
      {content}
    </NavLink>
  );

  return collapsed && railTooltipsEnabled ? (
    <NavRailHint label={label} description={leafActive ? t("nav.currentPage") : undefined}>
      {link}
    </NavRailHint>
  ) : (
    link
  );
}
