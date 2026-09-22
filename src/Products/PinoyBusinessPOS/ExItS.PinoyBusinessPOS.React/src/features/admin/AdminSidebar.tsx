import { useMemo } from "react";
import { Link, NavLink, useLocation } from "react-router-dom";
import { ArrowLeftRight } from "lucide-react";
import { ExitsTooltip } from "@/components/exits/ExitsTooltip";
import { SidebarBrandHeader } from "@/components/exits/SidebarBrandHeader";
import { SidebarNavGroup } from "@/components/exits/SidebarNavGroup";
import {
  buildAdminNavGroups,
  flattenAdminNavItems,
  matchAdminNavItem,
  resolveConfigureFulfillmentBranchId,
  type AdminNavItem,
} from "@/features/admin/admin-nav-config";
import {
  capturePreferencesReturnFrom,
  isPreferencesDestination,
  preferencesNavigationState,
} from "@/features/preferences/preferences-return";
import { findActiveGroupId } from "@/features/shell/sidebar-nav-group-accordion";
import { isAccordionNavGroup } from "@/features/shell/sidebar-nav-group-helpers";
import { useSidebarNavGroupAccordion } from "@/features/shell/useSidebarNavGroupAccordion";
import { useSidebarNavTooltipEnabled } from "@/features/shell/useSidebarNavTooltipEnabled";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

/** Desktop (lg+) Manage Business sidebar — group accordion; full-width labels retained. */
export function AdminSidebar() {
  const { t } = useI18n();
  const location = useLocation();
  const { sessionGrant, boundWorkspace, workspaces } = useWorkspace();
  const fulfillmentBranchId = resolveConfigureFulfillmentBranchId({
    boundBranchId: boundWorkspace?.branchId,
    organizationId: boundWorkspace?.organizationId,
    workspaces: workspaces ?? [],
  });
  const groups = buildAdminNavGroups(sessionGrant, {
    branchId: fulfillmentBranchId,
  });
  const items = flattenAdminNavItems(groups);
  const activeId = matchAdminNavItem(location.pathname, items);
  const accordionGroups = useMemo(() => groups.filter(isAccordionNavGroup), [groups]);
  const activeGroupId = useMemo(
    () => findActiveGroupId(accordionGroups, activeId),
    [accordionGroups, activeId],
  );
  const groupIds = useMemo(() => accordionGroups.map((g) => g.id), [accordionGroups]);
  const accordion = useSidebarNavGroupAccordion({
    scope: "admin",
    groupIds,
    activeGroupId,
  });
  const switchLabel = t("workspace.switch");
  const tooltipEnabled = useSidebarNavTooltipEnabled();

  if (groups.length === 0) {
    return null;
  }

  const renderItem = (item: AdminNavItem) => {
    const Icon = item.icon;
    const isActive = activeId === item.id;
    const preferencesState = isPreferencesDestination(item.to)
      ? preferencesNavigationState(location.pathname, location.search)
      : undefined;
    const label = t(item.labelKey);
    const accessibleLabel =
      item.locked && item.lockedReasonKey
        ? `${label} · ${t(item.lockedReasonKey)}`
        : label;
    const link = (
      <NavLink
        to={item.to}
        end={item.end}
        state={preferencesState}
        onClick={
          preferencesState
            ? () => capturePreferencesReturnFrom(location.pathname, location.search)
            : undefined
        }
        data-testid={item.testId}
        aria-label={accessibleLabel}
        aria-current={isActive ? "page" : undefined}
        className={cn(
          "admin-sidebar__link",
          isActive && "admin-sidebar__link--active",
          item.locked && "admin-sidebar__link--locked",
        )}
      >
        <Icon className="admin-sidebar__icon size-4 shrink-0" aria-hidden />
        <span className="admin-sidebar__label min-w-0 flex-1 truncate">
          {label}
          {item.locked && item.lockedReasonKey ? (
            <span className="admin-sidebar__lock-hint">
              {" "}
              · {t(item.lockedReasonKey)}
            </span>
          ) : null}
        </span>
      </NavLink>
    );
    return (
      <li key={item.id}>
        <ExitsTooltip content={label} disabled={!tooltipEnabled}>
          {link}
        </ExitsTooltip>
      </li>
    );
  };

  return (
    <aside
      className={cn("admin-sidebar", "admin-sidebar--expanded")}
      data-testid="admin-sidebar"
      aria-label={t("admin.nav.aria")}
    >
      <SidebarBrandHeader
        testId="admin-sidebar-brand"
        allGroupsExpanded={accordion.allExpanded}
        onToggleAllGroups={accordion.toggleAll}
      />

      <nav className="admin-sidebar__nav">
        {groups.map((group, index) => {
          if (!isAccordionNavGroup(group)) {
            return (
              <ul
                key={group.id}
                className={cn(
                  "admin-sidebar__solo m-0 list-none p-0",
                  index > 0 && "admin-sidebar__solo--after",
                )}
                data-testid={`sidebar-nav-solo-${group.id}`}
              >
                {group.items.map(renderItem)}
              </ul>
            );
          }

          const prev = groups[index - 1];
          const showRule = prev != null && !isAccordionNavGroup(prev);

          return (
            <div key={group.id} className="admin-sidebar__group-block">
              {showRule ? <div className="admin-sidebar__nav-rule" aria-hidden="true" /> : null}
              <SidebarNavGroup
                id={group.id}
                title={t(group.titleKey)}
                icon={group.icon}
                expanded={accordion.isGroupExpanded(group.id)}
                active={activeGroupId === group.id}
                onToggle={() => accordion.toggleGroup(group.id)}
              >
                <ul className="m-0 list-none space-y-0.5 p-0">{group.items.map(renderItem)}</ul>
              </SidebarNavGroup>
            </div>
          );
        })}
      </nav>

      <div className="admin-sidebar__footer">
        <ExitsTooltip content={switchLabel} disabled={!tooltipEnabled}>
          <Link
            to="/workspace"
            className="admin-sidebar__switch"
            data-testid="admin-sidebar-switch-workspace"
            aria-label={switchLabel}
          >
            <ArrowLeftRight className="size-4 shrink-0" aria-hidden />
            <span className="admin-sidebar__label">{switchLabel}</span>
          </Link>
        </ExitsTooltip>
      </div>
    </aside>
  );
}
